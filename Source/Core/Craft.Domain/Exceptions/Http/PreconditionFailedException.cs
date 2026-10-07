using System.Net;

namespace Craft.Domain.Exceptions.Http;

/// <summary>
/// Precondition failed for the request. HTTP 412.
/// </summary>
public class PreconditionFailedException : CraftException
{
    #region Public Constructors

    public PreconditionFailedException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "Precondition failed for the request", HttpStatusCode.PreconditionFailed, innerException, errors) { }

    public PreconditionFailedException(string headerName, string expectedValue, string actualValue)
        : this($"Precondition header '{headerName}' failed. Expected: '{expectedValue}', Actual: '{actualValue}'")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);
    }

    #endregion Public Constructors
}
