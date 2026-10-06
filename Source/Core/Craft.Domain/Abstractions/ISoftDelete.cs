namespace Craft.Domain.Abstractions;

/// <summary>
/// Defines a contract for entities that support soft deletion. Soft-deleted entities are marked as
/// deleted but not physically removed from the database.
/// </summary>
public interface ISoftDelete
{
    #region Public Fields

    /// <summary>
    /// The name of the database column for the IsDeleted property.
    /// </summary>
    public const string ColumnName = "IsDeleted";

    #endregion Public Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets a value indicating whether the entity is soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Marks the entity as deleted by setting IsDeleted to true.
    /// </summary>
    public void Delete() => IsDeleted = true;

    /// <summary>
    /// Restores a soft-deleted entity by setting IsDeleted to false.
    /// </summary>
    public void Restore() => IsDeleted = false;

    #endregion Public Methods
}
