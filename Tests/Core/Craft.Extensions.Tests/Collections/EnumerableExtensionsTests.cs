namespace Craft.Extensions.Tests.Collections;

public class EnumerableExtensionsTests
{
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

    private sealed class TestItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public override string ToString() => $"TestItem:{Id}:{Name}";
    }
}
