namespace DualSenser.Service.Hid.Native;

internal static class NativeConstants
{
    public const ushort SonyVendorId = 0x054C;
    public const ushort DualSenseStandardPid = 0x0CE6;
    public const ushort DualSenseEdgePid = 0x0DF2;

    public const uint DIGCF_PRESENT = 0x00000002;
    public const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;

    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;

    public const uint OPEN_EXISTING = 3;
    public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

    public const int BluetoothReport31Size = 78;
    public const int UsbReport01Size = 64;
    public const int FeatureReport05Size = 41;

    public const byte ReportIdFullBluetooth = 0x31;
    public const byte ReportIdUsb = 0x01;
    public const byte FeatureReportIdCalibration = 0x05;
}
