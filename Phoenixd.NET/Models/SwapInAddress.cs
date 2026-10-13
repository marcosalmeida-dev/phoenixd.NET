namespace Phoenixd.NET.Models;

/// <summary>The on-chain deposit address returned by <c>GET /getswapinaddress</c>.</summary>
public class SwapInAddress
{
    public string Address { get; set; } = string.Empty;

    /// <summary>Derivation index of the current swap-in address.</summary>
    public int Index { get; set; }
}
