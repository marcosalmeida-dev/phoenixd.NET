using Phoenixd.NET.Core.Models;

namespace Phoenixd.NET.Core.Interfaces;

public interface INodeService
{
    Task<CloseChannelResponse> CloseChannel(string channelId, string address, int feerateSatByte);
    Task<Balance> GetBalance();
    Task<NodeInfo> GetNodeInfo();
    Task<List<Channel>> ListChannels();
}