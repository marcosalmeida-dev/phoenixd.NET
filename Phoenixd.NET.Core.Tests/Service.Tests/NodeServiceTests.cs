using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Phoenixd.NET.Exceptions;
using Phoenixd.NET.Services;
using Phoenixd.NET.Tests.Helpers;

namespace Phoenixd.NET.Tests.Service.Tests;

public class NodeServiceTests
{
    private static NodeService CreateService(StubHttpMessageHandler handler) =>
        new(handler.CreateClient(), NullLogger<NodeService>.Instance);

    [Fact]
    public async Task GetNodeInfo_ParsesResponse()
    {
        const string json = """
            {
              "nodeId": "node123",
              "channels": [
                { "state": "Normal", "channelId": "channel123", "balanceSat": 500, "inboundLiquiditySat": 100, "capacitySat": 600, "fundingTxId": "tx" },
                { "state": "WaitForInit" }
              ],
              "chain": "testnet",
              "blockHeight": 42,
              "version": "0.9.2"
            }
            """;
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.GetNodeInfo();

        Assert.Equal("/getinfo", handler.LastPath);
        Assert.Equal("node123", result.NodeId);
        Assert.Equal("testnet", result.Chain);
        Assert.Equal(42, result.BlockHeight);
        Assert.Equal("0.9.2", result.Version);
        Assert.Equal(2, result.Channels.Count);
        Assert.Equal(500, result.Channels[0].BalanceSat);
        Assert.Equal("WaitForInit", result.Channels[1].State);
        Assert.Null(result.Channels[1].ChannelId);
    }

    [Fact]
    public async Task GetBalance_ParsesResponse()
    {
        var handler = new StubHttpMessageHandler("""{ "balanceSat": 1000, "feeCreditSat": 50 }""");
        var service = CreateService(handler);

        var result = await service.GetBalance();

        Assert.Equal("/getbalance", handler.LastPath);
        Assert.Equal(1000, result.BalanceSat);
        Assert.Equal(50, result.FeeCreditSat);
        Assert.Null(result.SwapIn);
    }

    [Fact]
    public async Task GetBalance_ParsesSwapInConfirmationBalances()
    {
        var handler = new StubHttpMessageHandler("""
            {
              "balanceSat": 1000,
              "feeCreditSat": 50,
              "swapIn": {
                "unconfirmedBalanceSat": 21000,
                "weaklyConfirmedBalanceSat": 42000,
                "deeplyConfirmedBalanceSat": 84000
              }
            }
            """);

        var result = await CreateService(handler).GetBalance();

        Assert.Equal(1000, result.BalanceSat);
        Assert.NotNull(result.SwapIn);
        Assert.Equal(21000, result.SwapIn.UnconfirmedBalanceSat);
        Assert.Equal(42000, result.SwapIn.WeaklyConfirmedBalanceSat);
        Assert.Equal(84000, result.SwapIn.DeeplyConfirmedBalanceSat);
    }

    [Fact]
    public async Task GetBalance_AcceptsUnavailableSwapInWallet()
    {
        var handler = new StubHttpMessageHandler("""{ "balanceSat": 1000, "feeCreditSat": 50, "swapIn": null }""");

        var result = await CreateService(handler).GetBalance();

        Assert.Null(result.SwapIn);
    }

    [Fact]
    public async Task ListChannels_GetsSummariesFromNodeInfo()
    {
        const string json = """
            { "nodeId": "node123", "chain": "testnet", "version": "0.9.2", "channels": [
                { "state": "Normal", "channelId": "channel123", "balanceSat": 500, "inboundLiquiditySat": 100, "capacitySat": 600, "fundingTxId": "tx" }
            ] }
            """;
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.ListChannels();

        Assert.Equal("/getinfo", handler.LastPath);
        Assert.Single(result);
        Assert.Equal("channel123", result[0].ChannelId);
        Assert.Equal("Normal", result[0].State);
        Assert.Equal(500, result[0].BalanceSat);
        Assert.Equal(100, result[0].InboundLiquiditySat);
        Assert.Equal(600, result[0].CapacitySat);
        Assert.Equal("tx", result[0].FundingTxId);
    }

    [Fact]
    public async Task ListChannelStates_PreservesPolymorphicStateAndCommitments()
    {
        // /listchannels uses lightning-kmp's full ChannelState serializers, unlike /getinfo.
        // The discriminator and nested commitments follow the official v1.13.2 serializers.
        const string json = """
            [
              {
                "type": "fr.acinq.lightning.channel.states.Normal",
                "commitments": {
                  "channelParams": { "channelId": "channel123" },
                  "active": [
                    {
                      "fundingTxIndex": 0,
                      "localFundingStatus": { "status": "confirmed", "txId": "tx123" },
                      "localCommit": { "spec": { "toLocal": 501234, "toRemote": 100000 } }
                    }
                  ]
                },
                "spliceStatus": { "status": "None" }
              },
              {
                "type": "fr.acinq.lightning.json.JsonSerializers.ChannelStateSurrogateSerializer",
                "forState": "fr.acinq.lightning.channel.states.WaitForInit"
              }
            ]
            """;
        var handler = new StubHttpMessageHandler(json);

        var states = await CreateService(handler).ListChannelStatesAsync();

        Assert.Equal("/listchannels", handler.LastPath);
        Assert.Equal(2, states.Count);
        Assert.Equal("fr.acinq.lightning.channel.states.Normal", states[0].GetProperty("type").GetString());
        var commitment = states[0].GetProperty("commitments").GetProperty("active")[0];
        Assert.Equal("tx123", commitment.GetProperty("localFundingStatus").GetProperty("txId").GetString());
        Assert.Equal(501234, commitment.GetProperty("localCommit").GetProperty("spec").GetProperty("toLocal").GetInt64());
        Assert.Equal("fr.acinq.lightning.channel.states.WaitForInit", states[1].GetProperty("forState").GetString());
    }

    [Fact]
    public async Task EstimateLiquidityFees_SendsAmountAndParsesResponse()
    {
        var handler = new StubHttpMessageHandler("""{ "miningFeeSat": 120, "serviceFeeSat": 300 }""");
        var service = CreateService(handler);

        var result = await service.EstimateLiquidityFees(100000);

        Assert.Equal("/estimateliquidityfees", handler.LastPath);
        Assert.Contains("amountSat=100000", handler.LastQuery);
        Assert.Equal(120, result.MiningFeeSat);
        Assert.Equal(300, result.ServiceFeeSat);
    }

    [Fact]
    public async Task CloseChannel_ReturnsOkWithTxId()
    {
        var txId = new string('a', 64);
        var handler = new StubHttpMessageHandler(txId, mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.CloseChannel("channel123", "bcrt1qaddress", 10);

        Assert.Equal("/closechannel", handler.LastPath);
        Assert.Contains("channelId=channel123", handler.LastRequestBody);
        Assert.Contains("feerateSatByte=10", handler.LastRequestBody);
        Assert.Equal("ok", result.Status);
        Assert.Equal(txId, result.Message);
    }

    [Theory]
    [InlineData("no channel available")]
    [InlineData("ChannelNotOpenedYet")]
    [InlineData("CannotCloseChannelWithPendingHtlcs")]
    public async Task CloseChannel_RecognizesFailureDespiteSuccessfulHttpStatus(string failure)
    {
        var handler = new StubHttpMessageHandler(failure, mediaType: "text/plain");

        var result = await CreateService(handler).CloseChannel("channel123", "bcrt1qaddress", 10);

        Assert.Equal("error", result.Status);
        Assert.Equal(failure, result.Message);
    }

    [Fact]
    public async Task CloseChannel_RecognizesEmptyResponse()
    {
        var handler = new StubHttpMessageHandler("", mediaType: "text/plain");

        var result = await CreateService(handler).CloseChannel("channel123", "bcrt1qaddress", 10);

        Assert.Equal("error", result.Status);
        Assert.Equal("Empty response", result.Message);
    }

    [Fact]
    public async Task BumpFee_PostsFeerateAndReturnsTxId()
    {
        var txId = new string('9', 64);
        var handler = new StubHttpMessageHandler(txId, mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.BumpFee(15);

        Assert.Equal("/bumpfee", handler.LastPath);
        Assert.Contains("feerateSatByte=15", handler.LastRequestBody);
        Assert.Equal(txId, result);
    }

    [Theory]
    [InlineData("no channel available")]
    [InlineData("either the tx is already confirmed or has a higher feerate")]
    public async Task BumpFee_RecognizesFailureDespiteSuccessfulHttpStatus(string failure)
    {
        var handler = new StubHttpMessageHandler(failure, mediaType: "text/plain");

        var exception = await Assert.ThrowsAsync<PhoenixdApiException>(() => CreateService(handler).BumpFee(15));

        Assert.Equal(failure, exception.ResponseBody);
    }

    [Fact]
    public async Task GetNodeInfo_OnErrorStatus_ThrowsPhoenixdApiException()
    {
        var handler = new StubHttpMessageHandler("unauthorized", HttpStatusCode.Unauthorized, "text/plain");
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<PhoenixdApiException>(() => service.GetNodeInfo());

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("unauthorized", ex.ResponseBody);
    }
}
