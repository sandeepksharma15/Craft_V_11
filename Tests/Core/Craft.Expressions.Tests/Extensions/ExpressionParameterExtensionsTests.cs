using System.Linq.Expressions;
using Craft.Expressions.Extensions;

namespace Craft.Expressions.Tests.Extensions;

public class ExpressionParameterExtensionsTests
{
    [Fact]
    public void ReplaceParameter_OnExpressionBody_ReplacesReferencedParameter()
    {
        Expression<Func<int, int>> expression = x => x + 2;
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        Expression replaced = expression.Body.ReplaceParameter(expression.Parameters[0], newParameter);
        var lambda = Expression.Lambda<Func<int, int>>(replaced, newParameter);

        Assert.Equal(5, lambda.Compile()(3));
    }

    [Fact]
    public void ReplaceParameter_OnExpressionBody_ThrowsForDeclaredLambdaParameter()
    {
        Expression<Func<int, int>> expression = x => x * 2;
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ((Expression)expression).ReplaceParameter(expression.Parameters[0], newParameter));

        Assert.Contains("lambda", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReplaceParameter_OnLambda_ReplacesDeclaredParameterAndCompiles()
    {
        Expression<Func<int, int>> expression = x => x * 2;
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        Expression<Func<int, int>> replaced = expression.ReplaceParameter(expression.Parameters[0], newParameter);

        Assert.Equal(10, replaced.Compile()(5));
    }

    [Fact]
    public void ReplaceParameter_OnLambda_ReplacesCapturedOuterParameterInsideNestedLambda()
    {
        Expression<Func<int, Func<int, int>>> expression = x => z => x + z;
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        Expression<Func<int, Func<int, int>>> replaced = expression.ReplaceParameter(expression.Parameters[0], newParameter);
        Func<int, Func<int, int>> compiled = replaced.Compile();

        Assert.Equal(8, compiled(3)(5));
    }

    [Fact]
    public void ReplaceParameter_OnLambda_DoesNotReplaceShadowedNestedParameter()
    {
        Expression<Func<int, Func<int, int>>> expression = x => x => x + 1;
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        Expression<Func<int, Func<int, int>>> replaced = expression.ReplaceParameter(expression.Parameters[0], newParameter);
        Func<int, Func<int, int>> compiled = replaced.Compile();

        Assert.Equal(6, compiled(3)(5));
    }

    [Fact]
    public void ReplaceParameter_OnExpressionBody_ThrowsWhenTypeDoesNotMatch()
    {
        Expression<Func<int, int>> expression = x => x + 1;
        ParameterExpression wrongTypeParameter = Expression.Parameter(typeof(string), "y");

        _ = Assert.Throws<ArgumentException>(() => expression.Body.ReplaceParameter(expression.Parameters[0], wrongTypeParameter));
    }

    [Fact]
    public void ReplaceParameter_OnLambda_ThrowsWhenTypeDoesNotMatch()
    {
        Expression<Func<int, int>> expression = x => x + 1;
        ParameterExpression wrongTypeParameter = Expression.Parameter(typeof(string), "y");

        _ = Assert.Throws<ArgumentException>(() => expression.ReplaceParameter(expression.Parameters[0], wrongTypeParameter));
    }
}
