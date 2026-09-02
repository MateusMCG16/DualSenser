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

}
