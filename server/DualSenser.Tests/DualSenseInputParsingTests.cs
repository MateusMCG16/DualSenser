using System;
using DualSenser.Service.Hid;
using DualSenser.Service.Models;
using Xunit;

namespace DualSenser.Tests;

public class DualSenseInputParsingTests
{
    [Fact]
    public void ParseInputState_Bluetooth_ExtractsSticksAndTriggers()
    {
        // Arrange
        byte[] buffer = new byte[78];
        buffer[0] = 0x31;
        buffer[2] = 255; // LX (Direita total)
        buffer[3] = 0;   // LY (Cima total)
        buffer[4] = 128; // RX (Centro)
        buffer[5] = 128; // RY (Centro)
        buffer[6] = 200; // L2 analógico
        buffer[7] = 255; // R2 analógico (Pressionado 100%)

        // Act
        var state = DualSenseReportParser.ParseInputState(buffer, ConnectionType.Bluetooth);

        // Assert
        Assert.Equal(255, state.LeftStickX);
        Assert.Equal(0, state.LeftStickY);
        Assert.Equal(128, state.RightStickX);
        Assert.Equal(128, state.RightStickY);
        Assert.Equal(200, state.L2Trigger);
        Assert.Equal(255, state.R2Trigger);
    }

    [Fact]
    public void ParseInputState_Bluetooth_ExtractsActionButtonsAndDPad()
    {
        // Arrange
        byte[] buffer = new byte[78];
        buffer[0] = 0x31;
        // Byte 9: DPad=0 (Cima), Cross=0x20, Triangle=0x80 -> 0xA0
        buffer[9] = 0xA0; 
        // Byte 10: L1=0x01, Options=0x20 -> 0x21
        buffer[10] = 0x21;
        // Byte 11: TouchClick=0x02, MicMute=0x04 -> 0x06
        buffer[11] = 0x06;

        // Act
        var state = DualSenseReportParser.ParseInputState(buffer, ConnectionType.Bluetooth);

        // Assert
        Assert.True(state.DPadUp);
        Assert.False(state.DPadDown);
        Assert.True(state.Cross);
        Assert.True(state.Triangle);
        Assert.False(state.Square);
        Assert.False(state.Circle);
        Assert.True(state.L1);
        Assert.True(state.Options);
        Assert.True(state.TouchpadClick);
        Assert.True(state.MicMute);
    }

    [Fact]
    public void ParseInputState_Bluetooth_ExtractsTouchpadCoordinates()
    {
        // Arrange: Byte 34..37 é o Touch 1 (offset 34 com Report ID)
        byte[] buffer = new byte[78];
        buffer[0] = 0x31;
        
        // Touch 1: Ativo (bit 7 = 0), TouchId = 5
        buffer[34] = 0x05; 
        // X = 1200 (0x04B0) -> Byte[35] = 0xB0, Byte[36] (bits 0..3) = 0x04
        buffer[35] = 0xB0;
        // Y = 800 (0x0320) -> Byte[36] (bits 4..7) = 0x00 (ou 0x20 >> 4 = 2), Byte[37] = 0x32
        // X = B0 | (0x04 << 8) = 1200
        // Y = (0x02) | (0x32 << 4) = 2 | 800 = 802
        buffer[36] = (0x04 & 0x0F) | ((0x02 & 0x0F) << 4);
        buffer[37] = 0x32;

        // Act
        var state = DualSenseReportParser.ParseInputState(buffer, ConnectionType.Bluetooth);

        // Assert
        Assert.True(state.Touch1.IsTouching);
        Assert.Equal(5, state.Touch1.TouchId);
        Assert.Equal(1200, state.Touch1.X);
        Assert.Equal(802, state.Touch1.Y);
    }

    [Fact]
    public void GetActivityDifferences_DetectsButtonPressedAndTouchMoving()
    {
        // Arrange
        var prev = DualSenseInputState.Empty;
        var current = prev with
        {
            Cross = true,
            R1 = true,
            Touch1 = new TouchPoint(true, 1, 500, 300)
        };

        // Act
        var diffs = current.GetActivityDifferences(prev);

        // Assert
        Assert.Contains(diffs, d => d.Contains("Cruz (X)"));
        Assert.Contains(diffs, d => d.Contains("R1"));
        Assert.Contains(diffs, d => d.Contains("Trackpad"));
    }
}
