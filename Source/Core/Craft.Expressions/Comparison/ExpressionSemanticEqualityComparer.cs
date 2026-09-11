using System.Linq.Expressions;

namespace Craft.Expressions.Comparison;

/// <summary>
/// Provides semantic equality comparison for expression trees by canonicalizing equivalent forms.
/// </summary>
public sealed class ExpressionSemanticEqualityComparer : IEqualityComparer<Expression>
{
    public static ExpressionSemanticEqualityComparer Instance { get; } = new();

    public bool Equals(Expression? x, Expression? y)
    {
        return ReferenceEquals(x, y) || x is not null && y is not null && ExpressionStructuralComparer.AreEqual(Canonicalize(x), Canonicalize(y));
    }

    public int GetHashCode(Expression obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return StringComparer.Ordinal.GetHashCode(Canonicalize(obj).ToString());
    }

    private static Expression Canonicalize(Expression expression)
        => new ExpressionCanonicalizer().Visit(expression)
           ?? throw new InvalidOperationException("Expression canonicalization produced a null result.");

    private sealed class ExpressionCanonicalizer : ExpressionVisitor
    {
        private readonly Stack<Dictionary<ParameterExpression, ParameterExpression>> _scopes = [];

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Expression left = Visit(node.Left)!;
            Expression right = Visit(node.Right)!;

            if (node.NodeType == ExpressionType.Equal)
            {
                if (IsBooleanConstant(right, true))
                    return left;

                if (IsBooleanConstant(left, true))
                    return right;

                if (IsBooleanConstant(right, false))
                    return Expression.Not(left);

                if (IsBooleanConstant(left, false))
                    return Expression.Not(right);
            }

            if (node.NodeType == ExpressionType.NotEqual)
            {
                if (IsBooleanConstant(right, false))
                    return left;

                if (IsBooleanConstant(left, false))
                    return right;

                if (IsBooleanConstant(right, true))
                    return Expression.Not(left);

                if (IsBooleanConstant(left, true))
                    return Expression.Not(right);
            }

            if (IsCommutative(node.NodeType) && string.CompareOrdinal(left.ToString(), right.ToString()) > 0)
                (left, right) = (right, left);

            return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method, node.Conversion);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Dictionary<ParameterExpression, ParameterExpression> scope = new(node.Parameters.Count);
            ParameterExpression[] parameters = new ParameterExpression[node.Parameters.Count];

            for (var i = 0; i < node.Parameters.Count; i++)
            {
                ParameterExpression original = node.Parameters[i];
                ParameterExpression canonical = Expression.Parameter(original.Type, $"p{i}");
                scope[original] = canonical;
                parameters[i] = canonical;
            }

            _scopes.Push(scope);

            try
            {
                Expression body = Visit(node.Body)!;
                return Expression.Lambda<T>(body, node.Name, node.TailCall, parameters);
            }
            finally
            {
                _ = _scopes.Pop();
            }
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            foreach (Dictionary<ParameterExpression, ParameterExpression> scope in _scopes)
            {
                if (scope.TryGetValue(node, out ParameterExpression? canonical))
                    return canonical;
            }

            return node;
        }

        private static bool IsBooleanConstant(Expression expression, bool value)
            => expression is ConstantExpression { Value: bool constantValue }
               && expression.Type == typeof(bool)
               && constantValue == value;

        private static bool IsCommutative(ExpressionType nodeType)
            => nodeType is ExpressionType.Equal
                or ExpressionType.NotEqual
                or ExpressionType.And
                or ExpressionType.AndAlso
                or ExpressionType.Or
                or ExpressionType.OrElse
                or ExpressionType.Add
                or ExpressionType.Multiply;
    }
}

internal static class ExpressionStructuralComparer
{
    public static bool AreEqual(Expression x, Expression y)
    {
        return ReferenceEquals(x, y) || x.NodeType == y.NodeType && x.Type == y.Type && (x, y) switch
        {
            (BinaryExpression left, BinaryExpression right) => AreBinaryExpressionsEqual(left, right),
            (ConstantExpression left, ConstantExpression right) => Equals(left.Value, right.Value),
            (LambdaExpression left, LambdaExpression right) => AreLambdaExpressionsEqual(left, right),
            (MemberExpression left, MemberExpression right) => left.Member == right.Member && AreNullableExpressionsEqual(left.Expression, right.Expression),
            (MethodCallExpression left, MethodCallExpression right) => AreMethodCallExpressionsEqual(left, right),
            (ParameterExpression left, ParameterExpression right) => left.Type == right.Type && left.Name == right.Name,
            (UnaryExpression left, UnaryExpression right) => left.Method == right.Method && AreEqual(left.Operand, right.Operand),
            _ => string.Equals(x.ToString(), y.ToString(), StringComparison.Ordinal)
        };
    }

    private static bool AreBinaryExpressionsEqual(BinaryExpression left, BinaryExpression right)
        => left.Method == right.Method
            && left.IsLifted == right.IsLifted
            && left.IsLiftedToNull == right.IsLiftedToNull
            && AreEqual(left.Left, right.Left)
            && AreEqual(left.Right, right.Right)
            && AreNullableExpressionsEqual(left.Conversion, right.Conversion);

    private static bool AreLambdaExpressionsEqual(LambdaExpression left, LambdaExpression right)
    {
        if (left.Parameters.Count != right.Parameters.Count)
            return false;

        for (var i = 0; i < left.Parameters.Count; i++)
            if (!AreEqual(left.Parameters[i], right.Parameters[i]))
                return false;

        return AreEqual(left.Body, right.Body);
    }

    private static bool AreMethodCallExpressionsEqual(MethodCallExpression left, MethodCallExpression right)
    {
        if (left.Method != right.Method || !AreNullableExpressionsEqual(left.Object, right.Object)
            || left.Arguments.Count != right.Arguments.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Arguments.Count; i++)
            if (!AreEqual(left.Arguments[i], right.Arguments[i]))
                return false;

        return true;
    }

    private static bool AreNullableExpressionsEqual(Expression? left, Expression? right)
        => left is null || right is null ? left is null && right is null : AreEqual(left, right);
}
