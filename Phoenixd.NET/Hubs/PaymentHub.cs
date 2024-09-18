using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Phoenixd.NET.Core.Hubs;

public class PaymentHub : Hub
{
    // Store connection IDs mapped to user identifiers (or another unique key)
    private static ConcurrentDictionary<string, string> _connectionIdToUserMap = new ConcurrentDictionary<string, string>();

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        // Here you might map the connectionId to a user identifier or some unique key
        // For simplicity, we'll just use the connectionId itself
        _connectionIdToUserMap[connectionId] = connectionId;

        await Clients.Caller.SendAsync("ReceiveConnectionId", connectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove the connection ID when the client disconnects
        _connectionIdToUserMap.TryRemove(Context.ConnectionId, out _);

        await base.OnDisconnectedAsync(exception);
    }

    // Method to send a message to a specific client based on connection ID
    public async Task SendMessageToClient(string connectionId, string message)
    {
        if (_connectionIdToUserMap.ContainsKey(connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
        }
    }
}
