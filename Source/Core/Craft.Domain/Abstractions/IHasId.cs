namespace Craft.Domain.Abstractions;

/// <summary>
/// Defines a contract for entities that have a strongly-typed identifier.
/// </summary>
/// <typeparam name="TKey"> The type of the identifier. </typeparam>
public interface IHasId<TKey>
{
    #region Public Fields

    /// <summary>
    /// The name of the database column for the Id property.
    /// </summary>
    public const string ColumnName = "Id";

    #endregion Public Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    TKey Id { get; set; }

    /// <summary>
    /// Gets a value indicating whether the entity is new (has the default identifier value).
    /// </summary>
    bool IsNew => Id!.Equals(default(TKey));

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    /// <returns> The entity identifier. </returns>
    TKey GetId() => Id;

    /// <summary>
    /// Sets the entity identifier.
    /// </summary>
    /// <param name="id"> The identifier to set. </param>
    void SetId(TKey id) => Id = id;

    #endregion Public Methods
}

/// <summary>
/// Defines a contract for entities that have the default KeyType identifier.
/// </summary>
public interface IHasId : IHasId<KeyType>;
