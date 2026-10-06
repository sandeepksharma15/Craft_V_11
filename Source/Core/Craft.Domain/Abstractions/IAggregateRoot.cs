namespace Craft.Domain.Abstractions;

/// <summary>
/// Marker interface for aggregate roots with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TKey"> The type of the aggregate root identifier. </typeparam>
/// <remarks>
/// <para>
/// An aggregate root is the entry point to an aggregate - a cluster of domain objects that can be
/// treated as a single unit for data changes.
/// </para>
/// </remarks>
public interface IAggregateRoot<TKey> : IEntity<TKey>;

/// <summary>
/// Marker interface for aggregate roots with the default KeyType identifier.
/// </summary>
public interface IAggregateRoot : IAggregateRoot<KeyType>, IEntity;
