using Craft.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Craft.Repositories.Tests;

public class ProviderTests
{
    public static TheoryData<string> Providers()
    {
        TheoryData<string> providers = new() { "Sqlite" };

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CRAFT_TEST_SQLSERVER")))
            providers.Add("SqlServer");

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CRAFT_TEST_POSTGRESQL"))
            || Environment.GetEnvironmentVariable("CRAFT_REQUIRE_POSTGRESQL") == "1")
            providers.Add("PostgreSql");

        return providers;
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Provider_UsesContextPipelineForRepositoryOperations(string provider)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        DbContextOptionsBuilder<ShopContext> options = new();

        switch (provider)
        {
            case "Sqlite":
                options.UseSqlite("Data Source=:memory:");
                break;
            case "SqlServer":
                string sqlServer = GetConnection("CRAFT_TEST_SQLSERVER");
                Assert.StartsWith("craft_test_", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer).InitialCatalog);
                options.UseSqlServer(sqlServer);
                break;
            case "PostgreSql":
                string postgres = GetConnection("CRAFT_TEST_POSTGRESQL");
                NpgsqlConnectionStringBuilder connection = new(postgres);
                Assert.Equal("craft_test_repositories_postgresql", connection.Database);
                options.UseNpgsql(postgres);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }

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

    private static string GetConnection(string variable)
    {
        string? connection = Environment.GetEnvironmentVariable(variable);

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException($"{variable} must point to a disposable test database.");

        return connection;
    }
}
