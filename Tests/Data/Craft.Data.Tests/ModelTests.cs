using Craft.Core.Abstractions;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Craft.Data.Tests;

public class ModelTests
{
    private static ModelContext Create(Action<ModelBuilder> configure, params PersistenceFeature[] features)
    {
        var options = new DbContextOptionsBuilder<ModelContext>().UseSqlite("Data Source=:memory:").EnableServiceProviderCaching(false);
        options.UseCraftData(features);
        return new ModelContext(options.Options, configure);
    }

    [Fact]
    public void Inheritance_RequiresContractsOnTheRoot()
    {
        using var valid = Create(m => { m.Entity<SoftRoot>(); m.Entity<SoftChild>(); }, new SoftDeleteFeature());
        Assert.NotNull(valid.Model.FindEntityType(typeof(SoftChild)));
        using var invalid = Create(m => { m.Entity<PlainRoot>(); m.Entity<SoftOnlyChild>(); }, new SoftDeleteFeature());
        Assert.Throws<InvalidOperationException>(() => invalid.Model);
        using var tenantValid = Create(m => { m.Entity<TenantRoot>(); m.Entity<TenantChild>(); }, new TenantFeature<string>(new Tenant("a")));
        Assert.NotNull(tenantValid.Model.FindEntityType(typeof(TenantChild)));
        using var tenantInvalid = Create(m => { m.Entity<PlainRoot>(); m.Entity<TenantOnlyChild>(); }, new TenantFeature<string>(new Tenant("a")));
        Assert.Throws<InvalidOperationException>(() => tenantInvalid.Model);
    }

    [Fact]
    public void OwnedContracts_AreRejected()
    {
        using var soft = Create(m => { m.Entity<Owner>().Ignore(o => o.Tenant); m.Entity<Owner>().OwnsOne(o => o.Soft); }, new SoftDeleteFeature());
        Assert.Throws<InvalidOperationException>(() => soft.Model);
        using var tenant = Create(m => { m.Entity<Owner>().Ignore(o => o.Soft); m.Entity<Owner>().OwnsOne(o => o.Tenant); }, new TenantFeature<string>(new Tenant("a")));
        Assert.Throws<InvalidOperationException>(() => tenant.Model);
    }

    [Theory]
    [InlineData(DeleteBehavior.Cascade, true)]
    [InlineData(DeleteBehavior.ClientCascade, true)]
    [InlineData(DeleteBehavior.SetNull, true)]
    [InlineData(DeleteBehavior.ClientSetNull, true)]
    [InlineData(DeleteBehavior.Restrict, false)]
    [InlineData(DeleteBehavior.NoAction, false)]
    public void SoftDeleteRelationships_RequireExplicitDeletion(DeleteBehavior behavior, bool rejected)
    {
        using var db = Create(m => m.Entity<Dependent>().HasOne<SoftRoot>().WithMany().HasForeignKey(d => d.ParentId).OnDelete(behavior), new SoftDeleteFeature());
        if (rejected) Assert.Throws<InvalidOperationException>(() => db.Model);
        else Assert.NotNull(db.Model);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TenantRelationships_RequireMatchingTenantKeyPosition(int mapping)
    {
        using var db = Create(m =>
        {
            m.Entity<TenantRoot>().Property(p => p.TenantId).HasMaxLength(128);
            var relationship = m.Entity<TenantDependent>().HasOne<TenantRoot>().WithMany();
            if (mapping == 0) relationship.HasForeignKey(d => d.ParentId);
            else if (mapping == 1) relationship.HasForeignKey(d => new { d.TenantId, d.ParentId }).HasPrincipalKey(p => new { p.Code, p.Id });
            else relationship.HasForeignKey(d => new { d.TenantId, d.ParentId }).HasPrincipalKey(p => new { p.TenantId, p.Id });
        }, new TenantFeature<string>(new Tenant("a")));
        if (mapping < 2) Assert.Throws<InvalidOperationException>(() => db.Model);
        else Assert.NotNull(db.Model);
    }

    [Fact]
    public void TenantKeyTypes_MustBeUnambiguousAndMatchConfiguration()
    {
        using var mismatch = Create(m => m.Entity<TenantRoot>(), new TenantFeature<int>(new NumericTenant(1)));
        Assert.Throws<InvalidOperationException>(() => mismatch.Model);
        using var ambiguous = Create(m => m.Entity<AmbiguousTenant>(), new TenantFeature<string>(new Tenant("a")));
        Assert.Throws<InvalidOperationException>(() => ambiguous.Model);
        Assert.Throws<InvalidOperationException>(() => new TenantFeature<int>(new NumericTenant(0)));
        Assert.Throws<InvalidOperationException>(() => new TenantFeature<string>(new MissingTenant()));
    }

    [Fact]
    public async Task PlainContext_RequiresNoFeaturesAndUsesNormalSaveSemantics()
    {
        var options = new DbContextOptionsBuilder<PlainContext>().UseSqlite("Data Source=:memory:");
        using var db = new PlainContext(options.Options);
        await db.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        db.Add(new Note()); Assert.Equal(1, db.SaveChanges());
        db.Add(new Note()); Assert.Equal(1, await db.SaveChangesAsync(TestContext.Current.CancellationToken));
        var plainOptions = new DbContextOptionsBuilder().UseSqlite("Data Source=:memory:");
        plainOptions.UseCraftData();
        using var plain = new DbContext(plainOptions.Options);
        Assert.NotNull(plain.Model);
        Assert.NotNull(new CraftModelCacheKeyFactory().Create(plain, true));
    }

    private sealed class ModelContext(DbContextOptions<ModelContext> options, Action<ModelBuilder> configure) : CraftDbContext(options)
    {
        protected override void ConfigureModel(ModelBuilder modelBuilder) => configure(modelBuilder);
    }
    private sealed class PlainContext(DbContextOptions<PlainContext> options) : CraftDbContext(options)
    {
        public DbSet<Note> Notes => Set<Note>();
    }
    private class SoftRoot : ISoftDelete { public int Id { get; set; } public bool IsDeleted { get; set; } }
    private sealed class SoftChild : SoftRoot { public string? Name { get; set; } }
    private class PlainRoot { public int Id { get; set; } }
    private sealed class SoftOnlyChild : PlainRoot, ISoftDelete { public bool IsDeleted { get; set; } }
    private class TenantRoot : IHasTenant<string> { public int Id { get; set; } public string TenantId { get; set; } = "a"; public string Code { get; set; } = "code"; }
    private sealed class TenantChild : TenantRoot { public string? Name { get; set; } }
    private sealed class TenantOnlyChild : PlainRoot, IHasTenant<string> { public string TenantId { get; set; } = "a"; }
    private sealed class TenantDependent : IHasTenant<string> { public int Id { get; set; } public string TenantId { get; set; } = "a"; public int ParentId { get; set; } }
    private sealed class Dependent { public int Id { get; set; } public int? ParentId { get; set; } }
    private sealed class Owner { public int Id { get; set; } public SoftRoot Soft { get; set; } = new(); public TenantRoot Tenant { get; set; } = new(); }
    private sealed class AmbiguousTenant : IHasTenant<string>, IHasTenant<int>
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = "a";
        int IHasTenant<int>.TenantId { get; set; }
    }
    private sealed class NumericTenant(int id) : ICurrentTenant<int>
    {
        public string? Identifier => null;
        public bool IsAvailable => true;
        public bool TryGetId(out int value) { value = id; return true; }
    }
    private sealed class MissingTenant : ICurrentTenant<string>
    {
        public string? Identifier => null;
        public bool IsAvailable => true;
        public bool TryGetId(out string value) { value = null!; return false; }
    }
}
