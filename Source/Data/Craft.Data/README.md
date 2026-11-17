# Craft.Data

Composable EF Core 11 persistence. Inherit a context, add DbSets, select features, and use normal EF migrations and SaveChanges.

```csharp
services.AddScoped<ICurrentTenant<string>, ApplicationTenant>(); // trusted, established identity
services.AddScoped<IAuditContext, ApplicationAuditContext>();
services.AddCraftAuditing();
services.AddCraftDbContext<AppDbContext>(
    options => options.UseSqlServer(configuration.GetConnectionString("Application")),
    features => features.UseSoftDelete().UseConcurrency().UseVersioning()
        .UseMultiTenancy<string>().UseAuditing());

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : CraftDbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureModel(ModelBuilder model)
        => model.Entity<Order>().Property(o => o.Name).HasMaxLength(200);
}
```

Reference `Craft.Data`, your EF provider, and only the feature packages needed. Auditing configuration comes from `Craft.Auditing.Persistence`; its service registration is in `Craft.Auditing.Configuration`. Data APIs are in `Craft.Data`. Use `UseNpgsql` instead of `UseSqlServer` for PostgreSQL. Data does not install or guess providers, connection strings, indexes for uniqueness, time-zone conversion, or migration deployment policy.

For Identity, reference `Craft.IdentityEF` and inherit `CraftIdentityDbContext` (or its generic user/role/key variant). Registration and feature configuration stay the same. Register `AddIdentityCore<YourUser>().AddRoles<YourRole>().AddEntityFrameworkStores<AppDbContext>()` normally. Use `UserManager` for passwords and account operations. Identity accounts are global here; tenant-scoped Identity uniqueness/membership and refresh-session persistence are deferred in TODO.md.

## Entity behavior

- `ISoftDelete`: `Remove` changes only `IsDeleted` plus configured stamps, preserving other columns even for attached stubs. `IncludeDeleted()` disables only the soft-delete filter; tenant and application filters remain active. Load a deleted entity, then call `db.Restore(entity)` and save. `db.Persistence.PermanentlyDelete(entity)` explicitly deletes a loaded, already-soft-deleted row.
- `IHasConcurrency`: rotates a concurrency token on insert/update. `IHasVersion`: starts at 1, increments from the original version with overflow checking, and participates in optimistic concurrency. Detached writes must supply original tokens; conflicts surface as `DbUpdateConcurrencyException`.
- `IHasTenant<TKey>`: requires `UseMultiTenancy<TKey>()`. The tenant is snapshotted per context, queries use that context's value, inserts acquire ownership, ownership changes are rejected, and tenant IDs participate in update/delete predicates. Mixed tenant key types are rejected. Relationships between tenant entities must include TenantId in both FK and principal key; configure these explicitly. Tenant IDs that participate in keys are generated from the context tenant before tracking; other tenant IDs are assigned during saving.
- Auditing remains explicitly opted in with `[Audit]`. Its feature captures after all business mutations, then writes finalized audit rows after database-generated values are available, in the same transaction. SaveChanges returns the business write count.

Soft-delete relationships must use `Restrict`/`NoAction`; automatic cascade/set-null behavior is rejected. Owned types cannot independently declare soft deletion or tenancy. Declare these contracts on inheritance roots. Restore does not restore an entire graph. Identity's built-in relationships are made restrictive when soft delete is enabled.

## Lifecycle and boundaries

Contexts are scoped, not thread-safe, and must not be pooled: feature dependencies and tenant identity belong to one scope. Features and registration must be fixed for a context type; model caching differentiates the feature type sequence. A custom feature must be immutable and must not vary mappings by request identity. Add features through `CraftDataBuilder.AddFeature`; validation precedes preparation; preparation stages are ownership, deletion, then enrichment/stamps; capture follows all preparation. Registration order does not change these stages. Completion callbacks may add persistence records but must not call SaveChanges or external services.

All saves require a relational provider and `acceptAllChangesOnSuccess=true`. An existing transaction uses a savepoint (SQL Server MARS is unsupported). Failed writes invalidate the context; outer transaction rollbacks also require disposal. Cancellation is propagated; rollback uses an uncancelled token. When enabling provider retries, execute the **whole unit with a fresh context per attempt** through the provider execution strategy; do not retry the two writes independently. Applications own idempotency and ambiguous commit handling.

`ExecuteUpdate`, `ExecuteDelete`, raw SQL, unrestricted `IgnoreQueryFilters`, and EF `Find` on already-tracked entities bypass parts of this pipeline. Do not expose them to untrusted application paths. Cross-tenant administration uses a separately authorized context. Query filters are defense in depth, not a replacement for database authorization/RLS. Repositories should stage changes and let the caller commit once.

For migrations, supply the same feature configuration in the host or an `IDesignTimeDbContextFactory`. The registered tenant accessor may report no tenant during design time: model generation does not require an active tenant, while runtime tenant queries/writes reject missing identities. Tenant values are parameterized, never embedded in the model. Use `dotnet ef migrations add Initial --project <migrations-project> --startup-project <host>` and deploy migrations explicitly. No automatic production migration/seeding is performed.

Tests use SQLite and SQL Server in Linux CI. PostgreSQL verification is blocked: the available Npgsql EF 11 provider is 11.0.0-rc.3 and pins prerelease EF, incompatible with this stable EF 11 dependency. Do not claim PostgreSQL production support until its compatible provider and relational tests pass. Provider integration connections must target disposable `craft_test_*` databases.
