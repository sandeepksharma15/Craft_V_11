using System.ComponentModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class EnumExtensions
{
    extension<T>(T value) where T : struct, Enum
    {
        public string GetDescription()
        {
            var name = value.ToString();
            var member = typeof(T).GetMember(name).FirstOrDefault();

            return member?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .OfType<DescriptionAttribute>()
                    .FirstOrDefault()?.Description
                ?? name;
        }

        public string GetName() => value.ToString();

        public bool TryGetSingleDescription(out string? description)
        {
            var name = value.ToString();

            if (name.Contains(',', StringComparison.Ordinal))
            {
                description = null;
                return false;
            }

            description = value.GetDescription();
            return true;
        }

        public bool TryGetSingleName(out string? name)
        {
            var valueName = value.ToString();

            if (valueName.Contains(',', StringComparison.Ordinal))
            {
                name = null;
                return false;
            }

            name = valueName;
            return true;
        }

        public T GetNextEnumValue()
        {
            var values = Enum.GetValues<T>();
            var index = Array.IndexOf(values, value);

            return index >= 0 && index < values.Length - 1
                ? values[index + 1]
                : values[0];
        }

        public T GetPrevEnumValue()
        {
            var values = Enum.GetValues<T>();
            var index = Array.IndexOf(values, value);

            return index > 0
                ? values[index - 1]
                : values[^1];
        }

        public IEnumerable<T> GetFlags()
        {
            if (!typeof(T).IsDefined(typeof(FlagsAttribute), false))
                return [];

            var numericValue = Convert.ToUInt64(value);

            return Enum.GetValues<T>()
                .Where(flag => !EqualityComparer<T>.Default.Equals(flag, default) &&
                               (numericValue & Convert.ToUInt64(flag)) == Convert.ToUInt64(flag));
        }

        public bool IsSet(T flags)
            => (Convert.ToUInt64(value) & Convert.ToUInt64(flags)) == Convert.ToUInt64(flags);

        public string ToStringInvariant() => value.ToString();
    }

    extension<T>(string value) where T : struct, Enum
    {
        public T ToEnum(bool ignoreCase = true)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            return Enum.Parse<T>(value, ignoreCase);
        }

        public bool Contains(T flags)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var names = flags.GetFlags()
                .Select(flag => flag.ToString())
                .Where(name => !string.IsNullOrEmpty(name));

            return flags.ToString().Contains(',', StringComparison.Ordinal)
                ? names.Any(name => value.Contains(name, StringComparison.InvariantCultureIgnoreCase))
                : value.Contains(flags.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }
    }

    extension<T>(T _) where T : struct, Enum
    {
        public static List<T> GetOrderedEnumValues()
            => [.. Enum.GetValues<T>()];

        public static T GetHighestEnumValue()
            => Enum.GetValues<T>().Max();

        public static T GetLowestEnumValue()
            => Enum.GetValues<T>().Min();

        public static Dictionary<T, string> GetDescriptions()
            => Enum.GetValues<T>()
                .GroupBy(value => value)
                .ToDictionary(group => group.Key, group => group.Key.GetDescription());

        public static Dictionary<T, string> GetNames()
            => Enum.GetValues<T>()
                .GroupBy(value => value)
                .ToDictionary(group => group.Key, group => group.Key.ToString());

        public static T[] GetValues() => Enum.GetValues<T>();
    }

    extension(Type enumType)
    {
        public List<(string Name, object Value)> GetEnumNameValuePairs()
        {
            var underlyingType = Nullable.GetUnderlyingType(enumType) ?? enumType;

            if (!underlyingType.IsEnum)
                return [];

            return [.. Enum.GetValues(underlyingType)
                .Cast<object>()
                .Select(value => (Name: value.ToString() ?? string.Empty, Value: value))];
        }
    }

    extension(int value)
    {
        public T ToEnum<T>() where T : struct, Enum
            => (T)Enum.ToObject(typeof(T), value);
    }
}
