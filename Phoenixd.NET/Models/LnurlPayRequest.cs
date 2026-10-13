namespace Phoenixd.NET.Models;

/// <summary>Request for paying an LNURL (<c>POST /lnurlpay</c>).</summary>
public class LnurlPayRequest
{
    public string Lnurl { get; set; } = string.Empty;

    /// <summary>Optional amount in satoshis; otherwise phoenixd uses the LNURL minimum.</summary>
    public long? AmountSat { get; set; }

    /// <summary>Use the maximum sendable Lightning balance. Mutually exclusive with AmountSat.</summary>
    public bool SendAll { get; set; }

    public string? Message { get; set; }
}
