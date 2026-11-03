# Craft.Core

Small .NET 11 application contracts with no package, EF, ASP.NET or other Craft dependencies.

```csharp
Result<string?> lookup = Result<string?>.Success(null);
Result<int> length = lookup.Map(value => value?.Length ?? 0);

var invalid = Result<string>.Failure(new Error(
    "name.required", "Name is required.", ErrorKind.Validation, target: "Name"));

var page = new Page<int>([11, 12], new PageInfo(currentPage: 2, pageSize: 10, totalCount: 12));
```

- `Result` represents completion without a payload; `Result<T>` represents completion with a payload. `IResult` exposes their shared outcome/errors. Failure requires errors; success has none. Reading a failed result's `Value` throws.
- `Error` has a stable application code, safe message, semantic kind and optional field target. Collections are copied and read-only. Consumers own error codes and localization; never put sensitive diagnostics in public messages.
- `Map`, `Bind`, `Match`, `GetValueOrDefault`, `MapAsync` and `BindAsync` preserve nullable success values. Failure propagation retains every error. Async delegates receive cancellation tokens; exceptions/cancellation propagate rather than becoming ordinary failures. A binder must return a result.
- `PageInfo` uses exact 64-bit counts/offsets and checked arithmetic. Pages are one-based. Empty/out-of-range pages have `From = To = 0`; `HasPreviousPage` indicates a lower page number exists. `Page<T>` snapshots the collection, not mutable item objects, and rejects items beyond its available range. Count and items should describe the same logical snapshot.
- Normal System.Text.Json serialization supports page round trips, including custom naming policies; no converter registration is needed. Results are application flow objects, not HTTP payloads. HTTP adapters should return success values/pages and ProblemDetails for failures.
- `ICurrentUser<TKey>` and `ICurrentTenant<TKey>` expose read-only context. `TryGetId` explicitly represents a missing identity without treating zero/default as a sentinel. Implementations must return false for an unavailable identity and keep authentication/availability consistent. String, Guid and numeric keys are supported; non-generic interfaces use the repository's configured `KeyType`.

Breaking rewrite: `ServiceResult`, `ServerResponse`, HTTP status/mapping helpers, EF context contracts, DI marker interfaces and custom page converters are removed. Replace `PageResponse<T>` with `Page<T>` and `PageInfo`. Use explicit DI registration. Deferred Data/Web/Security integrations are tracked in the root [TODO.md](../../../TODO.md).
