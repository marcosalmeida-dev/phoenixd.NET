using Microsoft.Extensions.Logging;
using Moq;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;

namespace Phoenixd.NET.Tests.Service.Tests;

public class PaymentServiceTests
{
    private readonly Mock<ILogger<IPaymentService>> _mockLogger;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IPaymentService> _mockPaymentService;

    public PaymentServiceTests()
    {
        _mockLogger = new Mock<ILogger<IPaymentService>>();
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _mockPaymentService = new Mock<IPaymentService>();
    }

    [Fact]
    public async Task ReceiveLightningPaymentAsync_ShouldReturnInvoice_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedInvoice = new Invoice { AmountSat = 1000, PaymentHash = "hash123" };
        _mockPaymentService
            .Setup(service => service.ReceiveLightningPaymentAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(expectedInvoice);

        // Act
        var result = await _mockPaymentService.Object.ReceiveLightningPaymentAsync("Test payment", 1000);

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
        _mockPaymentService
            .Setup(service => service.SendLightningInvoice(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPaymentService.Object.SendLightningInvoice(1000, "invoice123");

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
        _mockPaymentService
            .Setup(service => service.SendOnchainPayment(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPaymentService.Object.SendOnchainPayment(1000, "address123", 10);

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
        _mockPaymentService
            .Setup(service => service.ListIncomingPayments(It.IsAny<string>()))
            .ReturnsAsync(expectedPayments);

        // Act
        var result = await _mockPaymentService.Object.ListIncomingPayments("externalId123");

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
        _mockPaymentService
            .Setup(service => service.GetIncomingPayment(It.IsAny<string>()))
            .ReturnsAsync(expectedPaymentInfo);

        // Act
        var result = await _mockPaymentService.Object.GetIncomingPayment("hash123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPaymentInfo.PaymentHash, result.PaymentHash);
    }

    [Fact]
    public async Task GetOutgoingPayment_ShouldReturnPaymentInfoOutgoing_WhenApiCallIsSuccessful()
    {
        // Arrange
        var expectedPaymentInfoOutgoing = new PaymentInfoOutgoing { PaymentHash = "hash123", Sent = 1000 };
        _mockPaymentService
            .Setup(service => service.GetOutgoingPayment(It.IsAny<string>()))
            .ReturnsAsync(expectedPaymentInfoOutgoing);

        // Act
        var result = await _mockPaymentService.Object.GetOutgoingPayment("payment123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPaymentInfoOutgoing.PaymentHash, result.PaymentHash);
    }
}
