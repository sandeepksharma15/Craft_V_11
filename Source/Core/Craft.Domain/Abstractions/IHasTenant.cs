namespace Craft.Domain.Abstractions;

/// <summary>Defines a contract for entities that belong to a specific tenant in a multi-tenant system.</summary>
public interface IHasTenant<TKey>
{
    /// <summary>
    /// The name of the database column for the TenantId property.
    /// </summary>
    public const string ColumnName = "TenantId";

    TKey TenantId { get; set; }

    TKey GetTenantId() => TenantId;

    /// <summary>
    /// Determines whether the tenant identifier is set to a non-default value.
    /// </summary>
    bool IsTenantIdSet() => TenantId != null && !EqualityComparer<TKey>.Default.Equals(TenantId, default);

    void SetTenantId(TKey tenantId) => TenantId = tenantId;
}

/// <summary>Defines a contract for entities that belong to a tenant with the default long identifier.</summary>
public interface IHasTenant : IHasTenant<long>;
