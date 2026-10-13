namespace Phoenixd.NET.Models;

/// <summary>A failed Lightning payment, which phoenixd can return with HTTP status 200.</summary>
public class PaymentFailed
{
    /// <summary>Response discriminator, <c>payment_failed</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Payment hash for a failed invoice payment, when available.</summary>
    public string? PaymentHash { get; set; }

    /// <summary>BOLT12 offer id for a failed offer payment, when available.</summary>
    public string? OfferId { get; set; }

    public string Reason { get; set; } = string.Empty;
}
