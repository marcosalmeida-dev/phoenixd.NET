using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Phoenixd.NET.Client;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;
using Phoenixd.NET.Services;

namespace Phoenixd.NET;

public static class ServiceCollection
{
    /// <summary>
    /// Registers the phoenixd API services, the resilient websocket client and the SignalR bridge.
    /// Bind a <c>PhoenixConfig</c> configuration section before calling this.
    /// </summary>
    public static IServiceCollection ConfigurePhoenixdServices(this IServiceCollection services, IConfiguration configuration)
    {
        var phoenixConfig = configuration.GetSection("PhoenixConfig").Get<PhoenixConfig>()
            ?? throw new InvalidOperationException("PhoenixConfig section was not found in configuration.");

        if (string.IsNullOrWhiteSpace(phoenixConfig.Host))
        {
            throw new InvalidOperationException("PhoenixConfig.Host must be configured.");
        }

        services.AddSingleton(phoenixConfig);

        // The websocket client manages a single long-lived connection.
        services.AddSingleton<PhoenixdClient>();

        // Typed HTTP clients give each service a properly pooled handler. NOTE: phoenixd payment
        // operations (payinvoice, payoffer, sendtoaddress, ...) are NOT idempotent — do not attach a
        // retrying resilience handler to these clients, or a transient failure could double-spend.
        services.AddHttpClient<INodeService, NodeService>(client => ConfigureClient(client, phoenixConfig));
        services.AddHttpClient<IPaymentService, PaymentService>(client => ConfigureClient(client, phoenixConfig));

        services.AddSingleton<PhoenixdManagerService>();
        services.AddHostedService<PhoenixdClientBackgroundService>();

        return services;
    }

    private static void ConfigureClient(HttpClient client, PhoenixConfig config)
    {
        client.BaseAddress = new Uri(config.Host);
        client.Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds > 0 ? config.RequestTimeoutSeconds : 30);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{config.Token}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
