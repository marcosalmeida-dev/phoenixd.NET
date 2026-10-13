namespace Phoenixd.NET.Models;

/// <summary>Request for paying a Lightning address using BIP353 or LNURL resolution.</summary>
public class PayLnAddressRequest
{
    public string Address { get; set; } = string.Empty;

    /// <summary>Amount in satoshis. Supply this or set SendAll.</summary>
    public long? AmountSat { get; set; }

    /// <summary>Use the maximum sendable Lightning balance. Mutually exclusive with AmountSat.</summary>
    public bool SendAll { get; set; }

    public string? Message { get; set; }
}
