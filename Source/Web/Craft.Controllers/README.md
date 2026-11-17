# Craft.Controllers

ASP.NET Core 11 controller bases over Craft.Repositories. Explicit request/response DTO mapping, authenticated access, bounded atomic batches, and optimistic concurrency. No entity serialization, automatic mapping, database-error text, or client-controlled commits.

```csharp
services.AddCraftRepositories<AppDbContext>(r => r.Add<Customer>());
services.AddCraftControllers();
// Configure authentication and authorization using the application's scheme/policies.
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

public sealed class CustomersController(IRepository<Customer> repository, IOptions<RepositoryOptions> options)
    : SoftDeleteEntityController<Customer, CustomerDto, CreateCustomer, UpdateCustomer>(repository, options)
{
    protected override Customer CreateEntity(CreateCustomer model) => new() { Name = model.Name };
    protected override void ApplyUpdate(UpdateCustomer model, Customer entity) => entity.Name = model.Name;
    protected override CustomerDto MapResponse(Customer entity)
        => new(entity.Id, entity.Name, ConcurrencyTags.Get(entity));
}
```

Use `EntityReadController<TEntity,TResponse>` for reads, `EntityController<TEntity,TResponse,TCreate,TUpdate>` for CRUD, and `SoftDeleteEntityController` to additionally expose restoration. Append `TKey` for an explicit key type. DTOs need no Craft interfaces. Apply DataAnnotations or application validators to request DTOs. Mapping must assign only allowed business fields; never map IDs, tenant IDs, deletion flags, or concurrency stamps from input. Configure Craft.Data's concurrency and soft-delete features for the corresponding entity interfaces.

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

`IHasConcurrency` entities require one strong, original ETag in `If-Match` for update/delete/restore: missing = 428, malformed/multiple/wildcard = 400, weak/stale = 412. A database race after validation also returns 412. Successful deletion returns the new ETag; authorized clients can retrieve a deleted row's current ETag before restoration. Responses are no-store; these tags are entity write-precondition tokens, not support for conditional GET caching. Detail representations must not be used as independently versioned aggregates without an application versioning policy.

Batches use create DTO arrays, `EntityUpdate<TKey,TUpdate>` (`id`, `model`, `ifMatch`) or `EntityReference<TKey>` (`id`, `ifMatch`). Include `ConcurrencyTags.Get(entity)` in response DTOs when batch clients need per-item tags. Each batch ID currently uses one filtered tracked lookup; choose modest batch limits for latency-sensitive APIs. All IDs, access checks and preconditions are validated before mapping updates or staging deletes; one repository range operation commits the batch. Failures discard the request scope. Empty, duplicate-ID, null-item and oversized batches are rejected. Limits use RepositoryOptions. Hosts must also set request-body limits, rate limits and timeouts before JSON binding.

The bases require authentication; hosts choose schemes and endpoint policies. Override `AuthorizeEntityAsync` for resource checks on individual reads and writes (including batches). Collection/count/any visibility must be enforced in the repository/query filters; per-entity authorization cannot safely filter a page after counting. Tenant identity must come from trusted host context. Cookie-authenticated writes require host antiforgery protection. `[AllowAnonymous]` is an explicit application choice.

Call `AddCraftControllers` and `UseExceptionHandler` for safe ProblemDetails. Unexpected failures return 500 with a trace ID; diagnostic exceptions are logged through ILogger (compatible with host Serilog). Request cancellation is not converted into a client validation error. Applications own domain-error translation and business validation; register specialized IExceptionHandler implementations before AddCraftControllers so they run before the fallback. Default DTO 400s use ASP.NET validation ProblemDetails. Keep MVC's normal routing setup; the GET action name is explicit so CreatedAtAction works even when Async suffix conventions change.

No unbounded get-all, permanent-delete endpoint, arbitrary query/filter bypass, database-specific exception parsing, API versioning policy or reflection-based rate-limit registration is shipped here. These belong to application policy or the integrations recorded in TODO.md. Tests exercise actual HTTP routing/model binding/authentication against SQLite with the Craft.Data/audit pipeline.
