using Craft.Auditing.Abstractions;
using Craft.Auditing.Configuration;
using Craft.Auditing.Persistence;
using Craft.Domain.Abstractions;
using Craft.Identity;
using Craft.Identity.EntityFrameworkCore;
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
