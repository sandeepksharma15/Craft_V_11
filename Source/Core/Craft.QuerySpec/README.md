# Craft.QuerySpec

Craft.QuerySpec describes queries without referencing EF Core or ASP.NET Core. It separates two inputs:

- `Query<TEntity>` is an immutable **server-side** specification. Application code supplies typed predicates and ordering, and can request known navigation paths.
- `QueryRequest` is an **HTTP request** containing filter groups, sort aliases and paging options. A server-owned `QueryPolicy<TEntity>` turns allowed aliases and operators into a specification. Do not deserialize an expression or accept navigation paths from the caller.

## Query from server code

Register a query repository instead of a standard repository for the same entity:

```csharp
services.AddCraftRepositories<AppDbContext>(repositories => repositories
    .AddQuery<AppDbContext, Customer, long>());
```

Inject `IQueryReadRepository<Customer, long>`. Supply the projection as an expression so EF can select the required columns:

```csharp
Query<Customer> query = new Query<Customer>()
    .Where(customer => customer.IsActive)
    .OrderBy(customer => customer.Name);

Page<CustomerSummary> page = await repository.PageAsync(
    query,
    customer => new CustomerSummary(customer.Id, customer.Name),
    page: 1,
    pageSize: 20,
    cancellationToken: cancellationToken);
```

Queries keep the context's global filters and read without change tracking. The repository appends the entity ID as an ordering tie-breaker. List and page sizes are bounded by repository options; EF offsets above `int.MaxValue` are rejected. Count and existence use the filtered set without ordering, includes or paging. A page reads its count and items separately, so the total is not a snapshot under concurrent writes.

Use `Include` only with server-owned paths. `WithDetails` requests includes configured by the repository, not an automatic walk of the entity graph. A projected response contains only what its projection selects; includes are not a way to control its shape.

## Expose a bounded HTTP query

Choose `QueryReadController`, `QueryEntityController` or `QuerySoftDeleteEntityController` to match the endpoint's normal CRUD capabilities. Provide a fixed DTO projection and a policy configured before serving requests:

```csharp
private static readonly QueryPolicy<Customer> _queryPolicy = new QueryPolicy<Customer>()
    .Allow("name", customer => customer.Name, true, FilterOperator.Equal, FilterOperator.Contains);

protected override QueryPolicy<Customer> QueryPolicy => _queryPolicy;

protected override Expression<Func<Customer, CustomerSummary>> Projection
    => customer => new CustomerSummary(customer.Id, customer.Name);
```

Field aliases are case-sensitive. Allow only the operations and fields that this endpoint should reveal. Authentication and EF filters remain in effect, but the allowlist does not authorize access to individual rows. Apply any additional application-level row scope before executing **all three** operations, including count and existence.

| Relative endpoint | Client method | Success payload |
| --- | --- | --- |
| `POST query` | `SearchAsync` | `Page<TResponse>` |
| `POST query/count` | `QueryCountAsync` | `long` |
| `POST query/any` | `QueryAnyAsync` | `bool` |

All three validate `QueryRequest`; count and existence do not apply its paging to the result. Invalid requests receive HTTP 400, and query-policy errors appear under `query` in validation problem details. Unknown JSON members are rejected. Clients cannot choose projection fields, disable query filters, or delete by query. The optional `IncludeDetails` flag can only request repository-configured includes.

For each controller variant, the matching client is `HttpQueryReadService`, `HttpQueryService` or `HttpQuerySoftDeleteService`. The client uses the existing `HttpResult<T>` contract:

```csharp
QueryRequest request = new()
{
    Filter = new FilterGroup
    {
        Filters = [new QueryFilter("name", FilterOperator.Contains, JsonSerializer.SerializeToElement("Sharma"))]
    },
    Sort = [new QuerySort("name")],
    PageSize = 20
};

HttpResult<Page<CustomerSummary>> response = await client.SearchAsync(request, cancellationToken);
```

Nested groups use `FilterLogic.And` or `FilterLogic.Or`. Membership operators take JSON arrays; `IsNull` and `IsNotNull` do not need a value. The default request is page 1 with 20 items. The default policy allows up to 200 items per page, subject to the repository's own maximum for search; it also caps nesting, filter nodes, sort fields, membership values and individual filter-value size. Hosts should configure their own HTTP request-size limit.

## Further reading

- [Craft.Repositories](../../Data/Craft.Repositories/README.md)
- [Craft.Controllers](../../Web/Craft.Controllers/README.md)
- [Craft.HttpServices](../../Web/Craft.HttpServices/README.md)

## Verification

From the repository root:

```sh
dotnet test --project Tests/Core/Craft.QuerySpec.Tests/Craft.QuerySpec.Tests.csproj --configuration Release
```

[Solution CI](../../../.github/workflows/solution.yml) also checks 100% line and branch coverage for the query library and integrations on Linux and Windows. EF translation depends on the provider; verify application selectors, projections and string matching on the database you deploy.
