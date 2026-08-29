using System;
using System.Text.Json.Serialization;
using DualSenser.Service.Models;

namespace DualSenser.Service.Models.Network;

public sealed record ControllerStatusDto(
    [property: JsonPropertyName("connected")] bool Connected,
    [property: JsonPropertyName("modelName")] string ModelName,
    [property: JsonPropertyName("connectionType")] string ConnectionType,
    [property: JsonPropertyName("batteryPercentage")] int BatteryPercentage,
    [property: JsonPropertyName("chargingStatus")] string ChargingStatus,
    [property: JsonPropertyName("isCharging")] bool IsCharging,
    [property: JsonPropertyName("isFullyCharged")] bool IsFullyCharged,
    [property: JsonPropertyName("isCritical")] bool IsCritical,
    [property: JsonPropertyName("isLow")] bool IsLow,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
)
{
    public static ControllerStatusDto FromState(DualSenseBatteryState state, DualSenseDeviceInfo? device)
    {
        return new ControllerStatusDto(
            Connected: state.IsConnected,
            ModelName: device?.ModelName ?? (state.IsConnected ? "DualSense Wireless Controller" : "None"),
            ConnectionType: state.ConnectionType.ToString(),
            BatteryPercentage: state.Percentage,
            ChargingStatus: state.ChargingStatus.ToString(),
            IsCharging: state.IsCharging,
            IsFullyCharged: state.IsFullyCharged,
            IsCritical: state.IsCritical,
            IsLow: state.IsLow,
            Timestamp: state.Timestamp
        );
    }

    public static ControllerStatusDto Disconnected => new(
        Connected: false,
        ModelName: "None",
        ConnectionType: "Unknown",
        BatteryPercentage: 0,
        ChargingStatus: "Unknown",
        IsCharging: false,
        IsFullyCharged: false,
        IsCritical: false,
        IsLow: false,
        Timestamp: DateTime.UtcNow
    );
}
