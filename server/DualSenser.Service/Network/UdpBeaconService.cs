using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DualSenser.Service.Common;
using DualSenser.Service.Models.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DualSenser.Service.Network;

public sealed class UdpBeaconService : BackgroundService
{
    private readonly AppConfig _config;
    private readonly ILogger<UdpBeaconService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public UdpBeaconService(
        AppConfig config,
        ILogger<UdpBeaconService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.EnableUdpBeacon)
        {
            _logger.LogInformation("UDP Beacon desabilitado na configuração.");
            return;
        }

        _logger.LogInformation("Iniciando serviço de descoberta UDP Beacon na porta {Port} (Intervalo: {Interval}s)...",
            _config.UdpBeaconPort, _config.UdpBeaconIntervalSeconds);

        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        var targetEndpoint = new IPEndPoint(IPAddress.Broadcast, _config.UdpBeaconPort);
        string serverName = Environment.MachineName;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var payload = new UdpBeaconPayloadDto(
                    Service: "DualSenser",
                    Version: "1.0",
                    Port: _config.HttpPort,
                    ServerName: serverName,
                    Timestamp: DateTime.UtcNow
                );

                byte[] data = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
                await udpClient.SendAsync(data, data.Length, targetEndpoint);

                _logger.LogDebug("Beacon UDP emitido para {Endpoint} com payload do servidor '{ServerName}'.", 
                    targetEndpoint, serverName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Falha temporária ao emitir pacote de beacon UDP.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.UdpBeaconIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Serviço de descoberta UDP Beacon finalizado.");
    }
}
