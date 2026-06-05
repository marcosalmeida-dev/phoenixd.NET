using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Hubs;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;
using Phoenixd.NET.WebService.Client;

namespace Phoenixd.NET.Services;

/// <summary>
/// Coordinates the phoenixd services and bridges incoming <c>payment_received</c> websocket events
/// to SignalR clients. The convention is that callers pass their SignalR connection id as the
/// invoice <c>externalId</c>; this service then forwards the event to that specific client.
/// </summary>
public class PhoenixdManagerService
{
    private readonly PhoenixdClient _phoenixdClient;
    private readonly IHubContext<PaymentHub> _hubContext;
    private readonly ILogger<PhoenixdManagerService> _logger;

    public INodeService NodeService { get; }
    public IPaymentService PaymentService { get; }

    /// <summary>Raised for every incoming payment, for consumers that don't use SignalR.</summary>
    public event Action<PaymentReceived>? PaymentReceived;

    public PhoenixdManagerService(
        PhoenixdClient phoenixdClient,
        IHubContext<PaymentHub> hubContext,
        INodeService nodeService,
        IPaymentService paymentService,
        ILogger<PhoenixdManagerService> logger)
    {
        _phoenixdClient = phoenixdClient ?? throw new ArgumentNullException(nameof(phoenixdClient));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        NodeService = nodeService ?? throw new ArgumentNullException(nameof(nodeService));
        PaymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));

        _phoenixdClient.OnPaymentReceived += HandlePaymentReceived;
    }

    private void HandlePaymentReceived(PaymentReceived paymentReceived)
    {
        try
        {
            PaymentReceived?.Invoke(paymentReceived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A PaymentReceived handler threw.");
        }

        // Fire-and-forget the SignalR notification; avoid async void so failures are observed/logged.
        _ = NotifyClientAsync(paymentReceived);
    }

    private async Task NotifyClientAsync(PaymentReceived paymentReceived)
    {
        var connectionId = paymentReceived.ExternalId;
        if (string.IsNullOrEmpty(connectionId))
        {
            return;
        }

        try
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceivePayment", paymentReceived);
            _logger.LogInformation("Forwarded payment {PaymentHash} to SignalR client {ConnectionId}.",
                paymentReceived.PaymentHash, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment notification to SignalR client {ConnectionId}.", connectionId);
        }
    }
}
