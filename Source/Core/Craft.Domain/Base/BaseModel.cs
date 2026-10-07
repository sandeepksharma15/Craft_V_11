using Craft.Domain.Abstractions;

namespace Craft.Domain.Base;

/// <summary>Mutable model for data transfer without a required input or output direction.</summary>
public abstract class BaseModel<TKey> : DataObject<TKey>;

/// <summary>Data model with the configured default identifier.</summary>
public abstract class BaseModel : BaseModel<KeyType>, IDataObject;
