namespace Phoenixd.NET.Core.Models;

public class Channel
{
    public string State { get; set; }
    public string ChannelId { get; set; }
    public long BalanceSat { get; set; }
    public long InboundLiquiditySat { get; set; }
    public long CapacitySat { get; set; }
    public string FundingTxId { get; set; }
}
