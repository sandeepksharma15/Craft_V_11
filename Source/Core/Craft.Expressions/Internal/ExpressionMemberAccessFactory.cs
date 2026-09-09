using System.Linq.Expressions;
using System.Reflection;

namespace Craft.Expressions;

internal static class ExpressionMemberAccessFactory
{
    public static LambdaExpression CreateMemberExpression(Type? type, string? memberName)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        MemberInfo member = FindMember(type, memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            ?? throw new ArgumentException($"Property or field '{memberName}' not found on type '{type.FullName}'.", nameof(memberName));

        var isStatic = IsStatic(member);
        ParameterExpression? parameter = isStatic ? null : Expression.Parameter(type, "x");
        Expression memberAccess = CreateMemberAccess(member, parameter);

        return Expression.Lambda(memberAccess, parameter is null ? [] : [parameter]);
    }

    public static Expression<Func<T, TResult>> CreateInstanceMemberExpression<T, TResult>(string? propertyOrFieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyOrFieldName);

        Type type = typeof(T);
        MemberInfo member = FindMember(type, propertyOrFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Property or field '{propertyOrFieldName}' not found on type '{type.FullName}'.", nameof(propertyOrFieldName));

        Type memberType = GetMemberType(member);
        if (!typeof(TResult).IsAssignableFrom(memberType))
        {
            throw new InvalidOperationException(
                $"Member '{propertyOrFieldName}' type '{memberType}' cannot be assigned to '{typeof(TResult)}'.");
        }

        ParameterExpression parameter = Expression.Parameter(type, "x");
        Expression memberAccess = CreateMemberAccess(member, parameter);

        return Expression.Lambda<Func<T, TResult>>(memberAccess, parameter);
    }

    public static Expression<Func<TResult>> CreateStaticMemberExpression<TResult>(Type? type, string? memberName)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        MemberInfo member = FindMember(type, memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Static property or field '{memberName}' not found on type '{type.FullName}'.", nameof(memberName));

        if (!IsStatic(member))
            throw new ArgumentException($"Member '{memberName}' on type '{type.FullName}' is not static.", nameof(memberName));

        Type memberType = GetMemberType(member);
        return !typeof(TResult).IsAssignableFrom(memberType)
            ? throw new InvalidOperationException(
                $"Member '{memberName}' type '{memberType}' cannot be assigned to '{typeof(TResult)}'.")
            : Expression.Lambda<Func<TResult>>(CreateMemberAccess(member, null));
    }

    private static MemberInfo? FindMember(Type type, string memberName, BindingFlags bindingFlags)
        => (MemberInfo?)type.GetProperty(memberName, bindingFlags)
           ?? type.GetField(memberName, bindingFlags);

    private static Expression CreateMemberAccess(MemberInfo member, ParameterExpression? parameter)
        => member switch
        {
            PropertyInfo property => Expression.Property(parameter, property),
            FieldInfo field => Expression.Field(parameter, field),
            _ => throw new InvalidOperationException("Only properties and fields are supported.")
        };

    private static Type GetMemberType(MemberInfo member)
        => member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new InvalidOperationException("Only properties and fields are supported.")
        };

    private static bool IsStatic(MemberInfo member)
        => member switch
        {
            PropertyInfo property => property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false,
            FieldInfo field => field.IsStatic,
            _ => false
        };
}
