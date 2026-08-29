using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DualSenser.Service.Network;

public interface IDualSenseWebSocketManager : IDisposable
{
    int ConnectedClientsCount { get; }
    Task HandleClientAsync(HttpContext context, CancellationToken cancellationToken);
    Task BroadcastAsync<T>(T payload, CancellationToken cancellationToken = default);
}
