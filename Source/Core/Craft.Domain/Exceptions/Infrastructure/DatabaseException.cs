using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>A database error occurred. HTTP 500.</summary>
public class DatabaseException : CraftException
{
    public DatabaseException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "A database error occurred", HttpStatusCode.InternalServerError, innerException, errors) { }

    public DatabaseException(string operation, string details)
        : this($"Database error during {operation}: {details}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
    }
}