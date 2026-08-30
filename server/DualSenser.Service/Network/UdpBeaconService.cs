using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
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

                // Envia para o broadcast global 255.255.255.255
                await udpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, _config.UdpBeaconPort));

                // Envia também para os endereços de broadcast específicos de cada placa de rede IPv4 ativa
                var broadcastAddresses = GetDirectedBroadcastAddresses();
                foreach (var directedBroadcast in broadcastAddresses)
                {
                    try
                    {
                        await udpClient.SendAsync(data, data.Length, new IPEndPoint(directedBroadcast, _config.UdpBeaconPort));
                    }
                    catch { }
                }

                _logger.LogDebug("Beacon UDP emitido para {Count} alvos na porta {Port} com servidor '{ServerName}'.", 
                    broadcastAddresses.Count + 1, _config.UdpBeaconPort, serverName);
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

    private static List<IPAddress> GetDirectedBroadcastAddresses()
    {
        var result = new List<IPAddress>();

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                var ipProps = ni.GetIPProperties();
                foreach (var unicast in ipProps.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork &&
                        unicast.IPv4Mask != null &&
                        !IPAddress.IsLoopback(unicast.Address))
                    {
                        byte[] ipBytes = unicast.Address.GetAddressBytes();
                        byte[] maskBytes = unicast.IPv4Mask.GetAddressBytes();
                        byte[] broadcastBytes = new byte[ipBytes.Length];

                        for (int i = 0; i < ipBytes.Length; i++)
                        {
                            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                        }

                        result.Add(new IPAddress(broadcastBytes));
                    }
                }
            }
        }
        catch { }

        return result;
    }
}
