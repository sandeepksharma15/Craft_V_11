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
        /// Reads a case-sensitive, dotted instance property path. Non-public access requires
        /// explicit opt-in.
        /// </summary>
        /// <exception cref="InvalidOperationException"> An intermediate property is null. </exception>
        public object? GetValue(string propertyPath, bool includeNonPublic = false)
        {
            ArgumentNullException.ThrowIfNull(obj);
            var names = GetPropertyPath(propertyPath);
            object? current = obj;

            foreach (var name in names)
            {
                if (current is null)
                    throw new InvalidOperationException($"Cannot read segment '{name}' in '{propertyPath}': its parent is null.");

                var property = ResolveProperty(current.GetType(), name, propertyPath);
                RequireAccessor(property, false, includeNonPublic, propertyPath);
                current = property.GetValue(current);
            }

            return current;
        }

        /// <summary>
        /// Reads a property path without converting its value. Null requires a nullable result type.
        /// </summary>
        public T? GetValue<T>(string propertyPath, bool includeNonPublic = false)
        {
            var value = obj.GetValue(propertyPath, includeNonPublic);

            if (value is T result)
                return result;

            if (value is null && default(T) is null)
                return default;

            throw new InvalidCastException($"Value at '{propertyPath}' cannot be assigned to '{typeof(T)}'.");
        }

        /// <summary>
        /// Sets a case-sensitive, dotted instance property path without value conversion or
        /// intermediate creation. Nested structs are written back. The root must be a reference
        /// type; init-only properties are read-only. Non-public access requires explicit opt-in.
        /// Accessor exceptions propagate without rollback.
        /// </summary>
        [Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
        public void SetValue(string propertyPath, object? value, bool includeNonPublic = false)
        {
            ArgumentNullException.ThrowIfNull(obj);
            var names = GetPropertyPath(propertyPath);

            if (obj.GetType().IsValueType)
                throw new ArgumentException("The root must be a reference type to avoid modifying a temporary boxed copy.", nameof(obj));

            var path = new List<(object Owner, PropertyInfo Property)>();
            object current = obj;

            for (var index = 0; index < names.Length; index++)
            {
                var property = ResolveProperty(current.GetType(), names[index], propertyPath);
                path.Add((current, property));

                if (index == names.Length - 1)
                    break;

                RequireAccessor(property, false, includeNonPublic, propertyPath);
                current = property.GetValue(current)
                    ?? throw new InvalidOperationException($"Cannot traverse '{property.Name}' in '{propertyPath}': its value is null.");
            }

            var leaf = path[^1];
            RequireAccessor(leaf.Property, true, includeNonPublic, propertyPath);
            var targetType = leaf.Property.PropertyType;
            var assignableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is null ? targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null
                : !assignableType.IsInstanceOfType(value))
                throw new ArgumentException($"Value cannot be assigned to '{leaf.Property.Name}' of type '{targetType}' in '{propertyPath}'.", nameof(value));

            // Check all required write-back accessors before invoking the leaf setter.
            var writeBackStart = path.Count - 2;

            for (var index = writeBackStart; index >= 0 && path[index].Property.PropertyType.IsValueType; index--)
                RequireAccessor(path[index].Property, true, includeNonPublic, propertyPath);

            leaf.Property.SetValue(leaf.Owner, value);

            for (var index = writeBackStart; index >= 0 && path[index].Property.PropertyType.IsValueType; index--)
                path[index].Property.SetValue(path[index].Owner, path[index + 1].Owner);
        }

        /// <summary>
        /// Alias for SetValue, including nested paths and explicit non-public access.
        /// </summary>
        public void SetPropertyValue(string propertyName, object? value, bool includeNonPublic = false)
            => obj.SetValue(propertyName, value, includeNonPublic);

        /// <summary>
        /// Alias for GetValue, including nested paths and explicit non-public access.
        /// </summary>
        public object? GetPropertyValue(string propertyName, bool includeNonPublic = false)
            => obj.GetValue(propertyName, includeNonPublic);
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

    private static string[] GetPropertyPath(string propertyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyPath);
        var names = propertyPath.Split('.');

        if (names.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Property paths must contain non-empty segments.", nameof(propertyPath));

        return names;
    }

    private static void RequireAccessor(PropertyInfo property, bool write, bool includeNonPublic, string propertyPath)
    {
        var accessor = write ? property.GetSetMethod(includeNonPublic) : property.GetGetMethod(includeNonPublic);

        if (accessor is null || (write && accessor.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit))))
            throw new ArgumentException($"Property '{property.Name}' in '{propertyPath}' has no permitted {(write ? "setter" : "getter")}.", nameof(propertyPath));
    }

    private static PropertyInfo ResolveProperty(Type type, string name, string propertyPath)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var property = current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .FirstOrDefault(property => property.Name == name);

            if (property is null)
                continue;

            if (property.GetIndexParameters().Length != 0 || property.PropertyType.IsByRef || property.PropertyType.IsByRefLike)
                throw new ArgumentException($"Property '{name}' in '{propertyPath}' is not a supported non-indexed property.", nameof(propertyPath));

            return property;
        }

        throw new ArgumentException($"Property segment '{name}' in '{propertyPath}' was not found on '{type}'.", nameof(propertyPath));
    }

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
