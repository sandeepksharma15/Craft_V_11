# Craft integration TODOs

Accepted direction, 30 October 2026: Craft.Core is independent of EF/ASP.NET. Application outcomes use Result/Result<T>; HTTP successes return typed payloads and HTTP failures use ProblemDetails. Breaking changes are intentional. These tasks belong to future projects, not unfinished Core implementations.

## Craft.Data

- [ ] Introduce the EF DbContext/persistence contracts here only where they add value; do not restore IDbContext or EF references in Core.
- [ ] Coordinate tenant validation, soft delete and stamps before audit capture; save business changes and finalized audit rows in one database transaction. Integrate the current auditing implementation through an optional adapter and remove competing context save coordinators.
- [ ] Define restore/permanent-delete APIs, cascades/orphans, filter composition and bulk-operation policies. Restoring deleted rows must not bypass tenant isolation.
- [ ] Define transaction/retry and concurrency contracts; repositories must not independently commit every operation.
- [ ] Translate PageInfo's long offset explicitly for provider APIs (e.g. int-limited Skip); reject unsupported ranges rather than truncate. Enforce application query limits and decide count/items consistency. Add cursor pagination only when needed.
- [ ] Validate relational behavior against supported real providers, including generated keys, rollback, cancellation, filtering and tenant isolation.

## Web / ASP.NET integration

- [ ] Map Result<T> success to typed payloads, and failures to ProblemDetails/ValidationProblemDetails; do not recreate ServerResponse envelopes. Choose 200/201/204 at the endpoint according to operation semantics.
- [ ] Define one explicit ErrorKind-to-HTTP policy, including mixed error kinds, field-targeted validation errors and stable application error codes. Do not infer severity from enum ordinal or map every timeout to 408.
- [ ] Handle unexpected exceptions centrally, log diagnostic details with correlation, return safe messages, and preserve cancellation semantics. Do not reintroduce FromException helpers that expose exception messages.
- [ ] Integrate existing Domain exception error information deliberately; avoid duplicating error catalogs or making Core depend on Domain.
- [ ] Add typed client support for success/page payloads and ProblemDetails, including unknown application error codes and non-JSON/network failures.
- [ ] Add request pagination validation, configured page-size limits and source-generated JSON/OpenAPI coverage for actual endpoint DTOs. No custom pagination converter should be necessary.

## Security / MultiTenant

- [ ] Implement scoped read-only ICurrentUser<TKey>/ICurrentTenant<TKey> for HTTP, jobs and other hosts. Test missing/malformed identities, string/Guid/numeric IDs, and cross-request isolation.
- [ ] Keep JWT/token access, claims/roles/permissions, profile lookup and trusted identity establishment in explicit Security contracts; no SetCurrentUserId on Core's read interface.
- [ ] Separate identity from authorization and tenant activation policy. UI visibility must never substitute for server-side authorization.
- [ ] Establish context before persistence/auditing/job execution; adapt it to IAuditContext without a dependency from Core to Auditing.

## Composition and verification

- [ ] Prefer explicit feature registration. Introduce assembly scanning only for a demonstrated need, with lifetime-conflict and trimming behavior defined; removed DI marker interfaces are not registrations.
- [ ] Add an end-to-end sample showing nullable result success, structured validation, safe HTTP failures, pagination, and atomic soft-delete/audit behavior.
- [x] Enforce Core's dependency boundary and complete line/branch coverage in CI; keep future integrations optional.

## Authentication / Identity split (16 November 2026)

- [ ] Implement a shared durable ITokenRevocationStore; retain revocations through expiration plus skew and fail closed on store outages. In-memory registration is development-only and loses revocations on restart.
- [ ] Implement IRefreshSessionStore in an optional Data.Identity adapter: store only unique token hashes, atomically rotate/consume, reject expired/revoked sessions, revoke families on reuse and preserve absolute family expiry. Add concurrent replay/rollback tests before exposing a refresh endpoint.
- [ ] Move account EF mappings/context contracts into Data.Identity. Review LoginHistory shape and soft-delete relationships; base Craft.Data must not require JWT or Blazor services.
- [ ] Add Blazor integration later using validated framework authentication state and protected cookies/OIDC as appropriate. Never authenticate ReadJwtToken output, pass tokens in URLs, block on AuthenticationStateProvider, or expose GET sign-in/sign-out. Define antiforgery, safe local redirects, circuit revalidation and sign-out semantics.
- [ ] Add explicit external OIDC/provider integration separately. Local JWT issuance in Craft.Authentication is optional and is not an authorization-server implementation.
- [ ] Add trusted-origin token acquisition/forwarding only where needed; never forward an incoming cookie token to arbitrary HTTP destinations.
- [ ] Hosts must apply authorization, login throttling/lockout, MFA policy, callback allowlists, trusted proxy/IP handling and safe error responses. Account DTO attributes alone do not provide these controls.
