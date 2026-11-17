# Craft.Repositories

EF Core 11 repositories bound to a Craft context. Public interfaces return materialized results; custom repositories retain protected LINQ access. Read interfaces do not expose commits or tracked loading.

```csharp
services.AddCraftDbContext<AppDbContext>(o => o.UseSqlServer(connectionString),
    f => f.UseSoftDelete().UseConcurrency().UseVersioning().UseAuditing());
services.AddCraftRepositories<AppDbContext>(repositories => repositories
    .Add<Customer>()                         // IEntity with the configured KeyType
    .Add<Order, Guid>(),                     // IEntity<Guid>
    options => options.MaximumPageSize = 100);
```

Inject `IReadRepository<Customer>` for no-tracking reads or `IRepository<Customer>` for writes. Generic key variants are also available. All aliases for a registered repository share one scoped instance/context. The context must already be registered; do not use repositories concurrently or make them singleton. Register each entity/key pair once; multiple context bindings for the same entity require explicit custom interfaces.

## Writes and commits

`AddAsync`, `AddRangeAsync`, `UpdateAsync`, `DeleteAsync`, `RestoreAsync` and their range variants default to **autoSave: true**. Pass `false` to stage several operations and call `SaveChangesAsync` once. Every save commits **all pending changes in the shared context**, including changes from other repositories. Empty batches never commit unrelated work. Returned entities remain tracked; generated IDs and stamps are available after saving.

```csharp
var customer = await customers.LoadForUpdateAsync(id, cancellationToken: token);
if (customer is null) return;
customer.Name = newName;
await customers.UpdateAsync(customer, autoSave: false, cancellationToken: token);
await orders.AddAsync(order, autoSave: false, cancellationToken: token);
await customers.SaveChangesAsync(token);
```

Existing entities must be loaded/tracked before update/delete/restore; detached DTO replacement is deliberately rejected. Modify allowed fields on the tracked entity to preserve original audit values, concurrency tokens and change detection. `UpdateAsync` detects actual scalar changes; it does not mark an entire graph modified. New entities must be detached before adding; EF adds reachable new entities normally. Batch limits and null/duplicate-reference/state validation run before staging; any later staging/save failure requires discarding the unit/context.

Deletion/restoration/concurrency/versions/auditing are owned by Craft.Data. A soft-deletable entity requires `UseSoftDelete()`; otherwise repository deletion fails instead of silently deleting it physically. `LoadDeletedAsync` loads only deleted rows and disables only the named soft-delete filter. Restore with `RestoreAsync`; an explicit `PermanentlyDeleteAsync` is available for loaded, already-soft-deleted rows. Tenant/application filters remain active. No implicit relationship cascade or bulk SQL bypass is introduced.

## Reads and details

`GetAsync`, `GetAllAsync`, `GetPagedListAsync`, `GetCountAsync`, `ExistsAsync` and `AnyAsync` respect context filters. `LoadForUpdateAsync` explicitly tracks; lookup always executes a filtered database query instead of EF Find's local-cache shortcut. A missing ID returns null.

`includeDetails: true` applies a repository's configured default includes. The base implementation adds none. Automatic EF model includes are suppressed in these generic operations; custom methods can select their own policy. No navigation discovery/recursive graph loading occurs.

```csharp
public sealed class CustomerRepository(AppDbContext db, IOptions<RepositoryOptions> options)
    : Repository<Customer, AppDbContext>(db, options)
{
    protected override IQueryable<Customer> ApplyDetails(IQueryable<Customer> query)
        => query.Include(c => c.Addresses);
}
// Register instead of Add<Customer>:
// repositories.AddCustom<Customer, CustomerRepository>();
```

Use `AddCustom<TEntity, TKey, TRepository>()` for other keys. Custom methods may use protected `Context`, `EntityQuery` and `ApplyDetails`, including explicit projections; keep their public results materialized. Application authors own authorization and safe query composition. Arbitrary `IQueryable`, DbSet, DbContext and global filter bypasses are not exposed through repository interfaces. Advanced specifications belong to the subsequent Craft.QuerySpec migration.

## Limits and consistency

Defaults: MaximumPageSize 200, MaximumListSize 1000, MaximumBatchSize 1000. Configure positive limits centrally; each repository snapshots them. Lists exceeding their bound throw rather than silently truncate. Batches enumerate once, bounded to the limit plus one.

Paging uses `Page<T>`, a deterministic unique Id order and checked offsets. Invalid page sizes, arithmetic overflow and offsets beyond EF's Int32 limit are rejected before executing queries. Count and items are separate database observations: total count is at least the observed page's lower bound, but is not an atomic snapshot under concurrent writes. If a consistent snapshot is required, the caller supplies an appropriate provider transaction/isolation level. Deep offset pagination remains expensive; cursor pagination is a future QuerySpec concern.

Only a single mapped `Id` primary key matching TKey is supported. Composite/keyless models require a custom query service. EF exceptions and cancellation propagate without repository retries or exception swallowing; Craft.Data owns transaction/retry constraints. EF logging uses the host's configured ILogger provider (including Serilog); repositories do not log entity contents or duplicate EF command logging.

## Tests and migration

SQLite and SQL Server integration tests cover default/deferred commits, shared units, filters, restore, concurrency, audit rollback, pagination and includes. `Craft.Testing.Repositories.RepositoryStub<TEntity, TKey>` is a separate fail-fast test helper: override configured operations; all others throw. It is not an EF simulator. Use relational tests for persistence behavior.

V10's no-op fakes, async wrappers around context/set getters, recursive include discovery, blanket graph updates and independent repository transactions are removed. `Craft.Data` and `Craft.QuerySpec` follow-up work is recorded in root TODO.md. PostgreSQL awaits the compatible stable EF 11 provider already tracked there.
