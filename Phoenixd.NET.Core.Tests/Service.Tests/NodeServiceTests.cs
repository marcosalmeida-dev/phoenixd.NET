using Microsoft.Extensions.Logging;
using Moq;
using Phoenixd.NET.Core.Models;
using Phoenixd.NET.Services;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;

namespace Phoenixd.NET.Tests.Services;

public class NodeServiceTests
{
    private readonly Mock<ILogger<NodeService>> _mockLogger;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly NodeService _nodeService;

    public NodeServiceTests()
    {
        _mockLogger = new Mock<ILogger<NodeService>>();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _nodeService = new NodeService(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task GetNodeInfo_ShouldReturnNodeInfo_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedNodeInfo = new NodeInfo { NodeId = "node123", Chain = "testchain", Version = "1.0.0" };
        _mockHttpMessageHandler
            .When("/getinfo")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedNodeInfo));

        // Act
        var result = await _nodeService.GetNodeInfo();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedNodeInfo.NodeId, result.NodeId);
        Assert.Equal(expectedNodeInfo.Chain, result.Chain);
        Assert.Equal(expectedNodeInfo.Version, result.Version);
    }

    [Fact]
    public async Task GetBalance_ShouldReturnBalance_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedBalance = new Balance { BalanceSat = 1000, FeeCreditSat = 50 };
        _mockHttpMessageHandler
            .When("/getbalance")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedBalance));

        // Act
        var result = await _nodeService.GetBalance();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedBalance.BalanceSat, result.BalanceSat);
        Assert.Equal(expectedBalance.FeeCreditSat, result.FeeCreditSat);
    }

    [Fact]
    public async Task ListChannels_ShouldReturnChannels_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedChannels = new List<Channel>
        {
            new Channel { ChannelId = "channel123", State = "open", BalanceSat = 500 }
        };
        _mockHttpMessageHandler
            .When("/listchannels")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedChannels));

        // Act
        var result = await _nodeService.ListChannels();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(expectedChannels[0].ChannelId, result[0].ChannelId);
        Assert.Equal(expectedChannels[0].State, result[0].State);
    }

    [Fact]
    public async Task CloseChannel_ShouldReturnCloseChannelResponse_WhenApiCallIsSuccessful()
    {
        // Arrange
        var channelId = "channel123";
        var address = "address123";
        var feerateSatByte = 10;

        _mockHttpMessageHandler
            .When(HttpMethod.Post, "/closechannel")
            .Respond(HttpStatusCode.OK, new StringContent("ok"));

        // Act
        var result = await _nodeService.CloseChannel(channelId, address, feerateSatByte);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ok", result.Status);
    }

    [Fact]
    public async Task CloseChannel_ShouldReturnErrorResponse_WhenUnexpectedResponseReceived()
    {
        // Arrange
        var channelId = "channel123";
        var address = "address123";
        var feerateSatByte = 10;

        _mockHttpMessageHandler
            .When(HttpMethod.Post, "/closechannel")
            .Respond(HttpStatusCode.OK, new StringContent("unexpected response"));

        // Act
        var result = await _nodeService.CloseChannel(channelId, address, feerateSatByte);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("error", result.Status);
        Assert.Equal("Unexpected response", result.Message);
    }
}
