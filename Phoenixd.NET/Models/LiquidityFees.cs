namespace Phoenixd.NET.Models;

/// <summary>
/// Best-effort liquidity fee estimate returned by <c>GET /estimateliquidityfees</c>. These are the
/// fees that would be charged if inbound liquidity had to be purchased to receive a given amount.
/// </summary>
public class LiquidityFees
{
    /// <summary>Estimated on-chain mining fee in satoshis.</summary>
    public long MiningFeeSat { get; set; }

    /// <summary>Estimated ACINQ service fee in satoshis.</summary>
    public long ServiceFeeSat { get; set; }
}
