using System.Text;
using Phoenixd.NET.Webhooks;

namespace Phoenixd.NET.Tests.Service.Tests;

public class PhoenixdWebhookValidatorTests
{
    [Fact]
    public void ComputeSignature_MatchesKnownHmacSha256Vector()
    {
        // RFC-style well-known HMAC-SHA256 test vector.
        var body = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");

        var signature = PhoenixdWebhookValidator.ComputeSignature("key", body);

        Assert.Equal("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8", signature);
    }

    [Fact]
    public void IsValid_ReturnsTrueForMatchingSignature()
    {
        var body = Encoding.UTF8.GetBytes("""{ "type": "payment_received", "amountSat": 1000 }""");
        var signature = PhoenixdWebhookValidator.ComputeSignature("secret", body);

        Assert.True(PhoenixdWebhookValidator.IsValid("secret", body, signature));
        Assert.True(PhoenixdWebhookValidator.IsValid("secret", body, signature.ToUpperInvariant()));
    }

    [Fact]
    public void IsValid_ReturnsFalseForTamperedBody()
    {
        var body = Encoding.UTF8.GetBytes("""{ "amountSat": 1000 }""");
        var signature = PhoenixdWebhookValidator.ComputeSignature("secret", body);
        var tampered = Encoding.UTF8.GetBytes("""{ "amountSat": 9999 }""");

        Assert.False(PhoenixdWebhookValidator.IsValid("secret", tampered, signature));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-signature")]
    public void IsValid_ReturnsFalseForMissingOrBadSignature(string? signature)
    {
        var body = Encoding.UTF8.GetBytes("payload");
        Assert.False(PhoenixdWebhookValidator.IsValid("secret", body, signature));
    }
}
