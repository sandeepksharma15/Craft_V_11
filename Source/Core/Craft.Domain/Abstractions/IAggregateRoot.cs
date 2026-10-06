namespace Craft.Domain.Abstractions;

/// <summary>Optional marker for an entity that controls changes within a consistency boundary.</summary>
/// <remarks>Does not enforce transactions, repository restrictions, or domain event handling.</remarks>
public interface IAggregateRoot<TKey> : IEntity<TKey>;

/// <summary>Aggregate root marker with the configured default identifier.</summary>
public interface IAggregateRoot : IAggregateRoot<KeyType>, IEntity;
