using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DualSenser.Service.Common;
using DualSenser.Service.Hid;
using DualSenser.Service.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DualSenser.Service.Services;

public sealed class DualSenseMonitorWorker : BackgroundService
{
    private readonly IDualSenseHidReader _hidReader;
    private readonly AppConfig _config;
    private readonly ILogger<DualSenseMonitorWorker> _logger;

    private DualSenseInputState _previousInputState = DualSenseInputState.Empty;

    public DualSenseMonitorWorker(
        IDualSenseHidReader hidReader,
        AppConfig config,
        ILogger<DualSenseMonitorWorker> logger)
    {
        _hidReader = hidReader;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=================================================");
        _logger.LogInformation("Serviço DualSenser iniciado com sucesso.");
        _logger.LogInformation("Monitorando controles DualSense (Bluetooth e USB)...");
        if (_config.ShowControllerActivity)
        {
            _logger.LogInformation(">>> [CONFIG] ShowControllerActivity = TRUE: exibindo ações do usuário em tempo real.");
        }
        else
        {
            _logger.LogInformation(">>> [CONFIG] ShowControllerActivity = FALSE (configure em Config/config.ini).");
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
    }

    private void OnDeviceDisconnected()
    {
        _logger.LogInformation("<<< DISPOSITIVO DESCONECTADO. Aguardando reconexão...");
        _previousInputState = DualSenseInputState.Empty;
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
    }

    private void OnInputStateChanged(DualSenseInputState current)
    {
        var diffs = current.GetActivityDifferences(_previousInputState);

        if (diffs.Count > 0)
        {
            foreach (var diff in diffs)
            {
                _logger.LogInformation("[INPUT] {Activity}", diff);
            }
            _previousInputState = current;
        }
    }
}
