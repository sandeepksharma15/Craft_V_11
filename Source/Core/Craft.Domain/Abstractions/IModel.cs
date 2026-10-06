namespace Craft.Domain.Abstractions;

/// <summary>Defines a base contract for data transfer models with a strongly-typed identifier.</summary>
public interface IModel<TKey> : IHasId<TKey>;

/// <summary>Defines a base contract for data transfer models with the default long identifier.</summary>
public interface IModel : IModel<long>;
