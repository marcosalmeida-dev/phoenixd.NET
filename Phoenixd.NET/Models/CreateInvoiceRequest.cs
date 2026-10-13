namespace Phoenixd.NET.Models;

/// <summary>Creates a BOLT11 invoice; omit <see cref="AmountSat"/> for an amountless invoice.</summary>
public class CreateInvoiceRequest
{
    public long? AmountSat { get; set; }

    /// <summary>Invoice description, at most 128 characters. Supply this or DescriptionHash.</summary>
    public string? Description { get; set; }

    /// <summary>SHA-256 description hash, mutually exclusive with Description.</summary>
    public string? DescriptionHash { get; set; }

    public string? ExternalId { get; set; }
    public long? ExpirySeconds { get; set; }
    public string? WebhookUrl { get; set; }
}
