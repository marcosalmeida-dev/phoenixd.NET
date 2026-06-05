using System.Text.Json.Serialization;

namespace Phoenixd.NET.Models;

/// <summary>
/// The <c>payment_received</c> event pushed over the phoenixd websocket and delivered to webhooks.
/// </summary>
public class PaymentReceived
{
    /// <summary>Event discriminator; always <c>"payment_received"</c> for this type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("amountSat")]
    public long AmountSat { get; set; }

    [JsonPropertyName("paymentHash")]
    public string? PaymentHash { get; set; }

    /// <summary>Caller-supplied identifier attached to the invoice when it was created.</summary>
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }

    /// <summary>Optional note left by the payer (BOLT12).</summary>
    [JsonPropertyName("payerNote")]
    public string? PayerNote { get; set; }

    /// <summary>Optional public key of the payer (BOLT12).</summary>
    [JsonPropertyName("payerKey")]
    public string? PayerKey { get; set; }
}
