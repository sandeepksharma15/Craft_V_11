using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>The service is temporarily unavailable. HTTP 503.</summary>
public class ServiceUnavailableException : CraftException
{
    public ServiceUnavailableException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The service is temporarily unavailable", HttpStatusCode.ServiceUnavailable, innerException, errors) { }
}