namespace Craft.Domain.Abstractions;

/// <summary>Defines a contract for entities that have a strongly-typed identifier.</summary>
public interface IHasId<TKey>
{
    /// <summary>
    /// The name of the database column for the Id property.
    /// </summary>
    public const string ColumnName = "Id";

    TKey Id { get; set; }

    /// <summary>
    /// Gets a value indicating whether the entity is new (has the default identifier value).
    /// </summary>
    bool IsNew => EqualityComparer<TKey>.Default.Equals(Id, default);

    TKey GetId() => Id;

    void SetId(TKey id) => Id = id;
}

/// <summary>Defines a contract for entities that have the configured default identifier.</summary>
public interface IHasId : IHasId<KeyType>;
