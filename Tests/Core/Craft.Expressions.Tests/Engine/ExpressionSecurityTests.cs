using Craft.Expressions.Engine;
using Craft.Expressions.Exceptions;

namespace Craft.Expressions.Tests;

public class ExpressionSecurityTests
{
    private static ExpressionSerializer<TestClass> Serializer => new();

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [Fact]
    public void Deserialize_ThrowsArgumentException_WhenInputIsNull()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => Serializer.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_ThrowsArgumentException_WhenInputIsEmpty()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => Serializer.Deserialize(string.Empty));
    }

    [Fact]
    public void Deserialize_ThrowsArgumentException_WhenInputIsWhitespace()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => Serializer.Deserialize("   "));
    }

    [Fact]
    public void Deserialize_ThrowsArgumentException_WhenExpressionExceedsMaxLength()
    {
        // Arrange
        string longExpression = new('a', ExpressionSerializer<TestClass>.MaxExpressionLength + 1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Serializer.Deserialize(longExpression));
        Assert.Contains("exceeds maximum length", ex.Message);
    }

    [Fact]
    public void Deserialize_Succeeds_WhenExpressionIsAtMaxLength()
    {
        // Arrange - Create valid expression at max length
        string baseExpression = "Age == 42";
        string padding = new(' ', ExpressionSerializer<TestClass>.MaxExpressionLength - baseExpression.Length);
        string maxLengthExpression = baseExpression + padding;

        // Act - Should not throw
        var result = Serializer.Deserialize(maxLengthExpression);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Deserialize_ThrowsExpressionParseException_WhenDepthExceedsMaximum()
    {
        // Arrange - Create deeply nested expression
        int depth = 101; // More than max depth of 100
        string expression = string.Concat(Enumerable.Repeat("(", depth)) +
                        "Age == 42" +
                        string.Concat(Enumerable.Repeat(")", depth));

        // Act & Assert
        var ex = Assert.Throws<ExpressionParseException>(() => Serializer.Deserialize(expression));
        Assert.Contains("depth exceeds maximum", ex.Message);
    }

    [Fact]
    public void Deserialize_Succeeds_WithDeeplyNestedButValidExpression()
    {
        // Arrange - Create nested expression within limit
        int depth = 50; // Well within max depth of 100
        string expression = string.Concat(Enumerable.Repeat("(", depth)) +
                        "Age == 42" +
                        string.Concat(Enumerable.Repeat(")", depth));

        // Act
        var result = Serializer.Deserialize(expression);

        // Assert
        Assert.NotNull(result);
        var compiled = result.Compile();
        Assert.True(compiled(new TestClass { Age = 42 }));
    }

    [Fact]
    public void Serialize_ThrowsArgumentNullException_WhenExpressionIsNull()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => Serializer.Serialize(null!));
    }
}
