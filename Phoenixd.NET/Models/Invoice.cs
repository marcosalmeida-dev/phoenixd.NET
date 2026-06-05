namespace Phoenixd.NET.Models;

/// <summary>
/// Result of <c>POST /createinvoice</c> (phoenixd <c>GeneratedInvoice</c>).
/// </summary>
public class Invoice
{
    /// <summary>Requested amount in satoshis, or <c>null</c> for an any-amount invoice.</summary>
    public long? AmountSat { get; set; }

    public string PaymentHash { get; set; } = string.Empty;

    /// <summary>The BOLT11 payment request that can be encoded into a QR code.</summary>
    public string Serialized { get; set; } = string.Empty;
}
