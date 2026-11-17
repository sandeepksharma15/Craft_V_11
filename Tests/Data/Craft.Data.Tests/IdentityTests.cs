using Craft.Auditing.Abstractions;
using Craft.Auditing.Configuration;
using Craft.Auditing.Persistence;
using Craft.Domain.Abstractions;
using Craft.Identity;
using Craft.IdentityEF;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Data.Tests;

public sealed class AccountContext(DbContextOptions<AccountContext> options) : CraftIdentityDbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

public sealed class Note : ISoftDelete
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
}

public class IdentityTests
{
    [Fact]
    public async Task GenericIdentityContext_SupportsStandardIdentityWithoutOptionalFeatures()
    {
        var options = new DbContextOptionsBuilder<GlobalIdentityContext>().UseSqlite("Data Source=:memory:");
        using var db = new GlobalIdentityContext(options.Options);
        await db.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        db.Add(new IdentityUser("standard"));
        Assert.Equal(1, db.SaveChanges());
        db.Add(new IdentityUser("another"));
        Assert.Equal(1, await db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void TenantScopedIdentity_IsRejectedUntilDedicatedStoresExist()
    {
        using var userContext = new TenantUserContext(new DbContextOptionsBuilder<TenantUserContext>().UseSqlite("Data Source=:memory:").Options);
        Assert.Throws<NotSupportedException>(() => userContext.Model);
        using var roleContext = new TenantRoleContext(new DbContextOptionsBuilder<TenantRoleContext>().UseSqlite("Data Source=:memory:").Options);
        Assert.Throws<NotSupportedException>(() => roleContext.Model);
    }

    private sealed class GlobalIdentityContext(DbContextOptions<GlobalIdentityContext> options) : CraftIdentityDbContext<IdentityUser, IdentityRole, string>(options);
    private sealed class TenantUserContext(DbContextOptions<TenantUserContext> options) : CraftIdentityDbContext<TenantUser, CraftRole, KeyType>(options);
    private sealed class TenantRoleContext(DbContextOptions<TenantRoleContext> options) : CraftIdentityDbContext<CraftUser, TenantRole, KeyType>(options);
    private sealed class TenantUser : CraftUser, IHasTenant<string> { public string TenantId { get; set; } = "tenant"; }
    private sealed class TenantRole : CraftRole, IHasTenant<string> { public string TenantId { get; set; } = "tenant"; }

    [Fact]
    public async Task Registration_UsesIdentityStoresAndTheSharedPersistencePipeline()
    {
        var token = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(token);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IAuditContext, Actor>();
        services.AddCraftAuditing();
        services.AddCraftDbContext<AccountContext>(options => options.UseSqlite(connection),
            features => features.UseAuditing().UseSoftDelete().UseConcurrency());
        services.AddIdentityCore<CraftUser>().AddRoles<CraftRole>().AddEntityFrameworkStores<AccountContext>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountContext>();
        await db.Database.EnsureCreatedAsync(token);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<CraftUser>>();
        var user = new CraftUser { UserName = "sample", Email = "sample@example.test" };
        var result = await users.CreateAsync(user, "Valid-Test-123!");
        Assert.True(result.Succeeded, string.Join(";", result.Errors.Select(e => e.Description)));
        Assert.NotNull(user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, "Valid-Test-123!"));
        Assert.True((await users.DeleteAsync(user)).Succeeded);
        Assert.Null(await users.FindByNameAsync("sample"));
        Assert.True((await db.Users.IncludeDeleted().SingleAsync(token)).IsDeleted);
        var note = new Note(); db.Add(note); Assert.Equal(1, db.SaveChanges());
        db.Remove(note); db.SaveChanges();
        Assert.Empty(await db.Notes.ToListAsync(token));
    }
}
