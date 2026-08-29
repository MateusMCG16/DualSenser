using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DualSenser.Service.Hid;
using DualSenser.Service.Models.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DualSenser.Service.Network;

public sealed class DualSenseWebSocketManager : IDualSenseWebSocketManager
{
    private readonly IDualSenseHidReader _hidReader;
    private readonly ILogger<DualSenseWebSocketManager> _logger;
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public int ConnectedClientsCount => _clients.Count;

    public DualSenseWebSocketManager(
        IDualSenseHidReader hidReader,
        ILogger<DualSenseWebSocketManager> logger)
    {
        _hidReader = hidReader;
        _logger = logger;
    }

    public async Task HandleClientAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Requisição inválida. Esperado upgrade de WebSocket.", cancellationToken);
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        string connectionId = Guid.NewGuid().ToString("N")[..8];
        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        _clients.TryAdd(connectionId, webSocket);
        _logger.LogInformation(">>> [WS] Novo cliente conectado: ID={ConnectionId} de {ClientIp} (Total conectado: {Total})",
            connectionId, clientIp, _clients.Count);

        try
        {
            // 1. Envia imediatamente o estado atual do controle assim que o cliente conecta
            var initialStatus = ControllerStatusDto.FromState(_hidReader.CurrentState, _hidReader.CurrentDevice);
            await SendDirectAsync(webSocket, initialStatus, cancellationToken);

            // 2. Loop de escuta para manter a conexão aberta e detectar encerramento pelo cliente
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Encerramento solicitado pelo cliente",
                            cancellationToken
                        );
                        break;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelamento esperado durante shutdown do servidor
        }
        catch (WebSocketException wsEx)
        {
            _logger.LogDebug(wsEx, "Conexão WebSocket ID={ConnectionId} encerrada abruptamente.", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no loop da conexão WebSocket ID={ConnectionId}.", connectionId);
        }
        finally
        {
            _clients.TryRemove(connectionId, out _);
            _logger.LogInformation("<<< [WS] Cliente desconectado: ID={ConnectionId} (Total conectado: {Total})",
                connectionId, _clients.Count);
        }
    }

    public async Task BroadcastAsync<T>(T payload, CancellationToken cancellationToken = default)
    {
        if (_clients.IsEmpty)
        {
            return;
        }

        byte[] messageBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var sendTasks = new List<Task>(_clients.Count);

        foreach (var (id, socket) in _clients)
        {
            if (socket.State == WebSocketState.Open)
            {
                sendTasks.Add(SendSocketMessageAsync(id, socket, messageBytes, cancellationToken));
            }
            else
            {
                _clients.TryRemove(id, out _);
            }
        }

        if (sendTasks.Count > 0)
        {
            await Task.WhenAll(sendTasks);
        }
    }

    private async Task SendSocketMessageAsync(string id, WebSocket socket, byte[] data, CancellationToken ct)
    {
        try
        {
            await socket.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                endOfMessage: true,
                ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Falha ao enviar mensagem para WebSocket ID={ConnectionId}. Removendo socket.", id);
            _clients.TryRemove(id, out _);
            try
            {
                socket.Dispose();
            }
            catch { }
        }
    }

    private async Task SendDirectAsync<T>(WebSocket socket, T payload, CancellationToken ct)
    {
        if (socket.State != WebSocketState.Open)
            return;

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            ct
        );
    }

    public void Dispose()
    {
        foreach (var (id, socket) in _clients)
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Servidor encerrando", CancellationToken.None).GetAwaiter().GetResult();
                }
                socket.Dispose();
            }
            catch { }
        }
        _clients.Clear();
    }
}
