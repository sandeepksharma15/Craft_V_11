namespace Craft.Auditing;

public static class AuditQueryExtensions
{
    extension(IQueryable<AuditTrail> query)
    {
        public IQueryable<AuditTrail> ForEntity(string entityName, IReadOnlyDictionary<string, object?> keys, string? tenantId)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
            var json = AuditKey.Serialize(keys);
            var fingerprint = AuditKey.Fingerprint(json);
            return query.Where(a => a.EntityName == entityName && a.TenantId == tenantId && a.EntityKey == fingerprint && a.KeyValues == json);
        }

        public IQueryable<AuditTrail> InPeriod(DateTime fromUtc, DateTime untilUtc)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (fromUtc.Kind != DateTimeKind.Utc || untilUtc.Kind != DateTimeKind.Utc || fromUtc > untilUtc)
                throw new ArgumentException("Use an ordered UTC range; the upper bound is exclusive.");
            return query.Where(a => a.OccurredAtUtc >= fromUtc && a.OccurredAtUtc < untilUtc);
        }

        public IOrderedQueryable<AuditTrail> MostRecentFirst()
        {
            ArgumentNullException.ThrowIfNull(query);
            return query.OrderByDescending(a => a.OccurredAtUtc).ThenByDescending(a => a.Id);
        }
    }
}