namespace DualSenser.Service.Models;

public sealed record DualSenseDeviceInfo(
    string DevicePath,
    ushort VendorId,
    ushort ProductId,
    ushort VersionNumber,
    ConnectionType ConnectionType,
    string ModelName
);
