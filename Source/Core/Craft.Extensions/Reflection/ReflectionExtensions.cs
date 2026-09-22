using System.ComponentModel;
using System.Linq.Expressions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Reflection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ReflectionExtensions
{
    extension(Type type)
    {
        /// <summary>
        /// Gets a property descriptor for a member by name.
        /// </summary>
        public PropertyDescriptor? GetMemberByName(string memberName)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

            var members = TypeDescriptor.GetProperties(type);

            if (!memberName.Contains('.'))
                return members.Find(memberName, true);

            var memberNameParts = memberName.Split('.', 2);
            var topLevelMember = members.Find(memberNameParts[0], true);

            return topLevelMember?.GetChildProperties()?.Find(memberNameParts[1], true);
        }

        /// <summary>
        /// Gets all properties declared on the type and its base types.
        /// </summary>
        public List<PropertyInfo> GetAllProperties()
        {
            ArgumentNullException.ThrowIfNull(type);

            var properties = new List<PropertyInfo>();

            while (type != null)
            {
                properties.AddRange(
                    type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

                type = type.BaseType;
            }

            return properties;
        }

        /// <summary>
        /// Gets a property by name.
        /// </summary>
        public PropertyInfo GetPropertyInfo(string name)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var property = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return property ?? throw new ArgumentException(
                $"Property '{name}' not found in type '{type.FullName}'", nameof(name));
        }
    }

    extension<TSource, TProperty>(Expression<Func<TSource, TProperty>> property)
    {
        /// <summary>
        /// Gets the member name.
        /// </summary>
        public string GetMemberName() => property.GetPropertyInfo().Name;

        /// <summary>
        /// Gets the underlying member type.
        /// </summary>
        public Type? GetMemberType()
        {
            var type = property.GetPropertyInfo().GetMemberUnderlyingType();

            return Nullable.GetUnderlyingType(type!) ?? type;
        }

        /// <summary>
        /// Gets the property represented by the expression.
        /// </summary>
        public PropertyInfo GetPropertyInfo()
            => (PropertyInfo)GetMemberExpression(property.Body).Member;
    }

    extension<T>(Expression<Func<T, object>> expression)
    {
        /// <summary>
        /// Gets the property represented by the expression.
        /// </summary>
        public PropertyInfo GetPropertyInfo()
            => (PropertyInfo)GetMemberExpression(expression.Body).Member;
    }

    extension(LambdaExpression expression)
    {
        /// <summary>
        /// Gets the property represented by the expression.
        /// </summary>
        public PropertyInfo GetPropertyInfo()
            => (PropertyInfo)GetMemberExpression(expression.Body).Member;
    }

    extension<T>(T input)
    {
        /// <summary>
        /// Deep clones an object using public instance properties.
        /// </summary>
        public T GetClone()
        {
            ArgumentNullException.ThrowIfNull(input);

            var type = typeof(T);
            var visited = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);

            return (T)CloneObject(input!, type, visited);
        }
    }

    extension(object obj)
    {
        /// <summary>
        /// Sets a property value.
        /// </summary>
        public void SetPropertyValue(string propertyName, object value)
        {
            var prop = obj.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            if (prop == null || !prop.CanWrite)
                throw new ArgumentException($"Property '{propertyName}' not found or not writable.");

            prop.SetValue(obj, value);
        }

        /// <summary>
        /// Gets a property value.
        /// </summary>
        public object? GetPropertyValue(string propertyName)
        {
            var prop = obj.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            return prop == null
                ? throw new ArgumentException($"Property '{propertyName}' not found.")
                : prop.GetValue(obj);
        }
    }

    private static MemberExpression GetMemberExpression(Expression body)
        => body is MemberExpression memberExpr
            ? memberExpr
            : body is UnaryExpression { Operand: MemberExpression member }
                ? member
                : throw new ArgumentException("Invalid expression. Expected a property access expression.");

    private static object CloneObject(object input, Type type, Dictionary<object, object> visited)
    {
        if (visited.TryGetValue(input, out var existing))
            return existing;

        var clonedObj = Activator.CreateInstance(type)!;
        visited.Add(input, clonedObj);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
                continue;

            var value = property.GetValue(input);
            var propertyType = property.PropertyType;

            if (value != null && propertyType.IsClass &&
                !propertyType.FullName!.StartsWith("System.", StringComparison.Ordinal))
            {
                var clonedValue = CloneObject(value, propertyType, visited);
                property.SetValue(clonedObj, clonedValue);
            }
            else
            {
                property.SetValue(clonedObj, value);
            }
        }

        return clonedObj;
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
