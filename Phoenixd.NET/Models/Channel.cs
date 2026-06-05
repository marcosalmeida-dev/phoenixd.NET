namespace Phoenixd.NET.Models;

/// <summary>A Lightning channel as reported by <c>GET /listchannels</c> and <c>GET /getinfo</c>.</summary>
public class Channel
{
    public string State { get; set; } = string.Empty;
    public string? ChannelId { get; set; }
    public long BalanceSat { get; set; }
    public long InboundLiquiditySat { get; set; }
    public long CapacitySat { get; set; }
    public string? FundingTxId { get; set; }
}
