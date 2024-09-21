using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Hubs;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;
using Phoenixd.NET.WebService.Client;

namespace Phoenixd.NET.Services
{
    public class PhoenixdManagerService
    {
        private readonly HttpClient _httpClient;
        private readonly PhoenixdClient _phoenixdClient;
        private readonly IHubContext<PaymentHub> _hubContext;
        private readonly ILogger<PhoenixdManagerService> _logger;

        public readonly INodeService NodeService;
        public readonly IPaymentService PaymentService;

        public PhoenixdManagerService(
            HttpClient httpClient,
            PhoenixdClient phoenixdClient,
            IHubContext<PaymentHub> hubContext,
            INodeService nodeService,
            IPaymentService paymentService,
            ILogger<PhoenixdManagerService> logger)
        {
            _httpClient = httpClient;
            _phoenixdClient = phoenixdClient;
            _hubContext = hubContext;
            _logger = logger;

            NodeService = nodeService;
            PaymentService = paymentService;

            _phoenixdClient.OnMessageReceived += OnMessageReceived;
        }

        private async void OnMessageReceived(string message)
        {
            var paymentReceived = JsonSerializer.Deserialize<PaymentReceived>(message);

            if (!string.IsNullOrEmpty(paymentReceived?.ExternalId))
            {
                try
                {
                    await _hubContext.Clients.Client(paymentReceived.ExternalId).SendAsync("ReceivePayment", paymentReceived);
                    _logger.LogInformation($"Message sent to client with ConnectionId: {paymentReceived.ExternalId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending message to client {paymentReceived.ExternalId}");
                }
            }
        }
    }
}
