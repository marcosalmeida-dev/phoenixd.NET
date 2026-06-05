namespace Phoenixd.NET.Models;

/// <summary>
/// An incoming payment as returned by <c>GET /payments/incoming</c> and
/// <c>GET /payments/incoming/{paymentHash}</c>.
/// </summary>
public class PaymentInfo
{
    /// <summary>Discriminates the kind of incoming payment (e.g. bolt11, bolt12, etc.).</summary>
    public string? SubType { get; set; }

    public string PaymentHash { get; set; } = string.Empty;
    public string? Preimage { get; set; }
    public string? ExternalId { get; set; }
    public string? Description { get; set; }

    /// <summary>The BOLT11 invoice associated with this payment, if any.</summary>
    public string? Invoice { get; set; }

    public bool IsPaid { get; set; }

    /// <summary>True when the invoice expired before being paid.</summary>
    public bool IsExpired { get; set; }

    /// <summary>Amount requested on the invoice in satoshis (null for any-amount invoices).</summary>
    public long? RequestedSat { get; set; }

    /// <summary>Amount actually received in satoshis.</summary>
    public long ReceivedSat { get; set; }

    /// <summary>Fees in <b>millisatoshis</b> (phoenixd reports incoming fees with millisat precision).</summary>
    public long Fees { get; set; }

    /// <summary>Optional note left by the payer (BOLT12).</summary>
    public string? PayerNote { get; set; }

    /// <summary>Optional public key of the payer (BOLT12).</summary>
    public string? PayerKey { get; set; }

    /// <summary>Unix timestamp (ms) at which the invoice expires.</summary>
    public long? ExpiresAt { get; set; }

    /// <summary>Unix timestamp (ms) at which the payment completed.</summary>
    public long? CompletedAt { get; set; }

    /// <summary>Unix timestamp (ms) at which the invoice was created.</summary>
    public long CreatedAt { get; set; }
}
