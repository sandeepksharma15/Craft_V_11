using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Craft.Core.Results;

namespace Craft.HttpServices;

/// <summary>The caller owns the HttpClient. Each operation owns its request and response.</summary>
public abstract class HttpServiceBase
{
    #region Fields

    private readonly HttpClient _client;
    private readonly TimeSpan _timeout;
    private readonly int _maximumResponseBytes;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    #region Constructors

    protected HttpServiceBase(HttpClient client, HttpServiceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        options ??= new HttpServiceOptions();
        ArgumentNullException.ThrowIfNull(options.JsonOptions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumResponseBytes);
        if (options.Timeout <= TimeSpan.Zero || options.Timeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(options), "Timeout must be positive and supported by CancellationTokenSource.");

        _client = client;
        _timeout = options.Timeout;
        _maximumResponseBytes = options.MaximumResponseBytes;
        _jsonOptions = new JsonSerializerOptions(options.JsonOptions);
    }

    #endregion

    #region Methods

    protected Task<HttpResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null,
        string? ifMatch = null, CancellationToken cancellationToken = default)
        => ExecuteAsync<T>(method, path, body, ifMatch, true, cancellationToken);

    protected async Task<HttpResult> SendAsync(HttpMethod method, string path, object? body = null,
        string? ifMatch = null, CancellationToken cancellationToken = default)
    {
        HttpResult<bool> result = await ExecuteAsync<bool>(method, path, body, ifMatch, false, cancellationToken).ConfigureAwait(false);
        Result outcome = result.Outcome.IsSuccess ? Result.Success() : Result.Failure(result.Outcome.Errors);

        return new HttpResult(outcome, result.Response);
    }

    private async Task<HttpResult<T>> ExecuteAsync<T>(HttpMethod method, string path, object? body,
        string? ifMatch, bool readBody, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Uri.TryCreate(path, UriKind.Relative, out Uri? uri) || path.StartsWith('/') || path.Contains('\\'))
            throw new ArgumentException("Use a relative path without a leading slash.", nameof(path));

        using HttpRequestMessage request = new(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (ifMatch is not null)
        {
            EntityTagHeaderValue tag = EntityTagHeaderValue.Parse(ifMatch);
            if (tag.IsWeak || tag.Tag == "*")
                throw new ArgumentException("Use one original strong ETag.", nameof(ifMatch));
            request.Headers.IfMatch.Add(tag);
        }
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: _jsonOptions);

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        HttpResponseInfo info = new();

        try
        {
            using HttpResponseMessage response = await _client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            info = new HttpResponseInfo(response.StatusCode, response.Headers.ETag?.ToString(), response.Headers.Location);

            if (response.IsSuccessStatusCode && !readBody)
                return new HttpResult<T>(Result<T>.Success(default!), info);

            byte[] payload = await ReadBodyAsync(response.Content, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                HttpProblem? problem = ReadProblem(payload);
                info = info with { Problem = problem };
                return new HttpResult<T>(Result<T>.Failure(ReadErrors(response.StatusCode, problem)), info);
            }

            T? value = JsonSerializer.Deserialize<T>(payload, _jsonOptions);
            if (value is null)
                return Failure<T>("http.invalid_response", "The server returned no value.", ErrorKind.Failure, info);

            return new HttpResult<T>(Result<T>.Success(value), info);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure<T>("http.timeout", "The request timed out.", ErrorKind.Timeout, info);
        }
        catch (HttpRequestException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Failure<T>("http.unavailable", "The server could not be reached.", ErrorKind.Unavailable, info);
        }
        catch (IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Failure<T>("http.unavailable", "The response could not be read.", ErrorKind.Unavailable, info);
        }
        catch (JsonException)
        {
            return Failure<T>("http.invalid_response", "The server returned an invalid response.", ErrorKind.Failure, info);
        }
    }

    private async Task<byte[]> ReadBodyAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > _maximumResponseBytes)
            throw new JsonException("Response limit exceeded.");

        using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using MemoryStream buffer = new();
        byte[] chunk = new byte[Math.Min(8192, _maximumResponseBytes)];

        while (true)
        {
            int count = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
            if (count == 0)
                return buffer.ToArray();
            if (buffer.Length + count > _maximumResponseBytes)
                throw new JsonException("Response limit exceeded.");

            buffer.Write(chunk, 0, count);
        }
    }

    private HttpProblem? ReadProblem(byte[] payload)
    {
        try
        {
            return JsonSerializer.Deserialize<HttpProblem>(payload, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<Error> ReadErrors(HttpStatusCode status, HttpProblem? problem)
    {
        ErrorKind kind = (int)status switch
        {
            400 or 422 => ErrorKind.Validation,
            401 => ErrorKind.Unauthorized,
            403 => ErrorKind.Forbidden,
            404 => ErrorKind.NotFound,
            409 or 412 or 428 => ErrorKind.Conflict,
            408 or 504 => ErrorKind.Timeout,
            429 or 502 or 503 => ErrorKind.Unavailable,
            _ => ErrorKind.Failure
        };
        string code = $"http.{(int)status}";
        List<Error> errors = [];

        if (kind == ErrorKind.Validation && problem?.Errors is not null)
        {
            foreach (KeyValuePair<string, string[]> entry in problem.Errors)
            {
                foreach (string message in entry.Value ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(message))
                        errors.Add(new Error(code, message, kind, string.IsNullOrWhiteSpace(entry.Key) ? null : entry.Key));
                }
            }
        }
        if (errors.Count == 0)
            errors.Add(new Error(code, $"The server returned HTTP {(int)status}.", kind));

        return errors;
    }

    private static HttpResult<T> Failure<T>(string code, string message, ErrorKind kind, HttpResponseInfo info)
        => new(Result<T>.Failure(new Error(code, message, kind)), info);

    #endregion
}
