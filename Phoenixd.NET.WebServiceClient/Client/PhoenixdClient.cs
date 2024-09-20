using Microsoft.Extensions.Logging;
using Phoenixd.NET.Core.Models;
using System.Net.WebSockets;
using System.Text;

namespace Phoenixd.NET.WebService.Client;

public class PhoenixdClient
{
    private readonly PhoenixConfig _phoenixConfig;
    private readonly ILogger<PhoenixdClient> _logger;
    private ClientWebSocket _webSocket;

    public event Action<string> OnMessageReceived;

    public PhoenixdClient(PhoenixConfig phoenixConfig, ILogger<PhoenixdClient> logger)
    {
        _phoenixConfig = phoenixConfig;
        _logger = logger;
    }

    internal async Task ConnectWebSocketAsync()
    {
        var wsHost = _phoenixConfig.Host.Replace("http://", "").Replace("https://", "");
        var tokenBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{_phoenixConfig.Token}"));

        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Basic {tokenBase64}");

        var uri = new Uri($"ws://{wsHost}/websocket");

        try
        {
            _logger.LogInformation("Connecting to payments websocket...");
            await _webSocket.ConnectAsync(uri, CancellationToken.None);
            _logger.LogInformation("Connected to payments websocket!");

            _ = Task.Run(async () => await ReceiveMessagesAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket connection error!");
            throw;
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[4096];
        while (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closed");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation("Received payment: {0}", message);

                    OnMessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving WebSocket message");
            }
        }
    }

    internal async Task DisconnectWebSocketAsync()
    {
        if (_webSocket != null)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                _webSocket.Dispose();
                _logger.LogInformation("Disconnected from payments websocket");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from WebSocket");
                throw;
            }
        }
    }
}
