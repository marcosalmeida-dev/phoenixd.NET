using System.Text.Json;
using Phoenixd.NET.Models;

namespace Phoenixd.NET.Interfaces;

/// <summary>
/// Payment operations exposed by the phoenixd HTTP API: receiving (invoices, BOLT12 offers,
/// Lightning addresses), sending (invoices, offers, LN addresses, LNURL, on-chain) and querying
/// payment history.
/// </summary>
public interface IPaymentService
{
    // --- Receiving ---------------------------------------------------------------------------

    /// <summary>Creates a BOLT11 invoice, including amountless invoices. Supply exactly one description form.</summary>
    Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates a BOLT11 invoice (<c>POST /createinvoice</c>).</summary>
    Task<Invoice> ReceiveLightningPaymentAsync(
        string? description,
        long amountSat,
        string? externalId = null,
        string? descriptionHash = null,
        long? expirySeconds = null,
        string? webhookUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a reusable BOLT12 offer (<c>POST /createoffer</c>). Returns the serialized offer.</summary>
    Task<string> CreateOfferAsync(long? amountSat = null, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the node's default BOLT12 offer (<c>GET /getoffer</c>).</summary>
    Task<string> GetOfferAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the node's BOLT12 Lightning address (<c>GET /getlnaddress</c>).</summary>
    Task<string> GetLnAddressAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the swap-in wallet address and derivation index (<c>GET /getswapinaddress</c>).</summary>
    Task<SwapInAddress> GetSwapInAddressAsync(CancellationToken cancellationToken = default);

    // --- Sending -----------------------------------------------------------------------------

    /// <summary>Pays a BOLT11 invoice (<c>POST /payinvoice</c>). Requires full access.</summary>
    Task<PayInvoiceResponse> SendLightningInvoice(long amountSat, string invoice, CancellationToken cancellationToken = default);

    /// <summary>Pays an invoice using its amount, an explicit amount override, or SendAll. AmountSat and SendAll are mutually exclusive.</summary>
    Task<PayInvoiceResponse> SendLightningInvoice(PayInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Pays a BOLT12 offer (<c>POST /payoffer</c>). Requires full access.</summary>
    Task<PayInvoiceResponse> PayOfferAsync(string offer, long? amountSat = null, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>Pays a BOLT12 offer with optional amount override, SendAll and payer message.</summary>
    Task<PayInvoiceResponse> PayOfferAsync(PayOfferRequest request, CancellationToken cancellationToken = default);

    /// <summary>Pays a Lightning address (<c>POST /paylnaddress</c>). Requires full access.</summary>
    Task<PayInvoiceResponse> PayLnAddressAsync(string address, long amountSat, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>Pays a Lightning address using BIP353 or LNURL. Supply AmountSat or SendAll.</summary>
    Task<PayInvoiceResponse> PayLnAddressAsync(PayLnAddressRequest request, CancellationToken cancellationToken = default);

    /// <summary>Pays via an LNURL-pay request (<c>POST /lnurlpay</c>). Requires full access.</summary>
    Task<PayInvoiceResponse> LnurlPayAsync(string lnurl, long? amountSat = null, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>Pays an LNURL with an explicit amount, its minimum amount, or SendAll.</summary>
    Task<PayInvoiceResponse> LnurlPayAsync(LnurlPayRequest request, CancellationToken cancellationToken = default);

    /// <summary>Withdraws via an LNURL-withdraw request (<c>POST /lnurlwithdraw</c>).</summary>
    Task<JsonElement> LnurlWithdrawAsync(string lnurl, CancellationToken cancellationToken = default);

    /// <summary>Withdraws using LNURL and returns typed withdrawal details. Amounts are in millisatoshis.</summary>
    Task<LnurlWithdrawResponse> WithdrawLnurlAsync(string lnurl, CancellationToken cancellationToken = default);

    /// <summary>Authenticates via an LNURL-auth request (<c>POST /lnurlauth</c>). Requires full access.</summary>
    Task<JsonElement> LnurlAuthAsync(string lnurl, CancellationToken cancellationToken = default);

    /// <summary>Authenticates with LNURL and returns phoenixd's plain-text success message.</summary>
    Task<string> AuthenticateLnurlAsync(string lnurl, CancellationToken cancellationToken = default);

    /// <summary>Sends an on-chain payment (<c>POST /sendtoaddress</c>). Returns the txid. Requires full access.</summary>
    Task<string> SendOnchainPayment(long amountSat, string address, int feerateSatByte, CancellationToken cancellationToken = default);

    // --- Decoding ----------------------------------------------------------------------------

    /// <summary>Decodes a BOLT11 invoice (<c>POST /decodeinvoice</c>).</summary>
    Task<JsonElement> DecodeInvoiceAsync(string invoice, CancellationToken cancellationToken = default);

    /// <summary>Decodes a BOLT12 offer (<c>POST /decodeoffer</c>).</summary>
    Task<JsonElement> DecodeOfferAsync(string offer, CancellationToken cancellationToken = default);

    // --- History -----------------------------------------------------------------------------

    /// <summary>Lists incoming payments filtered by external id (<c>GET /payments/incoming</c>).</summary>
    Task<List<PaymentInfo>> ListIncomingPayments(string externalId, CancellationToken cancellationToken = default);

    /// <summary>Lists incoming payments with full filtering/pagination (<c>GET /payments/incoming</c>).</summary>
    Task<List<PaymentInfo>> ListIncomingPayments(PaymentsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gets a single incoming payment by payment hash (<c>GET /payments/incoming/{paymentHash}</c>).</summary>
    Task<PaymentInfo> GetIncomingPayment(string paymentHash, CancellationToken cancellationToken = default);

    /// <summary>Finds an incoming Lightning payment, returning null when phoenixd reports no match (404 or 204).</summary>
    Task<PaymentInfo?> FindIncomingPaymentAsync(string paymentHash, CancellationToken cancellationToken = default);

    /// <summary>Lists outgoing payments with filtering/pagination (<c>GET /payments/outgoing</c>).</summary>
    Task<List<PaymentInfoOutgoing>> ListOutgoingPayments(PaymentsQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gets a single outgoing payment by its UUID (<c>GET /payments/outgoing/{uuid}</c>).</summary>
    Task<PaymentInfoOutgoing> GetOutgoingPayment(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>Gets an outgoing payment by UUID (<c>GET /payments/outgoing/{uuid}</c>).</summary>
    Task<PaymentInfoOutgoing> GetOutgoingPayment(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>Finds an outgoing payment by UUID, returning null on 404 or 204.</summary>
    Task<PaymentInfoOutgoing?> FindOutgoingPaymentAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>Finds an outgoing payment by UUID, returning null on 404 or 204.</summary>
    Task<PaymentInfoOutgoing?> FindOutgoingPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>Gets a single outgoing payment by payment hash (<c>GET /payments/outgoingbyhash/{paymentHash}</c>).</summary>
    Task<PaymentInfoOutgoing> GetOutgoingPaymentByHash(string paymentHash, CancellationToken cancellationToken = default);

    /// <summary>Finds the best outgoing Lightning attempt by payment hash: successful, then pending, then failed. Returns null on 404 or 204.</summary>
    Task<PaymentInfoOutgoing?> FindOutgoingPaymentByHashAsync(string paymentHash, CancellationToken cancellationToken = default);

    /// <summary>Exports successful payment history to a CSV on the daemon host (<c>POST /export?from=...&amp;to=...</c>).</summary>
    Task<string> ExportPaymentsAsync(long? from = null, long? to = null, CancellationToken cancellationToken = default);
}
