using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Craft.Core.Results;
using Mapster;
using MapsterMapper;

namespace Craft.HttpServices.Tests;

public sealed class TransportTests
{
    [Theory]
    [InlineData(400, ErrorKind.Validation)]
    [InlineData(401, ErrorKind.Unauthorized)]
    [InlineData(403, ErrorKind.Forbidden)]
    [InlineData(404, ErrorKind.NotFound)]
    [InlineData(409, ErrorKind.Conflict)]
    [InlineData(412, ErrorKind.Conflict)]
    [InlineData(428, ErrorKind.Conflict)]
    [InlineData(408, ErrorKind.Timeout)]
    [InlineData(504, ErrorKind.Timeout)]
    [InlineData(429, ErrorKind.Unavailable)]
    [InlineData(502, ErrorKind.Unavailable)]
    [InlineData(503, ErrorKind.Unavailable)]
    [InlineData(500, ErrorKind.Failure)]
    public async Task HttpFailure_UsesActualStatusAndPreservesProblemMetadata(int status, ErrorKind kind)
    {
        const string json = """{"type":"about:blank","title":"Failure","status":200,"detail":"Remote detail","instance":"request/1","traceId":"trace"}""";
        using HttpClient client = Client((_, _) => Task.FromResult(Response(status, json)));
        Probe service = new(client);

        HttpResult<int> result = await service.Get<int>();

        Assert.Equal((HttpStatusCode)status, result.Response.StatusCode);
        Error error = Assert.Single(result.Outcome.Errors);
        Assert.Equal(kind, error.Kind);
        Assert.Equal($"http.{status}", error.Code);
        Assert.Equal("about:blank", result.Response.Problem?.Type);
        Assert.Equal("Failure", result.Response.Problem?.Title);
        Assert.Equal(200, result.Response.Problem?.Status);
        Assert.Equal("Remote detail", result.Response.Problem?.Detail);
        Assert.Equal("request/1", result.Response.Problem?.Instance);
        Assert.Equal("trace", result.Response.Problem?.Extensions?["traceId"].GetString());
        Assert.True(result.Map(value => value.ToString()).Outcome.IsFailure);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("<html>proxy failure</html>", 1)]
    [InlineData("null", 1)]
    [InlineData("{}", 1)]
    [InlineData("{\"errors\":{}}", 1)]
    [InlineData("{\"errors\":{\"Name\":[\"Required\",\" \"],\"\":[\"Invalid\"],\"unused\":null}}", 2)]
    [InlineData("{\"errors\":{\"Name\":null}}", 1)]
    public async Task ErrorBodies_AreOptionalAndValidationTargetsArePreserved(string payload, int count)
    {
        using HttpClient client = Client((_, _) => Task.FromResult(Response(422, payload)));
        HttpResult<int> result = await new Probe(client).Get<int>();

        Assert.Equal(count, result.Outcome.Errors.Count);
        Assert.All(result.Outcome.Errors, error => Assert.Equal(ErrorKind.Validation, error.Kind));
        if (count == 2)
        {
            Assert.Equal("Name", result.Outcome.Errors[0].Target);
            Assert.Null(result.Outcome.Errors[1].Target);
        }
    }

    [Theory]
    [InlineData(200, "invalid")]
    [InlineData(200, "null")]
    [InlineData(204, "")]
    public async Task InvalidSuccessBody_IsTypedFailure(int status, string payload)
    {
        using HttpClient client = Client((_, _) => Task.FromResult(Response(status, payload)));
        HttpResult<string> result = await new Probe(client).Get<string>();

        Assert.Equal("http.invalid_response", Assert.Single(result.Outcome.Errors).Code);
        Assert.Equal((HttpStatusCode)status, result.Response.StatusCode);
    }

    [Fact]
    public async Task Request_UsesSnapshotOptionsPerRequestHeadersAndDisposesContent()
    {
        HttpRequestMessage? captured = null;
        TrackedContent content = new("{\"value\":7}", true);
        using HttpClient client = Client(async (request, token) =>
        {
            captured = request;
            Assert.Equal("https://example.test/prefix/items", request.RequestUri!.AbsoluteUri);
            Assert.Equal("\"old\"", Assert.Single(request.Headers.IfMatch).ToString());
            Assert.Equal("application/json", Assert.Single(request.Headers.Accept).MediaType);
            Assert.Equal("{\"name\":\"sent\"}", await request.Content!.ReadAsStringAsync(token));
            HttpResponseMessage response = new(HttpStatusCode.OK) { Content = content };
            response.Headers.ETag = new EntityTagHeaderValue("\"new\"");
            response.Headers.Location = new Uri("items/7", UriKind.Relative);
            return response;
        });
        HttpServiceOptions options = new();
        Probe service = new(client, options);
        options.JsonOptions.PropertyNameCaseInsensitive = false;
        options.MaximumResponseBytes = 1;
        options.Timeout = TimeSpan.Zero;

        HttpResult<Payload> result = await service.Post<Payload>(new { Name = "sent" }, "\"old\"");

        Assert.Equal(7, result.Outcome.Value.Value);
        Assert.Equal("\"new\"", result.Response.ETag);
        Assert.Equal("items/7", result.Response.Location?.OriginalString);
        Assert.True(content.Disposed);
        Assert.Empty(client.DefaultRequestHeaders.IfMatch);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => captured!.Content!.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResponseSizeLimit_HandlesKnownAndStreamingBodies(bool knownLength)
    {
        TrackedContent content = new("123456", knownLength);
        using HttpClient client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        Probe service = new(client, new HttpServiceOptions { MaximumResponseBytes = 5 });

        HttpResult<int> result = await service.Get<int>();

        Assert.Equal("http.invalid_response", Assert.Single(result.Outcome.Errors).Code);
        Assert.True(content.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NetworkFailure_IsSanitizedUnlessCallerCancelled(bool cancel)
    {
        using CancellationTokenSource source = new();
        using HttpClient client = Client((_, _) =>
        {
            if (cancel)
                source.Cancel();
            throw new HttpRequestException("private URL and credentials");
        });
        Probe service = new(client);

        if (cancel)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.Get<int>(source.Token));
        else
        {
            HttpResult<int> result = await service.Get<int>(source.Token);
            Assert.Null(result.Response.StatusCode);
            Assert.Equal(ErrorKind.Unavailable, Assert.Single(result.Outcome.Errors).Kind);
            Assert.DoesNotContain("credentials", result.Outcome.Errors[0].Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StreamFailure_IsSanitizedUnlessCallerCancelled(bool cancel)
    {
        using CancellationTokenSource source = new();
        using HttpClient client = Client((_, _) =>
        {
            if (cancel)
                source.Cancel();
            throw new IOException("private file path");
        });
        Probe service = new(client);

        if (cancel)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.Get<int>(source.Token));
        else
        {
            HttpResult<int> result = await service.Get<int>(source.Token);
            Assert.Equal(ErrorKind.Unavailable, Assert.Single(result.Outcome.Errors).Kind);
            Assert.DoesNotContain("private", result.Outcome.Errors[0].Message);
        }
    }

    [Fact]
    public async Task Timeout_IsTypedWhileCallerCancellationPropagates()
    {
        using HttpClient client = Client(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException();
        });
        Probe service = new(client, new HttpServiceOptions { Timeout = TimeSpan.FromMilliseconds(20) });
        HttpResult<int> timedOut = await service.Get<int>();
        Assert.Equal(ErrorKind.Timeout, Assert.Single(timedOut.Outcome.Errors).Kind);

        using CancellationTokenSource source = new();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.Get<int>(source.Token));
    }

    [Fact]
    public async Task Timeout_CoversBodyReadingAfterHeaders()
    {
        using HttpClient client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new WaitingStream())
        }));
        Probe service = new(client, new HttpServiceOptions { Timeout = TimeSpan.FromMilliseconds(100) });

        HttpResult<int> result = await service.Get<int>();

        Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
        Assert.Equal(ErrorKind.Timeout, Assert.Single(result.Outcome.Errors).Kind);
    }

    [Fact]
    public async Task NoContentOperations_PreserveMetadataAndFailures()
    {
        using HttpClient success = Client((_, _) => Task.FromResult(Response(204, "")));
        HttpResult result = await new Probe(success).Delete();
        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal(HttpStatusCode.NoContent, result.Response.StatusCode);

        using HttpClient failure = Client((_, _) => Task.FromResult(Response(500, "")));
        Assert.True((await new Probe(failure).Delete()).Outcome.IsFailure);
    }

    [Fact]
    public async Task ProgrammingErrors_AreNotConvertedToRemoteFailures()
    {
        using HttpClient client = Client((_, _) => throw new InvalidOperationException("bug"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new Probe(client).Get<int>());
    }

    [Theory]
    [InlineData("https://evil.test/items")]
    [InlineData("/items")]
    [InlineData("items\\elsewhere")]
    [InlineData("")]
    public async Task InvalidPaths_AreRejectedBeforeSending(string path)
    {
        using HttpClient client = Client((_, _) => throw new InvalidOperationException("Must not send"));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new Probe(client).GetPath(path));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("W/\"weak\"")]
    [InlineData("\"one\", \"two\"")]
    [InlineData("unquoted")]
    public async Task InvalidPreconditions_AreRejectedBeforeSending(string tag)
    {
        using HttpClient client = Client((_, _) => throw new InvalidOperationException("Must not send"));
        Exception? error = await Record.ExceptionAsync(() => new Probe(client).Post<int>(new { }, tag));
        Assert.True(error is ArgumentException or FormatException);
    }

    [Fact]
    public async Task DependencyAndOptionGuards_AreExplicit()
    {
        using HttpClient client = Client((_, _) => Task.FromResult(Response(200, "1")));
        Assert.Throws<ArgumentNullException>(() => new Probe(null!));
        Assert.Throws<ArgumentNullException>(() => new Probe(client, new HttpServiceOptions { JsonOptions = null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Probe(client, new HttpServiceOptions { MaximumResponseBytes = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Probe(client, new HttpServiceOptions { Timeout = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Probe(client, new HttpServiceOptions { Timeout = TimeSpan.FromDays(60) }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new Probe(client).NullMethod());
    }

    private static HttpClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        => new(new Handler(send)) { BaseAddress = new Uri("https://example.test/prefix/") };

    private static HttpResponseMessage Response(int status, string body)
        => new((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public sealed record Payload(int Value);

    private sealed class Probe(HttpClient client, HttpServiceOptions? options = null) : HttpServiceBase(client, options)
    {
        public Task<HttpResult<T>> Get<T>(CancellationToken token = default)
            => SendAsync<T>(HttpMethod.Get, "items", cancellationToken: token);

        public Task<HttpResult<T>> Post<T>(object model, string? tag)
            => SendAsync<T>(HttpMethod.Post, "items", model, tag);

        public Task<HttpResult> Delete()
            => SendAsync(HttpMethod.Delete, "items");

        public Task<HttpResult<int>> GetPath(string path)
            => SendAsync<int>(HttpMethod.Get, path);

        public Task<HttpResult<int>> NullMethod()
            => SendAsync<int>(null!, "items");
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }

    private sealed class TrackedContent(string value, bool knownLength) : HttpContent
    {
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(value);

        public bool Disposed { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => stream.WriteAsync(_bytes).AsTask();

        protected override Task<Stream> CreateContentReadStreamAsync()
            => Task.FromResult<Stream>(new MemoryStream(_bytes));

        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return knownLength;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class WaitingStream : MemoryStream
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
