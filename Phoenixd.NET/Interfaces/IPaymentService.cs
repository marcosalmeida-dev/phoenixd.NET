using Phoenixd.NET.Models;

namespace Phoenixd.NET.Interfaces;

public interface IPaymentService
{
    Task<PaymentInfo> GetIncomingPayment(string paymentHash);
    Task<PaymentInfoOutgoing> GetOutgoingPayment(string paymentId);
    Task<List<PaymentInfo>> ListIncomingPayments(string externalId);
    Task<Invoice> ReceiveLightningPaymentAsync(string description, long amountSat, string externalId = "");
    Task<PayInvoiceResponse> SendLightningInvoice(long amountSat, string invoice);
    Task<string> SendOnchainPayment(long amountSat, string address, int feerateSatByte);
}
