using Craft.Domain.Abstractions;

namespace Craft.Domain.Base;

/// <summary>Mutable view model with a caller-selected identifier type.</summary>
public abstract class BaseVm<TKey> : DataObject<TKey>;

/// <summary>View model with the configured default identifier.</summary>
public abstract class BaseVm : BaseVm<KeyType>, IDataObject;
