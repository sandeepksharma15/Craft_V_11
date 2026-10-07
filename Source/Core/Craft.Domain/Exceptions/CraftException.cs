using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Base for Craft failures with a fixed HTTP error status and an immutable error snapshot.</summary>
public abstract class CraftException : Exception
{
    protected CraftException(string message, HttpStatusCode statusCode,
        Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)statusCode, 400, nameof(statusCode));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)statusCode, 599, nameof(statusCode));

        StatusCode = statusCode;
        Errors = Array.AsReadOnly(errors?.ToArray() ?? []);
    }

    public IReadOnlyList<string> Errors { get; }
    public HttpStatusCode StatusCode { get; }
    public int StatusCodeValue => (int)StatusCode;

    /// <summary>Creates response data; diagnostic details must only be enabled for trusted destinations.</summary>
    public ExceptionInfo ToErrorInfo(bool includeDetails = false)
        => ExceptionInfo.FromException(this, includeDetails);
}