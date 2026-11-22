using Craft.Core.Pagination;
using Craft.Repositories;
using Craft.Testing.Repositories;
using Craft.Repositories.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Craft.QuerySpec.Tests;

public sealed class RepositoryTests
{
    [Fact]
    public async Task Queries_PreserveFiltersProjectionIncludesOrderingAndNoTracking()
    {
        using Fixture fixture = new();
        await using (ShopContext seed = fixture.Open())
        {
            seed.Products.AddRange(new Product { Name = "same", Details = [new Detail()] }, new Product { Name = "same" }, new Product { Name = "hidden" });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using (ShopContext other = fixture.Open("b"))
        {
            other.Products.Add(new Product { Name = "other tenant" });
            await other.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using ShopContext db = fixture.Open();
        QueryRepository<Product, int, ShopContext> repository = new(db, Fixture.Limits);
        Query<Product> query = new Query<Product>().Where(row => row.Name == "same").OrderBy(row => row.Name);
        CancellationToken token = TestContext.Current.CancellationToken;

        Assert.Equal(2, await repository.CountAsync(query, token));
        Assert.True(await repository.AnyAsync(query, token));
        Assert.Equal(2, (await repository.ListAsync(query, row => row.Name, token)).Count);
        Page<int> first = await repository.PageAsync(query, row => row.Id, 1, 1, token);
        Page<int> second = await repository.PageAsync(query, row => row.Id, 2, 1, token);
        Assert.True(first.Items[0] < second.Items[0]);
        Assert.Equal(2, second.Pagination.TotalCount);
        Assert.Empty((await repository.PageAsync(query, row => row.Id, 3, 1, token)).Items);
        Assert.Empty(db.ChangeTracker.Entries<Product>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SingleAsync(query, token));
        Assert.Equal(first.Items[0], (await repository.FirstAsync(query, token))!.Id);
        Assert.Null(await repository.SingleAsync(query.Where(row => row.Id < 0), token));
        Product included = (await repository.SingleAsync(query.Where(row => row.Id == first.Items[0]).Include("Details").WithDetails(), token))!;
        Assert.Single(included.Details);
        Assert.Empty(db.ChangeTracker.Entries<Detail>());
        Product tracked = (await repository.LoadForUpdateAsync(first.Items[0], cancellationToken: token))!;
        await repository.DeleteAsync(tracked, cancellationToken: token);
        Assert.Equal(1, await repository.CountAsync(query, token));
    }

    [Fact]
    public async Task LimitsCancellationAndDependencies_AreEnforced()
    {
        using Fixture fixture = new();
        await using ShopContext db = fixture.Open();
        db.Products.AddRange(new Product(), new Product());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        QueryRepository<Product, int, ShopContext> repository = new(db, Options.Create(new RepositoryOptions { MaximumListSize = 1, MaximumPageSize = 1 }));
        Query<Product> query = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ListAsync(query, row => row.Id, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.ListAsync<int>(query, null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PageAsync<int>(query, null!, cancellationToken: token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.CountAsync(null!, token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.PageAsync(query, row => row.Id, pageSize: 0, cancellationToken: token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.PageAsync(query, row => row.Id, pageSize: 2, cancellationToken: token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.PageAsync(query, row => row.Id, (long)int.MaxValue + 2, 1, token));
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.PageAsync(query, row => row.Id, 1, 1, cancelled.Token));
    }

    [Fact]
    public async Task UnconfiguredQueryStubs_FailExplicitly()
    {
        Stub stub = new();
        Assert.NotNull(new DefaultStub());
        Query<Product> query = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<NotSupportedException>(() => stub.SingleAsync(query, token));
        await Assert.ThrowsAsync<NotSupportedException>(() => stub.FirstAsync(query, token));
        await Assert.ThrowsAsync<NotSupportedException>(() => stub.CountAsync(query, token));
        await Assert.ThrowsAsync<NotSupportedException>(() => stub.AnyAsync(query, token));
        await Assert.ThrowsAsync<NotSupportedException>(() => stub.ListAsync(query, row => row.Id, token));
        await Assert.ThrowsAsync<NotSupportedException>(() => stub.PageAsync(query, row => row.Id, cancellationToken: token));
    }

    private sealed class Stub : QueryRepositoryStub<Product, int>;

    private sealed class DefaultStub : QueryRepositoryStub<Note>;

    [Fact]
    public void Registration_BindsAllInterfacesToTheSameScopedRepository()
    {
        using Fixture fixture = new();
        ServiceCollection services = new();
        services.AddScoped(_ => fixture.Open());
        services.AddCraftRepositories<ShopContext>(builder => builder.AddQuery<ShopContext, Product, int>().AddQuery<ShopContext, Note>());
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.Same(scope.ServiceProvider.GetRequiredService<IRepository<Product, int>>(), scope.ServiceProvider.GetRequiredService<IQueryReadRepository<Product, int>>());
        Assert.Same(scope.ServiceProvider.GetRequiredService<IRepository<Note>>(), scope.ServiceProvider.GetRequiredService<IQueryReadRepository<Note>>());
        Assert.Same(scope.ServiceProvider.GetRequiredService<IReadRepository<Note>>(), scope.ServiceProvider.GetRequiredService<IQueryReadRepository<Note>>());
        RepositoryBuilder<ShopContext> missing = null!;
        Assert.Throws<ArgumentNullException>(() => missing.AddQuery<ShopContext, Product, int>());
    }
}
