using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>A conflict occurred with the current state of the resource. HTTP 409.</summary>
public class ConflictException : CraftException
{
    public ConflictException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "A conflict occurred with the current state of the resource", HttpStatusCode.Conflict, innerException, errors) { }

    public ConflictException(string resourceName, string reason)
        : this($"Conflict with resource \"{resourceName}\": {reason}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
    }
}