using System.Linq.Expressions;

namespace Craft.Expressions.Tests;

public class ExpressionParameterExtensionsTests
{
    [Fact]
    public void ReplaceParameter_OnExpressionBody_ReplacesReferencedParameter()
    {
        Expression<Func<int, int>> expression = x => x + 2;
        var newParameter = Expression.Parameter(typeof(int), "y");

        var replaced = expression.Body.ReplaceParameter(expression.Parameters[0], newParameter);
        var lambda = Expression.Lambda<Func<int, int>>(replaced, newParameter);

        Assert.Equal(5, lambda.Compile()(3));
    }

    [Fact]
    public void ReplaceParameter_OnExpressionBody_ThrowsForDeclaredLambdaParameter()
    {
        Expression<Func<int, int>> expression = x => x * 2;
        var newParameter = Expression.Parameter(typeof(int), "y");

        var exception = Assert.Throws<ArgumentException>(() => ((Expression)expression).ReplaceParameter(expression.Parameters[0], newParameter));

        Assert.Contains("lambda", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReplaceParameter_OnLambda_ReplacesDeclaredParameterAndCompiles()
    {
        Expression<Func<int, int>> expression = x => x * 2;
        var newParameter = Expression.Parameter(typeof(int), "y");

        var replaced = expression.ReplaceParameter(expression.Parameters[0], newParameter);

        Assert.Equal(10, replaced.Compile()(5));
    }

    [Fact]
    public void ReplaceParameter_OnLambda_ReplacesCapturedOuterParameterInsideNestedLambda()
    {
        Expression<Func<int, Func<int, int>>> expression = x => z => x + z;
        var newParameter = Expression.Parameter(typeof(int), "y");

        var replaced = expression.ReplaceParameter(expression.Parameters[0], newParameter);
        var compiled = replaced.Compile();

        Assert.Equal(8, compiled(3)(5));
    }

    [Fact]
    public void ReplaceParameter_OnLambda_DoesNotReplaceShadowedNestedParameter()
    {
        Expression<Func<int, Func<int, int>>> expression = x => x => x + 1;
        var newParameter = Expression.Parameter(typeof(int), "y");

        var replaced = expression.ReplaceParameter(expression.Parameters[0], newParameter);
        var compiled = replaced.Compile();

        Assert.Equal(6, compiled(3)(5));
    }

    [Fact]
    public void ReplaceParameter_OnExpressionBody_ThrowsWhenTypeDoesNotMatch()
    {
        Expression<Func<int, int>> expression = x => x + 1;
        var wrongTypeParameter = Expression.Parameter(typeof(string), "y");

        Assert.Throws<ArgumentException>(() => expression.Body.ReplaceParameter(expression.Parameters[0], wrongTypeParameter));
    }

    [Fact]
    public void ReplaceParameter_OnLambda_ThrowsWhenTypeDoesNotMatch()
    {
        Expression<Func<int, int>> expression = x => x + 1;
        var wrongTypeParameter = Expression.Parameter(typeof(string), "y");

        Assert.Throws<ArgumentException>(() => expression.ReplaceParameter(expression.Parameters[0], wrongTypeParameter));
    }
}
