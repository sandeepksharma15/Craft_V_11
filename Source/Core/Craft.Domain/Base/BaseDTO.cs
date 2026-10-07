using Craft.Domain.Abstractions;

namespace Craft.Domain.Base;

/// <summary>Mutable data transfer object with a caller-selected identifier type.</summary>
public abstract class BaseDTO<TKey> : DataObject<TKey>;

/// <summary>Data transfer object with the configured default identifier.</summary>
public abstract class BaseDTO : BaseDTO<KeyType>, IDataObject;
