using Craft.Core.Abstractions;
using Craft.Domain.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Data.Tests;

public class ConfigurationTests
{
    [Fact]
    public void Registration_ValidatesMissingAndDuplicateFeatures()
    {
        Assert.Throws<ArgumentNullException>(() => CraftDataRegistration.AddCraftDbContext<DataContext>(null!, _ => { }, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCraftDbContext<DataContext>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCraftDbContext<DataContext>(_ => { }, null!));
        Assert.Throws<ArgumentNullException>(() => new CraftDataBuilder().AddFeature(null!));
        Assert.Throws<ArgumentNullException>(() => CraftDataOptionsExtensions.UseCraftData(null!, []));
        Assert.Throws<ArgumentNullException>(() => new DbContextOptionsBuilder().UseCraftData(null!));
        Assert.Throws<ArgumentException>(() => new DbContextOptionsBuilder().UseCraftData(new SoftDeleteFeature(), new SoftDeleteFeature()));
        Assert.Throws<ArgumentException>(() => new DbContextOptionsBuilder().UseCraftData([null!]));
        var services = new ServiceCollection();
        services.AddCraftDbContext<DataContext>(o => o.UseSqlite("Data Source=:memory:"), f => f.UseSoftDelete().UseSoftDelete());
        using var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<DataContext>());
    }

    [Fact]
    public void Tenancy_RequiresEstablishedIdentityAndSnapshotsIt()
    {
        Assert.Throws<ArgumentNullException>(() => new TenantFeature<string>(null!));
        Assert.Throws<InvalidOperationException>(() => new TenantFeature<string>(new Tenant(" ")));
        Assert.Throws<InvalidOperationException>(() => new TenantFeature<string>(new Tenant("a") { IsAvailable = false }));
        var tenant = new Tenant("a");
        var feature = new TenantFeature<string>(tenant);
        tenant.Id = "b";
        Assert.Equal("a", feature.TenantId);
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite("Data Source=:memory:");
        options.UseCraftData(new SoftDeleteFeature());
        var diagnostic = string.Join(" ", options.Options.Extensions.Select(e => e.Info.LogFragment));
        Assert.Contains("CraftData", diagnostic);
        Assert.DoesNotContain("Data Source", diagnostic);
        using var db = new DataContext(options.Options);
        Assert.Throws<InvalidOperationException>(() => db.Model);
    }

    [Fact]
    public void Model_RejectsImplicitSoftDeleteCascades()
    {
        var options = new DbContextOptionsBuilder<CascadeContext>().UseSqlite("Data Source=:memory:");
        options.UseCraftData(new SoftDeleteFeature());
        var diagnostic = string.Join(" ", options.Options.Extensions.Select(e => e.Info.LogFragment));
        Assert.Contains("CraftData", diagnostic);
        Assert.DoesNotContain("Data Source", diagnostic);
        using var db = new CascadeContext(options.Options);
        Assert.Throws<InvalidOperationException>(() => db.Model);
    }

    [Fact]
    public async Task TenantRegistration_ResolvesScopedContextIdentity()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); connection.Open();
        var services = new ServiceCollection();
        services.AddScoped<ICurrentTenant<string>>(_ => new Tenant("scoped"));
        services.AddCraftDbContext<DataContext>(o => o.UseSqlite(connection), f => f.UseMultiTenancy<string>().UseSoftDelete().UseVersioning().UseConcurrency());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        Assert.Equal("scoped", db.Persistence.FindFeature<TenantFeature<string>>()!.TenantId);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        db.Add(new Document());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Single(await db.Documents.ToListAsync(TestContext.Current.CancellationToken));
    }

    private sealed class CascadeContext(DbContextOptions<CascadeContext> options) : CraftDbContext(options)
    {
        protected override void ConfigureModel(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Dependent>().HasOne(d => d.Parent).WithMany().HasForeignKey(d => d.ParentId).OnDelete(DeleteBehavior.Cascade);
    }
    private sealed class Parent : ISoftDelete { public int Id { get; set; } public bool IsDeleted { get; set; } }
    private sealed class Dependent { public int Id { get; set; } public int ParentId { get; set; } public Parent Parent { get; set; } = null!; }
}
