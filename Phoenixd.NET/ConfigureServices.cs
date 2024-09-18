using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Core.Interfaces;
using Phoenixd.NET.Core.Models;
using Phoenixd.NET.Services;
using Phoenixd.NET.WebService.Client;
using Phoenixd.NET.WebServiceClient.Services;
using System.Net.Http.Headers;
using System.Text;

namespace Phoenixd.NET;

public static class ServiceCollection
{
    public static IServiceCollection ConfigurePhoenixdServices(this WebApplicationBuilder builder, HttpClient httpClient)
    {
        // Retrieve or set PhoenixConfig here
        var phoenixConfig = builder.Configuration.GetSection("PhoenixConfig").Get<PhoenixConfig>();

        if (phoenixConfig == null)
        {
            throw new ArgumentException("PhoenixConfig not found.");
        }

        // Configure the HttpClient with the PhoenixConfig settings
        httpClient.BaseAddress = new Uri(phoenixConfig.Host);

        var byteArray = Encoding.UTF8.GetBytes($"{phoenixConfig.Username}:{phoenixConfig.Token}");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        // Register PhoenixConfig as a singleton
        builder.Services.AddSingleton(phoenixConfig);

        // Register the HttpClient instance as a singleton
        builder.Services.AddSingleton(httpClient);

        // Register the PhoenixdClient
        builder.Services.AddSingleton<PhoenixdClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PhoenixdClient>>();
            return new PhoenixdClient(phoenixConfig, logger);
        });

        // Register the PhoenixdClientBackgroundService
        builder.Services.AddHostedService<PhoenixdClientBackgroundService>();

        // Register NodeService and PaymentService
        builder.Services.AddScoped<INodeService, NodeService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();

        // Register the PhoenixdManagerService
        builder.Services.AddSingleton<PhoenixdManagerService>();

        // Register SignalR hub
        builder.Services.AddSignalR();

        return builder.Services;
    }
}
