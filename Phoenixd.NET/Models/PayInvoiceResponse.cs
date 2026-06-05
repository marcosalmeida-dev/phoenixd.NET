namespace Phoenixd.NET.Models;

/// <summary>
/// Result of a successful outgoing Lightning payment (phoenixd <c>PaymentSent</c>), returned by
/// <c>POST /payinvoice</c>, <c>POST /payoffer</c>, <c>POST /paylnaddress</c> and <c>POST /lnurlpay</c>.
/// </summary>
public class PayInvoiceResponse
{
    public long RecipientAmountSat { get; set; }
    public long RoutingFeeSat { get; set; }

    /// <summary>The phoenixd payment id (UUID).</summary>
    public string PaymentId { get; set; } = string.Empty;

    public string PaymentHash { get; set; } = string.Empty;
    public string PaymentPreimage { get; set; } = string.Empty;
}
