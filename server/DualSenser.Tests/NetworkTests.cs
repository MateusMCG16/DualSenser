using System;
using System.Text.Json;
using DualSenser.Service.Common;
using DualSenser.Service.Models;
using DualSenser.Service.Models.Network;
using Xunit;

namespace DualSenser.Tests;

public class NetworkTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [Fact]
    public void ControllerStatusDto_SerializesToCamelCaseJson()
    {
        // Arrange
        var dto = new ControllerStatusDto(
            Connected: true,
            ModelName: "DualSense Wireless Controller",
            ConnectionType: "Bluetooth",
            BatteryPercentage: 75,
            ChargingStatus: "Discharging",
            IsCharging: false,
            IsFullyCharged: false,
            IsCritical: false,
            IsLow: false,
            Timestamp: DateTime.UtcNow
        );

        // Act
        string json = JsonSerializer.Serialize(dto, JsonOptions);

        // Assert
        Assert.Contains("\"connected\":true", json);
        Assert.Contains("\"modelName\":\"DualSense Wireless Controller\"", json);
        Assert.Contains("\"connectionType\":\"Bluetooth\"", json);
        Assert.Contains("\"batteryPercentage\":75", json);
        Assert.Contains("\"chargingStatus\":\"Discharging\"", json);
        Assert.Contains("\"isCharging\":false", json);
        Assert.Contains("\"isFullyCharged\":false", json);
        Assert.Contains("\"isCritical\":false", json);
        Assert.Contains("\"isLow\":false", json);
        Assert.Contains("\"timestamp\":", json);
    }

    [Fact]
    public void ControllerStatusDto_FromState_MapsCorrectly()
    {
        // Arrange
        var batteryState = new DualSenseBatteryState(
            Percentage: 10,
            ChargingStatus: BatteryChargingStatus.Discharging,
            IsCharging: false,
            IsFullyCharged: false,
            IsConnected: true,
            ConnectionType: ConnectionType.Bluetooth,
            RawStatusByte: 0x01,
            Timestamp: DateTime.UtcNow
        );

        var deviceInfo = new DualSenseDeviceInfo(
            DevicePath: "\\\\?\\hid#vid_054c&pid_0ce6...",
            VendorId: 0x054C,
            ProductId: 0x0CE6,
            VersionNumber: 1,
            ConnectionType: ConnectionType.Bluetooth,
            ModelName: "DualSense Wireless Controller"
        );

        // Act
        var dto = ControllerStatusDto.FromState(batteryState, deviceInfo);

        // Assert
        Assert.True(dto.Connected);
        Assert.Equal("DualSense Wireless Controller", dto.ModelName);
        Assert.Equal("Bluetooth", dto.ConnectionType);
        Assert.Equal(10, dto.BatteryPercentage);
        Assert.Equal("Discharging", dto.ChargingStatus);
        Assert.False(dto.IsCharging);
        Assert.False(dto.IsFullyCharged);
        Assert.True(dto.IsCritical);
        Assert.True(dto.IsLow);
    }

    [Fact]
    public void UdpBeaconPayloadDto_SerializesCorrectly()
    {
        // Arrange
        var payload = new UdpBeaconPayloadDto(
            Service: "DualSenser",
            Version: "1.0",
            Port: 5005,
            ServerName: "TEST-PC",
            Timestamp: DateTime.UtcNow
        );

        // Act
        string json = JsonSerializer.Serialize(payload, JsonOptions);

        // Assert
        Assert.Contains("\"service\":\"DualSenser\"", json);
        Assert.Contains("\"version\":\"1.0\"", json);
        Assert.Contains("\"port\":5005", json);
        Assert.Contains("\"serverName\":\"TEST-PC\"", json);
    }

    [Fact]
    public void HealthCheckDto_SerializesCorrectly()
    {
        // Arrange
        var health = new HealthCheckDto(
            Status: "healthy",
            Service: "DualSenser",
            Version: "1.0",
            ConnectedClients: 2,
            ControllerConnected: true,
            Timestamp: DateTime.UtcNow
        );

        // Act
        string json = JsonSerializer.Serialize(health, JsonOptions);

        // Assert
        Assert.Contains("\"status\":\"healthy\"", json);
        Assert.Contains("\"connectedClients\":2", json);
        Assert.Contains("\"controllerConnected\":true", json);
    }

    [Fact]
    public void ConfigManager_WritesAndReadsNetworkSection()
    {
        // Arrange
        string filePath = ConfigManager.GetConfigFilePath();

        // Act
        var config = ConfigManager.LoadOrCreateConfig();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.HttpPort > 0);
        Assert.True(config.UdpBeaconPort > 0);
        Assert.True(config.UdpBeaconIntervalSeconds > 0);

        string content = System.IO.File.ReadAllText(filePath);
        Assert.Contains("[Network]", content);
        Assert.Contains("HttpPort=", content);
        Assert.Contains("EnableUdpBeacon=", content);
        Assert.Contains("UdpBeaconPort=", content);
        Assert.Contains("UdpBeaconIntervalSeconds=", content);
    }
}
