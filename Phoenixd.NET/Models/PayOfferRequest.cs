namespace Phoenixd.NET.Models;

/// <summary>Request for paying a BOLT12 offer (<c>POST /payoffer</c>).</summary>
public class PayOfferRequest
{
    public string Offer { get; set; } = string.Empty;

    /// <summary>Optional amount override in satoshis. Mutually exclusive with SendAll.</summary>
    public long? AmountSat { get; set; }

    /// <summary>Use the maximum sendable Lightning balance. Only true is sent to phoenixd.</summary>
    public bool SendAll { get; set; }

    public string? Message { get; set; }
}
