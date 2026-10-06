using Craft.Domain.Base;

namespace Craft.Domain.Abstractions;

/// <summary>
/// Defines the contract for data transfer objects used in API communication.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by both <see cref="BaseDto{TKey}" /> and
/// <see cref="BaseVm{TKey}" /> to ensure consistent API contract behavior.
/// </para>
/// </remarks>
/// <typeparam name="TKey"> The type of the identifier. </typeparam>
public interface IDataObject<TKey> : IModel<TKey>, IHasConcurrency, ISoftDelete;

/// <summary>
/// Defines the contract for data transfer objects with the default KeyType identifier.
/// </summary>
public interface IDataObject : IDataObject<long>, IModel;
