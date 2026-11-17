using Craft.Auditing.Model;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Craft.Data.Tests;

public class PersistenceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static Task<int> Save(DataContext db, bool asynchronous) => asynchronous ? db.SaveChangesAsync(Token) : Task.FromResult(db.SaveChanges());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_StagesSoftDeletionAndStampsBeforeTransactionalAudit(bool asynchronous)
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open();
        var entity = new Document();
        db.Add(entity);
        Assert.Equal(1, await Save(db, asynchronous));
        Assert.Equal("a", entity.TenantId);
        Assert.Equal(1, entity.Version);
        var stamp = entity.ConcurrencyStamp;
        Assert.NotNull(stamp);
        db.Remove(entity);
        Assert.Equal(1, await Save(db, asynchronous));
        Assert.Equal(2, entity.Version);
        Assert.NotEqual(stamp, entity.ConcurrencyStamp);
        Assert.Empty(await db.Documents.ToListAsync(Token));
        Assert.Single(await db.Documents.IncludeDeleted().ToListAsync(Token));
        Assert.Contains(await db.AuditTrails.ToListAsync(Token), a => a.Operation == AuditOperation.SoftDeleted);
        db.Restore(entity);
        await Save(db, asynchronous);
        Assert.Single(await db.Documents.ToListAsync(Token));
        Assert.Equal(3, entity.Version);
        Assert.Contains(await db.AuditTrails.ToListAsync(Token), a => a.Operation == AuditOperation.Restored);
        Assert.Equal(0, await Save(db, asynchronous));
        Assert.Equal(3, entity.Version);
    }

    [Fact]
    public async Task TenantFilters_UseEachContextAndRestoreKeepsIsolationAndApplicationFilter()
    {
        using var fixture = new DataFixture();
        using (var first = fixture.Open("a"))
        {
            first.AddRange(new Document { IsDeleted = true }, new Document { Name = "hidden" });
            await first.SaveChangesAsync(Token);
        }
        using (var second = fixture.Open("b"))
        {
            second.Add(new Document());
            await second.SaveChangesAsync(Token);
            Assert.Single(await second.Documents.ToListAsync(Token));
            Assert.All(await second.Documents.IncludeDeleted().ToListAsync(Token), d => Assert.Equal("b", d.TenantId));
        }
        using var again = fixture.Open("a");
        Assert.Empty(await again.Documents.ToListAsync(Token));
        var deleted = await again.Documents.IncludeDeleted().SingleAsync(Token);
        again.Restore(deleted);
        await again.SaveChangesAsync(Token);
        Assert.Equal("a", (await again.Documents.SingleAsync(Token)).TenantId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetachedDelete_UpdatesOnlyDeletionAndStamps(bool asynchronous)
    {
        using var fixture = new DataFixture();
        Document original;
        using (var seed = fixture.Open())
        {
            original = new Document { Name = "keep" };
            seed.Add(original);
            await seed.SaveChangesAsync(Token);
        }
        using var db = fixture.Open();
        var stub = new Document { Id = original.Id, TenantId = "a", Version = original.Version, ConcurrencyStamp = original.ConcurrencyStamp };
        db.Remove(stub);
        await Save(db, asynchronous);
        using var verify = fixture.Open();
        Assert.Equal("keep", (await verify.Documents.IncludeDeleted().SingleAsync(Token)).Name);
    }

    [Fact]
    public async Task TenantWrites_RejectChangedOwnershipAndForgedDetachedUpdates()
    {
        using var fixture = new DataFixture();
        int id;
        using (var seed = fixture.Open("b"))
        {
            var entity = new Document(); seed.Add(entity); await seed.SaveChangesAsync(Token); id = entity.Id;
        }
        using (var db = fixture.Open("a"))
        {
            db.Add(new Document { TenantId = "b" });
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Token));
            Assert.Empty(await db.AuditTrails.Where(a => a.Operation != AuditOperation.Created).ToListAsync(Token));
        }
        using (var db = fixture.Open("a"))
        {
            var forged = new Document { Id = id, TenantId = "a", Name = "attack", Version = 1 };
            db.Update(forged);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync(Token));
        }
        using var owned = fixture.Open("b");
        var loaded = await owned.Documents.SingleAsync(Token);
        loaded.TenantId = "a";
        await Assert.ThrowsAsync<InvalidOperationException>(() => owned.SaveChangesAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentUpdates_RollBackAndRequireFreshContext(bool asynchronous)
    {
        using var fixture = new DataFixture();
        using (var seed = fixture.Open()) { seed.Add(new Document()); await seed.SaveChangesAsync(Token); }
        using var first = fixture.Open();
        using var second = fixture.Open();
        var a = await first.Documents.SingleAsync(Token);
        var b = await second.Documents.SingleAsync(Token);
        a.Name = "winner"; await Save(first, asynchronous);
        b.Name = "loser";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Save(second, asynchronous));
        Assert.Empty(second.ChangeTracker.Entries());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Save(second, asynchronous));
        using var verify = fixture.Open();
        Assert.Equal("winner", (await verify.Documents.SingleAsync(Token)).Name);
        Assert.Equal(2, await verify.AuditTrails.CountAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletionFailure_RollsBackBusinessAndAuditWrites(bool asynchronous)
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open("a", new FailCompletion());
        db.Add(new Document());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Save(db, asynchronous));
        using var verify = fixture.Open();
        Assert.Empty(await verify.Documents.ToListAsync(Token));
        Assert.Empty(await verify.AuditTrails.ToListAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingTransaction_SavepointFailurePreservesEarlierWork(bool asynchronous)
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open("a", new FailCompletion());
        await using var transaction = await db.Database.BeginTransactionAsync(Token);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Documents (TenantId,Name,IsDeleted,Version) VALUES ('a','earlier',0,1)", Token);
        db.Add(new Document());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Save(db, asynchronous));
        await transaction.CommitAsync(Token);
        using var verify = fixture.Open();
        Assert.Equal("earlier", (await verify.Documents.SingleAsync(Token)).Name);
    }

    [Fact]
    public async Task CancellationAndDeferredAcceptance_DoNotWrite()
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open();
        db.Add(new Document());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => db.SaveChangesAsync(new CancellationToken(true)));
        Assert.Throws<NotSupportedException>(() => db.SaveChanges(false));
        await Assert.ThrowsAsync<NotSupportedException>(() => db.SaveChangesAsync(false, Token));
        using var verify = fixture.Open();
        Assert.Empty(await verify.Documents.ToListAsync(Token));
        Assert.Throws<InvalidOperationException>(() => db.Restore(new Document()));
    }

    [Fact]
    public async Task PermanentDelete_IsExplicitAndAudited()
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open();
        var entity = new Document();
        Assert.Throws<InvalidOperationException>(() => db.Persistence.PermanentlyDelete(entity));
        db.Add(entity); await db.SaveChangesAsync(Token);
        db.Remove(entity); await db.SaveChangesAsync(Token);
        db.Persistence.PermanentlyDelete(entity); await db.SaveChangesAsync(Token);
        Assert.Empty(await db.Documents.IncludeDeleted().ToListAsync(Token));
        Assert.Contains(await db.AuditTrails.ToListAsync(Token), a => a.Operation == AuditOperation.Deleted);
    }

    private sealed class FailCompletion : PersistenceFeature
    {
        public override Action CaptureChanges(DbContext context) => () => throw new InvalidOperationException("Completion failed");
    }
}
