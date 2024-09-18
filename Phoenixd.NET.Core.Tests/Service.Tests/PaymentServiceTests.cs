using Microsoft.Extensions.Logging;
using Moq;
using Phoenixd.NET.Core.Models;
using Phoenixd.NET.WebServiceClient.Services;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;

namespace Phoenixd.NET.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _mockLogger = new Mock<ILogger<PaymentService>>();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _paymentService = new PaymentService(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task ReceiveLightningPaymentAsync_ShouldReturnInvoice_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedInvoice = new Invoice { AmountSat = 1000, PaymentHash = "hash123" };
        _mockHttpMessageHandler
            .When("/createinvoice")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedInvoice));

        // Act
        var result = await _paymentService.ReceiveLightningPaymentAsync("Test payment", 1000);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedInvoice.AmountSat, result.AmountSat);
        Assert.Equal(expectedInvoice.PaymentHash, result.PaymentHash);
    }

    [Fact]
    public async Task SendLightningInvoice_ShouldReturnPayInvoiceResponse_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedResponse = new PayInvoiceResponse { RecipientAmountSat = 1000, PaymentHash = "hash123" };
        _mockHttpMessageHandler
            .When("/payinvoice")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        // Act
        var result = await _paymentService.SendLightningInvoice(1000, "invoice123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.RecipientAmountSat, result.RecipientAmountSat);
        Assert.Equal(expectedResponse.PaymentHash, result.PaymentHash);
    }

    [Fact]
    public async Task SendOnchainPayment_ShouldReturnTransactionId_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedResponse = "tx123";
        _mockHttpMessageHandler
            .When("/sendtoaddress")
            .Respond(HttpStatusCode.OK, new StringContent(expectedResponse));

        // Act
        var result = await _paymentService.SendOnchainPayment(1000, "address123", 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task ListIncomingPayments_ShouldReturnPaymentsList_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedPayments = new List<PaymentInfo>
        {
            new PaymentInfo { PaymentHash = "hash123", ReceivedSat = 1000 }
        };
        _mockHttpMessageHandler
            .When("/payments/incoming")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedPayments));

        // Act
        var result = await _paymentService.ListIncomingPayments("externalId123");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(expectedPayments[0].PaymentHash, result[0].PaymentHash);
    }

    [Fact]
    public async Task GetIncomingPayment_ShouldReturnPaymentInfo_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedPaymentInfo = new PaymentInfo { PaymentHash = "hash123", ReceivedSat = 1000 };
        _mockHttpMessageHandler
            .When("/payments/incoming/hash123")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedPaymentInfo));

        // Act
        var result = await _paymentService.GetIncomingPayment("hash123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPaymentInfo.PaymentHash, result.PaymentHash);
    }

    [Fact]
    public async Task GetOutgoingPayment_ShouldReturnPaymentInfoOutgoing_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedPaymentInfoOutgoing = new PaymentInfoOutgoing { PaymentHash = "hash123", Sent = 1000 };
        _mockHttpMessageHandler
            .When("/payments/outgoing/payment123")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedPaymentInfoOutgoing));

        // Act
        var result = await _paymentService.GetOutgoingPayment("payment123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPaymentInfoOutgoing.PaymentHash, result.PaymentHash);
    }
}
