namespace Phoenixd.NET.Models;

public class Invoice
{
    public long AmountSat { get; set; }
    public string PaymentHash { get; set; }
    public string Serialized { get; set; }
}
