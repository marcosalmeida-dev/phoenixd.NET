namespace Phoenixd.NET.Models;

public class NodeInfo
{
    public string NodeId { get; set; }
    public List<Channel> Channels { get; set; }
    public string Chain { get; set; }
    public string Version { get; set; }
}
