namespace Phoenixd.NET.Core.Models;

public class PayInvoiceResponse
{
    public long RecipientAmountSat { get; set; }
    public long RoutingFeeSat { get; set; }
    public string PaymentId { get; set; }
    public string PaymentHash { get; set; }
    public string PaymentPreimage { get; set; }
}
