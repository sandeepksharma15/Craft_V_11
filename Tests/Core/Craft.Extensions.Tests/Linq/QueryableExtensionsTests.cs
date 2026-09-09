using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Craft.Extensions.Tests.Linq;

public class QueryableExtensionsTests
{
    [Fact]
    public void SupportsAsync_WithEnumerableQuery_ReturnsFalse()
    {
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();

        bool result = queryable.SupportsAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task SupportsAsync_WithEntityFrameworkQueryable_ReturnsTrue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AsyncQueryDbContext context = CreateAsyncQueryContext();
        await SeedAsyncQueryContextAsync(context, cancellationToken);

        IQueryable<AsyncQueryEntity> queryable = context.Items.OrderBy(item => item.Id);

        bool result = queryable.SupportsAsync();

        Assert.True(result);
    }

    [Fact]
    public void SupportsAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        IQueryable<int>? queryable = null;

        Assert.Throws<ArgumentNullException>(() => queryable!.SupportsAsync());
    }

    [Fact]
    public async Task ToListSafeAsync_WithEnumerableQuery_ReturnsMaterializedList()
    {
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        List<int> result = await queryable.ToListSafeAsync(cancellationToken);

        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public async Task ToListSafeAsync_WithEntityFrameworkQueryable_ReturnsMaterializedList()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AsyncQueryDbContext context = CreateAsyncQueryContext();
        await SeedAsyncQueryContextAsync(context, cancellationToken);

        IQueryable<string> queryable = context.Items
            .OrderBy(item => item.Id)
            .Select(item => item.Name);

        List<string> result = await queryable.ToListSafeAsync(cancellationToken);

        Assert.Equal(["One", "Two", "Three"], result);
    }

    [Fact]
    public async Task ToListSafeAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        IQueryable<int>? queryable = null;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentNullException>(() => queryable!.ToListSafeAsync(cancellationToken));
    }

    [Fact]
    public async Task LongCountSafeAsync_WithEnumerableQuery_ReturnsElementCount()
    {
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        long result = await queryable.LongCountSafeAsync(cancellationToken);

        Assert.Equal(3L, result);
    }

    [Fact]
    public async Task LongCountSafeAsync_WithEntityFrameworkQueryable_ReturnsElementCount()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AsyncQueryDbContext context = CreateAsyncQueryContext();
        await SeedAsyncQueryContextAsync(context, cancellationToken);

        long result = await context.Items.LongCountSafeAsync(cancellationToken);

        Assert.Equal(3L, result);
    }

    [Fact]
    public async Task LongCountSafeAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        IQueryable<int>? queryable = null;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentNullException>(() => queryable!.LongCountSafeAsync(cancellationToken));
    }

    [Fact]
    public async Task CountSafeAsync_WithEnumerableQuery_ReturnsElementCount()
    {
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        long result = await queryable.CountSafeAsync(cancellationToken);

        Assert.Equal(3L, result);
    }

    [Fact]
    public async Task CountSafeAsync_WithEntityFrameworkQueryable_ReturnsElementCount()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AsyncQueryDbContext context = CreateAsyncQueryContext();
        await SeedAsyncQueryContextAsync(context, cancellationToken);

        long result = await context.Items.CountSafeAsync(cancellationToken);

        Assert.Equal(3L, result);
    }

    [Fact]
    public async Task CountSafeAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        IQueryable<int>? queryable = null;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentNullException>(() => queryable!.CountSafeAsync(cancellationToken));
    }

    [Fact]
    public async Task IncludeDetails_WhenTrue_LoadsAutoIncludedNavigation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databaseName = $"QueryableExtensionsTests-AutoInclude-{Guid.NewGuid()}";

        await using (AutoIncludeDbContext seedContext = CreateAutoIncludeContext(databaseName))
        {
            await SeedAutoIncludeContextAsync(seedContext, cancellationToken);
        }

        await using AutoIncludeDbContext context = CreateAutoIncludeContext(databaseName);

        AutoIncludeEntity entity = Assert.Single(await context.Entities.IncludeDetails(true).ToListAsync(cancellationToken));

        Assert.True(context.Entry(entity).Reference(item => item.Detail).IsLoaded);
    }

    [Fact]
    public async Task IncludeDetails_WhenFalse_SkipsAutoIncludedNavigation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databaseName = $"QueryableExtensionsTests-AutoInclude-{Guid.NewGuid()}";

        await using (AutoIncludeDbContext seedContext = CreateAutoIncludeContext(databaseName))
        {
            await SeedAutoIncludeContextAsync(seedContext, cancellationToken);
        }

        await using AutoIncludeDbContext context = CreateAutoIncludeContext(databaseName);

        AutoIncludeEntity entity = Assert.Single(await context.Entities.IncludeDetails(false).ToListAsync(cancellationToken));

        Assert.False(context.Entry(entity).Reference(item => item.Detail).IsLoaded);
    }

    [Fact]
    public void IncludeDetails_WithNullQueryable_ThrowsArgumentNullException()
    {
        IQueryable<AutoIncludeEntity>? queryable = null;

        Assert.Throws<ArgumentNullException>(() => queryable!.IncludeDetails(true));
    }

    [Fact]
    public async Task ApplyQueryFilter_WithConfiguredFilter_ReturnsFilteredResults()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FilteredQueryDbContext context = CreateFilteredQueryContext();
        await SeedFilteredQueryContextAsync(context, cancellationToken);

        List<FilteredQueryEntity> result = await context.Entities
            .IgnoreQueryFilters()
            .OrderBy(entity => entity.Id)
            .ApplyQueryFilter(context.Entities)
            .ToListAsync(cancellationToken);

        FilteredQueryEntity entity = Assert.Single(result);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public async Task ApplyQueryFilter_WithoutConfiguredFilter_ReturnsOriginalResults()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using UnfilteredQueryDbContext context = CreateUnfilteredQueryContext();
        await SeedUnfilteredQueryContextAsync(context, cancellationToken);

        List<FilteredQueryEntity> result = await context.Entities
            .OrderBy(entity => entity.Id)
            .ApplyQueryFilter(context.Entities)
            .ToListAsync(cancellationToken);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplyQueryFilter_WithNullQueryable_ThrowsArgumentNullException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FilteredQueryDbContext context = CreateFilteredQueryContext();
        IQueryable<FilteredQueryEntity>? queryable = null;

        Assert.Throws<ArgumentNullException>(() => queryable!.ApplyQueryFilter(context.Entities));
    }

    [Fact]
    public async Task ApplyQueryFilter_WithNullDbSet_ThrowsArgumentNullException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FilteredQueryDbContext context = CreateFilteredQueryContext();
        await SeedFilteredQueryContextAsync(context, cancellationToken);
        DbSet<FilteredQueryEntity> dbSet = null!;

        Assert.Throws<ArgumentNullException>(() => context.Entities.ApplyQueryFilter(dbSet));
    }

    [Fact]
    public async Task IncludeIf_WhenTrue_LoadsNavigation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databaseName = $"QueryableExtensionsTests-ConditionalInclude-{Guid.NewGuid()}";

        await using (ConditionalIncludeDbContext seedContext = CreateConditionalIncludeContext(databaseName))
        {
            await SeedConditionalIncludeContextAsync(seedContext, cancellationToken);
        }

        await using ConditionalIncludeDbContext context = CreateConditionalIncludeContext(databaseName);

        ConditionalIncludeEntity entity = Assert.Single(await context.Entities
            .IncludeIf(true, item => item.Detail)
            .ToListAsync(cancellationToken));

        Assert.True(context.Entry(entity).Reference(item => item.Detail).IsLoaded);
    }

    [Fact]
    public async Task IncludeIf_WhenFalse_DoesNotLoadNavigation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databaseName = $"QueryableExtensionsTests-ConditionalInclude-{Guid.NewGuid()}";

        await using (ConditionalIncludeDbContext seedContext = CreateConditionalIncludeContext(databaseName))
        {
            await SeedConditionalIncludeContextAsync(seedContext, cancellationToken);
        }

        await using ConditionalIncludeDbContext context = CreateConditionalIncludeContext(databaseName);

        ConditionalIncludeEntity entity = Assert.Single(await context.Entities
            .IncludeIf(false, item => item.Detail)
            .ToListAsync(cancellationToken));

        Assert.False(context.Entry(entity).Reference(item => item.Detail).IsLoaded);
    }

    [Fact]
    public void IncludeIf_WithNullQueryable_ThrowsArgumentNullException()
    {
        IQueryable<ConditionalIncludeEntity>? queryable = null;
        Expression<Func<ConditionalIncludeEntity, ConditionalIncludeDetail?>> navigationPropertyPath = item => item.Detail;

        Assert.Throws<ArgumentNullException>(() => queryable!.IncludeIf(true, navigationPropertyPath));
    }

    [Fact]
    public async Task IncludeIf_WithNullNavigationPropertyPath_ThrowsArgumentNullException()
    {
        await using ConditionalIncludeDbContext context = CreateConditionalIncludeContext($"QueryableExtensionsTests-ConditionalInclude-{Guid.NewGuid()}");
        Expression<Func<ConditionalIncludeEntity, ConditionalIncludeDetail?>> navigationPropertyPath = null!;

        Assert.Throws<ArgumentNullException>(() => context.Entities.IncludeIf(true, navigationPropertyPath));
    }

    [Fact]
    public async Task IgnoreQueryFiltersIf_WhenTrue_ReturnsEntitiesBypassingFilters()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FilteredQueryDbContext context = CreateFilteredQueryContext();
        await SeedFilteredQueryContextAsync(context, cancellationToken);

        List<FilteredQueryEntity> result = await context.Entities
            .OrderBy(entity => entity.Id)
            .IgnoreQueryFiltersIf(true)
            .ToListAsync(cancellationToken);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task IgnoreQueryFiltersIf_WhenFalse_KeepsConfiguredFiltersApplied()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FilteredQueryDbContext context = CreateFilteredQueryContext();
        await SeedFilteredQueryContextAsync(context, cancellationToken);

        List<FilteredQueryEntity> result = await context.Entities
            .OrderBy(entity => entity.Id)
            .IgnoreQueryFiltersIf(false)
            .ToListAsync(cancellationToken);

        FilteredQueryEntity entity = Assert.Single(result);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void IgnoreQueryFiltersIf_WithNullQueryable_ThrowsArgumentNullException()
    {
        IQueryable<FilteredQueryEntity>? queryable = null;

        Assert.Throws<ArgumentNullException>(() => queryable!.IgnoreQueryFiltersIf(true));
    }

    private static AsyncQueryDbContext CreateAsyncQueryContext()
    {
        DbContextOptions<AsyncQueryDbContext> options = new DbContextOptionsBuilder<AsyncQueryDbContext>()
            .UseInMemoryDatabase($"QueryableExtensionsTests-Async-{Guid.NewGuid()}")
            .Options;

        return new AsyncQueryDbContext(options);
    }

    private static FilteredQueryDbContext CreateFilteredQueryContext()
    {
        DbContextOptions<FilteredQueryDbContext> options = new DbContextOptionsBuilder<FilteredQueryDbContext>()
            .UseInMemoryDatabase($"QueryableExtensionsTests-Filtered-{Guid.NewGuid()}")
            .Options;

        return new FilteredQueryDbContext(options);
    }

    private static UnfilteredQueryDbContext CreateUnfilteredQueryContext()
    {
        DbContextOptions<UnfilteredQueryDbContext> options = new DbContextOptionsBuilder<UnfilteredQueryDbContext>()
            .UseInMemoryDatabase($"QueryableExtensionsTests-Unfiltered-{Guid.NewGuid()}")
            .Options;

        return new UnfilteredQueryDbContext(options);
    }

    private static AutoIncludeDbContext CreateAutoIncludeContext(string databaseName)
    {
        DbContextOptions<AutoIncludeDbContext> options = new DbContextOptionsBuilder<AutoIncludeDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AutoIncludeDbContext(options);
    }

    private static ConditionalIncludeDbContext CreateConditionalIncludeContext(string databaseName)
    {
        DbContextOptions<ConditionalIncludeDbContext> options = new DbContextOptionsBuilder<ConditionalIncludeDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ConditionalIncludeDbContext(options);
    }

    private static async Task SeedAsyncQueryContextAsync(AsyncQueryDbContext context, CancellationToken cancellationToken)
    {
        await context.Items.AddRangeAsync(
        [
            new AsyncQueryEntity { Id = 1, Name = "One" },
            new AsyncQueryEntity { Id = 2, Name = "Two" },
            new AsyncQueryEntity { Id = 3, Name = "Three" }
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFilteredQueryContextAsync(FilteredQueryDbContext context, CancellationToken cancellationToken)
    {
        await context.Entities.AddRangeAsync(
        [
            new FilteredQueryEntity { Id = 1, IsActive = true, Name = "One" },
            new FilteredQueryEntity { Id = 2, IsActive = false, Name = "Two" }
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedUnfilteredQueryContextAsync(UnfilteredQueryDbContext context, CancellationToken cancellationToken)
    {
        await context.Entities.AddRangeAsync(
        [
            new FilteredQueryEntity { Id = 1, IsActive = true, Name = "One" },
            new FilteredQueryEntity { Id = 2, IsActive = false, Name = "Two" }
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAutoIncludeContextAsync(AutoIncludeDbContext context, CancellationToken cancellationToken)
    {
        await context.Entities.AddAsync(new AutoIncludeEntity
        {
            Id = 1,
            Name = "Entity",
            Detail = new AutoIncludeDetail { Id = 1, Description = "Detail" }
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedConditionalIncludeContextAsync(ConditionalIncludeDbContext context, CancellationToken cancellationToken)
    {
        await context.Entities.AddAsync(new ConditionalIncludeEntity
        {
            Id = 1,
            Name = "Entity",
            Detail = new ConditionalIncludeDetail { Id = 1, Description = "Detail" }
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed class AsyncQueryDbContext(DbContextOptions<AsyncQueryDbContext> options) : DbContext(options)
    {
        public DbSet<AsyncQueryEntity> Items => Set<AsyncQueryEntity>();
    }

    private sealed class FilteredQueryDbContext(DbContextOptions<FilteredQueryDbContext> options) : DbContext(options)
    {
        public DbSet<FilteredQueryEntity> Entities => Set<FilteredQueryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FilteredQueryEntity>().HasQueryFilter(entity => entity.IsActive);
        }
    }

    private sealed class UnfilteredQueryDbContext(DbContextOptions<UnfilteredQueryDbContext> options) : DbContext(options)
    {
        public DbSet<FilteredQueryEntity> Entities => Set<FilteredQueryEntity>();
    }

    private sealed class AutoIncludeDbContext(DbContextOptions<AutoIncludeDbContext> options) : DbContext(options)
    {
        public DbSet<AutoIncludeEntity> Entities => Set<AutoIncludeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AutoIncludeEntity>()
                .HasOne(entity => entity.Detail)
                .WithMany()
                .HasForeignKey(entity => entity.DetailId);

            modelBuilder.Entity<AutoIncludeEntity>()
                .Navigation(entity => entity.Detail)
                .AutoInclude();
        }
    }

    private sealed class ConditionalIncludeDbContext(DbContextOptions<ConditionalIncludeDbContext> options) : DbContext(options)
    {
        public DbSet<ConditionalIncludeEntity> Entities => Set<ConditionalIncludeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConditionalIncludeEntity>()
                .HasOne(entity => entity.Detail)
                .WithMany()
                .HasForeignKey(entity => entity.DetailId);
        }
    }

    private sealed record AsyncQueryEntity
    {
        public int Id { get; init; }
        public required string Name { get; init; }
    }

    private sealed class FilteredQueryEntity
    {
        public int Id { get; init; }
        public bool IsActive { get; init; }
        public required string Name { get; init; }
    }

    private sealed class AutoIncludeEntity
    {
        public int Id { get; init; }
        public required string Name { get; init; }
        public int DetailId { get; init; }
        public AutoIncludeDetail Detail { get; init; } = null!;
    }

    private sealed class AutoIncludeDetail
    {
        public int Id { get; init; }
        public required string Description { get; init; }
    }

    private sealed class ConditionalIncludeEntity
    {
        public int Id { get; init; }
        public required string Name { get; init; }
        public int DetailId { get; init; }
        public ConditionalIncludeDetail? Detail { get; init; }
    }

    private sealed class ConditionalIncludeDetail
    {
        public int Id { get; init; }
        public required string Description { get; init; }
    }
}
