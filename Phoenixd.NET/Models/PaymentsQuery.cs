namespace Phoenixd.NET.Models;

/// <summary>
/// Filtering and pagination options for listing payments
/// (<c>GET /payments/incoming</c> and <c>GET /payments/outgoing</c>).
/// Only non-null values are sent as query parameters.
/// </summary>
public class PaymentsQuery
{
    /// <summary>Only include payments created at/after this Unix timestamp (ms).</summary>
    public long? From { get; set; }

    /// <summary>Only include payments created at/before this Unix timestamp (ms).</summary>
    public long? To { get; set; }

    /// <summary>Maximum number of results to return.</summary>
    public int? Limit { get; set; }

    /// <summary>Number of results to skip (for paging).</summary>
    public int? Offset { get; set; }

    /// <summary>When true, include payments that are not yet paid/confirmed.</summary>
    public bool? All { get; set; }

    /// <summary>Filter by the caller-supplied external id (incoming payments only).</summary>
    public string? ExternalId { get; set; }
}
