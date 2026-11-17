using Craft.Core.Abstractions;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Craft.Data.Tests;

public class GenericTenantTests
{
    [Fact]
    public Task NumericTenants_SupportNonDefaultNegativeIds() => Check(-42L);

    [Fact]
    public Task GuidTenants_AreParameterizedPerContext() => Check(Guid.NewGuid());

    [Fact]
    public Task StringTenantKeys_AreAssignedBeforeTracking() => Check("tenant");

    private static async Task Check<TKey>(TKey tenantId) where TKey : notnull
    {
        var token = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<TenantContext<TKey>>().UseSqlite("Data Source=:memory:");
        options.UseCraftData(new TenantFeature<TKey>(new CurrentTenant<TKey>(tenantId)));
        await using var db = new TenantContext<TKey>(options.Options);
        await db.Database.OpenConnectionAsync(token);
        await db.Database.EnsureCreatedAsync(token);
        var row = new TenantRow<TKey>();
        db.Add(row);
        Assert.Equal(tenantId, row.TenantId);
        await db.SaveChangesAsync(token);
        Assert.Equal(tenantId, row.TenantId);
        db.ChangeTracker.Clear();
        Assert.Equal(tenantId, (await db.Rows.SingleAsync(token)).TenantId);
        var existing = await db.Rows.SingleAsync(token);
        existing.Name = "updated";
        await db.SaveChangesAsync(token);
        db.Remove(existing);
        await db.SaveChangesAsync(token);
        Assert.Empty(await db.Rows.ToListAsync(token));
    }

    private sealed class TenantRow<TKey> : IHasTenant<TKey> where TKey : notnull
    {
        public int Id { get; set; }
        public TKey TenantId { get; set; } = default!;
        public string Name { get; set; } = "name";
    }

    private sealed class TenantContext<TKey>(DbContextOptions<TenantContext<TKey>> options) : CraftDbContext(options) where TKey : notnull
    {
        public DbSet<TenantRow<TKey>> Rows => Set<TenantRow<TKey>>();
        protected override void ConfigureModel(ModelBuilder model)
            => model.Entity<TenantRow<TKey>>().HasAlternateKey(e => new { e.TenantId, e.Id });
    }

    private sealed class CurrentTenant<TKey>(TKey id) : ICurrentTenant<TKey> where TKey : notnull
    {
        public string? Identifier => null;
        public bool IsAvailable => true;
        public bool TryGetId(out TKey value) { value = id; return true; }
    }
}
