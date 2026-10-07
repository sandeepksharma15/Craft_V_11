using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Represents an HTTP error status without a dedicated Craft exception type.</summary>
public class HttpStatusException : CraftException
{
    public HttpStatusException(HttpStatusCode statusCode, string message,
        Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message, statusCode, innerException, errors) { }
}