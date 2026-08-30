using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DualSenser.Service.Hid.Native;
using DualSenser.Service.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace DualSenser.Service.Hid;

public sealed class DualSenseHidReader : IDualSenseHidReader
{
    private readonly ILogger<DualSenseHidReader> _logger;
    private readonly object _stateLock = new();

    private SafeFileHandle? _deviceHandle;
    private FileStream? _fileStream;
    private CancellationTokenSource? _readerCts;
    private Task? _readLoopTask;

    public event Action<DualSenseBatteryState>? BatteryStateChanged;
    public event Action<DualSenseInputState>? InputStateChanged;
    public event Action<DualSenseDeviceInfo>? DeviceConnected;
    public event Action? DeviceDisconnected;

    public DualSenseBatteryState CurrentState { get; private set; } = DualSenseBatteryState.Disconnected;
    public DualSenseInputState CurrentInputState { get; private set; } = DualSenseInputState.Empty;
    public DualSenseDeviceInfo? CurrentDevice { get; private set; }
    public bool IsRunning { get; private set; }

    public DualSenseHidReader(ILogger<DualSenseHidReader> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<DualSenseDeviceInfo> ScanDevices()
    {
        var devices = new List<DualSenseDeviceInfo>();
        NativeMethods.HidD_GetHidGuid(out Guid hidGuid);

        IntPtr hDevInfo = NativeMethods.SetupDiGetClassDevs(
            ref hidGuid,
            IntPtr.Zero,
            IntPtr.Zero,
            NativeConstants.DIGCF_PRESENT | NativeConstants.DIGCF_DEVICEINTERFACE
        );

        if (hDevInfo == IntPtr.Zero || hDevInfo.ToInt64() == -1)
        {
            _logger.LogWarning("Não foi possível obter a lista de interfaces de dispositivos HID (SetupDiGetClassDevs).");
            return devices;
        }

        try
        {
            var interfaceData = new NativeStructs.SP_DEVICE_INTERFACE_DATA();
            interfaceData.cbSize = Marshal.SizeOf<NativeStructs.SP_DEVICE_INTERFACE_DATA>();
            uint memberIndex = 0;

            while (NativeMethods.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref hidGuid, memberIndex++, ref interfaceData))
            {
                NativeMethods.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, IntPtr.Zero, 0, out uint requiredSize, IntPtr.Zero);
                if (requiredSize == 0)
                {
                    continue;
                }

                IntPtr detailBuffer = Marshal.AllocHGlobal((int)requiredSize);
                try
                {
                    int cbSize = (IntPtr.Size == 8) ? 8 : 5;
                    Marshal.WriteInt32(detailBuffer, cbSize);

                    if (NativeMethods.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, detailBuffer, requiredSize, out _, IntPtr.Zero))
                    {
                        IntPtr pDevicePath = IntPtr.Add(detailBuffer, 4);
                        string? devicePath = Marshal.PtrToStringAuto(pDevicePath);

                        if (string.IsNullOrWhiteSpace(devicePath))
                        {
                            continue;
                        }

                        using var queryHandle = NativeMethods.CreateFile(
                            devicePath,
                            0,
                            NativeConstants.FILE_SHARE_READ | NativeConstants.FILE_SHARE_WRITE,
                            IntPtr.Zero,
                            NativeConstants.OPEN_EXISTING,
                            0,
                            IntPtr.Zero
                        );

                        if (!queryHandle.IsInvalid)
                        {
                            var attributes = new NativeStructs.HIDD_ATTRIBUTES();
                            attributes.Size = Marshal.SizeOf<NativeStructs.HIDD_ATTRIBUTES>();

                            if (NativeMethods.HidD_GetAttributes(queryHandle, ref attributes))
                            {
                                if (attributes.VendorID == NativeConstants.SonyVendorId &&
                                    (attributes.ProductID == NativeConstants.DualSenseStandardPid ||
                                     attributes.ProductID == NativeConstants.DualSenseEdgePid))
                                {
                                    bool isBluetooth = devicePath.Contains("{00001124-0000-1000-8000-00805f9b34fb}", StringComparison.OrdinalIgnoreCase) ||
                                                       devicePath.Contains("vid&0002054c", StringComparison.OrdinalIgnoreCase) ||
                                                       devicePath.Contains("bth", StringComparison.OrdinalIgnoreCase);

                                    var connectionType = isBluetooth ? ConnectionType.Bluetooth : ConnectionType.Usb;
                                    string modelName = attributes.ProductID == NativeConstants.DualSenseEdgePid 
                                        ? "DualSense Edge Wireless Controller" 
                                        : "DualSense Wireless Controller";

                                    devices.Add(new DualSenseDeviceInfo(
                                        DevicePath: devicePath,
                                        VendorId: attributes.VendorID,
                                        ProductId: attributes.ProductID,
                                        VersionNumber: attributes.VersionNumber,
                                        ConnectionType: connectionType,
                                        ModelName: modelName
                                    ));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detailBuffer);
                }
            }
        }
        finally
        {
            NativeMethods.SetupDiDestroyDeviceInfoList(hDevInfo);
        }

        return devices;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;
        _logger.LogInformation("Iniciando monitoramento de hardware HID do DualSense...");

        using var reg = cancellationToken.Register(() =>
        {
            Stop();
        });

        while (!cancellationToken.IsCancellationRequested && IsRunning)
        {
            try
            {
                if (CurrentDevice == null || _deviceHandle == null || _deviceHandle.IsInvalid || _deviceHandle.IsClosed)
                {
                    var devices = ScanDevices();
                    var candidate = devices.FirstOrDefault();

                    if (candidate != null)
                    {
                        _logger.LogInformation("Controle detectado: {ModelName} ({ConnectionType}) em {DevicePath}",
                            candidate.ModelName, candidate.ConnectionType, candidate.DevicePath);

                        if (TryOpenDevice(candidate))
                        {
                            CurrentDevice = candidate;
                            DeviceConnected?.Invoke(candidate);

                            _readerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            _readLoopTask = ReadLoopAsync(candidate, _readerCts.Token);
                            await _readLoopTask;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exceção transitória no ciclo de conexão HID.");
            }

            if (CurrentDevice == null && !cancellationToken.IsCancellationRequested && IsRunning)
            {
                try
                {
                    await Task.Delay(2000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        Stop();
    }

    private bool TryOpenDevice(DualSenseDeviceInfo device)
    {
        try
        {
            _deviceHandle = NativeMethods.CreateFile(
                device.DevicePath,
                NativeConstants.GENERIC_READ | NativeConstants.GENERIC_WRITE,
                NativeConstants.FILE_SHARE_READ | NativeConstants.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeConstants.OPEN_EXISTING,
                NativeConstants.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero
            );

            if (_deviceHandle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("Falha ao abrir handle do dispositivo {DevicePath}. Código Win32: {ErrorCode}", device.DevicePath, error);
                return false;
            }

            if (device.ConnectionType == ConnectionType.Bluetooth)
            {
                Thread.Sleep(100);
                ActivateBluetoothExtendedMode(_deviceHandle);
            }

            int bufferSize = device.ConnectionType == ConnectionType.Bluetooth 
                ? NativeConstants.BluetoothReport31Size 
                : NativeConstants.UsbReport01Size;

            _fileStream = new FileStream(_deviceHandle, FileAccess.Read, bufferSize, isAsync: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar stream HID para {DevicePath}", device.DevicePath);
            CleanupHandle();
            return false;
        }
    }

    private void ActivateBluetoothExtendedMode(SafeFileHandle handle)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            byte[] featureBuffer = new byte[NativeConstants.FeatureReport05Size];
            featureBuffer[0] = NativeConstants.FeatureReportIdCalibration;

            bool success = NativeMethods.HidD_GetFeature(handle, featureBuffer, featureBuffer.Length);
            if (success)
            {
                _logger.LogDebug("Handshake de ativação Bluetooth (Feature Report 0x05) realizado com sucesso.");
                return;
            }

            _logger.LogDebug("Tentativa {Attempt}/3 do handshake Bluetooth falhou. Aguardando retentativa...", attempt);
            Thread.Sleep(100);
        }

        _logger.LogWarning("Não foi possível enviar Feature Report 0x05 de ativação Bluetooth. O controle pode permanecer em modo simples.");
    }

    private async Task ReadLoopAsync(DualSenseDeviceInfo device, CancellationToken cancellationToken)
    {
        int bufferSize = device.ConnectionType == ConnectionType.Bluetooth 
            ? NativeConstants.BluetoothReport31Size 
            : NativeConstants.UsbReport01Size;

        byte[] buffer = new byte[bufferSize];

        using var loopReg = cancellationToken.Register(() =>
        {
            CleanupHandle();
        });

        try
        {
            while (!cancellationToken.IsCancellationRequested && _fileStream != null)
            {
                int bytesRead = await _fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

                if (bytesRead > 0)
                {
                    var span = buffer.AsSpan(0, bytesRead);

                    // 1. Processar estado da Bateria
                    var newState = DualSenseReportParser.Parse(span, device.ConnectionType);
                    if (newState.IsConnected)
                    {
                        bool stateChanged = false;
                        lock (_stateLock)
                        {
                            if (CurrentState.Percentage != newState.Percentage ||
                                CurrentState.ChargingStatus != newState.ChargingStatus ||
                                CurrentState.IsConnected != newState.IsConnected ||
                                CurrentState.ConnectionType != newState.ConnectionType)
                            {
                                CurrentState = newState;
                                stateChanged = true;
                            }
                        }

                        if (stateChanged)
                        {
                            BatteryStateChanged?.Invoke(newState);
                        }
                    }

                    // 2. Processar estado dos Controles/Inputs
                    var newInputState = DualSenseReportParser.ParseInputState(span, device.ConnectionType);
                    InputStateChanged?.Invoke(newInputState);
                    CurrentInputState = newInputState;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelamento gracioso
        }
        catch (ObjectDisposedException)
        {
            // Stream fechado durante o shutdown
        }
        catch (IOException ioEx)
        {
            _logger.LogInformation("Controle {ModelName} desconectado ou sinal perdido ({Message}).", device.ModelName, ioEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no loop de leitura HID do controle.");
        }
        finally
        {
            HandleDisconnection();
        }
    }

    private void HandleDisconnection()
    {
        lock (_stateLock)
        {
            CurrentDevice = null;
            CurrentState = DualSenseBatteryState.Disconnected;
            CurrentInputState = DualSenseInputState.Empty;
        }

        CleanupHandle();
        DeviceDisconnected?.Invoke();
    }

    private void CleanupHandle()
    {
        try
        {
            _fileStream?.Dispose();
            _fileStream = null;
        }
        catch { }

        try
        {
            if (_deviceHandle != null && !_deviceHandle.IsClosed)
            {
                _deviceHandle.Dispose();
            }
            _deviceHandle = null;
        }
        catch { }
    }

    public void Stop()
    {
        IsRunning = false;
        try
        {
            _readerCts?.Cancel();
        }
        catch { }
        CleanupHandle();
    }

    public void Dispose()
    {
        Stop();
        _readerCts?.Dispose();
    }
}
