using Microsoft.Extensions.Logging;
using Phoenixd.NET.Models;
using System.Net.WebSockets;
using System.Text;

namespace Phoenixd.NET.WebService.Client;

public class PhoenixdClient
{
    private readonly PhoenixConfig _phoenixConfig;
    private readonly ILogger<PhoenixdClient> _logger;
    private ClientWebSocket _webSocket;
    private const int ReconnectDelayInSeconds = 5; // Time to wait before reconnecting

    public event Action<string> OnMessageReceived;

    public PhoenixdClient(PhoenixConfig phoenixConfig, ILogger<PhoenixdClient> logger)
    {
        _phoenixConfig = phoenixConfig;
        _logger = logger;
    }

    private void InitializeWebSocket()
    {
        if (_webSocket != null)
        {
            _logger.LogInformation("Disposing of the previous WebSocket instance.");
            _webSocket.Dispose();
        }
        _webSocket = new ClientWebSocket();
        var tokenBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{_phoenixConfig.Token}"));
        _webSocket.Options.SetRequestHeader("Authorization", $"Basic {tokenBase64}");
    }

    internal async Task ConnectWebSocketAsync()
    {
        var wsHost = _phoenixConfig.Host.Replace("http://", "").Replace("https://", "");
        var uri = new Uri($"ws://{wsHost}/websocket");

        while (true) // Keep attempting to connect
        {
            try
            {
                InitializeWebSocket(); // Always start with a fresh WebSocket instance

                _logger.LogInformation("Connecting to Phoenixd payments websocket...");
                await _webSocket.ConnectAsync(uri, CancellationToken.None);
                _logger.LogInformation("Connected to Phoenixd payments websocket!");

                // Start receiving messages
                _ = Task.Run(async () => await ReceiveMessagesAsync());

                // Break out of loop if successfully connected
                break;
            }
            catch (ObjectDisposedException)
            {
                _logger.LogError("WebSocket object was disposed unexpectedly. Creating a new WebSocket instance and retrying...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Phoenixd WebSocket connection error! Host: {uri}. Retrying in {ReconnectDelayInSeconds} seconds...");
            }

            // Wait before retrying
            await Task.Delay(TimeSpan.FromSeconds(ReconnectDelayInSeconds));
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
                _logger.LogError(ex, "Error receiving Phoenixd WebSocket message! Reconnecting...");
                await ReconnectAsync();
            }
        }
    }

    private async Task ReconnectAsync()
    {
        _logger.LogInformation($"Reconnecting WebSocket in {ReconnectDelayInSeconds} seconds...");
        await Task.Delay(TimeSpan.FromSeconds(ReconnectDelayInSeconds));
        await ConnectWebSocketAsync(); // Retry connection after delay
    }

    internal async Task DisconnectWebSocketAsync()
    {
        if (_webSocket != null && _webSocket.State != WebSocketState.Closed)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                _webSocket.Dispose();
                _logger.LogInformation("Disconnected from Phoenixd payments websocket");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Phoenixd WebSocket");
            }
        }
    }
}
