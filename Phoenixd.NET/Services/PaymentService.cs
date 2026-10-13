using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Exceptions;
using Phoenixd.NET.Interfaces;
using Phoenixd.NET.Json;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Services;

internal sealed class PaymentService : PhoenixdServiceBase, IPaymentService
{
    public PaymentService(HttpClient httpClient, ILogger<PaymentService> logger)
        : base(httpClient, logger)
    {
    }

    // --- Receiving ---------------------------------------------------------------------------

    public Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if ((request.Description is null) == (request.DescriptionHash is null))
        {
            throw new ArgumentException("Supply exactly one of Description or DescriptionHash.", nameof(request));
        }

        if (request.Description?.Length > 128)
        {
            throw new ArgumentException("Description must not exceed 128 characters.", nameof(request));
        }

        return PostJsonAsync<Invoice>("/createinvoice", Form(
            ("amountSat", Number(request.AmountSat)),
            ("description", request.Description),
            ("descriptionHash", request.DescriptionHash),
            ("externalId", request.ExternalId),
            ("expirySeconds", Number(request.ExpirySeconds)),
            ("webhookUrl", request.WebhookUrl)), nameof(CreateInvoiceAsync), cancellationToken);
    }

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
            ("amountSat", Number(amountSat)),
            // phoenixd accepts either description or descriptionHash, not both.
            ("description", string.IsNullOrEmpty(descriptionHash) ? description : null),
            ("descriptionHash", descriptionHash),
            ("externalId", string.IsNullOrEmpty(externalId) ? null : externalId),
            ("expirySeconds", Number(expirySeconds)),
            ("webhookUrl", webhookUrl));

        return PostJsonAsync<Invoice>("/createinvoice", content, nameof(ReceiveLightningPaymentAsync), cancellationToken);
    }

    public Task<string> CreateOfferAsync(long? amountSat = null, string? description = null, CancellationToken cancellationToken = default) =>
        PostStringAsync(
            "/createoffer",
            Form(("amountSat", Number(amountSat)), ("description", description)),
            nameof(CreateOfferAsync),
            cancellationToken);

    public Task<string> GetOfferAsync(CancellationToken cancellationToken = default) =>
        GetStringAsync("/getoffer", nameof(GetOfferAsync), cancellationToken);

    public Task<string> GetLnAddressAsync(CancellationToken cancellationToken = default) =>
        GetStringAsync("/getlnaddress", nameof(GetLnAddressAsync), cancellationToken);

    public Task<SwapInAddress> GetSwapInAddressAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<SwapInAddress>("/getswapinaddress", nameof(GetSwapInAddressAsync), cancellationToken);

    // --- Sending -----------------------------------------------------------------------------

    public Task<PayInvoiceResponse> SendLightningInvoice(long amountSat, string invoice, CancellationToken cancellationToken = default) =>
        SendLightningInvoice(new PayInvoiceRequest
        {
            Invoice = invoice,
            // Preserve the legacy 0 convention for using the invoice's amount.
            AmountSat = amountSat > 0 ? amountSat : null
        }, cancellationToken);

    public Task<PayInvoiceResponse> SendLightningInvoice(PayInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAmount(request.AmountSat, request.SendAll);
        var content = Form(
            ("amountSat", Number(request.AmountSat)),
            ("sendAll", request.SendAll ? "true" : null),
            ("invoice", request.Invoice));

        return PostPaymentAsync("/payinvoice", content, nameof(SendLightningInvoice), cancellationToken);
    }

    public Task<PayInvoiceResponse> PayOfferAsync(string offer, long? amountSat = null, string? message = null, CancellationToken cancellationToken = default) =>
        PayOfferAsync(new PayOfferRequest { Offer = offer, AmountSat = amountSat, Message = message }, cancellationToken);

    public Task<PayInvoiceResponse> PayOfferAsync(PayOfferRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAmount(request.AmountSat, request.SendAll);
        var content = Form(
            ("offer", request.Offer),
            ("amountSat", Number(request.AmountSat)),
            ("sendAll", request.SendAll ? "true" : null),
            ("message", request.Message));

        return PostPaymentAsync("/payoffer", content, nameof(PayOfferAsync), cancellationToken);
    }

    public Task<PayInvoiceResponse> PayLnAddressAsync(string address, long amountSat, string? message = null, CancellationToken cancellationToken = default) =>
        PayLnAddressAsync(new PayLnAddressRequest { Address = address, AmountSat = amountSat, Message = message }, cancellationToken);

    public Task<PayInvoiceResponse> PayLnAddressAsync(PayLnAddressRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAmount(request.AmountSat, request.SendAll);
        if (!request.AmountSat.HasValue && !request.SendAll)
        {
            throw new ArgumentException("Supply AmountSat or set SendAll when paying a Lightning address.", nameof(request));
        }

        var content = Form(
            ("address", request.Address),
            ("amountSat", Number(request.AmountSat)),
            ("sendAll", request.SendAll ? "true" : null),
            ("message", request.Message));

        return PostPaymentAsync("/paylnaddress", content, nameof(PayLnAddressAsync), cancellationToken);
    }

    public Task<PayInvoiceResponse> LnurlPayAsync(string lnurl, long? amountSat = null, string? message = null, CancellationToken cancellationToken = default) =>
        LnurlPayAsync(new LnurlPayRequest { Lnurl = lnurl, AmountSat = amountSat, Message = message }, cancellationToken);

    public Task<PayInvoiceResponse> LnurlPayAsync(LnurlPayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateAmount(request.AmountSat, request.SendAll);
        var content = Form(
            ("lnurl", request.Lnurl),
            ("amountSat", Number(request.AmountSat)),
            ("sendAll", request.SendAll ? "true" : null),
            ("message", request.Message));

        return PostPaymentAsync("/lnurlpay", content, nameof(LnurlPayAsync), cancellationToken);
    }

    public Task<JsonElement> LnurlWithdrawAsync(string lnurl, CancellationToken cancellationToken = default) =>
        PostJsonElementAsync("/lnurlwithdraw", Form(("lnurl", lnurl)), nameof(LnurlWithdrawAsync), cancellationToken);

    public Task<LnurlWithdrawResponse> WithdrawLnurlAsync(string lnurl, CancellationToken cancellationToken = default) =>
        PostJsonAsync<LnurlWithdrawResponse>("/lnurlwithdraw", Form(("lnurl", lnurl)), nameof(WithdrawLnurlAsync), cancellationToken);

    public async Task<JsonElement> LnurlAuthAsync(string lnurl, CancellationToken cancellationToken = default) =>
        JsonSerializer.SerializeToElement(await AuthenticateLnurlAsync(lnurl, cancellationToken).ConfigureAwait(false));

    public Task<string> AuthenticateLnurlAsync(string lnurl, CancellationToken cancellationToken = default) =>
        PostStringAsync("/lnurlauth", Form(("lnurl", lnurl)), nameof(AuthenticateLnurlAsync), cancellationToken);

    public async Task<string> SendOnchainPayment(long amountSat, string address, int feerateSatByte, CancellationToken cancellationToken = default)
    {
        var content = Form(
            ("amountSat", Number(amountSat)),
            ("address", address),
            ("feerateSatByte", Number(feerateSatByte)));

        var body = await PostStringAsync("/sendtoaddress", content, nameof(SendOnchainPayment), cancellationToken).ConfigureAwait(false);
        return ValidateTransactionId(body, nameof(SendOnchainPayment));
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

    public Task<PaymentInfo?> FindIncomingPaymentAsync(string paymentHash, CancellationToken cancellationToken = default) =>
        GetOptionalJsonAsync<PaymentInfo>(
            $"/payments/incoming/{Uri.EscapeDataString(paymentHash)}",
            nameof(FindIncomingPaymentAsync),
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

    public Task<PaymentInfoOutgoing> GetOutgoingPayment(Guid paymentId, CancellationToken cancellationToken = default) =>
        GetOutgoingPayment(paymentId.ToString("D"), cancellationToken);

    public Task<PaymentInfoOutgoing?> FindOutgoingPaymentAsync(string paymentId, CancellationToken cancellationToken = default) =>
        GetOptionalJsonAsync<PaymentInfoOutgoing>(
            $"/payments/outgoing/{Uri.EscapeDataString(paymentId)}",
            nameof(FindOutgoingPaymentAsync),
            cancellationToken);

    public Task<PaymentInfoOutgoing?> FindOutgoingPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        FindOutgoingPaymentAsync(paymentId.ToString("D"), cancellationToken);

    public Task<PaymentInfoOutgoing> GetOutgoingPaymentByHash(string paymentHash, CancellationToken cancellationToken = default) =>
        GetJsonAsync<PaymentInfoOutgoing>(
            $"/payments/outgoingbyhash/{Uri.EscapeDataString(paymentHash)}",
            nameof(GetOutgoingPaymentByHash),
            cancellationToken);

    public Task<PaymentInfoOutgoing?> FindOutgoingPaymentByHashAsync(string paymentHash, CancellationToken cancellationToken = default) =>
        GetOptionalJsonAsync<PaymentInfoOutgoing>(
            $"/payments/outgoingbyhash/{Uri.EscapeDataString(paymentHash)}",
            nameof(FindOutgoingPaymentByHashAsync),
            cancellationToken);

    public Task<string> ExportPaymentsAsync(long? from = null, long? to = null, CancellationToken cancellationToken = default) =>
        PostStringAsync("/export" + BuildQueryString(new PaymentsQuery { From = from, To = to }, includeExternalId: false),
            Form(), nameof(ExportPaymentsAsync), cancellationToken);

    private static string BuildQueryString(PaymentsQuery query, bool includeExternalId)
    {
        var parameters = new List<string>();

        void Add(string key, string? value)
        {
            if (value is not null)
            {
                parameters.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        ArgumentNullException.ThrowIfNull(query);
        Add("from", Number(query.From));
        Add("to", Number(query.To));
        Add("limit", Number(query.Limit));
        Add("offset", Number(query.Offset));
        Add("all", query.All?.ToString().ToLowerInvariant());
        if (includeExternalId)
        {
            Add("externalId", query.ExternalId);
        }

        return parameters.Count == 0 ? string.Empty : "?" + string.Join("&", parameters);
    }

    private static string? Number(long? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static void ValidateAmount(long? amountSat, bool sendAll)
    {
        if (amountSat.HasValue && sendAll)
        {
            throw new ArgumentException("AmountSat and SendAll cannot be supplied together.");
        }
    }

    private async Task<PayInvoiceResponse> PostPaymentAsync(string path, FormUrlEncodedContent content, string operation, CancellationToken cancellationToken)
    {
        var raw = await PostStringAsync(path, content, operation, cancellationToken).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Expected a payment response object.");
            }

            string? type = null;
            if (root.TryGetProperty("type", out var discriminator))
            {
                if (discriminator.ValueKind != JsonValueKind.String)
                {
                    throw new JsonException("Invalid payment response type.");
                }

                type = discriminator.GetString();
            }
            if (type == "payment_failed")
            {
                var failure = root.Deserialize<PaymentFailed>(PhoenixdJson.Default)
                    ?? throw new JsonException("Empty payment failure response.");
                throw new PhoenixdPaymentException(failure, raw);
            }

            if (type is not null and not "payment_sent"
                || !root.TryGetProperty("recipientAmountSat", out _)
                || !root.TryGetProperty("routingFeeSat", out _))
            {
                throw new JsonException("Expected a payment_sent response.");
            }

            var result = root.Deserialize<PayInvoiceResponse>(PhoenixdJson.Default)
                ?? throw new JsonException("Empty payment response.");
            if (string.IsNullOrEmpty(result.PaymentId) || string.IsNullOrEmpty(result.PaymentHash) || string.IsNullOrEmpty(result.PaymentPreimage))
            {
                throw new JsonException("Incomplete payment_sent response.");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new PhoenixdApiException($"{operation}: invalid payment response.", 200, raw, ex);
        }
    }
}
