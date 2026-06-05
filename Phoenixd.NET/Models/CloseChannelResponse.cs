namespace Phoenixd.NET.Models;

/// <summary>Outcome of <c>POST /closechannel</c>, normalised into a status + optional message.</summary>
public class CloseChannelResponse
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}
