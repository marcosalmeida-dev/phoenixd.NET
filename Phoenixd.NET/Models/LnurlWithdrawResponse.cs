namespace Phoenixd.NET.Models;

/// <summary>LNURL-withdraw result returned by <c>POST /lnurlwithdraw</c>.</summary>
public class LnurlWithdrawResponse
{
    public string Url { get; set; } = string.Empty;

    /// <summary>Minimum withdrawable amount in millisatoshis.</summary>
    public long MinWithdrawable { get; set; }

    /// <summary>Maximum withdrawable amount in millisatoshis.</summary>
    public long MaxWithdrawable { get; set; }

    public string Description { get; set; } = string.Empty;
    public string K1 { get; set; } = string.Empty;

    /// <summary>BOLT11 invoice submitted to the withdraw service.</summary>
    public string Invoice { get; set; } = string.Empty;
}
