namespace Craft.Domain.Exceptions;

/// <summary>An external service error occurred. HTTP 502.</summary>
public class ExternalServiceException : BadGatewayException
{
    public ExternalServiceException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "An external service error occurred", innerException, errors) { }

    public ExternalServiceException(string serviceName, string errorDetails)
        : this($"External service \"{serviceName}\" error: {errorDetails}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
    }

    public ExternalServiceException(string serviceName, int statusCode, string errorDetails)
        : this($"External service \"{serviceName}\" returned status {statusCode}: {errorDetails}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
    }
}