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
    /// Optional limited-access HTTP password (phoenixd <c>http-password-limited-access</c>), which
    /// can read data and create invoices but cannot send payments. Reserved for callers that only
    /// need read/receive access.
    /// </summary>
    public string? LimitedAccessToken { get; set; }

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
}
