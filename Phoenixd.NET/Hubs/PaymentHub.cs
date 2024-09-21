using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Phoenixd.NET.Hubs;

public class PaymentHub : Hub
{
    private static ConcurrentDictionary<string, string> _connectionIdToUserMap = new ConcurrentDictionary<string, string>();

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        _connectionIdToUserMap[connectionId] = connectionId;

        await Clients.Caller.SendAsync("ReceiveConnectionId", connectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionIdToUserMap.TryRemove(Context.ConnectionId, out _);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessageToClient(string connectionId, string message)
    {
        if (_connectionIdToUserMap.ContainsKey(connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
        }
    }

    public async Task SendMessageToAll(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}
