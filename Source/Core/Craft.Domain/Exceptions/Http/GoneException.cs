using System.Net;

namespace Craft.Domain.Exceptions;

/// <summary>The requested resource is no longer available. HTTP 410.</summary>
public class GoneException : CraftException
{
    public GoneException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The requested resource is no longer available", HttpStatusCode.Gone, innerException, errors) { }

    public GoneException(string entityName, object key)
        : this($"Entity \"{entityName}\" ({key}) has been permanently deleted and is no longer available.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(key);
    }

    public GoneException(string entityName, object key, DateTime deletedAt)
        : this($"Entity \"{entityName}\" ({key}) was permanently deleted on {deletedAt:yyyy-MM-dd HH:mm:ss} UTC and is no longer available.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        if (deletedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Deletion time must be UTC.", nameof(deletedAt));
        ArgumentNullException.ThrowIfNull(key);
    }
}