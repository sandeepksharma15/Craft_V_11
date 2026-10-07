using System.Net;

namespace Craft.Domain.Exceptions.Http;

/// <summary>
/// The request is invalid. HTTP 400.
/// </summary>
public class BadRequestException : CraftException
{
    #region Public Constructors

    public BadRequestException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The request is invalid", HttpStatusCode.BadRequest, innerException, errors) { }

    #endregion Public Constructors
}
