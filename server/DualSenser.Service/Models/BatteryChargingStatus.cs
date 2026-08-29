namespace DualSenser.Service.Models;

public enum BatteryChargingStatus
{
    Discharging = 0x0,
    Charging = 0x1,
    Full = 0x2,
    VoltageOrTemperatureOutOfRange = 0xA,
    TemperatureError = 0xB,
    ChargingError = 0xF,
    Unknown = 0xFF
}
