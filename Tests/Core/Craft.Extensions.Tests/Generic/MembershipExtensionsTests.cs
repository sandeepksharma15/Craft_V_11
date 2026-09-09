namespace Craft.Extensions.Tests.Generic;

public class MembershipExtensionsTests
{
    [Fact]
    public void IsIn_ReturnsTrue_IfItemIsInCollection()
    {
        // Arrange
        int[] collection = [1, 2, 3];

        // Act & Assert
        Assert.True(2.IsIn(collection));
    }

    [Fact]
    public void IsIn_ReturnsFalse_IfItemIsNotInCollection()
    {
        // Arrange
        int[] collection = [1, 2, 3];

        // Act & Assert
        Assert.False(4.IsIn(collection));
    }

    [Fact]
    public void IsIn_WorksWithReferenceTypes()
    {
        // Arrange
        TestItem a = new() { Id = 1, Name = "A" };
        TestItem b = new() { Id = 2, Name = "B" };
        TestItem[] collection = [a, b];

        // Act & Assert
        Assert.True(a.IsIn(collection));
        Assert.False(new TestItem { Id = 1, Name = "A" }.IsIn(collection));
    }

    [Fact]
    public void IsIn_Should_Return_False_When_Item_Is_Not_In_List()
    {
        // Arrange
        const int item = 4;
        int[] list = [1, 2, 3];

        // Act
        bool result = item.IsIn(list);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsIn_Should_Return_False_When_Item_Is_Not_In_List_With_Multiple_Parameters()
    {
        // Arrange
        const string item = "lion";
        string[] list = ["cat", "dog", "elephant"];

        // Act
        bool result = item.IsIn(list);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsIn_Should_Return_True_When_Item_Is_In_List()
    {
        // Arrange
        const int item = 2;
        int[] list = [1, 2, 3];

        // Act
        bool result = item.IsIn(list);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsIn_Should_Return_True_When_Item_Is_In_List_With_Multiple_Parameters()
    {
        // Arrange
        const string item = "dog";
        string[] list = ["cat", "dog", "elephant"];

        // Act
        bool result = item.IsIn(list);

        // Assert
        Assert.True(result);
    }

    private sealed class TestItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public override string ToString() => $"TestItem:{Id}:{Name}";
    }
}
