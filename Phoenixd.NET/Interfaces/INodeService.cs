using Phoenixd.NET.Models;

namespace Phoenixd.NET.Interfaces;

/// <summary>Node- and channel-level operations exposed by the phoenixd HTTP API.</summary>
public interface INodeService
{
    Task<NodeInfo> GetNodeInfo(CancellationToken cancellationToken = default);

    Task<Balance> GetBalance(CancellationToken cancellationToken = default);

    Task<List<Channel>> ListChannels(CancellationToken cancellationToken = default);

    Task<CloseChannelResponse> CloseChannel(string channelId, string address, int feerateSatByte, CancellationToken cancellationToken = default);

    /// <summary>Best-effort estimate of the liquidity fees to receive <paramref name="amountSat"/> satoshis.</summary>
    Task<LiquidityFees> EstimateLiquidityFees(long amountSat, CancellationToken cancellationToken = default);

    /// <summary>Bumps the fee of unconfirmed on-chain transactions (CPFP). Returns the resulting txid.</summary>
    Task<string> BumpFee(int feerateSatByte, CancellationToken cancellationToken = default);
}
