namespace Craft.Domain.Abstractions;

/// <summary>Defines the contract for data transfer objects used in API communication.</summary>
public interface IDataTransferObject<TKey> : IModel<TKey>, IHasConcurrency, ISoftDelete;

/// <summary>Defines the contract for data transfer objects with the configured default identifier.</summary>
public interface IDataTransferObject : IDataTransferObject<KeyType>, IModel;
