using Microsoft.Extensions.Logging;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Services;

internal sealed class NodeService : PhoenixdServiceBase, INodeService
{
    public NodeService(HttpClient httpClient, ILogger<NodeService> logger)
        : base(httpClient, logger)
    {
    }

    public Task<NodeInfo> GetNodeInfo(CancellationToken cancellationToken = default) =>
        GetJsonAsync<NodeInfo>("/getinfo", nameof(GetNodeInfo), cancellationToken);

    public Task<Balance> GetBalance(CancellationToken cancellationToken = default) =>
        GetJsonAsync<Balance>("/getbalance", nameof(GetBalance), cancellationToken);

    public Task<List<Channel>> ListChannels(CancellationToken cancellationToken = default) =>
        GetJsonAsync<List<Channel>>("/listchannels", nameof(ListChannels), cancellationToken);

    public Task<LiquidityFees> EstimateLiquidityFees(long amountSat, CancellationToken cancellationToken = default) =>
        GetJsonAsync<LiquidityFees>(
            $"/estimateliquidityfees?amountSat={amountSat}",
            nameof(EstimateLiquidityFees),
            cancellationToken);

    public async Task<CloseChannelResponse> CloseChannel(string channelId, string address, int feerateSatByte, CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("channelId", channelId),
            ("address", address),
            ("feerateSatByte", feerateSatByte.ToString()));

        var body = await PostStringAsync("/closechannel", content, nameof(CloseChannel), cancellationToken)
            .ConfigureAwait(false);

        // phoenixd replies with the closing txid (or "ok"); anything else is treated as an error.
        return string.IsNullOrWhiteSpace(body)
            ? new CloseChannelResponse { Status = "error", Message = "Empty response" }
            : new CloseChannelResponse { Status = "ok", Message = body };
    }

    public Task<string> BumpFee(int feerateSatByte, CancellationToken cancellationToken = default) =>
        PostStringAsync(
            "/bumpfee",
            Form(("feerateSatByte", feerateSatByte.ToString())),
            nameof(BumpFee),
            cancellationToken);
}
