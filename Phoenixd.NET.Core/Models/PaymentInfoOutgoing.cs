namespace Phoenixd.NET.Core.Models;

public class PaymentInfoOutgoing
{
    public string PaymentHash { get; set; }
    public string Preimage { get; set; }
    public bool IsPaid { get; set; }
    public long Sent { get; set; }
    public long Fees { get; set; }
    public string Invoice { get; set; }
    public long CompletedAt { get; set; }
    public long CreatedAt { get; set; }
}
