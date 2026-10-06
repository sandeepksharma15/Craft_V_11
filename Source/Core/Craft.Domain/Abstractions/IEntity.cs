namespace Craft.Domain.Abstractions;

/// <summary>Defines a base contract for domain entities with a strongly-typed identifier.</summary>
public interface IEntity<TKey> : IHasId<TKey>;

/// <summary>Defines a base contract for domain entities with the configured default identifier.</summary>
public interface IEntity : IEntity<KeyType>;
