using System.Globalization;
using System.Text.Json;
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

    public async Task<List<Channel>> ListChannels(CancellationToken cancellationToken = default) =>
        (await GetNodeInfo(cancellationToken).ConfigureAwait(false)).Channels;

    public Task<List<JsonElement>> ListChannelStatesAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<List<JsonElement>>("/listchannels", nameof(ListChannelStatesAsync), cancellationToken);

    public Task<LiquidityFees> EstimateLiquidityFees(long amountSat, CancellationToken cancellationToken = default) =>
        GetJsonAsync<LiquidityFees>(
            $"/estimateliquidityfees?amountSat={amountSat.ToString(CultureInfo.InvariantCulture)}",
            nameof(EstimateLiquidityFees),
            cancellationToken);

    public async Task<CloseChannelResponse> CloseChannel(string channelId, string address, int feerateSatByte, CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("channelId", channelId),
            ("address", address),
            ("feerateSatByte", feerateSatByte.ToString(CultureInfo.InvariantCulture)));

        var body = await PostStringAsync("/closechannel", content, nameof(CloseChannel), cancellationToken)
            .ConfigureAwait(false);

        // Channel failures are also sent with status 200; only a transaction id indicates success.
        var succeeded = body.Length == 64 && body.All(Uri.IsHexDigit);
        return new CloseChannelResponse
        {
            Status = succeeded ? "ok" : "error",
            Message = string.IsNullOrWhiteSpace(body) ? "Empty response" : body
        };
    }

    public async Task<string> BumpFee(int feerateSatByte, CancellationToken cancellationToken = default)
    {
        var body = await PostStringAsync(
            "/bumpfee",
            Form(("feerateSatByte", feerateSatByte.ToString(CultureInfo.InvariantCulture))),
            nameof(BumpFee),
            cancellationToken).ConfigureAwait(false);

        return ValidateTransactionId(body, nameof(BumpFee));
    }
}
