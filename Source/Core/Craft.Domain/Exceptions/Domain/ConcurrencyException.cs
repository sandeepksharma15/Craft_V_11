namespace Craft.Domain.Exceptions;

/// <summary>A concurrency conflict occurred. The record has been modified by another user. HTTP 409.</summary>
public class ConcurrencyException : ConflictException
{
    public ConcurrencyException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "A concurrency conflict occurred. The record has been modified by another user.", innerException, errors) { }

    public ConcurrencyException(string entityName, object key)
        : this($"Concurrency conflict for entity \"{entityName}\" ({key}). The record has been modified by another user.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(key);
    }

    public ConcurrencyException(string entityName, object key, string expectedVersion, string actualVersion)
        : this($"Concurrency conflict for entity \"{entityName}\" ({key}). Expected version: {expectedVersion}, Actual version: {actualVersion}.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(key);
    }
}