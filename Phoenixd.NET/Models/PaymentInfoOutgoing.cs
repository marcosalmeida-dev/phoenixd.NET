namespace Phoenixd.NET.Models;

/// <summary>
/// An outgoing payment as returned by <c>GET /payments/outgoing</c>,
/// <c>GET /payments/outgoing/{uuid}</c> and <c>GET /payments/outgoingbyhash/{paymentHash}</c>.
/// </summary>
public class PaymentInfoOutgoing
{
    /// <summary>Response discriminator, <c>outgoing_payment</c>.</summary>
    public string? Type { get; set; }

    /// <summary>
    /// Payment category: lightning, auto_liquidity, manual_liquidity, splice_out, splice_cpfp,
    /// or channel_close.
    /// </summary>
    public string? SubType { get; set; }

    /// <summary>The phoenixd payment id (UUID).</summary>
    public string PaymentId { get; set; } = string.Empty;

    public string? PaymentHash { get; set; }
    public string? Preimage { get; set; }

    /// <summary>Transaction id for on-chain payments, including liquidity purchases and channel operations.</summary>
    public string? TxId { get; set; }

    public bool IsPaid { get; set; }

    /// <summary>Amount in satoshis, including the payment fees.</summary>
    public long Sent { get; set; }

    /// <summary>Fees in <b>millisatoshis</b>.</summary>
    public long Fees { get; set; }

    public string? Invoice { get; set; }

    /// <summary>Unix timestamp (ms) at which the payment completed.</summary>
    public long? CompletedAt { get; set; }

    /// <summary>Unix timestamp (ms) at which the payment was created.</summary>
    public long CreatedAt { get; set; }
}
