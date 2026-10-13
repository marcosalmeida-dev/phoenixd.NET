namespace Phoenixd.NET.Models;

/// <summary>On-chain swap-in balances returned by <c>GET /getbalance</c> in phoenixd 0.9.x.</summary>
public class SwapInBalance
{
    /// <summary>Funds whose transactions have not yet been confirmed, in satoshis.</summary>
    public long UnconfirmedBalanceSat { get; set; }

    /// <summary>Confirmed funds waiting for the required swap-in confirmations, in satoshis.</summary>
    public long WeaklyConfirmedBalanceSat { get; set; }

    /// <summary>Funds meeting the swap-in confirmation requirement, in satoshis.</summary>
    public long DeeplyConfirmedBalanceSat { get; set; }
}
