using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Client;

namespace Phoenixd.NET.Services;

/// <summary>
/// Hosts the long-running phoenixd websocket listener for the lifetime of the application. The
/// <see cref="PhoenixdClient"/> manages its own reconnection; this service simply drives it with the
/// host's stopping token so shutdown is graceful.
/// <para>
/// It also depends on <see cref="PhoenixdManagerService"/> so the websocket-to-SignalR bridge is
/// instantiated (and subscribed to payment events) at startup, rather than lazily on the first HTTP
/// request — otherwise payments could arrive before any subscriber exists.
/// </para>
/// </summary>
public class PhoenixdClientBackgroundService : BackgroundService
{
    private readonly PhoenixdClient _phoenixdClient;
    private readonly PhoenixdManagerService _managerService;
    private readonly ILogger<PhoenixdClientBackgroundService> _logger;

    public PhoenixdClientBackgroundService(
        PhoenixdClient phoenixdClient,
        PhoenixdManagerService managerService,
        ILogger<PhoenixdClientBackgroundService> logger)
    {
        _phoenixdClient = phoenixdClient ?? throw new ArgumentNullException(nameof(phoenixdClient));
        _managerService = managerService ?? throw new ArgumentNullException(nameof(managerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Touch the manager so its event subscription is wired before we start receiving messages.
        _ = _managerService;
        _logger.LogInformation("PhoenixdClientBackgroundService starting; payment bridge active.");

        try
        {
            await _phoenixdClient.RunAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PhoenixdClientBackgroundService terminated unexpectedly.");
        }
        finally
        {
            _logger.LogInformation("PhoenixdClientBackgroundService stopped.");
        }
    }
}
