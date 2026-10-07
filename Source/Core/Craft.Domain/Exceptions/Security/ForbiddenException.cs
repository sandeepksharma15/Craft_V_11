using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Access forbidden. HTTP 403.</summary>
public class ForbiddenException : CraftException
{
    public ForbiddenException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "Access forbidden", HttpStatusCode.Forbidden, innerException, errors) { }
}