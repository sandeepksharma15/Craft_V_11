using System.Linq.Expressions;
using Craft.Expressions.Extensions;

namespace Craft.Expressions.Tests;

public class ExpressionPropertyPathExtensionsTests
{
    [Fact]
    public void GetFullPropertyPath_SimpleProperty_ReturnsPropertyName()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Name;

        Assert.Equal("Name", expression.GetFullPropertyPath());
    }

    [Fact]
    public void GetFullPropertyPath_NavigationProperty_ReturnsFullPath()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Location.Name;

        Assert.Equal("Location.Name", expression.GetFullPropertyPath());
    }

    [Fact]
    public void GetFullPropertyPath_NullForgivingOperator_ReturnsFullPath()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Location!.Name;

        Assert.Equal("Location.Name", expression.GetFullPropertyPath());
    }

    [Fact]
    public void GetFullPropertyPath_DeeplyNestedProperty_ReturnsFullPath()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Location.Country.Code;

        Assert.Equal("Location.Country.Code", expression.GetFullPropertyPath());
    }

    [Fact]
    public void GetFullPropertyPath_ValueTypeProperty_ReturnsPropertyName()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Id;

        Assert.Equal("Id", expression.GetFullPropertyPath());
    }

    [Fact]
    public void GetFullPropertyPath_NavigationPropertyValueType_ReturnsFullPath()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Location.Id;

        Assert.Equal("Location.Id", expression.GetFullPropertyPath());
    }

    [Fact]
    public void GetFullPropertyPath_NullExpression_ThrowsArgumentNullException()
    {
        Expression<Func<TestEntity, object>> expression = null!;

        _ = Assert.Throws<ArgumentNullException>(expression.GetFullPropertyPath);
    }

    [Fact]
    public void GetFinalPropertyName_SimpleProperty_ReturnsPropertyName()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Name;

        Assert.Equal("Name", expression.GetFinalPropertyName());
    }

    [Fact]
    public void GetFinalPropertyName_NavigationProperty_ReturnsFinalPropertyName()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Location.Name;

        Assert.Equal("Name", expression.GetFinalPropertyName());
    }

    [Fact]
    public void GetFinalPropertyName_DeeplyNestedProperty_ReturnsFinalPropertyName()
    {
        Expression<Func<TestEntity, object>> expression = x => x.Location.Country.Code;

        Assert.Equal("Code", expression.GetFinalPropertyName());
    }

    [Fact]
    public void GetFullPropertyPath_LambdaExpression_ReturnsFullPath()
    {
        LambdaExpression expression = (Expression<Func<TestEntity, object>>)(x => x.Location.Name);

        Assert.Equal("Location.Name", expression.GetFullPropertyPath());
    }

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TestLocation Location { get; set; } = new();
    }

    private sealed class TestLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TestCountry Country { get; set; } = new();
    }

    private sealed class TestCountry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
