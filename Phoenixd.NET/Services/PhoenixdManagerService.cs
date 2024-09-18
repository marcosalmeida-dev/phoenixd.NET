using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Core.Hubs;
using Phoenixd.NET.Core.Interfaces;
using Phoenixd.NET.WebService.Client;
using Phoenixd.NET.WebServiceClient.Services;
using System;
using System.Threading.Tasks;

namespace Phoenixd.NET.Services
{
    public class PhoenixdManagerService
    {
        private readonly HttpClient _httpClient;
        private readonly PhoenixdClient _phoenixdClient;
        private readonly INodeService _nodeService;
        private readonly IPaymentService _paymentService;
        private readonly IHubContext<PaymentHub> _hubContext;
        private readonly ILogger<PhoenixdManagerService> _logger;

        public PhoenixdManagerService(
            HttpClient httpClient,
            PhoenixdClient phoenixdClient,
            INodeService nodeService,
            IPaymentService paymentService,
            IHubContext<PaymentHub> hubContext,
            ILogger<PhoenixdManagerService> logger)
        {
            _httpClient = httpClient;
            _phoenixdClient = phoenixdClient;
            _nodeService = nodeService;
            _paymentService = paymentService;
            _hubContext = hubContext;
            _logger = logger;

            _phoenixdClient.OnMessageReceived += OnMessageReceived;
        }

        private async void OnMessageReceived(string message)
        {
            // Extract the connection ID or user information from the message to determine the recipient
            string connectionId = ExtractConnectionId(message);

            if (!string.IsNullOrEmpty(connectionId))
            {
                try
                {
                    // Send the message to the specific client
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
                    _logger.LogInformation($"Message sent to client with ConnectionId: {connectionId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending message to client {connectionId}");
                }
            }
        }

        private string ExtractConnectionId(string message)
        {
            // Implement logic to extract connection ID from the message
            // For simplicity, assume the message contains the connection ID
            return message; // Replace with actual extraction logic
        }
    }
}
