using Craft.Domain.Exceptions.Domain;
using Craft.Domain.Exceptions.Http;

namespace Craft.Domain.Exceptions;

/// <summary>
/// JSON-serializable error data. Diagnostic chains are bounded to 16 exceptions.
/// </summary>
public sealed record ExceptionInfo
{
    public string? ExceptionType { get; init; }
    public string? Message { get; init; }
    public int? StatusCode { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? ValidationErrors { get; init; }
    public TimeSpan? RetryAfter { get; init; }
    public string? StackTrace { get; init; }
    public ExceptionInfo? InnerException { get; init; }

    internal static ExceptionInfo FromException(CraftException exception, bool includeDetails)
    {
        if (includeDetails)
            return CreateDiagnosticInfo(exception);

        bool serverError = exception.StatusCodeValue >= 500;

        return new ExceptionInfo
        {
            Message = serverError ? "An unexpected error occurred." : exception.Message,
            StatusCode = exception.StatusCodeValue,
            Errors = !serverError && exception.Errors.Count > 0 ? exception.Errors : null,
            ValidationErrors = (exception as ModelValidationException)?.ValidationErrors,
            RetryAfter = (exception as TooManyRequestsException)?.RetryAfter
        };
    }

    private static ExceptionInfo CreateDiagnosticInfo(Exception exception, int depth = 0)
    {
        CraftException? craftException = exception as CraftException;

        return new ExceptionInfo
        {
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StatusCode = craftException?.StatusCodeValue,
            Errors = craftException?.Errors.Count > 0 ? craftException.Errors : null,
            ValidationErrors = (exception as ModelValidationException)?.ValidationErrors,
            RetryAfter = (exception as TooManyRequestsException)?.RetryAfter,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException is not null && depth < 15
                ? CreateDiagnosticInfo(exception.InnerException, depth + 1)
                : null
        };
    }
}
