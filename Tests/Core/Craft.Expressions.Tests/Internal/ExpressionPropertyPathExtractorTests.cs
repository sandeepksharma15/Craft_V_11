using System.Linq.Expressions;
using Craft.Expressions.Internal;

namespace Craft.Expressions.Tests.Internal;

public class ExpressionPropertyPathExtractorTests
{
    [Fact]
    public void GetFullPropertyPath_NullExpression_ThrowsArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => ExpressionPropertyPathExtractor.GetFullPropertyPath(null!));
    }

    [Fact]
    public void GetFullPropertyPath_MemberExpression_ReturnsFullPath()
    {
        Expression<Func<TestEntity, string>> expression = x => x.Location.Country.Code;

        string path = ExpressionPropertyPathExtractor.GetFullPropertyPath(expression.Body);

        Assert.Equal("Location.Country.Code", path);
    }

    [Fact]
    public void GetFullPropertyPath_ConvertExpression_ReturnsPropertyName()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TestEntity), "x");
        MemberExpression member = Expression.Property(parameter, nameof(TestEntity.Id));
        UnaryExpression convert = Expression.Convert(member, typeof(object));

        string path = ExpressionPropertyPathExtractor.GetFullPropertyPath(convert);

        Assert.Equal("Id", path);
    }

    [Fact]
    public void GetFullPropertyPath_ParameterExpression_ThrowsArgumentException()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TestEntity), "x");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExpressionPropertyPathExtractor.GetFullPropertyPath(parameter));

        Assert.Equal("expression", exception.ParamName);
        Assert.Contains("Could not extract property path", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFullPropertyPath_UnsupportedExpression_ThrowsArgumentException()
    {
        Expression<Func<TestEntity, string>> expression = x => x.Name.ToUpper();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExpressionPropertyPathExtractor.GetFullPropertyPath(expression.Body));

        Assert.Equal("expression", exception.ParamName);
        Assert.Contains("is not supported", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFullPropertyPath_MethodCallWithMemberArgument_ReturnsMemberName()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TestEntity), "x");
        MemberExpression member = Expression.Property(parameter, nameof(TestEntity.Name));
        MethodCallExpression methodCall = Expression.Call(typeof(ExpressionPropertyPathExtractorTests), nameof(NullConditionalProxy), null, member);

        string path = ExpressionPropertyPathExtractor.GetFullPropertyPath(methodCall);

        Assert.Equal("Name", path);
    }

    [Fact]
    public void GetFullPropertyPath_IndexerMethodCallWithoutMemberArgument_ReturnsCollectedPath()
    {
        Expression<Func<TestEntity, int>> expression = x => x.Attributes["key"].Length;

        string path = ExpressionPropertyPathExtractor.GetFullPropertyPath(expression.Body);

        Assert.Equal("Length", path);
    }

    [Fact]
    public void GetFullPropertyPath_MethodCallWithoutMemberArgumentOnly_ThrowsArgumentException()
    {
        MethodCallExpression methodCall = Expression.Call(typeof(ExpressionPropertyPathExtractorTests), nameof(NullConditionalProxy), null, Expression.Constant("value"));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExpressionPropertyPathExtractor.GetFullPropertyPath(methodCall));

        Assert.Equal("expression", exception.ParamName);
        Assert.Contains("Could not extract property path", exception.Message, StringComparison.Ordinal);
    }

    public static string? NullConditionalProxy(string? value) => value;

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TestLocation Location { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = [];
    }

    private sealed class TestLocation
    {
        public TestCountry Country { get; set; } = new();
    }

    private sealed class TestCountry
    {
        public string Code { get; set; } = string.Empty;
    }
}
