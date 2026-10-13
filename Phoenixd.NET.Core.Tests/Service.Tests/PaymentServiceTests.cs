using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Phoenixd.NET.Exceptions;
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
        var txId = new string('a', 64);
        var handler = new StubHttpMessageHandler(txId, mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.SendOnchainPayment(1000, "bcrt1qaddr", 5);

        Assert.Equal("/sendtoaddress", handler.LastPath);
        Assert.Contains("feerateSatByte=5", handler.LastRequestBody);
        Assert.Equal(txId, result);
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

    [Fact]
    public async Task CreateInvoiceAsync_AmountlessInvoiceOmitsAmountAndSendsMetadata()
    {
        var handler = new StubHttpMessageHandler("""{ "paymentHash": "hash", "serialized": "lnbc" }""");
        var service = CreateService(handler);

        var result = await service.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            Description = "any amount",
            ExternalId = "invoice 1",
            ExpirySeconds = 3600,
            WebhookUrl = "https://example.org/paid?x=1"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/createinvoice", handler.LastPath);
        Assert.DoesNotContain("amountSat", handler.LastRequestBody);
        Assert.Contains("externalId=invoice+1", handler.LastRequestBody);
        Assert.Contains("expirySeconds=3600", handler.LastRequestBody);
        Assert.Contains("webhookUrl=https%3A%2F%2Fexample.org%2Fpaid%3Fx%3D1", handler.LastRequestBody);
        Assert.Null(result.AmountSat);
    }

    [Fact]
    public async Task CreateInvoiceAsync_DescriptionHashUsesOnlyHash()
    {
        var handler = new StubHttpMessageHandler("""{ "amountSat": 42, "paymentHash": "hash", "serialized": "lnbc" }""");
        var service = CreateService(handler);

        await service.CreateInvoiceAsync(new CreateInvoiceRequest { AmountSat = 42, DescriptionHash = new string('a', 64) });

        Assert.Contains("amountSat=42", handler.LastRequestBody);
        Assert.Contains("descriptionHash=", handler.LastRequestBody);
        Assert.DoesNotContain("description=", handler.LastRequestBody);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("description", "hash")]
    public async Task CreateInvoiceAsync_RejectsInvalidDescriptionCombination(string? description, string? hash)
    {
        var handler = new StubHttpMessageHandler("{}");
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            Description = description, DescriptionHash = hash
        }));
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task CreateInvoiceAsync_RejectsDescriptionOver128Characters()
    {
        var handler = new StubHttpMessageHandler("{}");
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateInvoiceAsync(new CreateInvoiceRequest { Description = new string('a', 129) }));
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task GetSwapInAddressAsync_ParsesAddressAndDerivationIndex()
    {
        var handler = new StubHttpMessageHandler("""{ "address": "bc1pswapin", "index": 7 }""");
        var service = CreateService(handler);

        var result = await service.GetSwapInAddressAsync();

        Assert.Equal("/getswapinaddress", handler.LastPath);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("bc1pswapin", result.Address);
        Assert.Equal(7, result.Index);
    }

    private const string SuccessfulPayment = """{ "type": "payment_sent", "recipientAmountSat": 1000, "routingFeeSat": 2, "paymentId": "id", "paymentHash": "hash", "paymentPreimage": "preimage" }""";

    private static Task<PayInvoiceResponse> Pay(PaymentService service, int endpoint, long? amountSat, bool sendAll) => endpoint switch
    {
        0 => service.SendLightningInvoice(new PayInvoiceRequest { Invoice = "lnbc", AmountSat = amountSat, SendAll = sendAll }),
        1 => service.PayOfferAsync(new PayOfferRequest { Offer = "lno", AmountSat = amountSat, SendAll = sendAll, Message = "thanks" }),
        2 => service.PayLnAddressAsync(new PayLnAddressRequest { Address = "alice@example.org", AmountSat = amountSat, SendAll = sendAll, Message = "thanks" }),
        3 => service.LnurlPayAsync(new LnurlPayRequest { Lnurl = "lnurl", AmountSat = amountSat, SendAll = sendAll, Message = "thanks" }),
        _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
    };

    [Theory]
    [InlineData(0, "/payinvoice", "invoice=lnbc")]
    [InlineData(1, "/payoffer", "offer=lno")]
    [InlineData(2, "/paylnaddress", "address=alice%40example.org")]
    [InlineData(3, "/lnurlpay", "lnurl=lnurl")]
    public async Task PayRequest_SendAllPostsOnlySendAllAmountChoice(int endpoint, string path, string destination)
    {
        var handler = new StubHttpMessageHandler(SuccessfulPayment);
        var service = CreateService(handler);

        var result = await Pay(service, endpoint, null, true);

        Assert.Equal(path, handler.LastPath);
        Assert.Contains(destination, handler.LastRequestBody);
        Assert.Contains("sendAll=true", handler.LastRequestBody);
        Assert.DoesNotContain("amountSat", handler.LastRequestBody);
        Assert.Equal(1000, result.RecipientAmountSat);
        if (endpoint != 0) Assert.Contains("message=thanks", handler.LastRequestBody);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PayRequest_SendAllFalseIsAbsent(int endpoint)
    {
        var handler = new StubHttpMessageHandler(SuccessfulPayment);
        var service = CreateService(handler);

        await Pay(service, endpoint, 500, false);

        Assert.Contains("amountSat=500", handler.LastRequestBody);
        // Upstream selects sendAll based on parameter presence, including sendAll=false.
        Assert.DoesNotContain("sendAll", handler.LastRequestBody);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PayRequest_RejectsAmountTogetherWithSendAllBeforeRequest(int endpoint)
    {
        var handler = new StubHttpMessageHandler(SuccessfulPayment);
        var service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => Pay(service, endpoint, 0, true));
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SendLightningInvoice_LegacyZeroUsesInvoiceAmount()
    {
        var handler = new StubHttpMessageHandler(SuccessfulPayment);
        var service = CreateService(handler);

        await service.SendLightningInvoice(0, "lnbc");

        Assert.DoesNotContain("amountSat", handler.LastRequestBody);
        Assert.DoesNotContain("sendAll", handler.LastRequestBody);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PaymentFailedHttp200_ThrowsTypedFailure(int endpoint)
    {
        const string json = """{ "type": "payment_failed", "paymentHash": "hash", "offerId": "offer", "reason": "insufficient balance" }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var error = await Assert.ThrowsAsync<PhoenixdPaymentException>(() => Pay(service, endpoint, 100, false));

        Assert.Equal("insufficient balance", error.Reason);
        Assert.Equal("hash", error.Failure.PaymentHash);
        Assert.Equal("offer", error.Failure.OfferId);
        Assert.Equal(200, error.StatusCode);
        Assert.Equal(json, error.ResponseBody);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"type\":42}")]
    [InlineData("{\"type\":\"other\"}")]
    [InlineData("{\"type\":\"payment_sent\",\"recipientAmountSat\":1,\"routingFeeSat\":0}")]
    public async Task IncompletePaymentResponse_ThrowsInsteadOfReportingSuccess(string json)
    {
        var service = CreateService(new StubHttpMessageHandler(json));

        var error = await Assert.ThrowsAsync<PhoenixdApiException>(() => service.SendLightningInvoice(1, "lnbc"));

        Assert.Equal(json, error.ResponseBody);
    }

    [Fact]
    public async Task AuthenticateLnurlAsync_AcceptsPlainTextSuccess()
    {
        var handler = new StubHttpMessageHandler("authentication success", mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.AuthenticateLnurlAsync("lnurl-auth");

        Assert.Equal("/lnurlauth", handler.LastPath);
        Assert.Equal("lnurl=lnurl-auth", handler.LastRequestBody);
        Assert.Equal("authentication success", result);
        var legacyResult = await service.LnurlAuthAsync("lnurl-auth");
        Assert.Equal(JsonValueKind.String, legacyResult.ValueKind);
        Assert.Equal("authentication success", legacyResult.GetString());
    }

    [Fact]
    public async Task WithdrawLnurlAsync_ParsesActualWithdrawalContractInMillisatoshis()
    {
        const string json = """{ "url": "https://example.org/withdraw", "minWithdrawable": 1000, "maxWithdrawable": 2100000, "description": "withdraw", "k1": "challenge", "invoice": "lnbc1" }""";
        var handler = new StubHttpMessageHandler(json);
        var service = CreateService(handler);

        var result = await service.WithdrawLnurlAsync("lnurl-withdraw");

        Assert.Equal("/lnurlwithdraw", handler.LastPath);
        Assert.Equal(1000, result.MinWithdrawable);
        Assert.Equal(2100000, result.MaxWithdrawable);
        Assert.Equal("challenge", result.K1);
        Assert.Equal("lnbc1", result.Invoice);
    }

    [Theory]
    [InlineData("no channel available")]
    [InlineData("ChannelFundingResponse.Failure")]
    [InlineData("not a txid")]
    public async Task SendOnchainPayment_Http200FailureTextIsAnError(string body)
    {
        var service = CreateService(new StubHttpMessageHandler(body, mediaType: "text/plain"));

        var error = await Assert.ThrowsAsync<PhoenixdApiException>(() => service.SendOnchainPayment(1000, "bc1qaddress", 5));

        Assert.Equal(body, error.ResponseBody);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task FindPayments_MissingPaymentsReturnNull(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler("Not found", statusCode, "text/plain");
        var service = CreateService(handler);

        Assert.Null(await service.FindIncomingPaymentAsync("hash"));
        Assert.Equal("/payments/incoming/hash", handler.LastPath);
        Assert.Null(await service.FindOutgoingPaymentAsync("uuid"));
        Assert.Equal("/payments/outgoing/uuid", handler.LastPath);
        Assert.Null(await service.FindOutgoingPaymentByHashAsync("hash"));
        Assert.Equal("/payments/outgoingbyhash/hash", handler.LastPath);
    }

    [Fact]
    public async Task FindPayments_AuthenticationFailuresStillThrow()
    {
        var service = CreateService(new StubHttpMessageHandler("Invalid authentication", HttpStatusCode.Unauthorized, "text/plain"));

        var error = await Assert.ThrowsAsync<PhoenixdApiException>(() => service.FindIncomingPaymentAsync("hash"));

        Assert.Equal(401, error.StatusCode);
    }

    [Fact]
    public async Task FindPayments_CancellationStillPropagates()
    {
        var handler = new StubHttpMessageHandler("{}");
        var service = CreateService(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.FindIncomingPaymentAsync("hash", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.FindOutgoingPaymentAsync(Guid.NewGuid(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.FindOutgoingPaymentByHashAsync("hash", cancellation.Token));
    }

    [Fact]
    public async Task FindPayments_ExistingPaymentsDeserialize()
    {
        const string incoming = """{ "paymentHash": "hash", "receivedSat": 1000, "isPaid": true, "createdAt": 1 }""";
        var incomingService = CreateService(new StubHttpMessageHandler(incoming));

        Assert.Equal("hash", (await incomingService.FindIncomingPaymentAsync("hash"))!.PaymentHash);

        const string outgoing = """{ "paymentId": "6d77185b-0a8c-43c2-963f-7c17c3f41670", "paymentHash": "hash", "subType": "lightning", "sent": 1000, "fees": 500, "isPaid": true, "createdAt": 1 }""";
        var handler = new StubHttpMessageHandler(outgoing);
        var outgoingService = CreateService(handler);
        var payment = await outgoingService.FindOutgoingPaymentAsync(Guid.Parse("6d77185b-0a8c-43c2-963f-7c17c3f41670"));

        Assert.Equal("/payments/outgoing/6d77185b-0a8c-43c2-963f-7c17c3f41670", handler.LastPath);
        Assert.Equal(500, payment!.Fees);
        Assert.Equal("hash", (await outgoingService.FindOutgoingPaymentByHashAsync("hash"))!.PaymentHash);
    }

    [Fact]
    public async Task GetOutgoingPayment_GuidUsesUuidEndpointNotHashEndpoint()
    {
        var id = Guid.Parse("6d77185b-0a8c-43c2-963f-7c17c3f41670");
        var handler = new StubHttpMessageHandler($$"""{ "paymentId": "{{id:D}}", "subType": "splice_out", "txId": "txid", "sent": 100, "fees": 1000, "isPaid": true, "createdAt": 1 }""");
        var service = CreateService(handler);

        var result = await service.GetOutgoingPayment(id);

        Assert.Equal($"/payments/outgoing/{id:D}", handler.LastPath);
        Assert.Equal("splice_out", result.SubType);
        Assert.Equal("txid", result.TxId);
    }

    [Fact]
    public async Task ExportPaymentsAsync_PostsRangeInQueryAndReturnsDaemonExportPath()
    {
        const string text = "payment history has been exported to /data/exports/export-123.csv";
        var handler = new StubHttpMessageHandler(text, mediaType: "text/plain");
        var service = CreateService(handler);

        var result = await service.ExportPaymentsAsync(100, 200);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/export", handler.LastPath);
        Assert.Equal("?from=100&to=200", handler.LastQuery);
        Assert.Equal(string.Empty, handler.LastRequestBody);
        Assert.Equal(text, result);
    }

    [Fact]
    public async Task PaymentQuery_PreservesEscapedExternalIdAndDoesNotSendItForOutgoing()
    {
        var handler = new StubHttpMessageHandler("[]");
        var service = CreateService(handler);
        var query = new PaymentsQuery { ExternalId = "client & +/", All = false };

        await service.ListIncomingPayments(query);
        Assert.Contains("externalId=client%20%26%20%2B%2F", handler.LastQuery);
        Assert.Contains("all=false", handler.LastQuery);

        await service.ListOutgoingPayments(query);
        Assert.DoesNotContain("externalId", handler.LastQuery);
    }

    [Fact]
    public async Task NumericRequestFields_UseInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        var custom = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        custom.NumberFormat.NegativeSign = "~";
        CultureInfo.CurrentCulture = custom;
        try
        {
            var handler = new StubHttpMessageHandler("[]");
            var service = CreateService(handler);

            await service.ListIncomingPayments(new PaymentsQuery { From = -1, To = 100 });

            Assert.Contains("from=-1", handler.LastQuery);
            Assert.DoesNotContain("~", handler.LastQuery);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
