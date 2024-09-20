namespace Phoenixd.NET.Core.Models;

public class ReceiveLightningPaymentRequest
{
    public string Description { get; set; }
    public long AmountSat { get; set; }
    public string ExternalId { get; set; }
}
