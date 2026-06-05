namespace Phoenixd.NET.Models;

/// <summary>Result of <c>GET /getinfo</c>.</summary>
public class NodeInfo
{
    public string NodeId { get; set; } = string.Empty;
    public List<Channel> Channels { get; set; } = new();
    public string Chain { get; set; } = string.Empty;

    /// <summary>Current block height as seen by the node. Added in newer phoenixd builds; may be null.</summary>
    public int? BlockHeight { get; set; }

    public string Version { get; set; } = string.Empty;
}
