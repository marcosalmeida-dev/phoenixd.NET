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
              "channels": [],
              "chain": "testnet",
              "blockHeight": 42,
              "version": "0.8.0"
            }
            """;
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.GetNodeInfo();

        Assert.Equal("/getinfo", handler.LastPath);
        Assert.Equal("node123", result.NodeId);
        Assert.Equal("testnet", result.Chain);
        Assert.Equal(42, result.BlockHeight);
        Assert.Equal("0.8.0", result.Version);
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
    }

    [Fact]
    public async Task ListChannels_ParsesResponse()
    {
        const string json = """
            [ { "state": "Normal", "channelId": "channel123", "balanceSat": 500, "inboundLiquiditySat": 100, "capacitySat": 600, "fundingTxId": "tx" } ]
            """;
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.ListChannels();

        Assert.Equal("/listchannels", handler.LastPath);
        Assert.Single(result);
        Assert.Equal("channel123", result[0].ChannelId);
        Assert.Equal("Normal", result[0].State);
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
        var handler = new StubHttpMessageHandler("abc123txid", mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.CloseChannel("channel123", "bcrt1qaddress", 10);

        Assert.Equal("/closechannel", handler.LastPath);
        Assert.Contains("channelId=channel123", handler.LastRequestBody);
        Assert.Contains("feerateSatByte=10", handler.LastRequestBody);
        Assert.Equal("ok", result.Status);
        Assert.Equal("abc123txid", result.Message);
    }

    [Fact]
    public async Task BumpFee_PostsFeerateAndReturnsTxId()
    {
        var handler = new StubHttpMessageHandler("txid999", mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.BumpFee(15);

        Assert.Equal("/bumpfee", handler.LastPath);
        Assert.Contains("feerateSatByte=15", handler.LastRequestBody);
        Assert.Equal("txid999", result);
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
