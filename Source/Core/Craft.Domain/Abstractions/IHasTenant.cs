namespace Craft.Domain.Abstractions;

/// <summary>
/// Defines a contract for entities that belong to a specific tenant in a multi-tenant system.
/// </summary>
/// <typeparam name="TKey"> The type of the tenant identifier. </typeparam>
public interface IHasTenant<TKey>
{
    #region Public Fields

    /// <summary>
    /// The name of the database column for the TenantId property.
    /// </summary>
    public const string ColumnName = "TenantId";

    #endregion Public Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    TKey TenantId { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    /// <returns> The tenant identifier. </returns>
    TKey GetTenantId() => TenantId;

    /// <summary>
    /// Determines whether the tenant identifier is set to a non-default value.
    /// </summary>
    /// <returns> True if the tenant identifier is set; otherwise, false. </returns>
    bool IsTenantIdSet() => TenantId != null && !EqualityComparer<TKey>.Default.Equals(TenantId, default);

    /// <summary>
    /// Sets the tenant identifier.
    /// </summary>
    /// <param name="tenantId"> The tenant identifier to set. </param>
    void SetTenantId(TKey tenantId) => TenantId = tenantId;

    #endregion Public Methods
}

/// <summary>
/// Defines a contract for entities that belong to a tenant with the default KeyType identifier.
/// </summary>
public interface IHasTenant : IHasTenant<long>;
