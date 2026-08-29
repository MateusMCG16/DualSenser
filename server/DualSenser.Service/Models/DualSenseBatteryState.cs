using System;

namespace DualSenser.Service.Models;

public sealed record DualSenseBatteryState(
    int Percentage,
    BatteryChargingStatus ChargingStatus,
    bool IsCharging,
    bool IsFullyCharged,
    bool IsConnected,
    ConnectionType ConnectionType,
    byte RawStatusByte,
    DateTime Timestamp
)
{
    public bool IsCritical => IsConnected && !IsCharging && Percentage <= 15;
    public bool IsLow => IsConnected && !IsCharging && Percentage <= 25;

    public static DualSenseBatteryState Disconnected => new(
        Percentage: 0,
        ChargingStatus: BatteryChargingStatus.Unknown,
        IsCharging: false,
        IsFullyCharged: false,
        IsConnected: false,
        ConnectionType: ConnectionType.Unknown,
        RawStatusByte: 0,
        Timestamp: DateTime.UtcNow
    );
}
