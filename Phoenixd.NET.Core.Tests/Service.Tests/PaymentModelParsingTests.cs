using System.Text.Json;
using Phoenixd.NET.Json;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Tests.Service.Tests;

public class PaymentModelParsingTests
{
    [Theory]
    [InlineData("auto_liquidity")]
    [InlineData("manual_liquidity")]
    [InlineData("splice_out")]
    [InlineData("splice_cpfp")]
    [InlineData("channel_close")]
    public void Deserializes_OnchainOutgoingPayment_WithItsUuid(string subType)
    {
        const string paymentId = "19d4c072-83fb-4128-9fe2-884bd245e7ab";
        var json = $$"""
            {
              "type": "outgoing_payment",
              "subType": "{{subType}}",
              "paymentId": "{{paymentId}}",
              "txId": "a0123",
              "isPaid": true,
              "sent": 50001,
              "fees": 1001,
              "completedAt": 1730000001000,
              "createdAt": 1730000000000
            }
            """;

        var result = JsonSerializer.Deserialize<PaymentInfoOutgoing>(json, PhoenixdJson.Default)!;

        Assert.Equal("outgoing_payment", result.Type);
        Assert.Equal(subType, result.SubType);
        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal("a0123", result.TxId);
        Assert.Null(result.PaymentHash);
        Assert.Null(result.Preimage);
        Assert.Null(result.Invoice);
        Assert.True(result.IsPaid);
        Assert.Equal(50001, result.Sent);
        Assert.Equal(1001, result.Fees);
        Assert.Equal(1730000001000, result.CompletedAt);
        Assert.Equal(1730000000000, result.CreatedAt);
    }

    [Fact]
    public void Deserializes_Bolt12IncomingPayment_WithMilllisatoshiFeesAndPayerMetadata()
    {
        const string json = """
            {
              "type": "incoming_payment",
              "subType": "lightning",
              "paymentHash": "payment-hash",
              "preimage": "payment-preimage",
              "externalId": "order-123",
              "isPaid": true,
              "isExpired": false,
              "receivedSat": 2123456,
              "fees": 500123,
              "payerNote": "invoice 123",
              "payerKey": "02abcdef",
              "completedAt": 1730000001000,
              "createdAt": 1730000000000
            }
            """;

        var result = JsonSerializer.Deserialize<PaymentInfo>(json, PhoenixdJson.Default)!;

        Assert.Equal("incoming_payment", result.Type);
        Assert.Equal("lightning", result.SubType);
        Assert.Equal("payment-hash", result.PaymentHash);
        Assert.Equal("payment-preimage", result.Preimage);
        Assert.Equal("order-123", result.ExternalId);
        Assert.True(result.IsPaid);
        Assert.False(result.IsExpired);
        Assert.Equal(2123456, result.ReceivedSat);
        Assert.Equal(500123, result.Fees);
        Assert.Equal("invoice 123", result.PayerNote);
        Assert.Equal("02abcdef", result.PayerKey);
        Assert.Equal(1730000001000, result.CompletedAt);
        Assert.Equal(1730000000000, result.CreatedAt);
        Assert.Null(result.RequestedSat);
        Assert.Null(result.Description);
        Assert.Null(result.Invoice);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public void Deserializes_AmountlessInvoice_WithoutAmountProperty()
    {
        var result = JsonSerializer.Deserialize<Invoice>(
            """{ "paymentHash": "payment-hash", "serialized": "lnbc1invoice" }""", PhoenixdJson.Default)!;

        Assert.Null(result.AmountSat);
        Assert.Equal("payment-hash", result.PaymentHash);
        Assert.Equal("lnbc1invoice", result.Serialized);
    }

    [Fact]
    public void Deserializes_LnurlWithdrawal_PreservingMillisatoshiLimits()
    {
        const string json = """
            {
              "url": "https://example.com/withdraw",
              "minWithdrawable": 1234,
              "maxWithdrawable": 987654321,
              "description": "withdraw rewards",
              "k1": "withdraw-challenge",
              "invoice": "lnbc1invoice"
            }
            """;

        var result = JsonSerializer.Deserialize<LnurlWithdrawResponse>(json, PhoenixdJson.Default)!;

        Assert.Equal("https://example.com/withdraw", result.Url);
        Assert.Equal(1234, result.MinWithdrawable);
        Assert.Equal(987654321, result.MaxWithdrawable);
        Assert.Equal("withdraw rewards", result.Description);
        Assert.Equal("withdraw-challenge", result.K1);
        Assert.Equal("lnbc1invoice", result.Invoice);
    }
}
