using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Json;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.WebService.Client;

/// <summary>
/// Maintains a resilient websocket connection to phoenixd's <c>/websocket</c> endpoint and surfaces
/// the <c>payment_received</c> events it pushes. The connection is re-established automatically with
/// exponential backoff until the supplied <see cref="CancellationToken"/> is cancelled.
/// </summary>
public sealed class PhoenixdClient
{
    private const int MaxBackoffSeconds = 60;

    private readonly PhoenixConfig _phoenixConfig;
    private readonly ILogger<PhoenixdClient> _logger;
    private readonly Uri _websocketUri;
    private readonly string _authorizationHeader;

    /// <summary>Raised with the raw JSON for every message received on the websocket.</summary>
    public event Action<string>? OnMessageReceived;

    /// <summary>Raised with the parsed <c>payment_received</c> event for incoming payments.</summary>
    public event Action<PaymentReceived>? OnPaymentReceived;

    public PhoenixdClient(PhoenixConfig phoenixConfig, ILogger<PhoenixdClient> logger)
    {
        _phoenixConfig = phoenixConfig ?? throw new ArgumentNullException(nameof(phoenixConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _websocketUri = BuildWebsocketUri(phoenixConfig.Host);
        _authorizationHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($":{phoenixConfig.Token}"));
    }

    /// <summary>
    /// Connects and processes messages until <paramref name="cancellationToken"/> is cancelled,
    /// reconnecting with exponential backoff on any failure. Intended to be driven by a hosted
    /// background service.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var webSocket = new ClientWebSocket();
                webSocket.Options.SetRequestHeader("Authorization", _authorizationHeader);

                _logger.LogInformation("Connecting to phoenixd websocket at {Uri}...", _websocketUri);
                await webSocket.ConnectAsync(_websocketUri, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Connected to phoenixd websocket.");
                attempt = 0;

                await ReceiveLoopAsync(webSocket, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                var delay = GetBackoffDelay(++attempt);
                _logger.LogError(ex, "phoenixd websocket error; reconnecting in {Seconds}s (attempt {Attempt}).",
                    delay.TotalSeconds, attempt);
                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("phoenixd websocket listener stopped.");
    }

    private async Task ReceiveLoopAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var messageStream = new MemoryStream();

        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            messageStream.SetLength(0);

            WebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("phoenixd websocket closed by server ({Status}: {Description}).",
                        result.CloseStatus, result.CloseStatusDescription);
                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                messageStream.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage); // reassemble messages that span multiple frames

            var message = Encoding.UTF8.GetString(messageStream.GetBuffer(), 0, (int)messageStream.Length);
            Dispatch(message);
        }
    }

    private void Dispatch(string message)
    {
        _logger.LogDebug("phoenixd websocket message: {Message}", message);

        if (OnMessageReceived is { } rawHandler)
        {
            try
            {
                rawHandler(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An OnMessageReceived handler threw.");
            }
        }

        if (OnPaymentReceived is null)
        {
            return;
        }

        try
        {
            var paymentReceived = JsonSerializer.Deserialize<PaymentReceived>(message, PhoenixdJson.Default);
            if (paymentReceived is not null &&
                string.Equals(paymentReceived.Type, "payment_received", StringComparison.Ordinal))
            {
                OnPaymentReceived(paymentReceived);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse phoenixd websocket message as a payment event.");
        }
    }

    private static Uri BuildWebsocketUri(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("PhoenixConfig.Host must be set.", nameof(host));
        }

        var builder = new UriBuilder(host)
        {
            Path = "/websocket",
            Query = string.Empty
        };

        builder.Scheme = string.Equals(builder.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? "wss"
            : "ws";

        return builder.Uri;
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        // 2, 4, 8, 16, 32, 60, 60... seconds.
        var seconds = Math.Min(MaxBackoffSeconds, Math.Pow(2, Math.Min(attempt, 6)));
        return TimeSpan.FromSeconds(seconds);
    }
}
