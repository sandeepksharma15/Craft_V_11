using System.Linq.Expressions;
using Craft.Expressions.Internal;

namespace Craft.Expressions.Extensions;

public static class ExpressionPropertyPathExtensions
{
    extension<T>(Expression<Func<T, object>> expression)
    {
        /// <summary>
        /// Gets the full property path from a lambda expression, including navigation properties.
        /// </summary>
        public string GetFullPropertyPath()
        {
            ArgumentNullException.ThrowIfNull(expression);
            return ExpressionPropertyPathExtractor.GetFullPropertyPath(expression.Body);
        }

        /// <summary>
        /// Gets the final property name from a lambda expression.
        /// </summary>
        public string GetFinalPropertyName()
        {
            ArgumentNullException.ThrowIfNull(expression);

            string path = ExpressionPropertyPathExtractor.GetFullPropertyPath(expression.Body);
            int lastDotIndex = path.LastIndexOf('.');
            return lastDotIndex >= 0 ? path[(lastDotIndex + 1)..] : path;
        }
    }

    extension(LambdaExpression expression)
    {
        /// <summary>
        /// Gets the full property path from a lambda expression, including navigation properties.
        /// </summary>
        public string GetFullPropertyPath()
        {
            ArgumentNullException.ThrowIfNull(expression);
            return ExpressionPropertyPathExtractor.GetFullPropertyPath(expression.Body);
        }
    }
}
