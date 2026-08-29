using System;
using DualSenser.Service.Hid;
using DualSenser.Service.Models;
using Xunit;

namespace DualSenser.Tests;

public class DualSenseReportParserTests
{
    [Theory]
    [InlineData(0x00, 0, BatteryChargingStatus.Discharging, false, false)]
    [InlineData(0x01, 10, BatteryChargingStatus.Discharging, false, false)]
    [InlineData(0x02, 20, BatteryChargingStatus.Discharging, false, false)]
    [InlineData(0x05, 50, BatteryChargingStatus.Discharging, false, false)]
    [InlineData(0x08, 80, BatteryChargingStatus.Discharging, false, false)]
    [InlineData(0x0A, 100, BatteryChargingStatus.Discharging, false, false)]
    [InlineData(0x15, 50, BatteryChargingStatus.Charging, true, false)]
    [InlineData(0x1A, 100, BatteryChargingStatus.Charging, true, false)]
    [InlineData(0x2A, 100, BatteryChargingStatus.Full, false, true)]
    [InlineData(0xA5, 50, BatteryChargingStatus.VoltageOrTemperatureOutOfRange, false, false)]
    [InlineData(0xB2, 20, BatteryChargingStatus.TemperatureError, false, false)]
    [InlineData(0xF1, 10, BatteryChargingStatus.ChargingError, false, false)]
    public void Parse_BluetoothReport_WithReportId_ExtractsCorrectState(
        byte statusByte, 
        int expectedPercentage, 
        BatteryChargingStatus expectedStatus, 
        bool expectedIsCharging, 
        bool expectedIsFull)
    {
        // Arrange: Buffer de 78 bytes com Report ID 0x31
        byte[] buffer = new byte[78];
        buffer[0] = 0x31;
        buffer[54] = statusByte;

        // Act
        var result = DualSenseReportParser.Parse(buffer);

        // Assert
        Assert.True(result.IsConnected);
        Assert.Equal(ConnectionType.Bluetooth, result.ConnectionType);
        Assert.Equal(expectedPercentage, result.Percentage);
        Assert.Equal(expectedStatus, result.ChargingStatus);
        Assert.Equal(expectedIsCharging, result.IsCharging);
        Assert.Equal(expectedIsFull, result.IsFullyCharged);
        Assert.Equal(statusByte, result.RawStatusByte);
    }

    [Fact]
    public void Parse_BluetoothReport_WithoutReportId_ExtractsCorrectState()
    {
        // Arrange: Buffer de 77 bytes (sem Report ID no byte 0), offset de bateria no byte 53
        byte[] buffer = new byte[77];
        buffer[53] = 0x17; // 70% e Carregando (0x1)

        // Act
        var result = DualSenseReportParser.Parse(buffer, ConnectionType.Bluetooth);

        // Assert
        Assert.True(result.IsConnected);
        Assert.Equal(ConnectionType.Bluetooth, result.ConnectionType);
        Assert.Equal(70, result.Percentage);
        Assert.Equal(BatteryChargingStatus.Charging, result.ChargingStatus);
        Assert.True(result.IsCharging);
    }

    [Fact]
    public void Parse_UsbReport_WithReportId_ExtractsCorrectState()
    {
        // Arrange: Buffer de 64 bytes com Report ID 0x01, offset de bateria no byte 53
        byte[] buffer = new byte[64];
        buffer[0] = 0x01;
        buffer[53] = 0x1A; // 100% e Carregando (0x1)

        // Act
        var result = DualSenseReportParser.Parse(buffer);

        // Assert
        Assert.True(result.IsConnected);
        Assert.Equal(ConnectionType.Usb, result.ConnectionType);
        Assert.Equal(100, result.Percentage);
        Assert.Equal(BatteryChargingStatus.Charging, result.ChargingStatus);
        Assert.True(result.IsCharging);
    }

    [Fact]
    public void Parse_EmptyOrInvalidBuffer_ReturnsDisconnected()
    {
        // Act & Assert para buffer vazio
        var emptyResult = DualSenseReportParser.Parse(ReadOnlySpan<byte>.Empty);
        Assert.False(emptyResult.IsConnected);
        Assert.Equal(0, emptyResult.Percentage);

        // Act & Assert para buffer muito curto
        byte[] shortBuffer = new byte[10];
        shortBuffer[0] = 0x31;
        var shortResult = DualSenseReportParser.Parse(shortBuffer);
        Assert.False(shortResult.IsConnected);
    }

    [Theory]
    [InlineData(10, false, true, true)]   // 10% descarregando: crítico e baixo
    [InlineData(15, false, true, true)]   // 15% descarregando: crítico e baixo
    [InlineData(20, false, false, true)]  // 20% descarregando: baixo, não crítico
    [InlineData(25, false, false, true)]  // 25% descarregando: baixo, não crítico
    [InlineData(30, false, false, false)] // 30% descarregando: normal
    [InlineData(10, true, false, false)]  // 10% carregando: não alerta crítico pois está na tomada
    public void BatteryThresholds_EvaluateCorrectly(int percentage, bool isCharging, bool expectCritical, bool expectLow)
    {
        var state = new DualSenseBatteryState(
            Percentage: percentage,
            ChargingStatus: isCharging ? BatteryChargingStatus.Charging : BatteryChargingStatus.Discharging,
            IsCharging: isCharging,
            IsFullyCharged: false,
            IsConnected: true,
            ConnectionType: ConnectionType.Bluetooth,
            RawStatusByte: 0,
            Timestamp: DateTime.UtcNow
        );

        Assert.Equal(expectCritical, state.IsCritical);
        Assert.Equal(expectLow, state.IsLow);
    }
}
