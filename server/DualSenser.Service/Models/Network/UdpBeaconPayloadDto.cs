using System;
using System.Text.Json.Serialization;

namespace DualSenser.Service.Models.Network;

public sealed record UdpBeaconPayloadDto(
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("serverName")] string ServerName,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);
