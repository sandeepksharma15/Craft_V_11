using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Authentication is required. HTTP 401.</summary>
public class UnauthorizedException : CraftException
{
    public UnauthorizedException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "Authentication is required", HttpStatusCode.Unauthorized, innerException, errors) { }
}