using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Core.Hubs;
using Phoenixd.NET.Core.Interfaces;
using Phoenixd.NET.Core.Models;
using Phoenixd.NET.Services;
using Phoenixd.NET.WebService.Client;
using Phoenixd.NET.WebServiceClient.Services;

namespace Phoenixd.NET;

public static class ServiceCollection
{
    public static IServiceCollection ConfigurePhoenixdServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Retrieve or set PhoenixConfig here
        var phoenixConfig = configuration.GetSection("PhoenixConfig").Get<PhoenixConfig>();

        if (phoenixConfig == null)
        {
            throw new ArgumentException("PhoenixConfig not found.");
        }

        // Register PhoenixConfig as a singleton
        services.AddSingleton(phoenixConfig);

        // Register the PhoenixdClient
        services.AddSingleton<PhoenixdClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PhoenixdClient>>();
            return new PhoenixdClient(phoenixConfig, logger);
        });

        // Register IHttpClientFactory and configure the named client
        services.AddHttpClient("PhoenixdHttpClient", client =>
        {
            client.BaseAddress = new Uri(phoenixConfig.Host);
            var byteArray = Encoding.UTF8.GetBytes($"{phoenixConfig.Username}:{phoenixConfig.Token}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        });

        services.AddSingleton<INodeService>(provider =>
        {
            var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = clientFactory.CreateClient("PhoenixdHttpClient");
            var logger = provider.GetRequiredService<ILogger<NodeService>>();
            return new NodeService(httpClient, logger);
        });

        services.AddSingleton<IPaymentService>(provider =>
        {
            var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = clientFactory.CreateClient("PhoenixdHttpClient");
            var logger = provider.GetRequiredService<ILogger<PaymentService>>();
            return new PaymentService(httpClient, logger);
        });

        // Register the PhoenixdClientBackgroundService
        services.AddHostedService<PhoenixdClientBackgroundService>(provider =>
        {
            var phoenixdClient = provider.GetRequiredService<PhoenixdClient>();
            var logger = provider.GetRequiredService<ILogger<PhoenixdClientBackgroundService>>();
            return new PhoenixdClientBackgroundService(phoenixdClient, logger);
        });

        // Register the PhoenixdManagerService
        services.AddSingleton<PhoenixdManagerService>(provider =>
        {
            var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = clientFactory.CreateClient("PhoenixdHttpClient");
            var phoenixdClient = provider.GetRequiredService<PhoenixdClient>();

            var hubContext = provider.GetRequiredService<IHubContext<PaymentHub>>();
            var nodeService = provider.GetRequiredService<INodeService>();
            var paymentService = provider.GetRequiredService<IPaymentService>();

            var logger = provider.GetRequiredService<ILogger<PhoenixdManagerService>>();

            return new PhoenixdManagerService(httpClient, phoenixdClient, hubContext, nodeService, paymentService, logger);
        });

        return services;
    }
}
