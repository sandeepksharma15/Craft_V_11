using System.Net;

namespace Craft.Domain.Exceptions.Http;

/// <summary>
/// The request could not be processed. HTTP 422.
/// </summary>
public class UnprocessableEntityException : CraftException
{
    #region Public Constructors

    public UnprocessableEntityException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The request could not be processed", HttpStatusCode.UnprocessableEntity, innerException, errors) { }

    #endregion Public Constructors
}
