using System.Globalization;
using System.Numerics;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class NumericExtensions
{
    extension<T>(T value) where T : INumber<T>
    {
        public bool IsBetween(T min, T max)
            => value >= min && value <= max;

        public bool IsPositive()
            => value > T.Zero;

        public bool IsNegative()
            => value < T.Zero;

        public bool IsFinite()
            => T.IsFinite(value);

        public bool IsEven()
            => T.IsEvenInteger(value);

        public bool IsOdd()
            => T.IsOddInteger(value);
    }

    extension(int value)
    {
        public bool IsZero()
            => value == 0;

        public string ToFormattedString()
            => value.ToString("N0", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(long value)
    {
        public bool IsZero()
            => value == 0;

        public string ToFormattedString()
            => value.ToString("N0", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(decimal value)
    {
        public bool IsZero()
            => value == 0;

        public string ToFormattedString(int decimalPlaces = 2)
            => value.ToString($\"N{decimalPlaces}\", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(double value)
    {
        public bool IsZero(double tolerance = 1e-10)
            => Math.Abs(value) < tolerance;

        public string ToFormattedString(int decimalPlaces = 2)
            => value.ToString($\"N{decimalPlaces}\", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(float value)
    {
        public bool IsZero(float tolerance = 1e-6f)
            => Math.Abs(value) < tolerance;
    }

    extension<T>(T value) where T : IFloatingPoint<T>
    {
        public string ToPercentage()
            => (value * T.CreateChecked(100))
                .ToString("0.##", CultureInfo.CurrentCulture) + "%";
    }
}
