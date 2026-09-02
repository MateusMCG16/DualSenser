using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DualSenser.Service.Common;
using DualSenser.Service.Hid;
using DualSenser.Service.Models;
using DualSenser.Service.Models.Network;
using DualSenser.Service.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DualSenser.Service.Services;

public sealed class DualSenseMonitorWorker : BackgroundService
{
    private readonly IDualSenseHidReader _hidReader;
    private readonly IDualSenseWebSocketManager _webSocketManager;
    private readonly AppConfig _config;
    private readonly ILogger<DualSenseMonitorWorker> _logger;

    public DualSenseMonitorWorker(
        IDualSenseHidReader hidReader,
        IDualSenseWebSocketManager webSocketManager,
        AppConfig config,
        ILogger<DualSenseMonitorWorker> logger)
    {
        _hidReader = hidReader;
        _webSocketManager = webSocketManager;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=================================================");
        _logger.LogInformation("Serviço DualSenser iniciado com sucesso.");
        _logger.LogInformation("Monitorando controles DualSense (Bluetooth e USB)...");
        _logger.LogInformation("Servidor HTTP & WebSockets ativo na porta: {HttpPort}", _config.HttpPort);
        _logger.LogInformation("=================================================");

        _hidReader.DeviceConnected += OnDeviceConnected;
        _hidReader.DeviceDisconnected += OnDeviceDisconnected;
        _hidReader.BatteryStateChanged += OnBatteryStateChanged;

        try
        {
            await _hidReader.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Serviço DualSenser solicitado para parar.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Erro fatal no serviço de monitoramento DualSenser.");
        }
        finally
        {
            _hidReader.DeviceConnected -= OnDeviceConnected;
            _hidReader.DeviceDisconnected -= OnDeviceDisconnected;
            _hidReader.BatteryStateChanged -= OnBatteryStateChanged;

            _logger.LogInformation("Serviço DualSenser finalizado.");
        }
    }

    private void OnDeviceConnected(DualSenseDeviceInfo device)
    {
        _logger.LogInformation(">>> DISPOSITIVO CONECTADO: {ModelName} via {ConnectionType} (VID: 0x{VendorId:X4}, PID: 0x{ProductId:X4})",
            device.ModelName, device.ConnectionType, device.VendorId, device.ProductId);

        // Notificar clientes WebSocket
        var statusDto = ControllerStatusDto.FromState(_hidReader.CurrentState, device);
        _ = _webSocketManager.BroadcastAsync(statusDto);
    }

    private void OnDeviceDisconnected()
    {
        _logger.LogInformation("<<< DISPOSITIVO DESCONECTADO. Aguardando reconexão...");

        // Notificar clientes WebSocket
        _ = _webSocketManager.BroadcastAsync(ControllerStatusDto.Disconnected);
    }

    private void OnBatteryStateChanged(DualSenseBatteryState state)
    {
        string statusDesc = state.ChargingStatus switch
        {
            BatteryChargingStatus.Charging => "Carregando",
            BatteryChargingStatus.Full => "Carga Completa (100%)",
            BatteryChargingStatus.Discharging => "Em uso na bateria",
            BatteryChargingStatus.VoltageOrTemperatureOutOfRange => "Aviso: Tensão/Temperatura fora da faixa",
            BatteryChargingStatus.TemperatureError => "Erro: Superaquecimento da bateria",
            BatteryChargingStatus.ChargingError => "Erro de carregamento",
            _ => "Status Desconhecido"
        };

        if (state.IsCritical)
        {
            _logger.LogWarning("ALERTA CRÍTICO: Bateria do DualSense em {Percentage}%! Conecte o cabo para não desligar.", state.Percentage);
        }
        else if (state.IsLow)
        {
            _logger.LogWarning("Aviso: Bateria do DualSense baixa ({Percentage}% - {Status})", state.Percentage, statusDesc);
        }
        else if (state.IsFullyCharged)
        {
            _logger.LogInformation("DualSense com Carga Completa (100%).");
        }
        else
        {
            _logger.LogInformation("DualSense Bateria: {Percentage}% ({Status}) [{ConnectionType}]",
                state.Percentage, statusDesc, state.ConnectionType);
        }

        // Notificar clientes WebSocket em tempo real
        var statusDto = ControllerStatusDto.FromState(state, _hidReader.CurrentDevice);
        _ = _webSocketManager.BroadcastAsync(statusDto);
    }
}
