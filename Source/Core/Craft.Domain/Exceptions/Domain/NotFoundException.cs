using System.Net;

namespace Craft.Domain.Exceptions.Domain;

/// <summary>
/// The requested resource was not found. HTTP 404.
/// </summary>
public class NotFoundException : CraftException
{
    #region Public Constructors

    public NotFoundException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "The requested resource was not found", HttpStatusCode.NotFound, innerException, errors) { }

    public NotFoundException(string entityName, object key) : this($"Entity \"{entityName}\" ({key}) was not found.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(key);
    }

    #endregion Public Constructors
}
