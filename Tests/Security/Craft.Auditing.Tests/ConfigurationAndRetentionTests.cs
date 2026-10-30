using Craft.Auditing.Abstractions;
using Craft.Auditing.Capture;
using Craft.Auditing.Configuration;
using Craft.Auditing.Model;
using Craft.Auditing.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Craft.Auditing.Tests;

public class ConfigurationAndRetentionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void Registration_UsesScopedFactoryAndValidatesOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuditContext>(new TestAuditContext());
        services.AddCraftAuditing().Configure(o => o.RetentionPeriod = TimeSpan.FromDays(1));
        services.AddCraftAuditing();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Single(scope.ServiceProvider.GetServices<AuditTrailFactory>());
        Assert.Same(scope.ServiceProvider.GetRequiredService<AuditTrailFactory>(), scope.ServiceProvider.GetRequiredService<AuditTrailFactory>());
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCraftAuditing(null!));
    }

    [Fact]
    public void DefaultOptions_AllowIndefiniteRetention()
    {
        var services = new ServiceCollection();
        services.AddCraftAuditing();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuditOptions>>().Value;
        Assert.Null(options.RetentionPeriod);
        Assert.Equal("[REDACTED]", options.RedactionText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidOptions_FailValidation(bool retention)
    {
        var services = new ServiceCollection();
        services.AddCraftAuditing().Configure(o => { if (retention) o.RetentionPeriod = TimeSpan.Zero; else o.RedactionText = " "; });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AuditOptions>>().Value);
    }

    [Fact]
    public void FactoryGuardsAndNonRelationalContexts_AreValidated()
    {
        var actor = new TestAuditContext(); var options = Options.Create(new AuditOptions()); var time = new TestClock();
        Assert.Throws<ArgumentNullException>(() => new AuditTrailFactory(null!, options, time));
        Assert.Throws<ArgumentNullException>(() => new AuditTrailFactory(actor, null!, time));
        Assert.Throws<ArgumentNullException>(() => new AuditTrailFactory(actor, options, null!));
        Assert.Throws<ArgumentException>(() => new AuditTrailFactory(actor, Options.Create(new AuditOptions { RetentionPeriod = TimeSpan.Zero }), time));
        Assert.Throws<ArgumentException>(() => new AuditTrailFactory(actor, Options.Create(new AuditOptions { RedactionText = " " }), time));
        var dbOptions = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        Assert.Throws<ArgumentNullException>(() => new TestDbContext(dbOptions, null!));
        using var db = new TestDbContext(dbOptions, new AuditTrailFactory(actor, options, time));
        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
    }

    [Fact]
    public async Task Options_AreSnapshotsAndRetentionCanBeDisabled()
    {
        using var f = new AuditFixture();
        f.Options.RetentionPeriod = null;
        f.Options.RedactionText = "MASK";
        using var db = f.Open();
        f.Options.RedactionText = "CHANGED";
        db.Add(new TestEntity { Secret = "hidden" });
        await db.SaveChangesAsync(Token);
        var audit = await db.AuditTrails.SingleAsync(Token);
        Assert.Null(audit.ArchiveAfterUtc);
        Assert.Contains("MASK", audit.NewValues!);
    }

    [Fact]
    public async Task Archival_IsBoundedIdempotentAndDoesNotCreateMoreAudits()
    {
        using var f = new AuditFixture();
        f.Db.AddRange(new TestEntity(), new TestEntity(), new TestEntity());
        await f.Db.SaveChangesAsync(Token);
        var now = f.Clock.GetUtcNow().UtcDateTime;
        Assert.Equal(0, await AuditArchiver.ArchiveDueAsync(f.Db, now, 2, Token));
        Assert.Equal(2, await AuditArchiver.ArchiveDueAsync(f.Db, now.AddDays(30), 2, Token));
        Assert.Equal(1, await AuditArchiver.ArchiveDueAsync(f.Db, now.AddDays(30), 2, Token));
        Assert.Equal(0, await AuditArchiver.ArchiveDueAsync(f.Db, now.AddDays(30), 2, Token));
        using var check = f.Open();
        Assert.All(await check.AuditTrails.ToListAsync(Token), a => Assert.True(a.IsArchived));
        Assert.Equal(3, await check.AuditTrails.CountAsync(Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AuditArchiver.ArchiveDueAsync(f.Db, now, 1, new CancellationToken(true)));
    }

    [Fact]
    public async Task ArchivalAndModelConfiguration_RejectInvalidArguments()
    {
        using var f = new AuditFixture();
        await Assert.ThrowsAsync<ArgumentNullException>(() => AuditArchiver.ArchiveDueAsync(null!, DateTime.UtcNow, 1, Token));
        await Assert.ThrowsAsync<ArgumentException>(() => AuditArchiver.ArchiveDueAsync(f.Db, DateTime.Now, 1, Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => AuditArchiver.ArchiveDueAsync(f.Db, DateTime.UtcNow, 0, Token));
        Assert.Throws<ArgumentNullException>(() => ((ModelBuilder)null!).ConfigureAuditing());
        var model = new ModelBuilder(new ConventionSet());
        Assert.Throws<ArgumentException>(() => model.ConfigureAuditing(" "));
        Assert.Same(model, model.ConfigureAuditing("History", "audit"));
        Assert.Equal("History", model.Model.FindEntityType(typeof(AuditTrail))!.GetTableName());
    }
}