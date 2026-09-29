using Microsoft.EntityFrameworkCore;

namespace Craft.Testing.EntityFramework;

public sealed class CompoundQueryFilterDbContext(DbContextOptions<CompoundQueryFilterDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<QueryFilterEntity>().HasQueryFilter(entity => entity.IsActive && !entity.IsDeleted);
    }

    public DbSet<QueryFilterEntity> Entities => Set<QueryFilterEntity>();
}

public sealed class QueryFilterEntity
{
    public int Id { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
}

public sealed class NoQueryFilterDbContext(DbContextOptions<NoQueryFilterDbContext> options) : DbContext(options)
{
    public DbSet<QueryFilterEntity> Entities => Set<QueryFilterEntity>();
}

public sealed class SingleQueryFilterDbContext(DbContextOptions<SingleQueryFilterDbContext> options) : DbContext(options)
{
    public DbSet<QueryFilterEntity> Entities => Set<QueryFilterEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<QueryFilterEntity>().HasQueryFilter(entity => entity.IsActive);
    }
}

public sealed class FilteredQueryEntity
{
    public int Id { get; init; }
    public bool IsActive { get; init; }
    public required string Name { get; init; }
}

public sealed class FilteredQueryDbContext(DbContextOptions<FilteredQueryDbContext> options) : DbContext(options)
{
    public DbSet<FilteredQueryEntity> Entities => Set<FilteredQueryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<FilteredQueryEntity>().HasQueryFilter(entity => entity.IsActive);
    }
}

public sealed class UnfilteredQueryDbContext(DbContextOptions<UnfilteredQueryDbContext> options) : DbContext(options)
{
    public DbSet<FilteredQueryEntity> Entities => Set<FilteredQueryEntity>();
}
