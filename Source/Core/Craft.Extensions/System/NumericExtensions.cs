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

        public bool IsZero()
            => value == T.Zero;

        public bool IsEven()
            => T.IsEvenInteger(value);

        public bool IsOdd()
            => T.IsOddInteger(value);
    }

    extension(double value)
    {
        public bool IsZero(double tolerance)
            => Math.Abs(value) < tolerance;

        public bool IsFinite()
            => double.IsFinite(value);
    }

    extension(float value)
    {
        public bool IsZero(float tolerance)
            => Math.Abs(value) < tolerance;

        public bool IsFinite()
            => float.IsFinite(value);
    }

    extension(int value)
    {
        public string ToFormattedString()
            => value.ToString("N0", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(long value)
    {
        public string ToFormattedString()
            => value.ToString("N0", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(decimal value)
    {
        public string ToFormattedString(int decimalPlaces = 2)
            => value.ToString($"N{decimalPlaces}", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(double value)
    {
        public string ToFormattedString(int decimalPlaces = 2)
            => value.ToString($"N{decimalPlaces}", CultureInfo.CurrentCulture);

        public string ToCurrency()
            => value.ToString("C", CultureInfo.CurrentCulture);
    }

    extension(long bytes)
    {
        public double ToKilobytes()
            => bytes / 1024d;

        public double ToMegabytes()
            => bytes / (1024d * 1024d);

        public double ToGigabytes()
            => bytes / (1024d * 1024d * 1024d);
    }
}
