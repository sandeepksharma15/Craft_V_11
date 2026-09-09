using System.Linq.Expressions;

namespace Craft.Extensions.Expressions;

internal static class ExpressionParameterRebinder
{
    public static Expression Replace(Expression expression, ParameterExpression oldParameter, Expression newExpression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(oldParameter);
        ArgumentNullException.ThrowIfNull(newExpression);

        if (oldParameter.Type != newExpression.Type)
            throw new ArgumentException("The type of the new expression must match the type of the old parameter.", nameof(newExpression));

        if (expression is LambdaExpression lambda && lambda.Parameters.Contains(oldParameter))
            throw new ArgumentException("Use the lambda overload when replacing a declared lambda parameter.", nameof(oldParameter));

        return ReplaceCore(expression, new Dictionary<ParameterExpression, Expression>
        {
            [oldParameter] = newExpression
        });
    }

    public static Expression<TDelegate> ReplaceParameter<TDelegate>(
        Expression<TDelegate> expression,
        ParameterExpression oldParameter,
        ParameterExpression newParameter) where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(oldParameter);
        ArgumentNullException.ThrowIfNull(newParameter);

        if (oldParameter.Type != newParameter.Type)
            throw new ArgumentException("The type of the new parameter must match the type of the old parameter.", nameof(newParameter));

        var parameters = new ParameterExpression[expression.Parameters.Count];
        var found = false;

        for (var i = 0; i < expression.Parameters.Count; i++)
        {
            var parameter = expression.Parameters[i];
            if (ReferenceEquals(parameter, oldParameter))
            {
                parameters[i] = newParameter;
                found = true;
                continue;
            }

            parameters[i] = parameter;
        }

        var body = ReplaceCore(expression.Body, new Dictionary<ParameterExpression, Expression>
        {
            [oldParameter] = newParameter
        });

        return !found
            ? expression.Update(body, expression.Parameters)
            : Expression.Lambda<TDelegate>(body, expression.Name, expression.TailCall, parameters);
    }

    internal static Expression Rebind(Expression expression, ParameterExpression sourceParameter, ParameterExpression targetParameter)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(sourceParameter);
        ArgumentNullException.ThrowIfNull(targetParameter);

        if (sourceParameter.Type != targetParameter.Type)
            throw new ArgumentException("The target parameter type must match the source parameter type.", nameof(targetParameter));

        return ReplaceCore(expression, new Dictionary<ParameterExpression, Expression>
        {
            [sourceParameter] = targetParameter
        });
    }

    private static Expression ReplaceCore(Expression expression, IReadOnlyDictionary<ParameterExpression, Expression> replacements)
        => new ParameterReplacingExpressionVisitor(replacements).Visit(expression)!;

    private sealed class ParameterReplacingExpressionVisitor(IReadOnlyDictionary<ParameterExpression, Expression> replacements) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => replacements.TryGetValue(node, out var replacement) ? replacement : node;

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Dictionary<ParameterExpression, Expression>? scopedReplacements = null;

            foreach (var parameter in node.Parameters)
            {
                if (!replacements.ContainsKey(parameter))
                    continue;

                scopedReplacements ??= new Dictionary<ParameterExpression, Expression>(replacements);
                scopedReplacements.Remove(parameter);
            }

            if (scopedReplacements is null)
                return base.VisitLambda(node);

            var body = new ParameterReplacingExpressionVisitor(scopedReplacements).Visit(node.Body)!;
            return node.Update(body, node.Parameters);
        }
    }
}
