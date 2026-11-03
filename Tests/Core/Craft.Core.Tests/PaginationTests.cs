using System.Text.Json;

namespace Craft.Core.Tests;

public class PaginationTests
{
    [Theory]
    [InlineData(1, 10, 0, 0, 0, 0, false, false)]
    [InlineData(1, 10, 25, 3, 1, 10, false, true)]
    [InlineData(2, 10, 25, 3, 11, 20, true, true)]
    [InlineData(3, 10, 25, 3, 21, 25, true, false)]
    [InlineData(4, 10, 25, 3, 0, 0, true, false)]
    [InlineData(2, 10, 20, 2, 11, 20, true, false)]
    public void Metadata_HandlesEmptyPartialAndOutOfRangePages(long current, int size, long total, long pages, long from, long to, bool previous, bool next)
    {
        var page = new PageInfo(current, size, total);
        Assert.Equal(current, page.CurrentPage);
        Assert.Equal(size, page.PageSize);
        Assert.Equal(total, page.TotalCount);
        Assert.Equal((current - 1) * size, page.Offset);
        Assert.Equal(pages, page.TotalPages);
        Assert.Equal(from, page.From);
        Assert.Equal(to, page.To);
        Assert.Equal(previous, page.HasPreviousPage);
        Assert.Equal(next, page.HasNextPage);
    }

    [Fact]
    public void Metadata_UsesExactLongArithmeticAndRejectsOverflow()
    {
        var last = new PageInfo(long.MaxValue, 1, long.MaxValue);
        Assert.Equal(long.MaxValue, last.TotalPages);
        Assert.Equal(long.MaxValue, last.From);
        Assert.Equal(long.MaxValue, last.To);
        Assert.False(last.HasNextPage);
        Assert.Equal(long.MaxValue / 2 + 1, new PageInfo(1, 2, long.MaxValue).TotalPages);
        Assert.Throws<OverflowException>(() => new PageInfo(long.MaxValue, 2, long.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageInfo(0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageInfo(1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageInfo(1, 1, -1));
    }

    [Fact]
    public void Page_SnapshotsItemsAndValidatesAvailableRange()
    {
        var items = new[] { 1, 2 };
        var info = new PageInfo(1, 2, 2);
        var page = new Page<int>(items, info);
        items[0] = 99;
        Assert.Equal([1, 2], page.Items);
        Assert.Same(info, page.Pagination);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)page.Items).Clear());
        Assert.Throws<ArgumentNullException>(() => new Page<int>(null!, info));
        Assert.Throws<ArgumentNullException>(() => new Page<int>([], null!));
        Assert.Throws<ArgumentException>(() => new Page<int>([1, 2, 3], info));
        Assert.Throws<ArgumentException>(() => new Page<int>([1], new PageInfo(2, 2, 2)));
        Assert.Empty(new Page<int>([], new PageInfo(2, 2, 2)).Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Page_RoundTripsWithStandardJsonNamingPolicies(int policy)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = policy switch { 1 => JsonNamingPolicy.CamelCase, 2 => JsonNamingPolicy.SnakeCaseLower, _ => null }
        };
        var original = new Page<string?>([null, "two"], new PageInfo(2, 2, 4));
        var restored = JsonSerializer.Deserialize<Page<string?>>(JsonSerializer.Serialize(original, options), options)!;
        Assert.Equal(original.Items, restored.Items);
        Assert.Equal(2, restored.Pagination.CurrentPage);
        Assert.Equal(4, restored.Pagination.TotalCount);
        Assert.Equal(3, restored.Pagination.From);
    }
}