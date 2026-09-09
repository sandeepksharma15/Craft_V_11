using System.Linq.Expressions;

namespace Craft.Expressions;

public static class ExpressionMemberAccessExtensions
{
    extension(string propertyOrFieldName)
    {
        /// <summary>
        /// Creates a lambda expression for accessing a specified property or field of the given type.
        /// </summary>
        public LambdaExpression CreateMemberExpression<T>()
            => ExpressionMemberAccessFactory.CreateMemberExpression(typeof(T), propertyOrFieldName);

        /// <summary>
        /// Creates a strongly typed lambda expression for accessing a specified instance property or field.
        /// </summary>
        public Expression<Func<T, TResult>> CreateMemberExpression<T, TResult>()
            => ExpressionMemberAccessFactory.CreateInstanceMemberExpression<T, TResult>(propertyOrFieldName);
    }

    extension(Type type)
    {
        /// <summary>
        /// Creates a lambda expression for accessing a specified property or field of the given type.
        /// </summary>
        public LambdaExpression CreateMemberExpression(string memberName)
            => ExpressionMemberAccessFactory.CreateMemberExpression(type, memberName);

        /// <summary>
        /// Creates a strongly typed lambda expression for accessing a specified static property or field.
        /// </summary>
        public Expression<Func<TResult>> CreateStaticMemberExpression<TResult>(string memberName)
            => ExpressionMemberAccessFactory.CreateStaticMemberExpression<TResult>(type, memberName);
    }
}
