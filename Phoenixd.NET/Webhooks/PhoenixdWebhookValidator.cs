using System.Security.Cryptography;
using System.Text;

namespace Phoenixd.NET.Webhooks;

/// <summary>
/// Verifies the authenticity of HTTP webhook callbacks sent by phoenixd.
/// <para>
/// phoenixd signs every webhook request by computing <c>HMAC-SHA256(rawBody, webhook-secret)</c>
/// and sending the lower-case hex digest in the <see cref="SignatureHeaderName"/> header. A
/// receiver MUST recompute the signature over the exact raw request body and compare it in
/// constant time before trusting the payload — otherwise an attacker could forge "payment
/// received" notifications.
/// </para>
/// </summary>
public static class PhoenixdWebhookValidator
{
    /// <summary>The HTTP header phoenixd uses to carry the webhook signature.</summary>
    public const string SignatureHeaderName = "X-Phoenix-Signature";

    /// <summary>
    /// Computes the expected signature (lower-case hex of HMAC-SHA256) for a raw webhook body.
    /// </summary>
    public static string ComputeSignature(string webhookSecret, ReadOnlySpan<byte> rawBody)
    {
        if (string.IsNullOrEmpty(webhookSecret))
        {
            throw new ArgumentException("Webhook secret must not be empty.", nameof(webhookSecret));
        }

        var key = Encoding.UTF8.GetBytes(webhookSecret);
        Span<byte> hash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(key, rawBody, hash);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Validates a webhook request body against the value of the <see cref="SignatureHeaderName"/>
    /// header using a constant-time comparison. Returns <c>false</c> on any mismatch or malformed
    /// signature rather than throwing, so it is safe to call directly from request handling.
    /// </summary>
    public static bool IsValid(string webhookSecret, ReadOnlySpan<byte> rawBody, string? signatureHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret) || string.IsNullOrWhiteSpace(signatureHeaderValue))
        {
            return false;
        }

        // phoenixd emits lower-case hex; normalising the supplied value keeps the comparison robust
        // to casing introduced by proxies. The fixed-time compare over equal-length digests is what
        // prevents timing attacks against the secret.
        var expected = ComputeSignature(webhookSecret, rawBody);
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        var actualBytes = Encoding.ASCII.GetBytes(signatureHeaderValue.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
