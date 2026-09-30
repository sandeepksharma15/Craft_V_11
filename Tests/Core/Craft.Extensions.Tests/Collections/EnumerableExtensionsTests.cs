using Craft.Testing.Models;

namespace Craft.Extensions.Tests.Collections;

public class EnumerableExtensionsTests
{
    #region Public Methods

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(10000)]
    public void GenerateRandomizedList_Enumerable_PreservesEveryOccurrence(int count)
    {
        IEnumerable<int> source = Enumerable.Range(0, count).Select(i => i % 3);

        List<int> result = source.GenerateRandomizedList();

        Assert.Equal(source.Order(), result.Order());
        Assert.Equal(count, result.Count);
    }

    [Fact]
    public void GenerateRandomizedList_Iterator_EnumeratesOnce()
    {
        int enumerations = 0;
        IEnumerable<int> Items()
        {
            enumerations++;
            yield return 1;
            yield return 2;
            yield return 1;
        }

        List<int> result = Items().GenerateRandomizedList();

        Assert.Equal([1, 1, 2], result.Order());
        Assert.Equal(1, enumerations);
    }

    [Fact]
    public void GenerateRandomizedList_NullSource_Throws()
    {
        IEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>("source", () => source.GenerateRandomizedList());
    }

    [Fact]
    public void GenerateRandomizedList_ReferenceAndNullItems_PreservesIdentity()
    {
        TestItem first = new() { Id = 1, Name = "first" };
        TestItem second = new() { Id = 2, Name = "second" };
        TestItem?[] source = [first, null, first, second];

        List<TestItem?> result = source.GenerateRandomizedList();

        Assert.Equal(4, result.Count);
        Assert.Equal(2, result.Count(item => ReferenceEquals(item, first)));
        Assert.Single(result, item => ReferenceEquals(item, second));
        Assert.Single(result, item => item is null);
        Assert.Equal([first, null, first, second], source);
    }

    [Fact]
    public void GetListDataForSelect_HandlesMissingProperty()
    {
        // Arrange
        TestItem[] items =
        [
            new TestItem { Id = 1, Name = "A" }
        ];

        // Act
        Dictionary<string, string> result = items.GetListDataForSelect("NonExistent", "Name");

        // Assert
        _ = Assert.Single(result);
        Assert.Equal(string.Empty, result.Keys.First());
        Assert.Equal("A", result.Values.First());
    }

    [Fact]
    public void GetListDataForSelect_HandlesNullItem_WhenFieldsAreNotNull()
    {
        // Arrange
        TestItem?[] items = [null];

        // Act
        Dictionary<string, string> result = items.GetListDataForSelect("Id", "Name");

        // Assert
        _ = Assert.Single(result);
        Assert.Equal(string.Empty, result.Keys.First());
        Assert.Equal(string.Empty, result.Values.First());
    }

    [Fact]
    public void GetListDataForSelect_HandlesNullItem_WhenFieldsAreNull()
    {
        // Arrange
        TestItem?[] items = [null];

        // Act
        Dictionary<string, string> result = items.GetListDataForSelect(null!, null!);

        // Assert
        _ = Assert.Single(result);
        Assert.Equal(string.Empty, result.Keys.First());
        Assert.Equal(string.Empty, result.Values.First());
    }

    [Fact]
    public void GetListDataForSelect_HandlesNullPropertyValues()
    {
        // Arrange
        TestItem[] items =
        [
            new TestItem { Id = 1, Name = null }
        ];

        // Act
        Dictionary<string, string> result = items.GetListDataForSelect("Id", "Name");

        // Assert
        _ = Assert.Single(result);
        Assert.Equal(string.Empty, result["1"]);
    }

    [Fact]
    public void GetListDataForSelect_ReturnsEmptyDictionary_WhenItemsIsNull()
    {
        // Arrange
        IEnumerable<TestItem>? items = null;

        // Act
        Dictionary<string, string> result = items.GetListDataForSelect("Id", "Name");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetListDataForSelect_ThrowsOnDuplicateKeys()
    {
        // Arrange
        TestItem[] items =
        [
            new TestItem { Id = 1, Name = "A" },
            new TestItem { Id = 1, Name = "B" }
        ];

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => items.GetListDataForSelect("Id", "Name"));
    }

    [Fact]
    public void GetListDataForSelect_UsesProperties_WhenFieldsAreValid()
    {
        // Arrange
        TestItem[] items =
        [
            new TestItem { Id = 1, Name = "A" },
            new TestItem { Id = 2, Name = "B" }
        ];

        // Act
        Dictionary<string, string> result = items.GetListDataForSelect("Id", "Name");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("A", result["1"]);
        Assert.Equal("B", result["2"]);
    }

    [Fact]
    public void GetListDataForSelect_UsesToString_WhenFieldsAreNull()
    {
        // Arrange
        TestItem[] items =
        [
            new TestItem { Id = 1, Name = "A" }
        ];

        // Act
        Dictionary<string, string> result = items.GetListDataForSelect(null!, null!);
        string expectedKey = items[0].ToString();

        // Assert
        _ = Assert.Single(result);
        Assert.Equal(expectedKey, result.Keys.First());
        Assert.Equal(expectedKey, result.Values.First());
    }

    #endregion Public Methods
}
