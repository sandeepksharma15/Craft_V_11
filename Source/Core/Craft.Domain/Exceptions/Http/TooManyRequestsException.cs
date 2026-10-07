using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Too many requests - rate limit exceeded. HTTP 429.</summary>
public class TooManyRequestsException : CraftException
{
    public TooManyRequestsException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "Too many requests - rate limit exceeded", HttpStatusCode.TooManyRequests, innerException, errors) { }

    public TimeSpan? RetryAfter { get; }

    public TooManyRequestsException(int limit, string period)
        : this($"Rate limit exceeded: {limit} requests per {period}")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentException.ThrowIfNullOrWhiteSpace(period);
    }

    public TooManyRequestsException(int retryAfterSeconds)
        : this($"Too many requests. Retry after {retryAfterSeconds} seconds")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retryAfterSeconds);
        RetryAfter = TimeSpan.FromSeconds(retryAfterSeconds);
    }
}