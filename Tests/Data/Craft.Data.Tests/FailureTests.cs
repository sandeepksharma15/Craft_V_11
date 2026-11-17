using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Data.Tests;

public class FailureTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NestedSave_IsRejectedBeforeWriting(bool asynchronous)
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open("a", new NestedSave());
        db.Add(new Document());
        if (asynchronous) await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Token));
        else Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        using var check = fixture.Open();
        Assert.Empty(await check.Documents.ToListAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransactionStartFailure_DoesNotWriteAndInvalidatesContext(bool asynchronous)
    {
        using var fixture = new DataFixture();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(fixture.Connection).AddInterceptors(new FailBegin());
        options.UseCraftData(DataFixture.Features("a"));
        using var db = new DataContext(options.Options);
        db.Add(new Document());
        if (asynchronous) await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Token));
        else Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Empty(db.ChangeTracker.Entries());
        using var check = fixture.Open();
        Assert.Empty(await check.Documents.ToListAsync(Token));
    }

    [Fact]
    public void RetryEnabledProvider_RequiresAnExplicitWholeUnitStrategy()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlServer("Server=unused;Database=craft_test_unused;Integrated Security=true", o => o.EnableRetryOnFailure());
        options.UseCraftData(DataFixture.Features("a"));
        using var db = new DataContext(options.Options);
        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
    }

    [Fact]
    public async Task VersionOverflow_DoesNotPersistOrWrap()
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open();
        db.Add(new Document()); await db.SaveChangesAsync(Token);
        await db.Database.ExecuteSqlRawAsync("UPDATE Documents SET Version = 9223372036854775807", Token);
        db.ChangeTracker.Clear();
        var row = await db.Documents.SingleAsync(Token);
        row.Name = "changed";
        await Assert.ThrowsAsync<OverflowException>(() => db.SaveChangesAsync(Token));
        using var verify = fixture.Open();
        Assert.Equal("original", (await verify.Documents.SingleAsync(Token)).Name);
    }

    [Fact]
    public void PublicGuards_RejectInvalidArguments()
    {
        using var fixture = new DataFixture(); using var db = fixture.Open();
        Assert.Throws<ArgumentNullException>(() => new CraftPersistence(null!, new DbContextOptionsBuilder().Options));
        Assert.Throws<ArgumentNullException>(() => new CraftPersistence(db, null!));
        Assert.Throws<ArgumentNullException>(() => db.Persistence.ConfigureModel(null!));
        Assert.Throws<ArgumentNullException>(() => db.Persistence.SaveChanges(true, null!));
        Assert.Throws<ArgumentNullException>(() => db.Persistence.PermanentlyDelete<Document>(null!));
        Assert.Throws<ArgumentNullException>(() => db.Restore<Document>(null!));
        Assert.Throws<ArgumentNullException>(() => ((DbContext)null!).Restore(new Document()));
        var row = new Document { Id = 42 }; db.Add(row);
        Assert.Throws<InvalidOperationException>(() => db.Persistence.PermanentlyDelete(row));
        Assert.Throws<InvalidOperationException>(() => db.Restore(row));
        db.Entry(row).State = EntityState.Unchanged;
        Assert.Throws<InvalidOperationException>(() => db.Persistence.PermanentlyDelete(row));
        db.Remove(row);
        Assert.Throws<InvalidOperationException>(() => db.Restore(row));
    }

    [Fact]
    public async Task AsyncGuard_RejectsMissingSaveCallback()
    {
        using var fixture = new DataFixture(); using var db = fixture.Open();
        await Assert.ThrowsAsync<ArgumentNullException>(() => db.Persistence.SaveChangesAsync(true, null!, Token));
    }

    [Fact]
    public void FeatureFactory_NullResultFailsDuringContextResolution()
    {
        var services = new ServiceCollection();
        services.AddCraftDbContext<DataContext>(o => o.UseSqlite("Data Source=:memory:"), f => f.AddFeature(_ => null!));
        using var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<DataContext>());
    }

    private sealed class NestedSave : PersistenceFeature
    {
        public override Action? CaptureChanges(DbContext context) { context.SaveChanges(); return null; }
    }

    private sealed class FailBegin : DbTransactionInterceptor
    {
        public override InterceptionResult<System.Data.Common.DbTransaction> TransactionStarting(System.Data.Common.DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<System.Data.Common.DbTransaction> result) => throw new InvalidOperationException("Begin failed");
        public override ValueTask<InterceptionResult<System.Data.Common.DbTransaction>> TransactionStartingAsync(System.Data.Common.DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<System.Data.Common.DbTransaction> result, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Begin failed");
    }
}
