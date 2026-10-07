using System.Net;

namespace Craft.Domain.Exceptions.Http;

/// <summary>Bad gateway - invalid response from upstream server. HTTP 502.</summary>
public class BadGatewayException : CraftException
{
    public BadGatewayException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "Bad gateway - invalid response from upstream server", HttpStatusCode.BadGateway, innerException, errors) { }

    public BadGatewayException(string upstreamService, string reason)
        : this($"Bad gateway from \"{upstreamService}\": {reason}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(upstreamService);
    }
}