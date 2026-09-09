using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Craft.Extensions.Tests.Linq;

public class DbSetExtensionsTests
{
    [Fact]
    public void GetQueryFilter_WithNoConfiguredFilter_ReturnsNull()
    {
        using NoQueryFilterDbContext context = CreateNoQueryFilterContext();

        Expression<Func<QueryFilterEntity, bool>>? result = context.Entities.GetQueryFilter();

        Assert.Null(result);
    }

    [Fact]
    public void GetQueryFilter_WithConfiguredFilter_ReturnsConfiguredExpression()
    {
        using SingleQueryFilterDbContext context = CreateSingleQueryFilterContext();

        Expression<Func<QueryFilterEntity, bool>>? result = context.Entities.GetQueryFilter();

        Assert.NotNull(result);
        Func<QueryFilterEntity, bool> compiledFilter = result.Compile();
        Assert.True(compiledFilter(new QueryFilterEntity { IsActive = true }));
    }

    [Fact]
    public void GetQueryFilter_WithNullDbSet_ThrowsArgumentNullException()
    {
        DbSet<QueryFilterEntity> dbSet = null!;

        Assert.Throws<ArgumentNullException>(() => dbSet.GetQueryFilter());
    }

    [Fact]
    public void RemoveFromQueryFilter_WithNoConfiguredFilter_ReturnsAllEntities()
    {
        using NoQueryFilterDbContext context = CreateNoQueryFilterContext();
        SeedEntities(context);

        List<QueryFilterEntity> result = context.Entities
            .RemoveFromQueryFilter(entity => entity.IsActive)
            .OrderBy(entity => entity.Id)
            .ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RemoveFromQueryFilter_WhenConditionMatchesEntireFilter_ReturnsAllEntities()
    {
        using SingleQueryFilterDbContext context = CreateSingleQueryFilterContext();
        SeedEntities(context);

        List<QueryFilterEntity> result = context.Entities
            .RemoveFromQueryFilter(entity => entity.IsActive == true)
            .OrderBy(entity => entity.Id)
            .ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RemoveFromQueryFilter_WhenConditionIsRemoved_PreservesRemainingConditions()
    {
        using CompoundQueryFilterDbContext context = CreateCompoundQueryFilterContext();
        context.Entities.AddRange(
        [
            new QueryFilterEntity { Id = 1, IsActive = true, IsDeleted = false },
            new QueryFilterEntity { Id = 2, IsActive = false, IsDeleted = false },
            new QueryFilterEntity { Id = 3, IsActive = true, IsDeleted = true }
        ]);
        context.SaveChanges();

        List<QueryFilterEntity> result = context.Entities
            .RemoveFromQueryFilter(entity => entity.IsActive == true)
            .OrderBy(entity => entity.Id)
            .ToList();

        Assert.Equal([1, 2], result.Select(entity => entity.Id).ToList());
    }

    [Fact]
    public void RemoveFromQueryFilter_WithNullDbSet_ThrowsArgumentNullException()
    {
        DbSet<QueryFilterEntity> dbSet = null!;
        Expression<Func<QueryFilterEntity, bool>> condition = entity => entity.IsActive;

        Assert.Throws<ArgumentNullException>(() => dbSet.RemoveFromQueryFilter(condition));
    }

    [Fact]
    public void RemoveFromQueryFilter_WithNullCondition_ThrowsArgumentNullException()
    {
        using SingleQueryFilterDbContext context = CreateSingleQueryFilterContext();
        Expression<Func<QueryFilterEntity, bool>> condition = null!;

        Assert.Throws<ArgumentNullException>(() => context.Entities.RemoveFromQueryFilter(condition));
    }

    private static NoQueryFilterDbContext CreateNoQueryFilterContext()
    {
        DbContextOptions<NoQueryFilterDbContext> options = new DbContextOptionsBuilder<NoQueryFilterDbContext>()
            .UseInMemoryDatabase($"DbSetExtensionsTests-NoFilter-{Guid.NewGuid()}")
            .Options;

        return new NoQueryFilterDbContext(options);
    }

    private static SingleQueryFilterDbContext CreateSingleQueryFilterContext()
    {
        DbContextOptions<SingleQueryFilterDbContext> options = new DbContextOptionsBuilder<SingleQueryFilterDbContext>()
            .UseInMemoryDatabase($"DbSetExtensionsTests-SingleFilter-{Guid.NewGuid()}")
            .Options;

        return new SingleQueryFilterDbContext(options);
    }

    private static CompoundQueryFilterDbContext CreateCompoundQueryFilterContext()
    {
        DbContextOptions<CompoundQueryFilterDbContext> options = new DbContextOptionsBuilder<CompoundQueryFilterDbContext>()
            .UseInMemoryDatabase($"DbSetExtensionsTests-CompoundFilter-{Guid.NewGuid()}")
            .Options;

        return new CompoundQueryFilterDbContext(options);
    }

    private static void SeedEntities(NoQueryFilterDbContext context)
    {
        context.Entities.AddRange(
        [
            new QueryFilterEntity { Id = 1, IsActive = true },
            new QueryFilterEntity { Id = 2, IsActive = false }
        ]);
        context.SaveChanges();
    }

    private static void SeedEntities(SingleQueryFilterDbContext context)
    {
        context.Entities.AddRange(
        [
            new QueryFilterEntity { Id = 1, IsActive = true },
            new QueryFilterEntity { Id = 2, IsActive = false }
        ]);
        context.SaveChanges();
    }

    private sealed class QueryFilterEntity
    {
        public int Id { get; init; }
        public bool IsActive { get; init; }
        public bool IsDeleted { get; init; }
    }

    private sealed class NoQueryFilterDbContext(DbContextOptions<NoQueryFilterDbContext> options) : DbContext(options)
    {
        public DbSet<QueryFilterEntity> Entities => Set<QueryFilterEntity>();
    }

    private sealed class SingleQueryFilterDbContext(DbContextOptions<SingleQueryFilterDbContext> options) : DbContext(options)
    {
        public DbSet<QueryFilterEntity> Entities => Set<QueryFilterEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueryFilterEntity>().HasQueryFilter(entity => entity.IsActive);
        }
    }

    private sealed class CompoundQueryFilterDbContext(DbContextOptions<CompoundQueryFilterDbContext> options) : DbContext(options)
    {
        public DbSet<QueryFilterEntity> Entities => Set<QueryFilterEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueryFilterEntity>().HasQueryFilter(entity => entity.IsActive && !entity.IsDeleted);
        }
    }
}
