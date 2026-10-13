namespace Phoenixd.NET.Models;

/// <summary>Result of <c>GET /getbalance</c>.</summary>
public class Balance
{
    /// <summary>Spendable balance in satoshis.</summary>
    public long BalanceSat { get; set; }

    /// <summary>
    /// Fee credit in satoshis. phoenixd accumulates small amounts as a credit until they are large
    /// enough to justify an on-chain channel operation; this credit is used towards future fees.
    /// </summary>
    public long FeeCreditSat { get; set; }

    /// <summary>
    /// On-chain funds waiting for automatic swap-in. Absent when the daemon's swap-in wallet
    /// is unavailable. These amounts are separate from the spendable Lightning balance.
    /// </summary>
    public SwapInBalance? SwapIn { get; set; }
}
