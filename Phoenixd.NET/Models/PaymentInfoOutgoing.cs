namespace Phoenixd.NET.Models;

/// <summary>
/// An outgoing payment as returned by <c>GET /payments/outgoing</c>,
/// <c>GET /payments/outgoing/{uuid}</c> and <c>GET /payments/outgoingbyhash/{paymentHash}</c>.
/// </summary>
public class PaymentInfoOutgoing
{
    /// <summary>Discriminates the kind of outgoing payment (e.g. lightning vs. on-chain).</summary>
    public string? SubType { get; set; }

    /// <summary>The phoenixd payment id (UUID).</summary>
    public string PaymentId { get; set; } = string.Empty;

    public string? PaymentHash { get; set; }
    public string? Preimage { get; set; }

    /// <summary>On-chain transaction id, for on-chain (sendtoaddress) payments.</summary>
    public string? TxId { get; set; }

    public bool IsPaid { get; set; }

    /// <summary>Amount sent in satoshis.</summary>
    public long Sent { get; set; }

    /// <summary>Fees in <b>millisatoshis</b>.</summary>
    public long Fees { get; set; }

    public string? Invoice { get; set; }

    /// <summary>Unix timestamp (ms) at which the payment completed.</summary>
    public long? CompletedAt { get; set; }

    /// <summary>Unix timestamp (ms) at which the payment was created.</summary>
    public long CreatedAt { get; set; }
}
