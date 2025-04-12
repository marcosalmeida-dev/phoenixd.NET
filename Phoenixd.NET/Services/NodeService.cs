using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Services;

internal class NodeService : INodeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NodeService> _logger;

    public NodeService(HttpClient httpClient, ILogger<NodeService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    public async Task<NodeInfo> GetNodeInfo()
    {
        try
        {
            var response = await _httpClient.GetAsync("/getinfo");
            response.EnsureSuccessStatusCode();
            var nodeInfo = await response.Content.ReadFromJsonAsync<NodeInfo>();
            if (nodeInfo == null)
            {
                throw new InvalidOperationException("Response content is null.");
            }
            return nodeInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node info");
            throw;
        }
    }

    public async Task<Balance> GetBalance()
    {
        try
        {
            var response = await _httpClient.GetAsync("/getbalance");
            response.EnsureSuccessStatusCode();
            var balance = await response.Content.ReadFromJsonAsync<Balance>();
            if (balance == null)
            {
                throw new InvalidOperationException("Response content is null.");
            }
            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance");
            throw;
        }
    }

    public async Task<List<Channel>> ListChannels()
    {
        try
        {
            var response = await _httpClient.GetAsync("/listchannels");
            response.EnsureSuccessStatusCode();
            var channels = await response.Content.ReadFromJsonAsync<List<Channel>>();
            if (channels == null)
            {
                throw new InvalidOperationException("Response content is null.");
            }
            return channels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing channels");
            throw;
        }
    }

    public async Task<CloseChannelResponse> CloseChannel(string channelId, string address, int feerateSatByte)
    {
        var data = new Dictionary<string, string>
        {
            { "channelId", channelId },
            { "address", address },
            { "feerateSatByte", feerateSatByte.ToString() }
        };

        try
        {
            var response = await _httpClient.PostAsync("/closechannel", new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            if (content == "ok")
            {
                return new CloseChannelResponse { Status = "ok" };
            }
            else
            {
                _logger.LogError("Unexpected response: {0}", content);
                return new CloseChannelResponse { Status = "error", Message = "Unexpected response" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing channel");
            throw;
        }
    }
}
