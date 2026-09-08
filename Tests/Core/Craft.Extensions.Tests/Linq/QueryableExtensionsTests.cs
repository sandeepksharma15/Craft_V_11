using Microsoft.EntityFrameworkCore;

namespace Craft.Extensions.Tests.Linq;

public class QueryableExtensionsTests
{
    [Fact]
    public void SupportsAsync_WithEnumerableQuery_ReturnsFalse()
    {
        // Arrange
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();

        // Act
        bool result = queryable.SupportsAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ToListSafeAsync_WithEnumerableQuery_ReturnsMaterializedList()
    {
        // Arrange
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        List<int> result = await queryable.ToListSafeAsync(cancellationToken);

        // Assert
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public async Task LongCountSafeAsync_WithEnumerableQuery_ReturnsElementCount()
    {
        // Arrange
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        long result = await queryable.LongCountSafeAsync(cancellationToken);

        // Assert
        Assert.Equal(3L, result);
    }

    [Fact]
    public async Task CountSafeAsync_WithEnumerableQuery_ReturnsElementCount()
    {
        // Arrange
        IQueryable<int> queryable = new[] { 1, 2, 3 }.AsQueryable();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        long result = await queryable.CountSafeAsync(cancellationToken);

        // Assert
        Assert.Equal(3L, result);
    }

    [Fact]
    public void SupportsAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<int>? queryable = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => queryable!.SupportsAsync());
    }

    [Fact]
    public async Task ToListSafeAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<int>? queryable = null;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => queryable!.ToListSafeAsync(cancellationToken));
    }

    [Fact]
    public async Task LongCountSafeAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<int>? queryable = null;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => queryable!.LongCountSafeAsync(cancellationToken));
    }

    [Fact]
    public async Task CountSafeAsync_WithNullQueryable_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<int>? queryable = null;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => queryable!.CountSafeAsync(cancellationToken));
    }

    [Fact]
    public async Task SupportsAsync_WithEntityFrameworkQueryable_ReturnsTrue()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = CreateContext();
        await context.Items.AddRangeAsync(
        [
            new TestEntity { Id = 1, Name = "One" },
            new TestEntity { Id = 2, Name = "Two" },
            new TestEntity { Id = 3, Name = "Three" }
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IQueryable<TestEntity> queryable = context.Items.OrderBy(item => item.Id);

        // Act
        bool result = queryable.SupportsAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ToListSafeAsync_WithEntityFrameworkQueryable_ReturnsMaterializedList()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = CreateContext();
        await context.Items.AddRangeAsync(
        [
            new TestEntity { Id = 1, Name = "One" },
            new TestEntity { Id = 2, Name = "Two" },
            new TestEntity { Id = 3, Name = "Three" }
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IQueryable<string> queryable = context.Items
            .OrderBy(item => item.Id)
            .Select(item => item.Name);

        // Act
        List<string> result = await queryable.ToListSafeAsync(cancellationToken);

        // Assert
        Assert.Equal(["One", "Two", "Three"], result);
    }

    [Fact]
    public async Task LongCountSafeAsync_WithEntityFrameworkQueryable_ReturnsElementCount()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = CreateContext();
        await context.Items.AddRangeAsync(
        [
            new TestEntity { Id = 1, Name = "One" },
            new TestEntity { Id = 2, Name = "Two" },
            new TestEntity { Id = 3, Name = "Three" }
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IQueryable<TestEntity> queryable = context.Items;

        // Act
        long result = await queryable.LongCountSafeAsync(cancellationToken);

        // Assert
        Assert.Equal(3L, result);
    }

    [Fact]
    public async Task CountSafeAsync_WithEntityFrameworkQueryable_ReturnsElementCount()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using TestDbContext context = CreateContext();
        await context.Items.AddRangeAsync(
        [
            new TestEntity { Id = 1, Name = "One" },
            new TestEntity { Id = 2, Name = "Two" },
            new TestEntity { Id = 3, Name = "Three" }
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IQueryable<TestEntity> queryable = context.Items;

        // Act
        long result = await queryable.CountSafeAsync(cancellationToken);

        // Assert
        Assert.Equal(3L, result);
    }

    private static TestDbContext CreateContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"QueryableExtensionsTests-{Guid.NewGuid()}")
            .Options;

        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> Items => Set<TestEntity>();
    }

    private sealed record TestEntity
    {
        public int Id { get; init; }
        public required string Name { get; init; }
    }
}
