using System.Linq.Expressions;

namespace Craft.Extensions.Expressions;

public static class PredicateCompositionExtensions
{
    extension<T>(Expression<Func<T, bool>> expression)
    {
        /// <summary>
        /// Combines two boolean expressions into a single expression using a logical AND operation.
        /// </summary>
        public Expression<Func<T, bool>> And(Expression<Func<T, bool>> other)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(other);

            var parameter = Expression.Parameter(typeof(T), "x");
            var left = ExpressionParameterRebinder.Rebind(expression.Body, expression.Parameters[0], parameter);
            var right = ExpressionParameterRebinder.Rebind(other.Body, other.Parameters[0], parameter);

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        }

        /// <summary>
        /// Combines two boolean expressions into a single expression using a logical OR operation.
        /// </summary>
        public Expression<Func<T, bool>> Or(Expression<Func<T, bool>> other)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(other);

            var parameter = Expression.Parameter(typeof(T), "x");
            var left = ExpressionParameterRebinder.Rebind(expression.Body, expression.Parameters[0], parameter);
            var right = ExpressionParameterRebinder.Rebind(other.Body, other.Parameters[0], parameter);

            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left, right), parameter);
        }
    }
}
