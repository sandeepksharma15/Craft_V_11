# Craft.Auditing

Opt-in EF Core auditing for .NET 11. Business changes and their audit rows commit in the **same relational database transaction**. Capture never changes business entity state or performs soft deletion.

## Setup

Implement `IAuditContext.GetIdentity()` using trusted application identity. IDs are strings so user/tenant key types are not coupled to the audit schema. A save uses one identity; use separate contexts for different tenants.

```csharp
services.AddScoped<IAuditContext, ApplicationAuditContext>(); // your implementation
services.AddCraftAuditing().Configure(options =>
{
    options.RetentionPeriod = TimeSpan.FromDays(90); // null disables retention
});
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, AuditTrailFactory audits)
    : AuditedDbContext(options, audits)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // required audit mapping
        modelBuilder.Entity<Customer>();
    }
}

[Audit]
public class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }
    [DoNotAudit] public string? PasswordHash { get; set; }
    [AuditRedact] public string? PersonalIdentifier { get; set; }
}
```

Consumers use normal `SaveChanges()` / `SaveChangesAsync(token)`. Returned counts cover business writes, excluding audit rows. For a different audit table/schema, call `modelBuilder.ConfigureAuditing("History", "audit")` after the base mapping. Create an EF migration before deployment.

## Capture rules

- `[Audit]` on an entity includes its mapped scalar properties; on an individual property it includes only that property. Attributes are inherited. `[DoNotAudit]` always wins; excluding an entity excludes it entirely.
- Primary keys are always recorded for identity, including shadow/composite/generated keys. Value payload exclusions do not remove identity keys. Navigation graphs are never serialized; foreign-key scalar changes are captured normally. Owned entities require their own opt-in.
- `[AuditRedact]` replaces values with configurable `RedactionText`. `ChangedProperties` still records the change; masked old/new values cannot reveal whether the original values were equal.
- Null values remain null. Updates contain only changed, included properties. Use `GetDifferences()` for JSON-aware per-entry comparisons; these deltas are not full historical snapshots. Corrupt JSON raises `JsonException` rather than silently returning empty data.
- Repository-managed `ISoftDelete.IsDeleted` transitions are classified as `SoftDeleted` / `Restored`. `Remove` remains a physical delete. Already-deleted entities with other changes produce normal updates.
- Actor, tenant, correlation, UTC timestamps and optional archive eligibility are recorded. Inject `TimeProvider` for clock control; options are snapshotted per factory scope.

## Queries and retention

```csharp
var history = await db.AuditTrails.AsNoTracking()
    .ForEntity(db.Model.FindEntityType(typeof(Customer))!.Name,
        new Dictionary<string, object?> { ["Id"] = customerId }, tenantId)
    .MostRecentFirst().Take(100).ToListAsync(cancellationToken);

var count = await AuditArchiver.ArchiveDueAsync(db, clock.GetUtcNow().UtcDateTime,
    batchSize: 500, cancellationToken);
```

Entity lookup uses canonical JSON plus an indexed SHA-256 fingerprint and exact tenant matching (`null` matches only null). Pass key names and CLR values from the EF model. `InPeriod` accepts UTC bounds, lower-inclusive and upper-exclusive. Recent ordering includes ID as a deterministic tie-breaker.

Archival marks one bounded batch with a set-based update. It does not move or delete data, does not create audit rows, and does not refresh tracked audit instances. Schedule repeated calls in your job runner; use no-tracking queries or a fresh context afterward. Archival is an administrative operation across the rows visible to that context.

## Transaction contract

- The coordinator saves business changes, resolves final keys, writes audits, then commits. Audit failure rolls back business writes. Existing transactions require savepoints; the caller owns the final commit/rollback. SQL Server MARS must not disable savepoints for caller-owned transactions.
- `acceptAllChangesOnSuccess: false` is explicitly unsupported. After a failed save or outer rollback, discard the context and mutated entity instances; recreate them for any retry. A database commit acknowledgment failure has an unknown outcome—use application-level idempotency.
- With retrying providers, execute the entire transaction through the provider's execution strategy, creating a **fresh context and entity graph per attempt**. Do not retry only the audit insert.
- Load entities before updates/deletes when original values matter. Detached stubs cannot supply historical values. Bulk operations/raw SQL, database-trigger side effects and computed columns not marked modified bypass normal tracked capture. Generated keys and created scalar values are finalized after the business save.
- This protects consistency, not against privileged database tampering. Enforce authorization, tenant filters, audit-reader permissions and database retention policy in the application. Audit payloads can contain sensitive data: explicitly exclude or redact it.

This replaces the old entity factories, mutable static configuration, reflection scans, JSON substring queries and background archival service. Audit records no longer inherit mutable business-entity/soft-delete semantics. Existing consumers and schemas require migration; old payloads are not silently reinterpreted.
