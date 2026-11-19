# Craft.Controllers

ASP.NET Core 11 controller bases over Craft.Repositories. Explicit request/response DTO mapping, authenticated access, bounded atomic batches, and optimistic concurrency. Mapster handles configured DTO mappings; tracked updates remain an explicit allowlist. No database-error text or client-controlled commits.

```csharp
TypeAdapterConfig mapping = new();
mapping.NewConfig<CreateCustomer, Customer>()
    .IgnoreNonMapped(true).Map(destination => destination.Name, source => source.Name);
mapping.NewConfig<UpdateCustomer, Customer>()
    .IgnoreNonMapped(true).Map(destination => destination.Name, source => source.Name);
mapping.NewConfig<Customer, CustomerDto>()
    .Map(destination => destination.ETag, source => ConcurrencyTags.Get(source));
services.AddSingleton(mapping);
services.AddScoped<IMapper, Mapper>();
services.AddCraftRepositories<AppDbContext>(r => r.Add<Customer>());
services.AddCraftControllers();
// Configure authentication and authorization using the application's scheme/policies.
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

public sealed class CustomersController(IRepository<Customer> repository, IOptions<RepositoryOptions> options, IMapper mapper)
    : SoftDeleteEntityController<Customer, CustomerDto, CreateCustomer, UpdateCustomer>(repository, options, mapper)
{
    protected override void ApplyUpdate(UpdateCustomer model, Customer entity)
        => Mapper.Map(model, entity);
}
```

`IMapper` is MapsterMapper's interface. Register a local TypeAdapterConfig; never mutate global configuration. CreateEntity and MapResponse use the injected mapper by default and remain overridable. ApplyUpdate is deliberately abstract: use an explicitly configured IgnoreNonMapped mapping onto the supplied tracked entity, or assign allowed fields directly. Do not replace the tracked entity.

Use `EntityReadController<TEntity,TResponse>` for reads, `EntityController<TEntity,TResponse,TCreate,TUpdate>` for CRUD, and `SoftDeleteEntityController` to additionally expose restoration. Append `TKey` for an explicit key type. DTOs need no Craft interfaces. Apply DataAnnotations or application validators to request DTOs. Mapping must assign only allowed business fields; never map IDs, tenant IDs, deletion flags, or concurrency stamps from input. Configure Craft.Data's concurrency/versioning and soft-delete features for the corresponding entity interfaces.

| Method | Route under `api/[controller]` | Result |
|---|---|---|
| GET | `?page=1&pageSize=20&includeDetails=false` | `Page<TResponse>` |
| GET | `items/{id}?includeDetails=false` | DTO + ETag, or 404 |
| GET | `count`, `any`, `items/{id}/exists` | Scalar |
| POST | root | 201, DTO, Location, ETag |
| PUT / DELETE | `items/{id}` | 200 DTO / 204 |
| GET / POST | `items/{id}/deleted`, `items/{id}/restore` | Deleted DTO / restored DTO |
| POST / PUT | `batch` | Created / updated DTO array |
| POST | `batch/delete`, `batch/restore` | 204 / restored DTO array |

`IHasConcurrency` entities (or `IHasVersion` entities without a concurrency stamp contract) require one strong, original ETag in `If-Match` for update/delete/restore: missing = 428, malformed/multiple/wildcard = 400, weak/stale = 412. A database race after validation also returns 412. Successful deletion returns the new ETag; authorized clients can retrieve a deleted row's current ETag before restoration. Responses are no-store; these tags are entity write-precondition tokens, not support for conditional GET caching. Detail representations must not be used as independently versioned aggregates without an application versioning policy.

Batches use create DTO arrays, `EntityUpdate<TKey,TUpdate>` (`id`, `model`, `ifMatch`) or `EntityReference<TKey>` (`id`, `ifMatch`). Include `ConcurrencyTags.Get(entity)` in response DTOs when batch clients need per-item tags. Each batch ID currently uses one filtered tracked lookup; choose modest batch limits for latency-sensitive APIs. All IDs, access checks and preconditions are validated before mapping updates or staging deletes; one repository range operation commits the batch. Failures discard the request scope. Empty, duplicate-ID, null-item and oversized batches are rejected. Limits use RepositoryOptions; omitted pageSize defaults to the smaller of 20 and MaximumPageSize. Hosts must also set request-body limits, rate limits and timeouts before JSON binding.

The bases require authentication; hosts choose schemes and endpoint policies. Override `AuthorizeEntityAsync` for resource checks on individual reads and writes (including batches). Collection/count/any visibility must be enforced in the repository/query filters; per-entity authorization cannot safely filter a page after counting. Tenant identity must come from trusted host context. Cookie-authenticated writes require host antiforgery protection. `[AllowAnonymous]` is an explicit application choice.

Call `AddCraftControllers` and `UseExceptionHandler` for safe ProblemDetails. Unexpected failures return 500 with a trace ID; diagnostic exceptions are logged through ILogger (compatible with host Serilog). Request cancellation is not converted into a client validation error. Applications own domain-error translation and business validation; register specialized IExceptionHandler implementations before AddCraftControllers so they run before the fallback. Default DTO 400s use ASP.NET validation ProblemDetails. Keep MVC's normal routing setup; the GET action name is explicit so CreatedAtAction works even when Async suffix conventions change.

No unbounded get-all, permanent-delete endpoint, arbitrary query/filter bypass, database-specific exception parsing, API versioning policy or reflection-based rate-limit registration is shipped here. These belong to application policy or the integrations recorded in TODO.md. Tests exercise actual HTTP routing/model binding/authentication against SQLite with the Craft.Data/audit pipeline.
