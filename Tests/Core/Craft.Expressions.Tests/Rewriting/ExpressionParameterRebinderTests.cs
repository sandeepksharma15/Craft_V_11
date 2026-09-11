using System.Linq.Expressions;
using Craft.Expressions.Rewriting;

namespace Craft.Expressions.Tests.Rewriting;

public class ExpressionParameterRebinderTests
{
    [Fact]
    public void Replace_NullExpression_ThrowsArgumentNullException()
    {
        ParameterExpression oldParameter = Expression.Parameter(typeof(int), "x");
        ConstantExpression newExpression = Expression.Constant(1);

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.Replace(null!, oldParameter, newExpression));
    }

    [Fact]
    public void Replace_NullOldParameter_ThrowsArgumentNullException()
    {
        Expression expression = Expression.Constant(1);
        ConstantExpression newExpression = Expression.Constant(1);

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.Replace(expression, null!, newExpression));
    }

    [Fact]
    public void Replace_NullNewExpression_ThrowsArgumentNullException()
    {
        Expression expression = Expression.Constant(1);
        ParameterExpression oldParameter = Expression.Parameter(typeof(int), "x");

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.Replace(expression, oldParameter, null!));
    }

    [Fact]
    public void Replace_TypeMismatch_ThrowsArgumentException()
    {
        ParameterExpression oldParameter = Expression.Parameter(typeof(int), "x");
        ConstantExpression newExpression = Expression.Constant("wrong");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExpressionParameterRebinder.Replace(oldParameter, oldParameter, newExpression));

        Assert.Equal("newExpression", exception.ParamName);
    }

    [Fact]
    public void Replace_DeclaredLambdaParameter_ThrowsArgumentException()
    {
        Expression<Func<int, int>> expression = x => x + 1;

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExpressionParameterRebinder.Replace(expression, expression.Parameters[0], Expression.Constant(5)));

        Assert.Equal("oldParameter", exception.ParamName);
        Assert.Contains("lambda", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Replace_BodyParameter_ReplacesParameterReference()
    {
        ParameterExpression oldParameter = Expression.Parameter(typeof(int), "x");
        BinaryExpression body = Expression.Add(oldParameter, Expression.Constant(2));

        Expression replaced = ExpressionParameterRebinder.Replace(body, oldParameter, Expression.Constant(10));
        int result = Expression.Lambda<Func<int>>(replaced).Compile()();

        Assert.Equal(12, result);
    }

    [Fact]
    public void ReplaceParameter_NullExpression_ThrowsArgumentNullException()
    {
        ParameterExpression oldParameter = Expression.Parameter(typeof(int), "x");
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.ReplaceParameter<Func<int, int>>(null!, oldParameter, newParameter));
    }

    [Fact]
    public void ReplaceParameter_NullOldParameter_ThrowsArgumentNullException()
    {
        Expression<Func<int, int>> expression = x => x + 1;
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.ReplaceParameter(expression, null!, newParameter));
    }

    [Fact]
    public void ReplaceParameter_NullNewParameter_ThrowsArgumentNullException()
    {
        Expression<Func<int, int>> expression = x => x + 1;

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.ReplaceParameter(expression, expression.Parameters[0], null!));
    }

    [Fact]
    public void ReplaceParameter_TypeMismatch_ThrowsArgumentException()
    {
        Expression<Func<int, int>> expression = x => x + 1;
        ParameterExpression wrongType = Expression.Parameter(typeof(string), "y");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExpressionParameterRebinder.ReplaceParameter(expression, expression.Parameters[0], wrongType));

        Assert.Equal("newParameter", exception.ParamName);
    }

    [Fact]
    public void ReplaceParameter_DeclaredLambdaParameter_ReplacesParameterAndBodyReference()
    {
        Expression<Func<int, int>> expression = x => x * 3;
        ParameterExpression newParameter = Expression.Parameter(typeof(int), "y");

        Expression<Func<int, int>> replaced = ExpressionParameterRebinder.ReplaceParameter(expression, expression.Parameters[0], newParameter);

        Assert.Same(newParameter, replaced.Parameters[0]);
        Assert.Equal(12, replaced.Compile()(4));
    }

    [Fact]
    public void ReplaceParameter_OldParameterNotDeclared_ReturnsUpdatedBodyWithOriginalParameters()
    {
        ParameterExpression outer = Expression.Parameter(typeof(int), "outer");
        ParameterExpression inner = Expression.Parameter(typeof(int), "inner");
        BinaryExpression body = Expression.Add(inner, Expression.Constant(1));
        Expression<Func<int, int>> expression = Expression.Lambda<Func<int, int>>(body, inner);

        Expression<Func<int, int>> replaced = ExpressionParameterRebinder.ReplaceParameter(expression, outer, Expression.Parameter(typeof(int), "target"));

        Assert.Same(expression.Parameters[0], replaced.Parameters[0]);
        Assert.Equal(6, replaced.Compile()(5));
    }

    [Fact]
    public void Rebind_NullExpression_ThrowsArgumentNullException()
    {
        ParameterExpression sourceParameter = Expression.Parameter(typeof(int), "x");
        ParameterExpression targetParameter = Expression.Parameter(typeof(int), "y");

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.Rebind(null!, sourceParameter, targetParameter));
    }

    [Fact]
    public void Rebind_NullSourceParameter_ThrowsArgumentNullException()
    {
        Expression expression = Expression.Constant(1);
        ParameterExpression targetParameter = Expression.Parameter(typeof(int), "y");

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.Rebind(expression, null!, targetParameter));
    }

    [Fact]
    public void Rebind_NullTargetParameter_ThrowsArgumentNullException()
    {
        Expression expression = Expression.Constant(1);
        ParameterExpression sourceParameter = Expression.Parameter(typeof(int), "x");

        _ = Assert.Throws<ArgumentNullException>(() => ExpressionParameterRebinder.Rebind(expression, sourceParameter, null!));
    }

    [Fact]
    public void Rebind_TypeMismatch_ThrowsArgumentException()
    {
        ParameterExpression sourceParameter = Expression.Parameter(typeof(int), "x");
        ParameterExpression targetParameter = Expression.Parameter(typeof(string), "y");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExpressionParameterRebinder.Rebind(sourceParameter, sourceParameter, targetParameter));

        Assert.Equal("targetParameter", exception.ParamName);
    }

    [Fact]
    public void Rebind_ParameterInExpression_ReplacesParameterReference()
    {
        ParameterExpression sourceParameter = Expression.Parameter(typeof(int), "x");
        ParameterExpression targetParameter = Expression.Parameter(typeof(int), "y");
        BinaryExpression body = Expression.Add(sourceParameter, Expression.Constant(1));

        Expression rebound = ExpressionParameterRebinder.Rebind(body, sourceParameter, targetParameter);
        var lambda = Expression.Lambda<Func<int, int>>(rebound, targetParameter);

        Assert.Equal(5, lambda.Compile()(4));
    }

    [Fact]
    public void Rebind_ShadowedNestedLambdaParameter_IsNotReplaced()
    {
        ParameterExpression outer = Expression.Parameter(typeof(int), "outer");
        ParameterExpression nested = Expression.Parameter(typeof(int), "outer");
        Expression<Func<int, Func<int, int>>> expression = Expression.Lambda<Func<int, Func<int, int>>>(
            Expression.Lambda<Func<int, int>>(Expression.Add(outer, nested), nested),
            outer);

        ParameterExpression target = Expression.Parameter(typeof(int), "target");
        Expression<Func<int, Func<int, int>>> rebound = (Expression<Func<int, Func<int, int>>>)ExpressionParameterRebinder.Rebind(expression, outer, target);
        Func<int, Func<int, int>> compiled = rebound.Compile();

        Assert.Equal(7, compiled(4)(3));
    }
}
