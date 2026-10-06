namespace Craft.Domain.Abstractions;

/// <summary>Contract for identified data objects with concurrency and soft-delete state.</summary>
public interface IDataObject<TKey> : IModel<TKey>, IHasConcurrency, ISoftDelete;

/// <summary>Data object contract with the configured default identifier.</summary>
public interface IDataObject : IDataObject<KeyType>, IModel;
