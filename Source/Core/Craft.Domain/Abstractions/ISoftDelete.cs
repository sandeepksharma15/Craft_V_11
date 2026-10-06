namespace Craft.Domain.Abstractions;

/// <summary>
/// Defines a contract for entities that support soft deletion.
/// Soft-deleted entities are marked as deleted but not physically removed from the database.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// The name of the database column for the IsDeleted property.
    /// </summary>
    public const string ColumnName = "IsDeleted";

    bool IsDeleted { get; set; }

    public void Delete() => IsDeleted = true;

    public void Restore() => IsDeleted = false;
}
