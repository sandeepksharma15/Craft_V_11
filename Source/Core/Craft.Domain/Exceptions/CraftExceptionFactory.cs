using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>Converts failures while preserving their causes and error details.</summary>
public static class CraftExceptionFactory
{
    /// <summary>Maps an error status (400–599); unrecognized error statuses are preserved.</summary>
    public static CraftException FromStatusCode(int statusCode, string message,
        IEnumerable<string>? errors = null, Exception? innerException = null)
        => statusCode switch
        {
            400 => new BadRequestException(message, innerException, errors),
            401 => new UnauthorizedException(message, innerException, errors),
            403 => new ForbiddenException(message, innerException, errors),
            404 => new NotFoundException(message, innerException, errors),
            409 => new ConflictException(message, innerException, errors),
            410 => new GoneException(message, innerException, errors),
            412 => new PreconditionFailedException(message, innerException, errors),
            413 => new PayloadTooLargeException(message, innerException, errors),
            415 => new UnsupportedMediaTypeException(message, innerException, errors),
            422 => new UnprocessableEntityException(message, innerException, errors),
            429 => new TooManyRequestsException(message, innerException, errors),
            500 => new InternalServerException(message, innerException, errors),
            501 => new FeatureNotImplementedException(message, innerException, errors),
            502 => new BadGatewayException(message, innerException, errors),
            503 => new ServiceUnavailableException(message, innerException, errors),
            504 => new GatewayTimeoutException(message, innerException, errors),
            _ => new HttpStatusException((HttpStatusCode)statusCode, message, innerException, errors)
        };

    /// <summary>Preserves cancellation and Craft exceptions; runtime faults are not inferred to be client errors.</summary>
    public static Exception FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            CraftException or OperationCanceledException => exception,
            UnauthorizedAccessException => new ForbiddenException(innerException: exception),
            TimeoutException => new GatewayTimeoutException(innerException: exception),
            NotImplementedException => new FeatureNotImplementedException(innerException: exception),
            _ => new InternalServerException(innerException: exception)
        };
    }
}