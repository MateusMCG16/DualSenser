using System;
using System.Text.Json.Serialization;

namespace DualSenser.Service.Models.Network;

public sealed record HealthCheckDto(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("connectedClients")] int ConnectedClients,
    [property: JsonPropertyName("controllerConnected")] bool ControllerConnected,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);
