using System.Net;

namespace Craft.Domain.Exceptions.Http;

/// <summary>
/// An internal server error occurred. HTTP 500.
/// </summary>
public class InternalServerException : CraftException
{
    #region Public Constructors

    public InternalServerException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "An internal server error occurred", HttpStatusCode.InternalServerError, innerException, errors) { }

    #endregion Public Constructors
}
