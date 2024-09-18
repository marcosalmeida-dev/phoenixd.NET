using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.WebService.Client;

namespace Phoenixd.NET.WebServiceClient.Services;

public class PhoenixdClientBackgroundService : BackgroundService
{
    private readonly PhoenixdClient _phoenixdClient;
    private readonly ILogger<PhoenixdClientBackgroundService> _logger;

    public PhoenixdClientBackgroundService(PhoenixdClient phoenixdClient, ILogger<PhoenixdClientBackgroundService> logger)
    {
        _phoenixdClient = phoenixdClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _phoenixdClient.OnMessageReceived += HandleMessageReceived;
            await _phoenixdClient.ConnectWebSocketAsync();

            stoppingToken.Register(async () =>
            {
                _logger.LogInformation("Background service stopping.");
                await _phoenixdClient.DisconnectWebSocketAsync();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PhoenixdClientBackgroundService");
        }
    }

    private void HandleMessageReceived(string message)
    {
        // Handle the message or pass it to a service to handle
    }
}
