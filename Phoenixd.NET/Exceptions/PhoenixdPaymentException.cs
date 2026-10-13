using Phoenixd.NET.Models;

namespace Phoenixd.NET.Exceptions;

/// <summary>A Lightning payment failed even though the daemon returned HTTP 200.</summary>
public sealed class PhoenixdPaymentException : PhoenixdApiException
{
    public PaymentFailed Failure { get; }

    public string Reason => Failure.Reason;

    public PhoenixdPaymentException(PaymentFailed failure, string responseBody)
        : base($"phoenixd payment failed: {failure.Reason}", 200, responseBody)
    {
        Failure = failure;
    }
}
