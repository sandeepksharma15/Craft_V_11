using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Craft.Core.Results;

namespace Craft.HttpServices;

public sealed record HttpResponseInfo(HttpStatusCode? StatusCode = null, string? ETag = null,
    Uri? Location = null, HttpProblem? Problem = null);

public sealed record HttpResult(Result Outcome, HttpResponseInfo Response);

public sealed record HttpResult<T>(Result<T> Outcome, HttpResponseInfo Response)
{
    public HttpResult<TResult> Map<TResult>(Func<T, TResult> map)
        => new(Outcome.Map(map), Response);
}

/// <summary>Remote problem data is untrusted display data, not HTML or a navigation target.</summary>
public sealed class HttpProblem
{
    public string? Type { get; init; }

    public string? Title { get; init; }

    public int? Status { get; init; }

    public string? Detail { get; init; }

    public string? Instance { get; init; }

    public Dictionary<string, string[]>? Errors { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; init; }
}
