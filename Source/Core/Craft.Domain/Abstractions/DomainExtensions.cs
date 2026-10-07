using System.Diagnostics.CodeAnalysis;

namespace Craft.Domain.Abstractions;

public static class DomainExtensions
{
    extension<TKey>([NotNullWhen(false)] IHasId<TKey>? entity)
    {
        /// <summary>Checks for a missing object or default ID, not whether it has been persisted.</summary>
        public bool IsNullOrDefault()
            => entity is null || EqualityComparer<TKey>.Default.Equals(entity.Id, default);
    }

    extension<TKey>([NotNullWhen(true)] IHasTenant<TKey>? entity)
    {
        /// <summary>Matches an assigned tenant ID; null objects and default IDs never match.</summary>
        public bool BelongsToTenant(TKey tenantId)
            => entity is not null
                && !EqualityComparer<TKey>.Default.Equals(tenantId, default)
                && EqualityComparer<TKey>.Default.Equals(entity.TenantId, tenantId);
    }

    extension<TKey>([NotNullWhen(true)] IHasUser<TKey>? entity)
    {
        /// <summary>Matches an assigned user ID; null objects and default IDs never match.</summary>
        public bool BelongsToUser(TKey userId)
            => entity is not null
                && !EqualityComparer<TKey>.Default.Equals(userId, default)
                && EqualityComparer<TKey>.Default.Equals(entity.UserId, userId);
    }
}