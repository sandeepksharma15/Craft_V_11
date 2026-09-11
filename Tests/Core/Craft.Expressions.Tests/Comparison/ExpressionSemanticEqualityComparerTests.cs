using System.Linq.Expressions;
using Craft.Expressions.Comparison;

namespace Craft.Expressions.Tests;

public class ExpressionSemanticEqualityComparerTests
{
    [Fact]
    public void Equals_ReturnsTrue_ForReferenceEqual()
    {
        ConstantExpression expr = Expression.Constant(5);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr, expr));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfEitherNull()
    {
        ConstantExpression expr = Expression.Constant(5);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.False(comparer.Equals(expr, null));
        Assert.False(comparer.Equals(null, expr));
        Assert.True(comparer.Equals(null, null));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfNodeTypeDiffers()
    {
        ConstantExpression expr1 = Expression.Constant(1);
        ParameterExpression expr2 = Expression.Parameter(typeof(int));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.False(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForEqualExpressions()
    {
        BinaryExpression expr1 = Expression.Equal(Expression.Constant(1), Expression.Constant(2));
        BinaryExpression expr2 = Expression.Equal(Expression.Constant(1), Expression.Constant(2));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForNotEqualExpressions()
    {
        BinaryExpression expr1 = Expression.NotEqual(Expression.Constant(1), Expression.Constant(2));
        BinaryExpression expr2 = Expression.Equal(Expression.Constant(1), Expression.Constant(2));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.False(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForCommutativeEquality()
    {
        ParameterExpression a = Expression.Parameter(typeof(int), "a");
        ParameterExpression b = Expression.Parameter(typeof(int), "b");
        BinaryExpression expr1 = Expression.Equal(a, b);
        BinaryExpression expr2 = Expression.Equal(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForCommutativeNotEqual()
    {
        ParameterExpression a = Expression.Parameter(typeof(int), "a");
        ParameterExpression b = Expression.Parameter(typeof(int), "b");
        BinaryExpression expr1 = Expression.NotEqual(a, b);
        BinaryExpression expr2 = Expression.NotEqual(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForNonCommutativeBinary()
    {
        ParameterExpression a = Expression.Parameter(typeof(int), "a");
        ParameterExpression b = Expression.Parameter(typeof(int), "b");
        BinaryExpression expr1 = Expression.Subtract(a, b);
        BinaryExpression expr2 = Expression.Subtract(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.False(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualExpressions()
    {
        BinaryExpression expr1 = Expression.Equal(Expression.Constant(1), Expression.Constant(2));
        BinaryExpression expr2 = Expression.Equal(Expression.Constant(1), Expression.Constant(2));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.Equal(comparer.GetHashCode(expr1), comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForCommutativeEquality()
    {
        ParameterExpression a = Expression.Parameter(typeof(int), "a");
        ParameterExpression b = Expression.Parameter(typeof(int), "b");
        BinaryExpression expr1 = Expression.Equal(a, b);
        BinaryExpression expr2 = Expression.Equal(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.Equal(comparer.GetHashCode(expr1), comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_Differs_ForDifferentExpressions()
    {
        BinaryExpression expr1 = Expression.Equal(Expression.Constant(1), Expression.Constant(2));
        BinaryExpression expr2 = Expression.Equal(Expression.Constant(2), Expression.Constant(3));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.NotEqual(comparer.GetHashCode(expr1), comparer.GetHashCode(expr2));
    }

    [Fact]
    public void Equals_Works_ForComplexExpressions()
    {
        ParameterExpression a = Expression.Parameter(typeof(int), "a");
        ParameterExpression b = Expression.Parameter(typeof(int), "b");
        BinaryExpression expr1 = Expression.AndAlso(Expression.Equal(a, b), Expression.Constant(true));
        BinaryExpression expr2 = Expression.AndAlso(Expression.Equal(b, a), Expression.Constant(true));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForEquivalentLambdas_WithDifferentParameterNames()
    {
        Expression<Func<int, bool>> expr1 = value => value > 5;
        Expression<Func<int, bool>> expr2 = number => number > 5;
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEquivalentLambdas_WithDifferentParameterNames()
    {
        Expression<Func<int, bool>> expr1 = value => value > 5;
        Expression<Func<int, bool>> expr2 = number => number > 5;
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.Equal(comparer.GetHashCode(expr1), comparer.GetHashCode(expr2));
    }
}
