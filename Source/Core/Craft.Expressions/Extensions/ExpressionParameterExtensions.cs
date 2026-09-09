using System.Linq.Expressions;

namespace Craft.Expressions;

public static class ExpressionParameterExtensions
{
    extension(Expression expression)
    {
        /// <summary>
        /// Replaces references to a specific parameter in an expression body with a new expression.
        /// </summary>
        public Expression ReplaceParameter(ParameterExpression oldParameter, Expression newExpression)
        {
            ArgumentNullException.ThrowIfNull(expression);
            return ExpressionParameterRebinder.Replace(expression, oldParameter, newExpression);
        }
    }

    extension<TDelegate>(Expression<TDelegate> expression) where TDelegate : Delegate
    {
        /// <summary>
        /// Replaces a declared lambda parameter with a new parameter while preserving a valid lambda.
        /// </summary>
        public Expression<TDelegate> ReplaceParameter(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            ArgumentNullException.ThrowIfNull(expression);
            return ExpressionParameterRebinder.ReplaceParameter(expression, oldParameter, newParameter);
        }
    }
}
