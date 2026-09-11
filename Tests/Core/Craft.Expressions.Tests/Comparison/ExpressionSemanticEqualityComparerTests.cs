using System.Linq.Expressions;
using Craft.Expressions.Comparison;

namespace Craft.Expressions.Tests.Comparison;

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

    [Fact]
    public void GetHashCode_Throws_ForNullExpression()
    {
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.Throws<ArgumentNullException>(() => comparer.GetHashCode(null!));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForEqualComparedToBooleanTrue()
    {
        ParameterExpression flag = Expression.Parameter(typeof(bool), "flag");
        BinaryExpression expr1 = Expression.Equal(flag, Expression.Constant(true));
        BinaryExpression expr2 = Expression.Equal(Expression.Constant(true), flag);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, flag));
        Assert.True(comparer.Equals(expr2, flag));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForEqualComparedToBooleanFalse()
    {
        ParameterExpression flag = Expression.Parameter(typeof(bool), "flag");
        BinaryExpression expr1 = Expression.Equal(flag, Expression.Constant(false));
        BinaryExpression expr2 = Expression.Equal(Expression.Constant(false), flag);
        UnaryExpression expected = Expression.Not(flag);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expected));
        Assert.True(comparer.Equals(expr2, expected));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForNotEqualComparedToBooleanFalse()
    {
        ParameterExpression flag = Expression.Parameter(typeof(bool), "flag");
        BinaryExpression expr1 = Expression.NotEqual(flag, Expression.Constant(false));
        BinaryExpression expr2 = Expression.NotEqual(Expression.Constant(false), flag);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, flag));
        Assert.True(comparer.Equals(expr2, flag));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForNotEqualComparedToBooleanTrue()
    {
        ParameterExpression flag = Expression.Parameter(typeof(bool), "flag");
        BinaryExpression expr1 = Expression.NotEqual(flag, Expression.Constant(true));
        BinaryExpression expr2 = Expression.NotEqual(Expression.Constant(true), flag);
        UnaryExpression expected = Expression.Not(flag);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expected));
        Assert.True(comparer.Equals(expr2, expected));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForCommutativeAdd()
    {
        ParameterExpression a = Expression.Parameter(typeof(int), "a");
        ParameterExpression b = Expression.Parameter(typeof(int), "b");
        BinaryExpression expr1 = Expression.Add(a, b);
        BinaryExpression expr2 = Expression.Add(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForCommutativeMultiply()
    {
        ParameterExpression a = Expression.Parameter(typeof(int), "a");
        ParameterExpression b = Expression.Parameter(typeof(int), "b");
        BinaryExpression expr1 = Expression.Multiply(a, b);
        BinaryExpression expr2 = Expression.Multiply(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForCommutativeAnd()
    {
        ParameterExpression a = Expression.Parameter(typeof(bool), "a");
        ParameterExpression b = Expression.Parameter(typeof(bool), "b");
        BinaryExpression expr1 = Expression.And(a, b);
        BinaryExpression expr2 = Expression.And(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForCommutativeOr()
    {
        ParameterExpression a = Expression.Parameter(typeof(bool), "a");
        ParameterExpression b = Expression.Parameter(typeof(bool), "b");
        BinaryExpression expr1 = Expression.Or(a, b);
        BinaryExpression expr2 = Expression.Or(b, a);
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForEquivalentMemberExpressions()
    {
        MemberExpression expr1 = Expression.Property(Expression.Constant("hello"), nameof(string.Length));
        MemberExpression expr2 = Expression.Property(Expression.Constant("hello"), nameof(string.Length));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentMemberExpressions()
    {
        MemberExpression expr1 = Expression.Property(Expression.Constant("hello"), nameof(string.Length));
        MemberExpression expr2 = Expression.Property(Expression.Constant("world!"), nameof(string.Length));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.False(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForEquivalentStaticMemberExpressions()
    {
        MemberExpression expr1 = Expression.Property(null, typeof(DateTime), nameof(DateTime.Now));
        MemberExpression expr2 = Expression.Property(null, typeof(DateTime), nameof(DateTime.Now));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsTrue_ForEquivalentMethodCalls()
    {
        MethodCallExpression expr1 = Expression.Call(Expression.Constant("hello"), nameof(string.Contains), Type.EmptyTypes, Expression.Constant("ell"));
        MethodCallExpression expr2 = Expression.Call(Expression.Constant("hello"), nameof(string.Contains), Type.EmptyTypes, Expression.Constant("ell"));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentMethodCallArguments()
    {
        MethodCallExpression expr1 = Expression.Call(Expression.Constant("hello"), nameof(string.Contains), Type.EmptyTypes, Expression.Constant("ell"));
        MethodCallExpression expr2 = Expression.Call(Expression.Constant("hello"), nameof(string.Contains), Type.EmptyTypes, Expression.Constant("wor"));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.False(comparer.Equals(expr1, expr2));
    }

    [Fact]
    public void Equals_UsesFallbackComparison_ForUnhandledExpressionNodeTypes()
    {
        ConditionalExpression expr1 = Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant(2));
        ConditionalExpression expr2 = Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant(2));
        ConditionalExpression expr3 = Expression.Condition(Expression.Constant(false), Expression.Constant(1), Expression.Constant(2));
        var comparer = new ExpressionSemanticEqualityComparer();

        Assert.True(comparer.Equals(expr1, expr2));
        Assert.False(comparer.Equals(expr1, expr3));
    }
}
