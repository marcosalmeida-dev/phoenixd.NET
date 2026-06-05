using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Phoenixd.NET.Exceptions;
using Phoenixd.NET.Json;

namespace Phoenixd.NET.Services;

/// <summary>
/// Shared HTTP plumbing for the phoenixd API services. Centralizes status-code checking, error-body
/// capture, JSON (de)serialization and structured logging so the concrete services stay focused on
/// the API surface. On a non-success status it raises a <see cref="PhoenixdApiException"/> carrying
/// the status code and raw body.
/// </summary>
internal abstract class PhoenixdServiceBase
{
    protected HttpClient HttpClient { get; }
    private readonly ILogger _logger;

    protected PhoenixdServiceBase(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Builds form content, dropping any field whose value is null.</summary>
    protected static FormUrlEncodedContent Form(params (string Key, string? Value)[] fields)
    {
        var present = fields
            .Where(f => f.Value is not null)
            .Select(f => new KeyValuePair<string, string>(f.Key, f.Value!));
        return new FormUrlEncodedContent(present);
    }

    protected async Task<T> GetJsonAsync<T>(string path, string operation, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await HttpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
            return await DeserializeAsync<T>(response, operation, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (LogRequestError(ex, operation))
        {
            throw; // unreachable: the filter returns false, this only logs.
        }
    }

    protected async Task<string> GetStringAsync(string path, string operation, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await HttpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAsync(response, operation, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return body.Trim();
        }
        catch (Exception ex) when (LogRequestError(ex, operation))
        {
            throw;
        }
    }

    protected async Task<T> PostJsonAsync<T>(string path, FormUrlEncodedContent content, string operation, CancellationToken cancellationToken)
    {
        try
        {
            using (content)
            using (var response = await HttpClient.PostAsync(path, content, cancellationToken).ConfigureAwait(false))
            {
                return await DeserializeAsync<T>(response, operation, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (LogRequestError(ex, operation))
        {
            throw;
        }
    }

    protected async Task<string> PostStringAsync(string path, FormUrlEncodedContent content, string operation, CancellationToken cancellationToken)
    {
        try
        {
            using (content)
            using (var response = await HttpClient.PostAsync(path, content, cancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccessAsync(response, operation, cancellationToken).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return body.Trim();
            }
        }
        catch (Exception ex) when (LogRequestError(ex, operation))
        {
            throw;
        }
    }

    protected async Task<JsonElement> PostJsonElementAsync(string path, FormUrlEncodedContent content, string operation, CancellationToken cancellationToken)
    {
        var raw = await PostStringAsync(path, content, operation, cancellationToken).ConfigureAwait(false);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new PhoenixdApiException($"{operation}: response was not valid JSON.", responseBody: raw, innerException: ex);
        }
    }

    private async Task<T> DeserializeAsync<T>(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, operation, cancellationToken).ConfigureAwait(false);
        T? result;
        try
        {
            result = await response.Content
                .ReadFromJsonAsync<T>(PhoenixdJson.Default, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            var body = await SafeReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
            throw new PhoenixdApiException($"{operation}: failed to deserialize response into {typeof(T).Name}.", responseBody: body, innerException: ex);
        }

        if (result is null)
        {
            throw new PhoenixdApiException($"{operation}: response body was empty.", (int)response.StatusCode);
        }

        return result;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await SafeReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        _logger.LogError("phoenixd {Operation} failed with status {StatusCode}: {Body}", operation, (int)response.StatusCode, body);
        throw new PhoenixdApiException(
            $"phoenixd {operation} failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).",
            (int)response.StatusCode,
            body);
    }

    private static async Task<string?> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    // Used as an exception filter: logs the transport/parse error with operation context and returns
    // false so the original exception keeps propagating with its stack trace intact. PhoenixdApiException
    // is skipped because EnsureSuccessAsync/DeserializeAsync already logged it.
    private bool LogRequestError(Exception ex, string operation)
    {
        if (ex is PhoenixdApiException || ex is OperationCanceledException)
        {
            return false;
        }

        _logger.LogError(ex, "phoenixd {Operation} request error", operation);
        return false;
    }
}
