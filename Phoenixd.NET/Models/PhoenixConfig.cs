using System.Text;

namespace Phoenixd.NET.Models;

/// <summary>
/// Connection settings for a phoenixd instance, bound from the <c>PhoenixConfig</c> configuration
/// section.
/// </summary>
public class PhoenixConfig
{
    /// <summary>
    /// The full-access HTTP password (phoenixd <c>http-password</c>). Required for both read and
    /// write operations, including payments.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Optional UTF-8 file containing the full-access HTTP password. Takes precedence over
    /// <see cref="Token"/>; surrounding whitespace is ignored, as with phoenixd's
    /// <c>--http-password-file</c> option. The file must be readable and non-empty.
    /// </summary>
    public string? TokenFile { get; set; }

    /// <summary>
    /// Optional limited-access HTTP password (phoenixd <c>http-password-limited-access</c>), which
    /// can read data and create invoices but cannot send payments. Reserved for callers that only
    /// need read/receive access.
    /// </summary>
    public string? LimitedAccessToken { get; set; }

    /// <summary>
    /// Optional file containing the limited-access password, taking precedence over
    /// <see cref="LimitedAccessToken"/>. Matches phoenixd's <c>--http-password-limited-access-file</c>.
    /// </summary>
    public string? LimitedAccessTokenFile { get; set; }

    /// <summary>Base URL of the phoenixd HTTP API. Defaults to the daemon's default bind address/port.</summary>
    public string Host { get; set; } = "http://127.0.0.1:9740";

    /// <summary>Basic-auth username. phoenixd ignores the username, but a value is still sent.</summary>
    public string Username { get; set; } = "phoenix";

    /// <summary>
    /// Shared secret used to verify incoming webhook callbacks (phoenixd <c>webhook-secret</c>).
    /// See <see cref="Webhooks.PhoenixdWebhookValidator"/>.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>HTTP request timeout in seconds applied to the phoenixd API client.</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    // Resolve once before registration so HTTP and websocket clients share the same credentials,
    // even if the mounted secret changes while the application is running.
    internal PhoenixConfig ResolvePasswordFiles() => new()
    {
        Token = ReadPasswordFile(TokenFile, nameof(TokenFile)) ?? Token,
        LimitedAccessToken = ReadPasswordFile(LimitedAccessTokenFile, nameof(LimitedAccessTokenFile)) ?? LimitedAccessToken,
        Host = Host,
        Username = Username,
        WebhookSecret = WebhookSecret,
        RequestTimeoutSeconds = RequestTimeoutSeconds
    };

    internal string BasicAuthenticationParameter =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Token}"));

    private static string? ReadPasswordFile(string? path, string setting)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string password;
        try
        {
            password = File.ReadAllText(path, Encoding.UTF8).Trim();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new InvalidOperationException($"PhoenixConfig.{setting} could not be read: {path}.", ex);
        }

        if (password.Length == 0)
        {
            throw new InvalidOperationException($"PhoenixConfig.{setting} must contain a non-empty password: {path}.");
        }

        return password;
    }
}
