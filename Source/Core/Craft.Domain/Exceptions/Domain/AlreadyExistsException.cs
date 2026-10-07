namespace Craft.Domain.Exceptions;

/// <summary>This resource already exists. HTTP 409.</summary>
public class AlreadyExistsException : ConflictException
{
    public AlreadyExistsException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "This resource already exists", innerException, errors) { }

    public AlreadyExistsException(string entityName, object key)
        : this($"Entity \"{entityName}\" ({key}) already exists")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(key);
    }
}