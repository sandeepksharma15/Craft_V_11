namespace Craft.Domain.Abstractions;

/// <summary>Defines a contract for entities that support optimistic concurrency control.</summary>
public interface IHasConcurrency
{
    /// <summary>
    /// The name of the database column for the ConcurrencyStamp property.
    /// </summary>
    public const string ColumnName = "ConcurrencyStamp";

    /// <summary>
    /// The maximum length of the concurrency stamp.
    /// </summary>
    public const int MaxLength = 40;

    string? ConcurrencyStamp { get; set; }

    public string? GetConcurrencyStamp() => ConcurrencyStamp;

    /// <summary>
    /// Determines whether the entity has a concurrency stamp set.
    /// </summary>
    public bool HasConcurrencyStamp() => !string.IsNullOrWhiteSpace(ConcurrencyStamp);

    /// <summary>
    /// Sets the concurrency stamp. If no stamp is provided, generates a new GUID.
    /// </summary>
    public void SetConcurrencyStamp(string? stamp = null)
        => ConcurrencyStamp = stamp ?? Guid.NewGuid().ToString();

    public void ClearConcurrencyStamp() => ConcurrencyStamp = null;
}
