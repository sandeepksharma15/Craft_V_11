using System.Globalization;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ObjectExtensions
{
    extension<T>(T value)
    {
        public T If(bool condition, Func<T, T> func)
        {
            ArgumentNullException.ThrowIfNull(func);
            return condition ? func(value) : value;
        }

        public T If(bool condition, Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (condition)
                action(value);

            return value;
        }
    }

    extension(object? value)
    {
        public T ToValue<T>() where T : struct
            => value.TryToValue(out T result) ? result : default;

        public bool TryToValue<T>(out T result) where T : struct
        {
            result = default;

            if (value is null)
                return false;

            if (typeof(T) == typeof(Guid))
            {
                if (!Guid.TryParse(value.ToString(), out var guid))
                    return false;

                result = (T)(object)guid;
                return true;
            }

            if (value is not IConvertible convertible)
                return false;

            try
            {
                result = (T)Convert.ChangeType(
                    convertible,
                    typeof(T),
                    CultureInfo.CurrentCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
