using Craft.Data;
using System.Data.Common;
using Craft.Auditing.Model;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Craft.Repositories.Tests;

public class RepositoryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Paging_RemainsValidWhenAnInsertOccursBetweenCountAndItems()
    {
        using var f = new Fixture();
        var interceptor = new InsertBetweenQueries();
        var options = new DbContextOptionsBuilder<ShopContext>().UseSqlite(f.Connection)
            .UseCraftData(Fixture.Features()).AddInterceptors(interceptor).Options;
        using var db = new ShopContext(options);
        var repository = new Repository<Note, ShopContext>(db, Fixture.Limits);
        var page = await repository.GetPagedListAsync(1, 10, cancellationToken: Token);
        Assert.True(interceptor.Inserted);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Pagination.TotalCount);
    }

    private sealed class InsertBetweenQueries : DbCommandInterceptor
    {
        public bool Inserted { get; private set; }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (!Inserted && command.CommandText.Contains("LIMIT", StringComparison.Ordinal))
            {
                await using var insert = command.Connection!.CreateCommand();
                insert.CommandText = "INSERT INTO Notes (Text) VALUES ('arrived')";
                await insert.ExecuteNonQueryAsync(cancellationToken);
                Inserted = true;
            }
            return result;
        }
    }

    [Fact]
    public async Task DefaultSave_PersistsAndKeepsOriginalValuesForTrackedEditing()
    {
        using var f = new Fixture(); using var db = f.Open();
        var repository = new Repository<Product, int, ShopContext>(db, Fixture.Limits);
        var product = new Product();
        Assert.Same(product, await repository.AddAsync(product, cancellationToken: Token));
        Assert.True(product.Id > 0); Assert.Equal(1, product.Version);
        Assert.Equal(EntityState.Unchanged, db.Entry(product).State);
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        product.Name = "changed";
        Assert.Same(product, await repository.UpdateAsync(product, cancellationToken: Token));
        Assert.Equal(2, product.Version);
        Assert.Equal(2, await db.Audits.CountAsync(Token));
        Assert.Contains("product", (await db.Audits.SingleAsync(a => a.Operation == AuditOperation.Updated, Token)).OldValues!);
        await repository.DeleteAsync(product, cancellationToken: Token);
        Assert.True(product.IsDeleted); Assert.Null(await repository.GetAsync(product.Id, cancellationToken: Token));
        Assert.Null(await repository.LoadForUpdateAsync(product.Id, cancellationToken: Token));
        var deleted = await repository.LoadDeletedAsync(product.Id, cancellationToken: Token);
        Assert.Same(product, deleted);
        await repository.RestoreAsync(deleted!, cancellationToken: Token);
        Assert.NotNull(await repository.GetAsync(product.Id, cancellationToken: Token));
        Assert.Null(await repository.LoadDeletedAsync(product.Id, cancellationToken: Token));
        await repository.DeleteAsync(product, cancellationToken: Token);
        await repository.PermanentlyDeleteAsync(product, cancellationToken: Token);
        Assert.Null(await repository.LoadDeletedAsync(product.Id, cancellationToken: Token));
        Assert.Contains(await db.Audits.ToListAsync(Token), a => a.Operation == AuditOperation.Deleted);
    }

    [Fact]
    public async Task SharedContext_AutoSaveCommitsAllPendingChanges()
    {
        using var f = new Fixture(); using var db = f.Open();
        var products = new Repository<Product, int, ShopContext>(db, Fixture.Limits);
        var notes = new Repository<Note, ShopContext>(db, Fixture.Limits);
        await products.AddAsync(new Product(), false, Token);
        using (var check = f.Open()) Assert.Empty(await check.Products.ToListAsync(Token));
        await notes.AddAsync(new Note(), cancellationToken: Token);
        using var verify = f.Open();
        Assert.Single(await verify.Products.ToListAsync(Token)); Assert.Single(await verify.Notes.ToListAsync(Token));
    }

    [Fact]
    public async Task Details_AreExplicitAndReadResultsAreNotTracked()
    {
        using var f = new Fixture();
        int id;
        using (var seed = f.Open())
        {
            var row = new Product { Details = [new Detail()] }; seed.Add(row); await seed.SaveChangesAsync(Token); id = row.Id;
        }
        using var db = f.Open(); var repository = new DetailsRepository(db, Fixture.Limits);
        Assert.Empty((await repository.GetAsync(id, cancellationToken: Token))!.Details);
        Assert.Single((await repository.GetAsync(id, true, Token))!.Details);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Single((await repository.GetAllAsync(true, Token)).Single().Details);
        Assert.Single((await repository.GetPagedListAsync(1, 10, true, Token)).Items.Single().Details);
        Assert.Single((await repository.LoadForUpdateAsync(id, true, Token))!.Details);
        Assert.NotEmpty(db.ChangeTracker.Entries());
        using var bareDb = f.Open(); var bare = new ReadRepository<Product, int, ShopContext>(bareDb, Fixture.Limits);
        Assert.Empty((await bare.GetAsync(id, true, Token))!.Details);
        Assert.True(await bare.AnyAsync(Token)); Assert.True(await bare.ExistsAsync(id, Token));
        Assert.False(await bare.ExistsAsync(-1, Token)); Assert.Equal(1, await bare.GetCountAsync(Token));
    }

    [Fact]
    public async Task BatchOperations_CanStageAnEntireUnitBeforeOneCommit()
    {
        using var f = new Fixture(); using var db = f.Open(); var repository = new Repository<Product, int, ShopContext>(db, Fixture.Limits);
        var rows = await repository.AddRangeAsync([new Product(), new Product()], false, Token);
        Assert.Equal(2, await repository.SaveChangesAsync(Token));
        foreach (var row in rows) row.Name = "updated";
        await repository.UpdateRangeAsync(rows, false, Token); await repository.SaveChangesAsync(Token);
        Assert.All(rows, row => Assert.Equal(2, row.Version));
        await repository.DeleteRangeAsync(rows, false, Token); await repository.SaveChangesAsync(Token);
        Assert.False(await repository.AnyAsync(Token));
        await repository.RestoreRangeAsync(rows, false, Token); await repository.SaveChangesAsync(Token);
        Assert.Equal(2, await repository.GetCountAsync(Token));
        foreach (var row in rows) row.Name = "again";
        await repository.UpdateRangeAsync(rows, cancellationToken: Token);
        await repository.DeleteRangeAsync(rows, cancellationToken: Token);
        await repository.RestoreRangeAsync(rows, cancellationToken: Token);
        await repository.AddRangeAsync([new Product()], cancellationToken: Token);
        Assert.Equal(3, await repository.GetCountAsync(Token));
    }

    [Fact]
    public async Task SingleOperations_RespectDeferredSaveAndCancellation()
    {
        using var f = new Fixture(); using var db = f.Open(); var repository = new Repository<Product, int, ShopContext>(db, Fixture.Limits);
        var row = await repository.AddAsync(new Product(), cancellationToken: Token);
        row.Name = "later"; await repository.UpdateAsync(row, false, Token); await repository.SaveChangesAsync(Token);
        await repository.DeleteAsync(row, false, Token); await repository.SaveChangesAsync(Token);
        await repository.RestoreAsync(row, false, Token); await repository.SaveChangesAsync(Token);
        await repository.DeleteAsync(row, cancellationToken: Token);
        await repository.PermanentlyDeleteAsync(row, false, Token); await repository.SaveChangesAsync(Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.AddAsync(new Product(), cancellationToken: new(true)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetAllAsync(cancellationToken: new(true)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetAsync(1, cancellationToken: new(true)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SaveChangesAsync(new(true)));
    }

    [Fact]
    public async Task EmptyBatches_DoNotCommitUnrelatedPendingChanges()
    {
        using var f = new Fixture(); using var db = f.Open(); var repository = new Repository<Product, int, ShopContext>(db, Fixture.Limits);
        await repository.AddAsync(new Product(), false, Token);
        Assert.Empty(await repository.AddRangeAsync([], cancellationToken: Token));
        Assert.Empty(await repository.UpdateRangeAsync([], cancellationToken: Token));
        Assert.Empty(await repository.DeleteRangeAsync([], cancellationToken: Token));
        Assert.Empty(await repository.RestoreRangeAsync([], cancellationToken: Token));
        using var check = f.Open(); Assert.Empty(await check.Products.ToListAsync(Token));
    }

    [Fact]
    public async Task Paging_ValidatesBoundsAndUsesStableIds()
    {
        using var f = new Fixture(); using var db = f.Open();
        var options = new RepositoryOptions { MaximumPageSize = 2, MaximumListSize = 2 };
        var repository = new Repository<Product, int, ShopContext>(db, Options.Create(options));
        await repository.AddRangeAsync([new Product(), new Product(), new Product()], cancellationToken: Token);
        var page = await repository.GetPagedListAsync(2, 2, cancellationToken: Token);
        Assert.Equal(3, page.Pagination.TotalCount); Assert.Equal(3, Assert.Single(page.Items).Id);
        Assert.Empty((await repository.GetPagedListAsync(3, 2, cancellationToken: Token)).Items);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetAllAsync(cancellationToken: Token));
        options.MaximumPageSize = 100;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetPagedListAsync(1, 3, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetPagedListAsync(0, 1, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetPagedListAsync(1, 0, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetPagedListAsync((long)int.MaxValue + 2, 1, cancellationToken: Token));
        await Assert.ThrowsAsync<OverflowException>(() => repository.GetPagedListAsync(long.MaxValue, 2, cancellationToken: Token));
    }

    [Fact]
    public async Task TenantAndApplicationFilters_ApplyToAllReadPathsAndRestoration()
    {
        using var f = new Fixture(); int id;
        using (var seed = f.Open("b")) { var row = new Product { IsDeleted = true }; seed.Add(row); await seed.SaveChangesAsync(Token); id = row.Id; }
        using var db = f.Open("a"); var repository = new Repository<Product, int, ShopContext>(db, Fixture.Limits);
        Assert.Null(await repository.GetAsync(id, cancellationToken: Token));
        Assert.Null(await repository.LoadForUpdateAsync(id, cancellationToken: Token));
        Assert.Null(await repository.LoadDeletedAsync(id, cancellationToken: Token));
        Assert.False(await repository.ExistsAsync(id, Token)); Assert.False(await repository.AnyAsync(Token));
        Assert.Equal(0, await repository.GetCountAsync(Token)); Assert.Empty(await repository.GetAllAsync(cancellationToken: Token));
        Assert.Empty((await repository.GetPagedListAsync(1, 10, cancellationToken: Token)).Items);
        await repository.AddAsync(new Product { Name = "hidden" }, cancellationToken: Token);
        Assert.False(await repository.AnyAsync(Token));
    }

    [Fact]
    public async Task ConcurrencyAndAuditFailures_RollBackWithoutRepositoryRetries()
    {
        using var f = new Fixture(); int id;
        using (var seed = f.Open()) { var r = new Repository<Product, int, ShopContext>(seed, Fixture.Limits); id = (await r.AddAsync(new Product(), cancellationToken: Token)).Id; }
        using var a = f.Open(); using var b = f.Open();
        var first = new Repository<Product, int, ShopContext>(a, Fixture.Limits); var second = new Repository<Product, int, ShopContext>(b, Fixture.Limits);
        var one = await first.LoadForUpdateAsync(id, cancellationToken: Token); var two = await second.LoadForUpdateAsync(id, cancellationToken: Token);
        one!.Name = "winner"; await first.UpdateAsync(one, cancellationToken: Token);
        two!.Name = "loser"; await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.UpdateAsync(two, cancellationToken: Token));
        using var failing = f.Open(); var repository = new Repository<Product, int, ShopContext>(failing, Fixture.Limits);
        await failing.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_audit BEFORE INSERT ON AuditTrails BEGIN SELECT RAISE(ABORT, 'audit failure'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(new Product(), cancellationToken: Token));
        using var verify = f.Open(); Assert.Single(await verify.Products.ToListAsync(Token)); Assert.Equal(2, await verify.Audits.CountAsync(Token));
    }
}
