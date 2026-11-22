# Craft.HttpServices

.NET 11 HTTP clients for Craft.Controllers: read → CRUD → soft-delete inheritance, with overridable methods. `HttpServiceBase` is the smaller base for custom endpoints. The client has no ASP.NET Core, EF Core or repository dependency.

## Setup

Use `HttpClientFactory` and an application-owned Mapster configuration. The same injected `MapsterMapper.IMapper` pattern applies to controllers and custom repositories; entity repositories do not require a mapper.

```csharp
TypeAdapterConfig mapping = new();
mapping.NewConfig<CustomerEditor, CreateCustomer>();
services.AddSingleton(mapping);
services.AddScoped<IMapper, Mapper>();
services.AddHttpClient<CustomersService>(client =>
    client.BaseAddress = new Uri(configuration["ApiBaseUrl"]!));

public sealed class CustomersService(HttpClient client, IMapper mapper)
    : HttpSoftDeleteService<CustomerDto, CreateCustomer, UpdateCustomer, Guid>(
        client, "api/customers", mapper)
{
    public Task<HttpResult<CustomerDto>> CreateFromEditorAsync(CustomerEditor editor,
        CancellationToken cancellationToken = default)
        => CreateAsync(Mapper.Map<CustomerEditor, CreateCustomer>(editor), cancellationToken);
}
```

The base address must end with `/`; endpoint paths are relative without leading/trailing slashes. Omit the last generic key argument to use the solution's `KeyType`. DTOs need no domain interfaces. Override `FormatKey` for custom keys; keys must fit one route segment (no slash, backslash or dot-only key).

## Results and concurrency

```csharp
HttpResult<CustomerDto> loaded = await customers.GetAsync(id, cancellationToken: token);
if (loaded.Outcome.IsSuccess)
{
    HttpResult<CustomerDto> saved = await customers.UpdateAsync(
        id, changes, loaded.Response.ETag, token);
    // Handle saved.Outcome; retain the new ETag after success.
}
```

`Outcome` is Craft.Core's `Result<T>` (or `Result` for no-content operations). `Response` preserves status, ETag, Location and optional problem details, validation fields and unknown JSON extensions. `Map` transforms a successful payload while preserving metadata and errors. There is no server-side result envelope.

HTTP errors, network failures, timeouts and invalid/oversized JSON responses become failed outcomes. Network failures have no HTTP status. Errors use stable `http.*` codes; remote application-specific data remains in `Response.Problem`. Caller cancellation propagates; configuration and programming errors are not hidden. Remote problem text is untrusted text, never HTML; Location is metadata and is not followed automatically.

Pass the original ETag to update/delete/restore. Missing tokens reach the server's 428 response; weak, wildcard and malformed tokens are rejected locally. There are no automatic write retries or token refreshes. A lost response can mean a write committed: reconcile before retrying. For restoration, use the deletion response ETag or explicitly fetch the deleted entity. Batch update/delete/restore use per-item `EntityUpdate<TKey,TUpdate>` / `EntityReference<TKey>` tokens; response DTOs must expose per-item ETags for batch editing.

Read methods include `GetAsync`, `GetPageAsync`, `CountAsync`, `AnyAsync` and `ExistsAsync`. CRUD adds single and batch create/update/delete; soft-delete adds `GetDeletedAsync`, `RestoreAsync` and `RestoreBatchAsync`. Page size omission uses the server default. Server limits and authorization remain authoritative. No unbounded get-all or permanent-delete endpoint is exposed.

## Custom endpoints and configuration

```csharp
public sealed class HealthService(HttpClient client) : HttpServiceBase(client)
{
    public Task<HttpResult<HealthDto>> GetAsync(CancellationToken cancellationToken = default)
        => SendAsync<HealthDto>(HttpMethod.Get, "api/health", cancellationToken: cancellationToken);
}
```

Protected `SendAsync<T>` reads a non-null JSON value; non-generic `SendAsync` expects no payload. Both accept method, relative path, optional body, ETag and cancellation. Pass `HttpServiceOptions` to a base constructor to customize JSON, the whole-operation timeout (30 seconds by default) and the buffered response limit (4 MiB by default). Options are snapshotted. Use host `IOptions<HttpServiceOptions>` when external configuration is needed. Large downloads/streaming belong in a dedicated service.

Hosts configure credentials, trusted origins, cookies/antiforgery, redirect policy and any safe-read resilience through HttpClient handlers. Cross-origin browser clients must expose `ETag` and `Location` via CORS and allow `If-Match`. Blazor authentication integration is deferred in TODO.md. This library does not own or dispose injected HttpClient instances; each operation disposes its request, response and streams.

## Query specifications

See [Craft.QuerySpec](../../Core/Craft.QuerySpec/README.md) for repository registration, server projections and bounded HTTP search, count and existence.
