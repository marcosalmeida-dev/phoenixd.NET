using System.Net;
using System.Net.Http.Headers;

namespace Phoenixd.NET.Tests.Helpers;

/// <summary>
/// A test <see cref="HttpMessageHandler"/> that captures the outgoing request and returns a
/// pre-configured response. Lets the service tests assert on the exact path, query string and form
/// body produced by the services without a live phoenixd instance.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;
    private readonly string _mediaType;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK, string mediaType = "application/json")
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
        _mediaType = mediaType;
    }

    public string? LastPath => LastRequest?.RequestUri?.AbsolutePath;
    public string? LastQuery => LastRequest?.RequestUri?.Query;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, System.Text.Encoding.UTF8)
            {
                Headers = { ContentType = new MediaTypeHeaderValue(_mediaType) }
            }
        };
    }

    /// <summary>Builds an <see cref="HttpClient"/> backed by this handler with the phoenixd base address.</summary>
    public HttpClient CreateClient() => new(this)
    {
        BaseAddress = new Uri("http://localhost:9740")
    };
}
