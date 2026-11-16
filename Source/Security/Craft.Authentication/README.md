# Craft.Authentication

Optional local JWT issuance, strict JWT bearer validation and read-only HTTP current-user integration for .NET 11. Replaces Craft.Security; it has no EF, Identity entity, auditing or Blazor dependency.

```csharp
services.AddSingleton<ITokenRevocationStore, ApplicationRevocationStore>(); // shared durable store
services.AddCraftAuthentication(options => configuration.GetSection("Authentication").Bind(options));
services.AddAuthorization();
// Protect endpoints with the CraftBearer scheme (or explicitly select it as the application's default).
```

Configure Issuer, Audience, ActiveKeyId and SigningKeys (key ID → base64 secret, at least 32 cryptographically random bytes). The active key signs HS256 tokens; retained keys validate older tokens during rotation. Keep secrets in a secret store. Register once. Local issuance is not an OAuth/OIDC authorization server; external-provider integration is deferred.

`JwtTokenService.IssueAccessToken(subject, claims)` issues unique access tokens. Reserved metadata cannot be overridden. `ValidateAsync` validates signature, algorithm, issuer, audience, time, subject, ID and revocation; null means invalid. Revocation-store failures propagate, so infrastructure failures cannot silently authorize requests. `RevokeAsync` only accepts valid signed tokens and retains revocation through expiration plus clock skew. Both service and ASP.NET paths use the same policy and clock. Use the explicit scheme on endpoints; registration does not replace other application defaults.

Revocation has no implicit in-memory fallback. `AddInMemoryTokenRevocation()` is an explicit single-process development option; its entries disappear on restart. Production stores must share state across validators, extend rather than shorten deadlines, and expire keys no earlier than the supplied deadline. Register them singleton/thread-safe, as the JWT service is singleton. Configuration is snapshotted; roll out key changes with overlapping keys before retiring old ones.

`CurrentUser<TKey>` reads authenticated HTTP identities without blocking or exposing mutation/token APIs. It supports IParsable keys (string, Guid, numeric); missing, malformed or conflicting IDs return false. Register generic current-user contracts explicitly when using a key different from the default.

No cookie sign-in endpoints, arbitrary bearer forwarding, unvalidated claim extraction or expired-token authentication is provided. Hosts own authorization, login throttling, account policy, transport security and endpoint response mapping. See root TODO.md for Blazor, external providers and transactional refresh-session integration.
