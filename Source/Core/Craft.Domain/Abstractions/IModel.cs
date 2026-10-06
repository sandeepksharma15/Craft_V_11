namespace Craft.Domain.Abstractions;

/// <summary>Marks identified types eligible for Craft model operations.</summary>
public interface IModel<TKey> : IHasId<TKey>;

/// <summary>Model eligibility contract with the configured default identifier.</summary>
public interface IModel : IModel<KeyType>;
