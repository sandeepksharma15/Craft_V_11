using Craft.Expressions.Linq;
using Microsoft.EntityFrameworkCore;

namespace Craft.Expressions.Tests.Linq;

public class QueryableFilterExtensionsTests
{
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

        _ = Assert.Throws<ArgumentNullException>(() => queryable!.ApplyQueryFilter(context.Entities));
    }

    [Fact]
    public async Task ApplyQueryFilter_WithNullDbSet_ThrowsArgumentNullException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using FilteredQueryDbContext context = CreateFilteredQueryContext();
        await SeedFilteredQueryContextAsync(context, cancellationToken);
        DbSet<FilteredQueryEntity> dbSet = null!;

        _ = Assert.Throws<ArgumentNullException>(() => context.Entities.ApplyQueryFilter(dbSet));
    }

    private static FilteredQueryDbContext CreateFilteredQueryContext()
    {
        DbContextOptions<FilteredQueryDbContext> options = new DbContextOptionsBuilder<FilteredQueryDbContext>()
            .UseInMemoryDatabase($"QueryableFilterExtensionsTests-Filtered-{Guid.NewGuid()}")
            .Options;

        return new FilteredQueryDbContext(options);
    }

    private static UnfilteredQueryDbContext CreateUnfilteredQueryContext()
    {
        DbContextOptions<UnfilteredQueryDbContext> options = new DbContextOptionsBuilder<UnfilteredQueryDbContext>()
            .UseInMemoryDatabase($"QueryableFilterExtensionsTests-Unfiltered-{Guid.NewGuid()}")
            .Options;

        return new UnfilteredQueryDbContext(options);
    }

    private static async Task SeedFilteredQueryContextAsync(FilteredQueryDbContext context, CancellationToken cancellationToken)
    {
        await context.Entities.AddRangeAsync(
        [
            new FilteredQueryEntity { Id = 1, IsActive = true, Name = "One" },
            new FilteredQueryEntity { Id = 2, IsActive = false, Name = "Two" }
        ], cancellationToken);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedUnfilteredQueryContextAsync(UnfilteredQueryDbContext context, CancellationToken cancellationToken)
    {
        await context.Entities.AddRangeAsync(
        [
            new FilteredQueryEntity { Id = 1, IsActive = true, Name = "One" },
            new FilteredQueryEntity { Id = 2, IsActive = false, Name = "Two" }
        ], cancellationToken);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private sealed class FilteredQueryDbContext(DbContextOptions<FilteredQueryDbContext> options) : DbContext(options)
    {
        public DbSet<FilteredQueryEntity> Entities => Set<FilteredQueryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<FilteredQueryEntity>().HasQueryFilter(entity => entity.IsActive);
        }
    }

    private sealed class UnfilteredQueryDbContext(DbContextOptions<UnfilteredQueryDbContext> options) : DbContext(options)
    {
        public DbSet<FilteredQueryEntity> Entities => Set<FilteredQueryEntity>();
    }

    private sealed class FilteredQueryEntity
    {
        public int Id { get; init; }
        public bool IsActive { get; init; }
        public required string Name { get; init; }
    }
}
