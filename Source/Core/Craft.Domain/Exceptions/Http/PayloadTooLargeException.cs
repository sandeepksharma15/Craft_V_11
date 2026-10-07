using System.Net;

namespace Craft.Domain.Exceptions.Http;

/// <summary>
/// The request payload is too large. HTTP 413.
/// </summary>
public class PayloadTooLargeException : CraftException
{
    #region Public Constructors

    public PayloadTooLargeException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The request payload is too large", HttpStatusCode.RequestEntityTooLarge, innerException, errors) { }

    public PayloadTooLargeException(long actualSize, long maxSize)
        : this($"Payload size {actualSize:N0} bytes exceeds maximum allowed size of {maxSize:N0} bytes")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(actualSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxSize);
        if (actualSize <= maxSize)
            throw new ArgumentException("Actual size must exceed the maximum size.", nameof(actualSize));
    }

    public PayloadTooLargeException(string resourceType, long actualSize, long maxSize)
        : this($"{resourceType} size {actualSize:N0} bytes exceeds maximum allowed size of {maxSize:N0} bytes")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentOutOfRangeException.ThrowIfNegative(actualSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxSize);
        if (actualSize <= maxSize)
            throw new ArgumentException("Actual size must exceed the maximum size.", nameof(actualSize));
    }

    #endregion Public Constructors
}
