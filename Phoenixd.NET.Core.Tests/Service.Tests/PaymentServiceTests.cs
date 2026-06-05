using Microsoft.Extensions.Logging.Abstractions;
using Phoenixd.NET.Models;
using Phoenixd.NET.Services;
using Phoenixd.NET.Tests.Helpers;

namespace Phoenixd.NET.Tests.Service.Tests;

public class PaymentServiceTests
{
    private static PaymentService CreateService(StubHttpMessageHandler handler) =>
        new(handler.CreateClient(), NullLogger<PaymentService>.Instance);

    [Fact]
    public async Task ReceiveLightningPaymentAsync_PostsFieldsAndParsesInvoice()
    {
        const string json = """{ "amountSat": 1000, "paymentHash": "hash123", "serialized": "lnbc..." }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.ReceiveLightningPaymentAsync("Test payment", 1000, "conn-1");

        Assert.Equal("/createinvoice", handler.LastPath);
        Assert.Contains("amountSat=1000", handler.LastRequestBody);
        Assert.Contains("description=Test+payment", handler.LastRequestBody);
        Assert.Contains("externalId=conn-1", handler.LastRequestBody);
        Assert.Equal(1000, result.AmountSat);
        Assert.Equal("hash123", result.PaymentHash);
        Assert.Equal("lnbc...", result.Serialized);
    }

    [Fact]
    public async Task ReceiveLightningPaymentAsync_WithDescriptionHash_OmitsDescription()
    {
        var handler = new StubHttpMessageHandler("""{ "paymentHash": "h", "serialized": "lnbc" }""");
        var service = CreateService(handler);

        await service.ReceiveLightningPaymentAsync("ignored", 500, descriptionHash: "deadbeef");

        Assert.Contains("descriptionHash=deadbeef", handler.LastRequestBody);
        Assert.DoesNotContain("description=ignored", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendLightningInvoice_ParsesPayInvoiceResponse()
    {
        const string json = """
            { "recipientAmountSat": 1000, "routingFeeSat": 2, "paymentId": "id", "paymentHash": "hash123", "paymentPreimage": "preimg" }
            """;
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.SendLightningInvoice(1000, "lnbc1invoice");

        Assert.Equal("/payinvoice", handler.LastPath);
        Assert.Contains("invoice=lnbc1invoice", handler.LastRequestBody);
        Assert.Equal(1000, result.RecipientAmountSat);
        Assert.Equal("hash123", result.PaymentHash);
        Assert.Equal("id", result.PaymentId);
    }

    [Fact]
    public async Task PayOfferAsync_PostsOfferAndMessage()
    {
        const string json = """{ "recipientAmountSat": 100, "routingFeeSat": 0, "paymentId": "id", "paymentHash": "h", "paymentPreimage": "p" }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        await service.PayOfferAsync("lno1offer", 100, "thanks");

        Assert.Equal("/payoffer", handler.LastPath);
        Assert.Contains("offer=lno1offer", handler.LastRequestBody);
        Assert.Contains("amountSat=100", handler.LastRequestBody);
        Assert.Contains("message=thanks", handler.LastRequestBody);
    }

    [Fact]
    public async Task PayLnAddressAsync_PostsAddressAndAmount()
    {
        const string json = """{ "recipientAmountSat": 100, "routingFeeSat": 0, "paymentId": "id", "paymentHash": "h", "paymentPreimage": "p" }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        await service.PayLnAddressAsync("user@example.com", 100);

        Assert.Equal("/paylnaddress", handler.LastPath);
        Assert.Contains("address=user%40example.com", handler.LastRequestBody);
        Assert.Contains("amountSat=100", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendOnchainPayment_ReturnsTxId()
    {
        var handler = new StubHttpMessageHandler("onchaintxid", mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.SendOnchainPayment(1000, "bcrt1qaddr", 5);

        Assert.Equal("/sendtoaddress", handler.LastPath);
        Assert.Contains("feerateSatByte=5", handler.LastRequestBody);
        Assert.Equal("onchaintxid", result);
    }

    [Fact]
    public async Task CreateOfferAsync_ReturnsSerializedOffer()
    {
        var handler = new StubHttpMessageHandler("lno1qcp4256ypqpq", mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.CreateOfferAsync(description: "tips");

        Assert.Equal("/createoffer", handler.LastPath);
        Assert.Equal("lno1qcp4256ypqpq", result);
    }

    [Fact]
    public async Task GetLnAddressAsync_ReturnsAddress()
    {
        var handler = new StubHttpMessageHandler("user@phoenix.example", mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.GetLnAddressAsync();

        Assert.Equal("/getlnaddress", handler.LastPath);
        Assert.Equal("user@phoenix.example", result);
    }

    [Fact]
    public async Task DecodeInvoiceAsync_ReturnsJsonElement()
    {
        const string json = """{ "amount": 1000, "paymentHash": "abc", "description": "coffee" }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.DecodeInvoiceAsync("lnbc1invoice");

        Assert.Equal("/decodeinvoice", handler.LastPath);
        Assert.Equal("coffee", result.GetProperty("description").GetString());
    }

    [Fact]
    public async Task ListIncomingPayments_ByExternalId_SetsQuery()
    {
        var handler = new StubHttpMessageHandler("[]");
        var service = CreateService(handler);

        var result = await service.ListIncomingPayments("conn-1");

        Assert.Equal("/payments/incoming", handler.LastPath);
        Assert.Contains("externalId=conn-1", handler.LastQuery);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListIncomingPayments_WithQuery_BuildsPagination()
    {
        const string json = """
            [ { "paymentHash": "hash123", "receivedSat": 1000, "fees": 1500, "isPaid": true, "createdAt": 1 } ]
            """;
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var query = new PaymentsQuery { Limit = 10, Offset = 20, All = true, From = 100, To = 200 };
        var result = await service.ListIncomingPayments(query);

        Assert.Contains("limit=10", handler.LastQuery);
        Assert.Contains("offset=20", handler.LastQuery);
        Assert.Contains("all=true", handler.LastQuery);
        Assert.Contains("from=100", handler.LastQuery);
        Assert.Contains("to=200", handler.LastQuery);
        Assert.Single(result);
        Assert.Equal(1500, result[0].Fees); // millisatoshis
    }

    [Fact]
    public async Task GetIncomingPayment_UsesPaymentHashInPath()
    {
        const string json = """{ "paymentHash": "hash123", "receivedSat": 1000, "isPaid": true, "createdAt": 1 }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.GetIncomingPayment("hash123");

        Assert.Equal("/payments/incoming/hash123", handler.LastPath);
        Assert.Equal("hash123", result.PaymentHash);
    }

    [Fact]
    public async Task GetOutgoingPaymentByHash_UsesCorrectPath()
    {
        const string json = """{ "paymentId": "uuid-1", "paymentHash": "hash123", "sent": 1000, "fees": 0, "isPaid": true, "createdAt": 1 }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.GetOutgoingPaymentByHash("hash123");

        Assert.Equal("/payments/outgoingbyhash/hash123", handler.LastPath);
        Assert.Equal("uuid-1", result.PaymentId);
        Assert.Equal("hash123", result.PaymentHash);
    }

    [Fact]
    public async Task ListOutgoingPayments_ParsesList()
    {
        const string json = """
            [ { "paymentId": "uuid-1", "sent": 5000, "fees": 100, "isPaid": true, "createdAt": 1, "subType": "lightning" } ]
            """;
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.ListOutgoingPayments(new PaymentsQuery { Limit = 5 });

        Assert.Equal("/payments/outgoing", handler.LastPath);
        Assert.Contains("limit=5", handler.LastQuery);
        Assert.Single(result);
        Assert.Equal("lightning", result[0].SubType);
    }
}
