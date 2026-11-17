# Craft.Identity.EntityFrameworkCore

EF Core Identity adapter using the Craft.Data persistence pipeline. Inherit `CraftIdentityDbContext`, or `CraftIdentityDbContext<TUser, TRole, TKey>` for custom types. Add DbSets and override `ConfigureModel` for mappings. Configure features with `AddCraftDbContext`, then register the normal Identity EF stores. See the Craft.Data README for setup, migrations, and transaction behavior.

Soft-deletable users/roles use restrictive relationships. Account and credential policy belongs to Identity/UserManager; no passwords are assigned by constructors. This adapter does not provide tenant-specific Identity uniqueness, refresh-token rotation, login endpoints, or automatic schema deployment.
