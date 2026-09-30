using Craft.Utilities.Helpers;

namespace Craft.Utilities.Tests.Helpers;

public class RandomHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(10000)]
    public void GenerateRandomizedList_PreservesInputAndAllOccurrences(int count)
    {
        int[] input = Enumerable.Range(0, count).Select(i => i % 3).ToArray();
        int[] original = [.. input];
        List<int> result = RandomHelper.GenerateRandomizedList(input);
        Assert.Equal(original, input);
        Assert.Equal(original.Order(), result.Order());
        Assert.NotSame(input, result);
    }

    [Fact]
    public void GenerateRandomizedList_EnumeratesInputOnce()
    {
        int enumerations = 0;
        IEnumerable<int> Items()
        {
            enumerations++;
            yield return 1;
            yield return 2;
        }
        Assert.Equal([1, 2], RandomHelper.GenerateRandomizedList(Items()).Order());
        Assert.Equal(1, enumerations);
    }

    [Fact]
    public void GenerateRandomizedList_Null_Throws()
        => Assert.Throws<ArgumentNullException>("items", () => RandomHelper.GenerateRandomizedList<int>(null!));

    [Theory]
    [InlineData(5, 10)]
    [InlineData(-10, -5)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void GetRandom_MinMax_StaysInRange(int min, int max)
    {
        for (int i = 0; i < 100; i++)
            Assert.InRange(RandomHelper.GetRandom(min, max), min, max - 1);
    }

    [Fact]
    public void GetRandom_BoundaryArguments_PreserveRandomContract()
    {
        Assert.Equal(5, RandomHelper.GetRandom(5, 5));
        Assert.Equal(0, RandomHelper.GetRandom(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RandomHelper.GetRandom(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => RandomHelper.GetRandom(2, 1));
        for (int i = 0; i < 100; i++)
        {
            Assert.InRange(RandomHelper.GetRandom(10), 0, 9);
            Assert.InRange(RandomHelper.GetRandom(), 0, int.MaxValue - 1);
        }
    }

    [Fact]
    public void GetRandomOf_Collections_SelectOnlyExistingItems()
    {
        string?[] items = ["a", "b", null];
        for (int i = 0; i < 100; i++)
        {
            Assert.Contains(RandomHelper.GetRandomOf(items), items);
            Assert.Contains(RandomHelper.GetRandomOfList(items), items);
        }
        Assert.Equal("only", RandomHelper.GetRandomOf("only"));
        Assert.Equal("only", RandomHelper.GetRandomOfList(new[] { "only" }));
    }

    [Fact]
    public void GetRandomOf_NullOrEmpty_ThrowsWithParameter()
    {
        Assert.Throws<ArgumentException>("objs", () => RandomHelper.GetRandomOf<string>(null!));
        Assert.Throws<ArgumentException>("objs", () => RandomHelper.GetRandomOf<string>());
        Assert.Throws<ArgumentException>("list", () => RandomHelper.GetRandomOfList<string>(null!));
        Assert.Throws<ArgumentException>("list", () => RandomHelper.GetRandomOfList(Array.Empty<string>()));
    }
}
