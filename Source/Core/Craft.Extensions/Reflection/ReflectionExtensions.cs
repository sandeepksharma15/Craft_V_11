using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Reflection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ReflectionExtensions
{
    extension(Type type)
    {
        /// <summary>
        /// Gets a property descriptor by name.
        /// </summary>
        public PropertyDescriptor? GetMemberByName(string memberName)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

            var names = memberName.Split('.');
            var properties = TypeDescriptor.GetProperties(type);

            for (var index = 0; index < names.Length - 1; index++)
            {
                var property = properties.Find(names[index], true);

                if (property is null)
                    return null;

                properties = property.GetChildProperties();
            }

            return properties.Find(names[^1], true);
        }

        /// <summary>
        /// Gets all instance properties declared on the type and its base types.
        /// </summary>
        public List<PropertyInfo> GetAllProperties()
        {
            ArgumentNullException.ThrowIfNull(type);

            var properties = new List<PropertyInfo>();

            for (var current = type; current is not null; current = current.BaseType)
                properties.AddRange(current.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            return properties;
        }

        /// <summary>
        /// Gets a property by name.
        /// </summary>
        public PropertyInfo GetPropertyInfo(string name)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new ArgumentException($"Property '{name}' not found in type '{type.FullName}'.", nameof(name));
        }
    }

    extension(LambdaExpression expression)
    {
        /// <summary>
        /// Gets the property represented by the expression.
        /// </summary>
        public PropertyInfo GetPropertyInfo()
        {
            ArgumentNullException.ThrowIfNull(expression);

            return ExtractPropertyInfo(expression.Body);
        }

        /// <summary>
        /// Gets the property name represented by the expression.
        /// </summary>
        public string GetMemberName() => expression.GetPropertyInfo().Name;

        /// <summary>
        /// Gets the underlying property type.
        /// </summary>
        public Type GetMemberType()
        {
            var type = expression.GetPropertyInfo().PropertyType;

            return Nullable.GetUnderlyingType(type) ?? type;
        }
    }

    extension<T>(T input)
    {
        /// <summary>
        /// Deep clones an object using public instance properties.
        /// </summary>
        public T GetClone()
        {
            ArgumentNullException.ThrowIfNull(input);

            if (input is string || input is ValueType)
                return input;

            var visited = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);

            return (T)CloneObject(input!, visited);
        }
    }

    extension(object obj)
    {
        /// <summary>
        /// Sets a property value.
        /// </summary>
        public void SetPropertyValue(string propertyName, object? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            var property = GetInstanceProperty(obj, propertyName);

            if (property is null || !property.CanWrite)
                throw new ArgumentException($"Property '{propertyName}' not found or not writable.", nameof(propertyName));

            property.SetValue(obj, value);
        }

        /// <summary>
        /// Gets a property value.
        /// </summary>
        public object? GetPropertyValue(string propertyName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            var property = GetInstanceProperty(obj, propertyName)
                ?? throw new ArgumentException($"Property '{propertyName}' not found.", nameof(propertyName));

            return property.GetValue(obj);
        }
    }

    #region Private Methods

    private static object CloneObject(object input, Dictionary<object, object> visited)
    {
        if (visited.TryGetValue(input, out var existing))
            return existing;

        var type = input.GetType();
        var clone = Activator.CreateInstance(type, nonPublic: true)
            ?? throw new ArgumentException($"Cannot create an instance of type '{type.FullName}'.", nameof(input));

        visited.Add(input, clone);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
                continue;

            var value = property.GetValue(input);

            if (value is not null && value.GetType().IsClass &&
                !value.GetType().FullName!.StartsWith("System.", StringComparison.Ordinal))
                value = CloneObject(value, visited);

            property.SetValue(clone, value);
        }

        return clone;
    }

    private static PropertyInfo ExtractPropertyInfo(Expression expression)
    {
        var member = expression switch
        {
            MemberExpression memberExpression => memberExpression.Member,
            UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression.Member,
            _ => throw new ArgumentException("Invalid expression. Expected a property access expression.", nameof(expression))
        };

        return member as PropertyInfo
            ?? throw new ArgumentException("Invalid expression. Expected a property access expression.", nameof(expression));
    }

    private static PropertyInfo? GetInstanceProperty(object obj, string propertyName)
        => obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    #endregion Private Methods

    #region Private Classes

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        #region Public Properties

        public static ReferenceEqualityComparer Instance { get; } = new();

        #endregion Public Properties

        #region Public Methods

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);

        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

        #endregion Public Methods
    }

    #endregion Private Classes
}
