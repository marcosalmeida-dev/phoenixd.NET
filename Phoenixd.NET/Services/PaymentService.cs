using System.Text.Json;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Services;

internal sealed class PaymentService : PhoenixdServiceBase, IPaymentService
{
    public PaymentService(HttpClient httpClient, ILogger<PaymentService> logger)
        : base(httpClient, logger)
    {
    }

    // --- Receiving ---------------------------------------------------------------------------

    public Task<Invoice> ReceiveLightningPaymentAsync(
        string? description,
        long amountSat,
        string? externalId = null,
        string? descriptionHash = null,
        long? expirySeconds = null,
        string? webhookUrl = null,
        CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("amountSat", amountSat.ToString()),
            // phoenixd accepts either description or descriptionHash, not both.
            ("description", string.IsNullOrEmpty(descriptionHash) ? description : null),
            ("descriptionHash", descriptionHash),
            ("externalId", string.IsNullOrEmpty(externalId) ? null : externalId),
            ("expirySeconds", expirySeconds?.ToString()),
            ("webhookUrl", webhookUrl));

        return PostJsonAsync<Invoice>("/createinvoice", content, nameof(ReceiveLightningPaymentAsync), cancellationToken);
    }

    public Task<string> CreateOfferAsync(long? amountSat = null, string? description = null, CancellationToken cancellationToken = default) =>
        PostStringAsync(
            "/createoffer",
            Form(("amountSat", amountSat?.ToString()), ("description", description)),
            nameof(CreateOfferAsync),
            cancellationToken);

    public Task<string> GetOfferAsync(CancellationToken cancellationToken = default) =>
        GetStringAsync("/getoffer", nameof(GetOfferAsync), cancellationToken);

    public Task<string> GetLnAddressAsync(CancellationToken cancellationToken = default) =>
        GetStringAsync("/getlnaddress", nameof(GetLnAddressAsync), cancellationToken);

    // --- Sending -----------------------------------------------------------------------------

    public Task<PayInvoiceResponse> SendLightningInvoice(long amountSat, string invoice, CancellationToken cancellationToken = default)
    {
        var content = Form(
            // amountSat is only sent for amountless invoices; 0 means "use the invoice amount".
            ("amountSat", amountSat > 0 ? amountSat.ToString() : null),
            ("invoice", invoice));

        return PostJsonAsync<PayInvoiceResponse>("/payinvoice", content, nameof(SendLightningInvoice), cancellationToken);
    }

    public Task<PayInvoiceResponse> PayOfferAsync(string offer, long? amountSat = null, string? message = null, CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("offer", offer),
            ("amountSat", amountSat?.ToString()),
            ("message", message));

        return PostJsonAsync<PayInvoiceResponse>("/payoffer", content, nameof(PayOfferAsync), cancellationToken);
    }

    public Task<PayInvoiceResponse> PayLnAddressAsync(string address, long amountSat, string? message = null, CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("address", address),
            ("amountSat", amountSat.ToString()),
            ("message", message));

        return PostJsonAsync<PayInvoiceResponse>("/paylnaddress", content, nameof(PayLnAddressAsync), cancellationToken);
    }

    public Task<PayInvoiceResponse> LnurlPayAsync(string lnurl, long? amountSat = null, string? message = null, CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("lnurl", lnurl),
            ("amountSat", amountSat?.ToString()),
            ("message", message));

        return PostJsonAsync<PayInvoiceResponse>("/lnurlpay", content, nameof(LnurlPayAsync), cancellationToken);
    }

    public Task<JsonElement> LnurlWithdrawAsync(string lnurl, CancellationToken cancellationToken = default) =>
        PostJsonElementAsync("/lnurlwithdraw", Form(("lnurl", lnurl)), nameof(LnurlWithdrawAsync), cancellationToken);

    public Task<JsonElement> LnurlAuthAsync(string lnurl, CancellationToken cancellationToken = default) =>
        PostJsonElementAsync("/lnurlauth", Form(("lnurl", lnurl)), nameof(LnurlAuthAsync), cancellationToken);

    public Task<string> SendOnchainPayment(long amountSat, string address, int feerateSatByte, CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("amountSat", amountSat.ToString()),
            ("address", address),
            ("feerateSatByte", feerateSatByte.ToString()));

        return PostStringAsync("/sendtoaddress", content, nameof(SendOnchainPayment), cancellationToken);
    }

    // --- Decoding ----------------------------------------------------------------------------

    public Task<JsonElement> DecodeInvoiceAsync(string invoice, CancellationToken cancellationToken = default) =>
        PostJsonElementAsync("/decodeinvoice", Form(("invoice", invoice)), nameof(DecodeInvoiceAsync), cancellationToken);

    public Task<JsonElement> DecodeOfferAsync(string offer, CancellationToken cancellationToken = default) =>
        PostJsonElementAsync("/decodeoffer", Form(("offer", offer)), nameof(DecodeOfferAsync), cancellationToken);

    // --- History -----------------------------------------------------------------------------

    public Task<List<PaymentInfo>> ListIncomingPayments(string externalId, CancellationToken cancellationToken = default) =>
        ListIncomingPayments(new PaymentsQuery { ExternalId = externalId }, cancellationToken);

    public Task<List<PaymentInfo>> ListIncomingPayments(PaymentsQuery query, CancellationToken cancellationToken = default) =>
        GetJsonAsync<List<PaymentInfo>>(
            "/payments/incoming" + BuildQueryString(query, includeExternalId: true),
            nameof(ListIncomingPayments),
            cancellationToken);

    public Task<PaymentInfo> GetIncomingPayment(string paymentHash, CancellationToken cancellationToken = default) =>
        GetJsonAsync<PaymentInfo>(
            $"/payments/incoming/{Uri.EscapeDataString(paymentHash)}",
            nameof(GetIncomingPayment),
            cancellationToken);

    public Task<List<PaymentInfoOutgoing>> ListOutgoingPayments(PaymentsQuery query, CancellationToken cancellationToken = default) =>
        GetJsonAsync<List<PaymentInfoOutgoing>>(
            "/payments/outgoing" + BuildQueryString(query, includeExternalId: false),
            nameof(ListOutgoingPayments),
            cancellationToken);

    public Task<PaymentInfoOutgoing> GetOutgoingPayment(string paymentId, CancellationToken cancellationToken = default) =>
        GetJsonAsync<PaymentInfoOutgoing>(
            $"/payments/outgoing/{Uri.EscapeDataString(paymentId)}",
            nameof(GetOutgoingPayment),
            cancellationToken);

    public Task<PaymentInfoOutgoing> GetOutgoingPaymentByHash(string paymentHash, CancellationToken cancellationToken = default) =>
        GetJsonAsync<PaymentInfoOutgoing>(
            $"/payments/outgoingbyhash/{Uri.EscapeDataString(paymentHash)}",
            nameof(GetOutgoingPaymentByHash),
            cancellationToken);

    private static string BuildQueryString(PaymentsQuery query, bool includeExternalId)
    {
        var parameters = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                parameters.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        Add("from", query.From?.ToString());
        Add("to", query.To?.ToString());
        Add("limit", query.Limit?.ToString());
        Add("offset", query.Offset?.ToString());
        Add("all", query.All?.ToString().ToLowerInvariant());
        if (includeExternalId)
        {
            Add("externalId", query.ExternalId);
        }

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }
}
