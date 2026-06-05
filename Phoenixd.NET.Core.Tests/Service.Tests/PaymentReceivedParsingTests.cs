using System.Text.Json;
using Phoenixd.NET.Json;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Tests.Service.Tests;

public class PaymentReceivedParsingTests
{
    [Fact]
    public void Deserializes_PaymentReceived_WebsocketEvent()
    {
        // Shape of the phoenixd "payment_received" websocket/webhook event.
        const string json = """
            {
              "type": "payment_received",
              "amountSat": 1234,
              "paymentHash": "abc123",
              "externalId": "conn-1",
              "payerNote": "thanks!",
              "payerKey": "02deadbeef",
              "timestamp": 1716200000000
            }
            """;

        var evt = JsonSerializer.Deserialize<PaymentReceived>(json, PhoenixdJson.Default);

        Assert.NotNull(evt);
        Assert.Equal("payment_received", evt!.Type);
        Assert.Equal(1234, evt.AmountSat);
        Assert.Equal("abc123", evt.PaymentHash);
        Assert.Equal("conn-1", evt.ExternalId);
        Assert.Equal("thanks!", evt.PayerNote);
        Assert.Equal("02deadbeef", evt.PayerKey);
        Assert.Equal(1716200000000, evt.Timestamp);
    }

    [Fact]
    public void Deserializes_PaymentReceived_WithOptionalFieldsAbsent()
    {
        const string json = """{ "type": "payment_received", "amountSat": 10, "paymentHash": "h" }""";

        var evt = JsonSerializer.Deserialize<PaymentReceived>(json, PhoenixdJson.Default);

        Assert.NotNull(evt);
        Assert.Equal(10, evt!.AmountSat);
        Assert.Null(evt.PayerNote);
        Assert.Null(evt.PayerKey);
        Assert.Null(evt.ExternalId);
    }
}
