using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DualSenser.Service.Models;

namespace DualSenser.Service.Hid;

public interface IDualSenseHidReader : IDisposable
{
    event Action<DualSenseBatteryState>? BatteryStateChanged;
    event Action<DualSenseInputState>? InputStateChanged;
    event Action<DualSenseDeviceInfo>? DeviceConnected;
    event Action? DeviceDisconnected;

    DualSenseBatteryState CurrentState { get; }
    DualSenseInputState CurrentInputState { get; }
    DualSenseDeviceInfo? CurrentDevice { get; }
    bool IsRunning { get; }

    IReadOnlyList<DualSenseDeviceInfo> ScanDevices();
    Task StartAsync(CancellationToken cancellationToken);
    void Stop();
}
