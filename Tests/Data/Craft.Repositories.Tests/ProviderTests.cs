using Microsoft.EntityFrameworkCore;

namespace Craft.Repositories.Tests;

public class ProviderTests
{
    public static IEnumerable<object[]> Providers()
    {
        yield return ["Sqlite", "Data Source=:memory:"];
        if (Environment.GetEnvironmentVariable("CRAFT_TEST_SQLSERVER") is { Length: > 0 } connection)
            yield return ["SqlServer", connection];
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Provider_UsesContextPipelineForRepositoryOperations(string provider, string connection)
    {
        var token = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<ShopContext>();
        if (provider == "SqlServer")
        {
            Assert.StartsWith("craft_test_", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
            options.UseSqlServer(connection);
        }
        else options.UseSqlite(connection);
        options.UseCraftData(Fixture.Features());
        await using var db = new ShopContext(options.Options);
        if (provider == "Sqlite") await db.Database.OpenConnectionAsync(token);
        await db.Database.EnsureDeletedAsync(token); await db.Database.EnsureCreatedAsync(token);
        try
        {
            var repository = new DetailsRepository(db, Fixture.Limits);
            var row = await repository.AddAsync(new Product { Details = [new Detail()] }, cancellationToken: token);
            Assert.True(row.Id > 0); Assert.Single((await repository.GetAsync(row.Id, true, token))!.Details);
            Assert.Equal(1, (await repository.GetPagedListAsync(1, 10, cancellationToken: token)).Pagination.TotalCount);
            await repository.DeleteAsync(row, cancellationToken: token); Assert.False(await repository.AnyAsync(token));
            Assert.NotNull(await repository.LoadDeletedAsync(row.Id, true, token));
            await repository.RestoreAsync(row, cancellationToken: token); Assert.True(await repository.AnyAsync(token));
            Assert.Equal(3, await db.Audits.CountAsync(token));
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            await repository.AddAsync(new Product(), cancellationToken: token); await transaction.RollbackAsync(token);
            db.ChangeTracker.Clear();
            Assert.Equal(1, await repository.GetCountAsync(token)); Assert.Equal(3, await db.Audits.CountAsync(token));
        }
        finally { await db.Database.EnsureDeletedAsync(CancellationToken.None); }
    }
}
