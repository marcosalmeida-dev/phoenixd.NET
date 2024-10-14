using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.WebService.Client;

namespace Phoenixd.NET.Services;

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
            _logger.LogInformation("PhoenixdClientBackgroundService service starting...");
            await _phoenixdClient.ConnectWebSocketAsync();

            stoppingToken.Register(async () =>
            {
                _logger.LogInformation("PhoenixdClientBackgroundService service stopping.");
                await _phoenixdClient.DisconnectWebSocketAsync();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PhoenixdClientBackgroundService");
        }
    }
}
