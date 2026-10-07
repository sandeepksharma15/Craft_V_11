using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>The request could not be processed. HTTP 422.</summary>
public class UnprocessableEntityException : CraftException
{
    public UnprocessableEntityException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The request could not be processed", HttpStatusCode.UnprocessableEntity, innerException, errors) { }
}