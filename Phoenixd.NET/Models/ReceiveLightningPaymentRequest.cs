namespace Phoenixd.NET.Models;

/// <summary>Request payload for creating a Lightning invoice (<c>POST /createinvoice</c>).</summary>
public class ReceiveLightningPaymentRequest
{
    public string? Description { get; set; }

    /// <summary>Amount in satoshis. Use 0 / a null-amount invoice for an any-amount invoice.</summary>
    public long AmountSat { get; set; }

    /// <summary>Optional caller-supplied identifier echoed back on payment events.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Optional SHA-256 hash of the description (mutually exclusive with <see cref="Description"/>).</summary>
    public string? DescriptionHash { get; set; }

    /// <summary>Optional invoice expiry in seconds.</summary>
    public long? ExpirySeconds { get; set; }

    /// <summary>Optional per-invoice webhook URL notified when this invoice is paid.</summary>
    public string? WebhookUrl { get; set; }
}
