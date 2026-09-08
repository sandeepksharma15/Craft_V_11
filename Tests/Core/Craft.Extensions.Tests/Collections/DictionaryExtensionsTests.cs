using System.Globalization;

namespace Craft.Extensions.Tests.Collections;

public class DictionaryExtensionsTests
{
    [Fact]
    public void GetOrAdd_WithExistingKey_DoesNotInvokeFactory()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };
        bool wasInvoked = false;

        // Act
        int result = dict.GetOrAdd("key1", _ =>
        {
            wasInvoked = true;
            return 42;
        });

        // Assert
        Assert.Equal(10, result);
        Assert.False(wasInvoked);
    }

    [Fact]
    public void GetOrAdd_WithExistingKey_ReturnsExistingValue()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        int result = dict.GetOrAdd("key1", _ => 42);

        // Assert
        Assert.Equal(10, result);
        _ = Assert.Single(dict);
    }

    [Fact]
    public void GetOrAdd_WithNonExistingKey_AddsAndReturnsNewValue()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        int result = dict.GetOrAdd("key2", _ => 42);

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(2, dict.Count);
        Assert.Equal(42, dict["key2"]);
    }

    [Fact]
    public void GetOrAdd_WithDefaultValue_AddsDefaultWhenKeyNotFound()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        int result = dict.GetOrAdd("key2", 42);

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(42, dict["key2"]);
    }

    [Fact]
    public void AddOrUpdate_WithExistingKey_UpdatesValue()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        dict.AddOrUpdate("key1", 20);

        // Assert
        Assert.Equal(20, dict["key1"]);
        _ = Assert.Single(dict);
    }

    [Fact]
    public void AddOrUpdate_WithNonExistingKey_AddsValue()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        dict.AddOrUpdate("key2", 20);

        // Assert
        Assert.Equal(20, dict["key2"]);
        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void Merge_WithOverwrite_OverwritesExistingKeys()
    {
        // Arrange
        Dictionary<string, int> target = new()
        { ["key1"] = 10, ["key2"] = 20 };
        Dictionary<string, int> source = new()
        { ["key2"] = 25, ["key3"] = 30 };

        // Act
        IDictionary<string, int> result = target.Merge(source, overwriteExisting: true);

        // Assert
        Assert.Same(target, result);
        Assert.Equal(3, result.Count);
        Assert.Equal(10, result["key1"]);
        Assert.Equal(25, result["key2"]);
        Assert.Equal(30, result["key3"]);
    }

    [Fact]
    public void Merge_WithoutOverwrite_DoesNotOverwriteExistingKeys()
    {
        // Arrange
        Dictionary<string, int> target = new()
        { ["key1"] = 10, ["key2"] = 20 };
        Dictionary<string, int> source = new()
        { ["key2"] = 25, ["key3"] = 30 };

        // Act
        IDictionary<string, int> result = target.Merge(source, overwriteExisting: false);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(10, result["key1"]);
        Assert.Equal(20, result["key2"]); // Not overwritten
        Assert.Equal(30, result["key3"]);
    }

    [Fact]
    public void ToQueryString_WithValidDictionary_ReturnsCorrectQueryString()
    {
        // Arrange
        Dictionary<string, string> dict = new()
        {
            ["age"] = "30",
            ["city"] = "New York",
            ["name"] = "John Doe"
        };

        // Act
        string result = dict.ToQueryString();

        // Assert
        Assert.Equal("age=30&city=New%20York&name=John%20Doe", result);
    }

    [Fact]
    public void ToQueryString_WithNullValue_UsesEmptyValue()
    {
        // Arrange
        Dictionary<string, string?> dict = new()
        {
            ["name"] = null
        };

        // Act
        string result = dict.ToQueryString();

        // Assert
        Assert.Equal("name=", result);
    }

    [Fact]
    public void ToQueryString_WithFormattableValue_UsesInvariantCulture()
    {
        // Arrange
        Dictionary<string, CultureAwareFormattable> dict = new()
        {
            ["value"] = new("current-culture", "invariant-culture")
        };

        // Act
        string result = dict.ToQueryString();

        // Assert
        Assert.Equal("value=invariant-culture", result);
    }

    [Fact]
    public void ToQueryString_WithEmptyDictionary_ReturnsEmptyString()
    {
        // Arrange
        Dictionary<string, string> dict = [];

        // Act
        string result = dict.ToQueryString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryRemove_WithExistingKey_RemovesAndReturnsTrue()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10, ["key2"] = 20 };

        // Act
        bool result = dict.TryRemove("key1");

        // Assert
        Assert.True(result);
        _ = Assert.Single(dict);
        Assert.False(dict.ContainsKey("key1"));
    }

    [Fact]
    public void TryRemove_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        bool result = dict.TryRemove("key2");

        // Assert
        Assert.False(result);
        _ = Assert.Single(dict);
    }

    [Fact]
    public void TryRemove_WithOutParameter_ReturnsValueWhenExists()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        bool result = dict.TryRemove("key1", out int value);

        // Assert
        Assert.True(result);
        Assert.Equal(10, value);
        Assert.Empty(dict);
    }

    [Fact]
    public void TryRemove_WithOutParameter_ReturnsDefaultWhenNotExists()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["key1"] = 10 };

        // Act
        bool result = dict.TryRemove("key2", out int value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
        _ = Assert.Single(dict);
    }

    [Fact]
    public void Clone_CreatesShallowCopy()
    {
        // Arrange
        Dictionary<string, int> original = new()
        { ["key1"] = 10, ["key2"] = 20 };

        // Act
        Dictionary<string, int> clone = original.Clone();

        // Assert
        Assert.NotSame(original, clone);
        Assert.Equal(original.Count, clone.Count);
        Assert.Equal(original["key1"], clone["key1"]);
        Assert.Equal(original["key2"], clone["key2"]);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        // Arrange
        Dictionary<string, int> original = new()
        { ["key1"] = 10 };

        // Act
        Dictionary<string, int> clone = original.Clone();
        clone["key1"] = 20;
        clone["key2"] = 30;

        // Assert
        Assert.Equal(10, original["key1"]);
        _ = Assert.Single(original);
    }

    [Fact]
    public void Clone_WithCustomComparer_PreservesComparerBehavior()
    {
        // Arrange
        Dictionary<string, int> original = new(StringComparer.OrdinalIgnoreCase)
        {
            ["key1"] = 10
        };

        // Act
        Dictionary<string, int> clone = original.Clone();

        // Assert
        Assert.True(clone.ContainsKey("KEY1"));
    }

    [Fact]
    public void Invert_SwapsKeysAndValues()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["one"] = 1, ["two"] = 2, ["three"] = 3 };

        // Act
        Dictionary<int, string> inverted = dict.Invert();

        // Assert
        Assert.Equal(3, inverted.Count);
        Assert.Equal("one", inverted[1]);
        Assert.Equal("two", inverted[2]);
        Assert.Equal("three", inverted[3]);
    }

    [Fact]
    public void Invert_WithDuplicateValues_KeepsLastOccurrence()
    {
        // Arrange
        Dictionary<string, int> dict = new()
        { ["one"] = 1, ["uno"] = 1, ["two"] = 2 };

        // Act
        Dictionary<int, string> inverted = dict.Invert();

        // Assert
        Assert.Equal(2, inverted.Count);
        Assert.Equal("uno", inverted[1]); // Last occurrence
        Assert.Equal("two", inverted[2]);
    }

    [Fact]
    public void Merge_ThrowsOnNullTarget()
    {
        // Arrange
        Dictionary<string, int>? target = null;
        Dictionary<string, int> source = [];

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => target!.Merge(source));
    }

    [Fact]
    public void Merge_ThrowsOnNullSource()
    {
        // Arrange
        Dictionary<string, int> target = [];
        Dictionary<string, int>? source = null;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => target.Merge(source!));
    }
}

readonly file record struct CultureAwareFormattable(string CurrentCultureValue, string InvariantCultureValue) : IFormattable
{
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        ReferenceEquals(formatProvider, CultureInfo.InvariantCulture)
            ? InvariantCultureValue
            : CurrentCultureValue;
}
