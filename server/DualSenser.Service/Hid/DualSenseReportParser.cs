using System;
using DualSenser.Service.Models;

namespace DualSenser.Service.Hid;

public static class DualSenseReportParser
{
    public const byte BluetoothReportId = 0x31;
    public const byte UsbReportId = 0x01;

    public const int BluetoothReportSize = 78;
    public const int UsbReportSize = 64;

    public const int BluetoothBatteryOffsetWithId = 54;
    public const int BluetoothBatteryOffsetWithoutId = 53;

    public const int UsbBatteryOffsetWithId = 53;
    public const int UsbBatteryOffsetWithoutId = 52;

    public static DualSenseBatteryState Parse(ReadOnlySpan<byte> buffer, ConnectionType connectionType = ConnectionType.Unknown)
    {
        if (buffer.IsEmpty)
        {
            return DualSenseBatteryState.Disconnected;
        }

        byte statusByte;
        ConnectionType detectedConnection = connectionType;

        // Caso 1: Relatório Bluetooth completo de 78 bytes com Report ID 0x31 no byte 0
        if (buffer.Length >= BluetoothReportSize && buffer[0] == BluetoothReportId)
        {
            statusByte = buffer[BluetoothBatteryOffsetWithId];
            detectedConnection = ConnectionType.Bluetooth;
        }
        // Caso 2: Relatório Bluetooth sem Report ID no byte 0 (tamanho 77 bytes)
        else if (buffer.Length == BluetoothReportSize - 1 && (connectionType == ConnectionType.Bluetooth || connectionType == ConnectionType.Unknown))
        {
            statusByte = buffer[BluetoothBatteryOffsetWithoutId];
            detectedConnection = ConnectionType.Bluetooth;
        }
        // Caso 3: Relatório USB completo de 64 bytes com Report ID 0x01 no byte 0
        else if (buffer.Length >= UsbReportSize && buffer[0] == UsbReportId)
        {
            statusByte = buffer[UsbBatteryOffsetWithId];
            detectedConnection = ConnectionType.Usb;
        }
        // Caso 4: Relatório USB sem Report ID no byte 0 (tamanho 63 bytes)
        else if (buffer.Length == UsbReportSize - 1 && (connectionType == ConnectionType.Usb || connectionType == ConnectionType.Unknown))
        {
            statusByte = buffer[UsbBatteryOffsetWithoutId];
            detectedConnection = ConnectionType.Usb;
        }
        else
        {
            return DualSenseBatteryState.Disconnected;
        }

        // 1. Extração do nível de bateria (Bits 0-3: 0x0 a 0xA -> 0% a 100%)
        int rawCapacity = statusByte & 0x0F;
        int percentage = Math.Min(rawCapacity * 10, 100);

        // 2. Extração do status de carregamento (Bits 4-7)
        int rawCharging = (statusByte >> 4) & 0x0F;
        var chargingStatus = rawCharging switch
        {
            0x0 => BatteryChargingStatus.Discharging,
            0x1 => BatteryChargingStatus.Charging,
            0x2 => BatteryChargingStatus.Full,
            0xA => BatteryChargingStatus.VoltageOrTemperatureOutOfRange,
            0xB => BatteryChargingStatus.TemperatureError,
            0xF => BatteryChargingStatus.ChargingError,
            _ => BatteryChargingStatus.Unknown
        };

        bool isCharging = chargingStatus == BatteryChargingStatus.Charging;
        bool isFullyCharged = chargingStatus == BatteryChargingStatus.Full;

        return new DualSenseBatteryState(
            Percentage: percentage,
            ChargingStatus: chargingStatus,
            IsCharging: isCharging,
            IsFullyCharged: isFullyCharged,
            IsConnected: true,
            ConnectionType: detectedConnection,
            RawStatusByte: statusByte,
            Timestamp: DateTime.UtcNow
        );
    }

    public static DualSenseInputState ParseInputState(ReadOnlySpan<byte> buffer, ConnectionType connectionType = ConnectionType.Unknown)
    {
        if (buffer.IsEmpty)
            return DualSenseInputState.Empty;

        int offset = 0;
        bool isBluetooth = (buffer.Length >= BluetoothReportSize && buffer[0] == BluetoothReportId) ||
                           (buffer.Length == BluetoothReportSize - 1) ||
                           connectionType == ConnectionType.Bluetooth;

        bool hasReportId = (isBluetooth && buffer[0] == BluetoothReportId) || (!isBluetooth && buffer[0] == UsbReportId);

        if (isBluetooth)
        {
            // Bluetooth com Report ID: dados analógicos iniciam no índice 2
            offset = hasReportId ? 2 : 1;
        }
        else
        {
            // USB com Report ID: dados analógicos iniciam no índice 1
            offset = hasReportId ? 1 : 0;
        }

        if (buffer.Length < offset + 40)
            return DualSenseInputState.Empty;

        byte lx = buffer[offset];
        byte ly = buffer[offset + 1];
        byte rx = buffer[offset + 2];
        byte ry = buffer[offset + 3];
        byte l2 = buffer[offset + 4];
        byte r2 = buffer[offset + 5];

        // Botões (D-Pad e Ação)
        byte btn1 = buffer[offset + 7];
        int dpad = btn1 & 0x0F;
        bool dpadUp = dpad is 0 or 1 or 7;
        bool dpadRight = dpad is 1 or 2 or 3;
        bool dpadDown = dpad is 3 or 4 or 5;
        bool dpadLeft = dpad is 5 or 6 or 7;

        bool square = (btn1 & 0x10) != 0;
        bool cross = (btn1 & 0x20) != 0;
        bool circle = (btn1 & 0x40) != 0;
        bool triangle = (btn1 & 0x80) != 0;

        // Botões Digitais 2
        byte btn2 = buffer[offset + 8];
        bool l1 = (btn2 & 0x01) != 0;
        bool r1 = (btn2 & 0x02) != 0;
        bool l2Btn = (btn2 & 0x04) != 0;
        bool r2Btn = (btn2 & 0x08) != 0;
        bool create = (btn2 & 0x10) != 0;
        bool options = (btn2 & 0x20) != 0;
        bool l3 = (btn2 & 0x40) != 0;
        bool r3 = (btn2 & 0x80) != 0;

        // Botões Especiais
        byte btn3 = buffer[offset + 9];
        bool ps = (btn3 & 0x01) != 0;
        bool touchClick = (btn3 & 0x02) != 0;
        bool micMute = (btn3 & 0x04) != 0;

        // Trackpad (Offset relativo aos analógicos é +32)
        int touchOffset = offset + 32;
        TouchPoint touch1 = ParseTouchPoint(buffer.Slice(touchOffset, 4));
        TouchPoint touch2 = ParseTouchPoint(buffer.Slice(touchOffset + 4, 4));

        return new DualSenseInputState(
            LeftStickX: lx,
            LeftStickY: ly,
            RightStickX: rx,
            RightStickY: ry,
            L2Trigger: l2,
            R2Trigger: r2,
            Square: square,
            Cross: cross,
            Circle: circle,
            Triangle: triangle,
            DPadUp: dpadUp,
            DPadDown: dpadDown,
            DPadLeft: dpadLeft,
            DPadRight: dpadRight,
            L1: l1,
            R1: r1,
            L2Button: l2Btn,
            R2Button: r2Btn,
            Create: create,
            Options: options,
            L3: l3,
            R3: r3,
            PSButton: ps,
            TouchpadClick: touchClick,
            MicMute: micMute,
            Touch1: touch1,
            Touch2: touch2
        );
    }

    private static TouchPoint ParseTouchPoint(ReadOnlySpan<byte> touchBytes)
    {
        if (touchBytes.Length < 4)
            return new TouchPoint(false, 0, 0, 0);

        byte rawHeader = touchBytes[0];
        bool isTouching = (rawHeader & 0x80) == 0; // Bit 7 = 0 indica contato ativo
        int touchId = rawHeader & 0x7F;

        int x = touchBytes[1] | ((touchBytes[2] & 0x0F) << 8);
        int y = ((touchBytes[2] >> 4) & 0x0F) | (touchBytes[3] << 4);

        return new TouchPoint(isTouching, touchId, x, y);
    }
}
