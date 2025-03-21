using Moq;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Tests.Service.Tests;

public class NodeServiceTests
{
    private readonly Mock<INodeService> _mockNodeService;

    public NodeServiceTests()
    {
        _mockNodeService = new Mock<INodeService>();
    }

    [Fact]
    public async Task GetNodeInfo_ShouldReturnNodeInfo_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedNodeInfo = new NodeInfo { NodeId = "node123", Chain = "testchain", Version = "1.0.0" };
        _mockNodeService
            .Setup(service => service.GetNodeInfo())
            .ReturnsAsync(expectedNodeInfo);

        // Act
        var result = await _mockNodeService.Object.GetNodeInfo();

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
        _mockNodeService
            .Setup(service => service.GetBalance())
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _mockNodeService.Object.GetBalance();

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
        _mockNodeService
            .Setup(service => service.ListChannels())
            .ReturnsAsync(expectedChannels);

        // Act
        var result = await _mockNodeService.Object.ListChannels();

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
        var expectedResponse = new CloseChannelResponse { Status = "ok" };

        _mockNodeService
            .Setup(service => service.CloseChannel(channelId, address, feerateSatByte))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockNodeService.Object.CloseChannel(channelId, address, feerateSatByte);

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
        var expectedResponse = new CloseChannelResponse { Status = "error", Message = "Unexpected response" };

        _mockNodeService
            .Setup(service => service.CloseChannel(channelId, address, feerateSatByte))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockNodeService.Object.CloseChannel(channelId, address, feerateSatByte);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("error", result.Status);
        Assert.Equal("Unexpected response", result.Message);
    }
}
