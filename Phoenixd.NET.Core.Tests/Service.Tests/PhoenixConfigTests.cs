using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Phoenixd.NET.Client;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;
using Phoenixd.NET.Services;
using Phoenixd.NET.Tests.Helpers;

namespace Phoenixd.NET.Tests.Service.Tests;

public class PhoenixConfigTests
{
    [Fact]
    public void ResolvePasswordFiles_PreservesLiteralPasswordsAndOtherSettings()
    {
        var config = new PhoenixConfig
        {
            Token = " literal password ",
            LimitedAccessToken = " limited password ",
            Host = "https://localhost:9740",
            Username = "user",
            WebhookSecret = "webhook",
            RequestTimeoutSeconds = 42
        };

        var resolved = config.ResolvePasswordFiles();

        Assert.Equal(config.Token, resolved.Token);
        Assert.Equal(config.LimitedAccessToken, resolved.LimitedAccessToken);
        Assert.Equal(config.Host, resolved.Host);
        Assert.Equal(config.Username, resolved.Username);
        Assert.Equal(config.WebhookSecret, resolved.WebhookSecret);
        Assert.Equal(config.RequestTimeoutSeconds, resolved.RequestTimeoutSeconds);
    }

    [Fact]
    public void ResolvePasswordFiles_TrimsFilesAndOverridesLiteralPasswords()
    {
        using var fullAccess = new PasswordFile("  full-access\r\n");
        using var limitedAccess = new PasswordFile("\tlimited-access\n");
        var config = new PhoenixConfig
        {
            Token = "old-full",
            TokenFile = fullAccess.Path,
            LimitedAccessToken = "old-limited",
            LimitedAccessTokenFile = limitedAccess.Path
        };

        var resolved = config.ResolvePasswordFiles();

        Assert.Equal("full-access", resolved.Token);
        Assert.Equal("limited-access", resolved.LimitedAccessToken);
        Assert.Equal("old-full", config.Token);
        Assert.Equal("old-limited", config.LimitedAccessToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public void ResolvePasswordFiles_RejectsEmptyFilesWithoutFallingBack(string content)
    {
        using var file = new PasswordFile(content);
        var config = new PhoenixConfig { Token = "literal-fallback", TokenFile = file.Path };

        var error = Assert.Throws<InvalidOperationException>(() => config.ResolvePasswordFiles());

        Assert.Contains("TokenFile", error.Message);
        Assert.Contains("non-empty", error.Message);
        Assert.DoesNotContain(config.Token, error.Message);
    }

    [Fact]
    public void ResolvePasswordFiles_RejectsMissingAndUnreadableFiles()
    {
        var missingPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"phoenixd-missing-{Guid.NewGuid():N}");
        var missingConfig = new PhoenixConfig { TokenFile = missingPath };
        var unreadableConfig = new PhoenixConfig { LimitedAccessTokenFile = System.IO.Path.GetTempPath() };

        var missingError = Assert.Throws<InvalidOperationException>(() => missingConfig.ResolvePasswordFiles());
        var unreadableError = Assert.Throws<InvalidOperationException>(() => unreadableConfig.ResolvePasswordFiles());

        Assert.Contains("TokenFile", missingError.Message);
        Assert.Contains("LimitedAccessTokenFile", unreadableError.Message);
    }

    [Fact]
    public void Registration_RejectsInvalidPasswordFilesImmediately()
    {
        using var file = new PasswordFile("\n");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PhoenixConfig:TokenFile"] = file.Path
        }).Build();

        Assert.Throws<InvalidOperationException>(() => new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .ConfigurePhoenixdServices(configuration));
    }

    [Fact]
    public async Task Registration_UsesSameResolvedCredentialsForHttpAndWebsocket()
    {
        using var file = new PasswordFile("  original-password\r\n");
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PhoenixConfig:Host"] = $"http://127.0.0.1:{port}",
            ["PhoenixConfig:Username"] = "custom-user",
            ["PhoenixConfig:Token"] = "overridden-password",
            ["PhoenixConfig:TokenFile"] = file.Path
        }).Build();
        var handler = new StubHttpMessageHandler("""{"nodeId":"node","channels":[],"chain":"testnet","blockHeight":1,"version":"0.9.2"}""");
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.ConfigurePhoenixdServices(configuration);
        services.AddHttpClient<INodeService, NodeService>().ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();

        // Credentials are captured together at registration, so replacing a secret never makes
        // new HTTP clients disagree with the long-lived websocket client.
        File.WriteAllText(file.Path, "replacement-password");
        await provider.GetRequiredService<INodeService>().GetNodeInfo();
        var httpAuthorization = handler.LastRequest!.Headers.Authorization!.ToString();

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var websocketTask = provider.GetRequiredService<PhoenixdClient>().RunAsync(cancellation.Token);
        using var connection = await listener.AcceptTcpClientAsync(cancellation.Token);
        using var reader = new StreamReader(connection.GetStream(), Encoding.ASCII, leaveOpen: true);
        string? websocketAuthorization = null;
        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellation.Token)))
        {
            if (line.StartsWith("Authorization: ", StringComparison.OrdinalIgnoreCase))
            {
                websocketAuthorization = line["Authorization: ".Length..];
            }
        }

        cancellation.Cancel();
        await websocketTask;

        var expectedAuthorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("custom-user:original-password"));
        Assert.Equal(expectedAuthorization, httpAuthorization);
        Assert.Equal(httpAuthorization, websocketAuthorization);
        Assert.Equal("original-password", provider.GetRequiredService<PhoenixConfig>().Token);
    }

    private sealed class PasswordFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"phoenixd-test-password-{Guid.NewGuid():N}");

        public PasswordFile(string contents) => File.WriteAllText(Path, contents, new UTF8Encoding(false));

        public void Dispose() => File.Delete(Path);
    }
}
