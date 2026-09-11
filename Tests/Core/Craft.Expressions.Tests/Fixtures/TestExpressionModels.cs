using Craft.Expressions.Ast;
using Microsoft.EntityFrameworkCore;

namespace Craft.Expressions.Tests.Fixtures;

public sealed class Company
{
    public long CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class Store
{
    public string City { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public Company? Company { get; set; }
}

public sealed class TestEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string Name { get; set; } = string.Empty;
    public TestLocation Location { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = [];
}

public sealed class TestLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TestCountry Country { get; set; } = new();
}

public sealed class TestCountry
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class TestUser
{
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public sealed class TestClass
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public double Price { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public TestClass? Child { get; set; }
    public int ScoreField;
}

public sealed class MyClass
{
    public int AnotherProperty { get; set; }
    public string? PropertyName { get; set; }
    public static int StaticField;
    private int _privateField;

    public void SetPrivateField(int value) => _privateField = value;
}

public sealed class MethodHost
{
    public string Echo(string value) => value;
    public string TakesObject(object value) => value.ToString() ?? string.Empty;
    public double DoubleInput(double value) => value;
    public int NeedsInt(int value) => value;
    public string Ambiguous(string? value) => value ?? string.Empty;
    public string Ambiguous(Uri? value) => value?.ToString() ?? string.Empty;
}

public sealed class MethodTargetContainer
{
    public MethodHost Target { get; set; } = new();
}

public sealed class DummyAstNode : AstNode;

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

public sealed class CompoundQueryFilterDbContext(DbContextOptions<CompoundQueryFilterDbContext> options) : DbContext(options)
{
    public DbSet<QueryFilterEntity> Entities => Set<QueryFilterEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<QueryFilterEntity>().HasQueryFilter(entity => entity.IsActive && !entity.IsDeleted);
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
