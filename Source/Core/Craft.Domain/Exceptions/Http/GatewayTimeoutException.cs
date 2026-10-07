using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Gateway timeout - no response from upstream server. HTTP 504.</summary>
public class GatewayTimeoutException : CraftException
{
    public GatewayTimeoutException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "Gateway timeout - no response from upstream server", HttpStatusCode.GatewayTimeout, innerException, errors) { }

    public GatewayTimeoutException(string upstreamService, int timeoutSeconds)
        : this($"Gateway timeout from \"{upstreamService}\" after {timeoutSeconds} seconds")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(upstreamService);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutSeconds);
    }
}