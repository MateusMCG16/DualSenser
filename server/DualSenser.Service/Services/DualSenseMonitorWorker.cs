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
    private readonly ILogger _activityLogger;

    private DualSenseInputState _previousInputState = DualSenseInputState.Empty;

    public DualSenseMonitorWorker(
        IDualSenseHidReader hidReader,
        IDualSenseWebSocketManager webSocketManager,
        AppConfig config,
        ILogger<DualSenseMonitorWorker> logger,
        ILoggerFactory loggerFactory)
    {
        _hidReader = hidReader;
        _webSocketManager = webSocketManager;
        _config = config;
        _logger = logger;
        _activityLogger = loggerFactory.CreateLogger(LoggerConfig.ActivitySourceContext);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=================================================");
        _logger.LogInformation("Serviço DualSenser iniciado com sucesso.");
        _logger.LogInformation("Monitorando controles DualSense (Bluetooth e USB)...");
        _logger.LogInformation("Servidor HTTP & WebSockets ativo na porta: {HttpPort}", _config.HttpPort);

        if (_config.ShowControllerActivity)
        {
            _logger.LogInformation(">>> [MODO ATIVIDADE ATIVADO] ShowControllerActivity=TRUE");
            _logger.LogInformation(">>> As ações do controle serão exibidas no terminal e salvas em 'Logs/dualsenser-activity-*.log'.");
        }
        else
        {
            _logger.LogInformation(">>> [MODO NORMAL] ShowControllerActivity=FALSE (Exibindo logs de rede e status da bateria).");
        }
        _logger.LogInformation("=================================================");

        _hidReader.DeviceConnected += OnDeviceConnected;
        _hidReader.DeviceDisconnected += OnDeviceDisconnected;
        _hidReader.BatteryStateChanged += OnBatteryStateChanged;

        if (_config.ShowControllerActivity)
        {
            _hidReader.InputStateChanged += OnInputStateChanged;
        }

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

            if (_config.ShowControllerActivity)
            {
                _hidReader.InputStateChanged -= OnInputStateChanged;
            }

            _logger.LogInformation("Serviço DualSenser finalizado.");
        }
    }

    private void OnDeviceConnected(DualSenseDeviceInfo device)
    {
        _logger.LogInformation(">>> DISPOSITIVO CONECTADO: {ModelName} via {ConnectionType} (VID: 0x{VendorId:X4}, PID: 0x{ProductId:X4})",
            device.ModelName, device.ConnectionType, device.VendorId, device.ProductId);
        _previousInputState = DualSenseInputState.Empty;

        // Notificar clientes WebSocket
        var statusDto = ControllerStatusDto.FromState(_hidReader.CurrentState, device);
        _ = _webSocketManager.BroadcastAsync(statusDto);
    }

    private void OnDeviceDisconnected()
    {
        _logger.LogInformation("<<< DISPOSITIVO DESCONECTADO. Aguardando reconexão...");
        _previousInputState = DualSenseInputState.Empty;

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

    private void OnInputStateChanged(DualSenseInputState current)
    {
        var diffs = current.GetActivityDifferences(_previousInputState);

        if (diffs.Count > 0)
        {
            foreach (var diff in diffs)
            {
                // Registra através do logger específico de atividade
                _activityLogger.LogInformation("[INPUT] {Activity}", diff);
            }
            _previousInputState = current;
        }
    }
}
