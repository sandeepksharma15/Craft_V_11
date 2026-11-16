# Craft.Identity

Account models and contracts for ASP.NET Core Identity, separate from JWT authentication and EF persistence.

- CraftUser<TKey>/CraftRole<TKey> and default-key variants retain account/profile, active and soft-delete capabilities. Passwords must be set through UserManager; constructors never assign PasswordHash. Let Identity's configured normalizer handle normalized names.
- Account request DTOs provide basic required/email validation, not complete account policy. Hosts must validate password policy, authorize target users, derive IP addresses from trusted server context and allowlist callback URLs. A valid URL is not automatically trusted.
- LoginHistory is account metadata; table names/relationships should be reviewed in the future Data.Identity adapter. No audit dependency or automatic audit opt-in is introduced.
- RefreshTokenSecret generates 256-bit opaque secrets and hashes them with SHA-256 for storage. Return the secret once; never store/log it. RefreshSession and IRefreshSessionStore specify the persistence boundary, including atomic rotation and family revocation on reuse. No store or refresh endpoint is supplied yet; do not implement refresh by trusting an expired access token alone.

Breaking changes: raw RefreshToken entities, JWT response wrappers, EF context interfaces and the password-taking user constructor are removed. JWT functionality is in Craft.Authentication. Blazor and Data integration tasks are in root TODO.md.
