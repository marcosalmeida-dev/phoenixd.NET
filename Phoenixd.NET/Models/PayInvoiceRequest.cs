namespace Phoenixd.NET.Models;

/// <summary>Request for paying a BOLT11 invoice (<c>POST /payinvoice</c>).</summary>
public class PayInvoiceRequest
{
    public string Invoice { get; set; } = string.Empty;

    /// <summary>Optional amount override in satoshis. Mutually exclusive with SendAll.</summary>
    public long? AmountSat { get; set; }

    /// <summary>Use the maximum sendable Lightning balance. Only true is sent to phoenixd.</summary>
    public bool SendAll { get; set; }
}
