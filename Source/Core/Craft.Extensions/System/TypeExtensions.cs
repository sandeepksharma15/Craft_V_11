using System.Reflection;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class TypeExtensions
{
    extension(Type? type)
    {
        public IList<string> GetClassesWithAttribute<T>() where T : Attribute
            => type is null ? [] : GetDerivedClasses(type, typeof(T), includeAttribute: true, includeBaseType: true);

        public IList<string> GetClassesWithoutAttribute<T>() where T : Attribute
            => type is null ? [] : GetDerivedClasses(type, typeof(T), includeAttribute: false, includeBaseType: true);

        public IList<string> GetInheritedClasses()
            => type is null ? [] : GetDerivedClasses(type, attributeType: null, includeAttribute: false, includeBaseType: false);

        public bool HasAttribute<T>() where T : Attribute
            => type?.GetCustomAttributes(typeof(T), inherit: true).Length > 0;

        public Type? GetNonNullableType()
            => type is null ? null : Nullable.GetUnderlyingType(type) ?? type;

        public bool IsNullable()
            => type is not null && Nullable.GetUnderlyingType(type) is not null;

        public bool IsNumeric()
            => type.IsNumericTypeCode();

        public bool IsIntegral()
            => type.IsNumericTypeCode(
                TypeCode.Byte, TypeCode.SByte, TypeCode.UInt16, TypeCode.UInt32,
                TypeCode.UInt64, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64);

        public bool IsFloating()
            => type.IsNumericTypeCode(TypeCode.Decimal, TypeCode.Double, TypeCode.Single);

        public bool IsDateTime()
        {
            var underlyingType = type?.GetNonNullableType();
            return underlyingType == typeof(DateTime)
                || underlyingType == typeof(DateTimeOffset)
                || underlyingType == typeof(DateOnly);
        }

        public bool IsBoolean()
            => type?.GetNonNullableType() == typeof(bool);

        public bool IsEnumType()
            => type?.GetNonNullableType()?.IsEnum == true;

        public bool HasImplementedInterface(Type? interfaceType)
            => type is not null
                && interfaceType?.IsInterface == true
                && interfaceType.IsAssignableFrom(type);

        public bool IsDerivedFromClass(Type baseType)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(baseType);

            return baseType.IsAssignableFrom(type);
        }

        public bool IsCompatibleWith(Type otherType)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(otherType);

            return type.IsAssignableFrom(otherType) || otherType.IsAssignableFrom(type);
        }

        public bool IsNotCompatibleWith(Type otherType)
            => !type.IsCompatibleWith(otherType);

        public string? GetClassName()
            => type?.ToString().GetStringAfterLastDelimiter();
    }

    extension(MemberInfo? member)
    {
        public Type? GetMemberUnderlyingType()
            => member?.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                MemberTypes.Event => ((EventInfo)member).EventHandlerType,
                null => null,
                _ => throw new ArgumentException(
                    "MemberInfo must be a field, property, or event.",
                    nameof(member))
            };
    }

    private static IList<string> GetDerivedClasses(
        Type baseType,
        Type? attributeType,
        bool includeAttribute,
        bool includeBaseType)
        => GetLoadableTypes()
            .Where(candidate =>
                candidate.IsClass
                && !candidate.IsAbstract
                && (includeBaseType || candidate != baseType)
                && baseType.IsAssignableFrom(candidate)
                && (attributeType is null
                    || (candidate.GetCustomAttributes(attributeType, inherit: true).Length > 0) == includeAttribute))
            .Select(candidate => candidate.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    private static IEnumerable<Type> GetLoadableTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(static type => type is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
                yield return type;
        }
    }

    private static bool IsNumericTypeCode(this Type? type, params TypeCode[] numericTypes)
    {
        if (type is null)
            return false;

        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        var typeCode = Type.GetTypeCode(underlyingType);

        if (numericTypes.Length == 0)
        {
            return typeCode is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.UInt16
                or TypeCode.UInt32
                or TypeCode.UInt64
                or TypeCode.Int16
                or TypeCode.Int32
                or TypeCode.Int64
                or TypeCode.Decimal
                or TypeCode.Double
                or TypeCode.Single;
        }

        return numericTypes.Contains(typeCode);
    }
}
