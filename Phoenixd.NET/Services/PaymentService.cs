using Microsoft.Extensions.Logging;
using Phoenixd.NET.Core.Interfaces;
using Phoenixd.NET.Core.Models;
using System.Net.Http.Json;

namespace Phoenixd.NET.WebServiceClient.Services;

internal class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(HttpClient httpClient, ILogger<PaymentService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    public async Task<Invoice> ReceiveLightningPaymentAsync(string description, long amountSat, string externalId = "")
    {
        var data = new Dictionary<string, string>
        {
            { "description", description },
            { "amountSat", amountSat.ToString() }
        };

        if (!string.IsNullOrEmpty(externalId))
        {
            data.Add("externalId", externalId);
        }

        try
        {
            var response = await _httpClient.PostAsync("/createinvoice", new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Lightning payment created successfully.");

            return await response.Content.ReadFromJsonAsync<Invoice>(); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lightning payment");
            throw;
        }
    }

    public async Task<PayInvoiceResponse> SendLightningInvoice(long amountSat, string invoice)
    {
        var data = new Dictionary<string, string>
        {
            { "amountSat", amountSat.ToString() },
            { "invoice", invoice }
        };

        try
        {
            var response = await _httpClient.PostAsync("/payinvoice", new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Lightning invoice sent successfully.");
            return await response.Content.ReadFromJsonAsync<PayInvoiceResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending lightning invoice");
            throw;
        }
    }

    public async Task<string> SendOnchainPayment(long amountSat, string address, int feerateSatByte)
    {
        var data = new Dictionary<string, string>
        {
            { "amountSat", amountSat.ToString() },
            { "address", address },
            { "feerateSatByte", feerateSatByte.ToString() }
        };

        try
        {
            var response = await _httpClient.PostAsync("/sendtoaddress", new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Onchain payment sent successfully.");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending onchain payment");
            throw;
        }
    }

    public async Task<List<PaymentInfo>> ListIncomingPayments(string externalId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/payments/incoming?externalId={externalId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PaymentInfo>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing incoming payments");
            throw;
        }
    }

    public async Task<PaymentInfo> GetIncomingPayment(string paymentHash)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/payments/incoming/{paymentHash}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting incoming payment");
            throw;
        }
    }

    public async Task<PaymentInfoOutgoing> GetOutgoingPayment(string paymentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/payments/outgoing/{paymentId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentInfoOutgoing>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting outgoing payment");
            throw;
        }
    }
}
