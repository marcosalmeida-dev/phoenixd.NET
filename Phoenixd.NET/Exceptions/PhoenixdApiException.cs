namespace Phoenixd.NET.Exceptions;

/// <summary>
/// Thrown when the phoenixd HTTP API returns a non-success status code or an unparseable body.
/// Carries the HTTP status code and the raw response body so callers can react to specific
/// failures (for example an insufficient-balance error on a payment) instead of a generic
/// <see cref="HttpRequestException"/>.
/// </summary>
public class PhoenixdApiException : Exception
{
    /// <summary>The HTTP status code returned by phoenixd, when the failure was an HTTP error.</summary>
    public int? StatusCode { get; }

    /// <summary>The raw response body returned by phoenixd, when available.</summary>
    public string? ResponseBody { get; }

    public PhoenixdApiException(string message, int? statusCode = null, string? responseBody = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
