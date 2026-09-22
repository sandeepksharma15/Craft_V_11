using System.Globalization;

namespace Craft.Extensions.Tests.System;

public class NumericExtensionsTests
{
    [Theory]
    [InlineData(5, 0, 10, true)]
    [InlineData(0, 0, 10, true)]
    [InlineData(10, 0, 10, true)]
    [InlineData(-1, 0, 10, false)]
    [InlineData(11, 0, 10, false)]
    [InlineData(5, 10, 0, false)]
    public void IsBetween_Int_ReturnsExpected(
        int value, int min, int max, bool expected)
        => Assert.Equal(expected, value.IsBetween(min, max));

    [Theory]
    [InlineData(5.5, 0, 10, true)]
    [InlineData(-0.1, 0, 10, false)]
    [InlineData(10.1, 0, 10, false)]
    public void IsBetween_Double_ReturnsExpected(
        double value, double min, double max, bool expected)
        => Assert.Equal(expected, value.IsBetween(min, max));

    [Theory]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void IsPositive_Int_ReturnsExpected(int value, bool expected)
        => Assert.Equal(expected, value.IsPositive());

    [Theory]
    [InlineData(5L, true)]
    [InlineData(0L, false)]
    [InlineData(-5L, false)]
    public void IsPositive_Long_ReturnsExpected(long value, bool expected)
        => Assert.Equal(expected, value.IsPositive());

    [Theory]
    [InlineData(5.5, true)]
    [InlineData(0, false)]
    [InlineData(-5.5, false)]
    public void IsPositive_Double_ReturnsExpected(double value, bool expected)
        => Assert.Equal(expected, value.IsPositive());

    [Theory]
    [InlineData(-5, true)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    public void IsNegative_Int_ReturnsExpected(int value, bool expected)
        => Assert.Equal(expected, value.IsNegative());

    [Theory]
    [InlineData(-5L, true)]
    [InlineData(0L, false)]
    [InlineData(5L, false)]
    public void IsNegative_Long_ReturnsExpected(long value, bool expected)
        => Assert.Equal(expected, value.IsNegative());

    [Theory]
    [InlineData(-5.5, true)]
    [InlineData(0, false)]
    [InlineData(5.5, false)]
    public void IsNegative_Double_ReturnsExpected(double value, bool expected)
        => Assert.Equal(expected, value.IsNegative());

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, false)]
    [InlineData(-5, false)]
    public void IsZero_Int_ReturnsExpected(int value, bool expected)
        => Assert.Equal(expected, value.IsZero());

    [Theory]
    [InlineData(0L, true)]
    [InlineData(5L, false)]
    [InlineData(-5L, false)]
    public void IsZero_Long_ReturnsExpected(long value, bool expected)
        => Assert.Equal(expected, value.IsZero());

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, false)]
    [InlineData(-5, false)]
    public void IsZero_Decimal_ReturnsExpected(int value, bool expected)
        => Assert.Equal(expected, ((decimal)value).IsZero());

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(1e-11, true)]
    [InlineData(-1e-11, true)]
    [InlineData(1e-10, false)]
    [InlineData(0.001, false)]
    public void IsZero_Double_UsesDefaultTolerance(double value, bool expected)
        => Assert.Equal(expected, value.IsZero());

    [Fact]
    public void IsZero_Double_UsesSpecifiedTolerance()
    {
        Assert.True(0.001d.IsZero(0.01d));
        Assert.False(0.001d.IsZero(0.001d));
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(1e-7f, true)]
    [InlineData(-1e-7f, true)]
    [InlineData(1e-6f, false)]
    [InlineData(0.001f, false)]
    public void IsZero_Float_UsesDefaultTolerance(float value, bool expected)
        => Assert.Equal(expected, value.IsZero());

    [Fact]
    public void IsZero_Float_UsesSpecifiedTolerance()
    {
        Assert.True(0.01f.IsZero(0.1f));
        Assert.False(0.01f.IsZero(0.01f));
    }

    [Theory]
    [InlineData(0, true, false)]
    [InlineData(2, true, false)]
    [InlineData(3, false, true)]
    [InlineData(-2, true, false)]
    [InlineData(-3, false, true)]
    public void IsEvenAndOdd_Int_ReturnExpected(int value, bool even, bool odd)
    {
        Assert.Equal(even, value.IsEven());
        Assert.Equal(odd, value.IsOdd());
    }

    [Theory]
    [InlineData(2.0, true, false)]
    [InlineData(3.0, false, true)]
    [InlineData(2.5, false, false)]
    public void IsEvenAndOdd_Double_UseIntegerSemantics(
        double value, bool even, bool odd)
    {
        Assert.Equal(even, value.IsEven());
        Assert.Equal(odd, value.IsOdd());
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, true)]
    public void IsFinite_Int_ReturnsTrue(int value)
        => Assert.True(value.IsFinite());

    [Theory]
    [InlineData(0d, true)]
    [InlineData(1.5d, true)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    [InlineData(double.NegativeInfinity, false)]
    public void IsFinite_Double_ReturnsExpected(double value, bool expected)
        => Assert.Equal(expected, value.IsFinite());

    [Theory]
    [InlineData(0f, true)]
    [InlineData(1.5f, true)]
    [InlineData(float.NaN, false)]
    [InlineData(float.PositiveInfinity, false)]
    [InlineData(float.NegativeInfinity, false)]
    public void IsFinite_Float_ReturnsExpected(float value, bool expected)
        => Assert.Equal(expected, value.IsFinite());

    [Theory]
    [InlineData(1000)]
    [InlineData(1000000)]
    [InlineData(-1000)]
    public void ToFormattedString_Int_UsesCurrentCulture(int value)
    {
        var expected = value.ToString("N0", CultureInfo.CurrentCulture);

        Assert.Equal(expected, value.ToFormattedString());
    }

    [Theory]
    [InlineData(1000L)]
    [InlineData(-1000L)]
    public void ToFormattedString_Long_UsesCurrentCulture(long value)
    {
        var expected = value.ToString("N0", CultureInfo.CurrentCulture);

        Assert.Equal(expected, value.ToFormattedString());
    }

    [Theory]
    [InlineData(1234.5, 2)]
    [InlineData(1234.5678, 3)]
    [InlineData(-12.5, 0)]
    public void ToFormattedString_Decimal_UsesRequestedPrecision(
        double value, int decimalPlaces)
    {
        var decimalValue = (decimal)value;
        var expected = decimalValue.ToString($"N{decimalPlaces}", CultureInfo.CurrentCulture);

        Assert.Equal(expected, decimalValue.ToFormattedString(decimalPlaces));
    }

    [Theory]
    [InlineData(1234.5, 2)]
    [InlineData(1234.5678, 3)]
    [InlineData(-12.5, 0)]
    public void ToFormattedString_Double_UsesRequestedPrecision(
        double value, int decimalPlaces)
    {
        var expected = value.ToString($"N{decimalPlaces}", CultureInfo.CurrentCulture);

        Assert.Equal(expected, value.ToFormattedString(decimalPlaces));
    }

    [Theory]
    [InlineData(1234.5)]
    [InlineData(-12.5)]
    public void ToFormattedString_Decimal_UsesDefaultPrecision(double value)
    {
        var decimalValue = (decimal)value;
        var expected = decimalValue.ToString("N2", CultureInfo.CurrentCulture);

        Assert.Equal(expected, decimalValue.ToFormattedString());
    }

    [Theory]
    [InlineData(1234.5)]
    [InlineData(-12.5)]
    public void ToFormattedString_Double_UsesDefaultPrecision(double value)
    {
        var expected = value.ToString("N2", CultureInfo.CurrentCulture);

        Assert.Equal(expected, value.ToFormattedString());
    }

    [Fact]
    public void ToCurrency_UsesCurrentCulture()
    {
        decimal decimalValue = 1234.56m;
        double doubleValue = 1234.56d;
        const int intValue = 1234;
        const long longValue = 1234L;

        Assert.Equal(decimalValue.ToString("C", CultureInfo.CurrentCulture), decimalValue.ToCurrency());
        Assert.Equal(doubleValue.ToString("C", CultureInfo.CurrentCulture), doubleValue.ToCurrency());
        Assert.Equal(intValue.ToString("C", CultureInfo.CurrentCulture), intValue.ToCurrency());
        Assert.Equal(longValue.ToString("C", CultureInfo.CurrentCulture), longValue.ToCurrency());
    }

    [Theory]
    [InlineData(1024, 1.0)]
    [InlineData(2048, 2.0)]
    [InlineData(512, 0.5)]
    [InlineData(0, 0.0)]
    public void ToKibibytes_ConvertsUsingBinaryUnits(long bytes, double expected)
        => Assert.Equal(expected, bytes.ToKibibytes());

    [Theory]
    [InlineData(1048576, 1.0)]
    [InlineData(2097152, 2.0)]
    [InlineData(524288, 0.5)]
    [InlineData(0, 0.0)]
    public void ToMebibytes_ConvertsUsingBinaryUnits(long bytes, double expected)
        => Assert.Equal(expected, bytes.ToMebibytes());

    [Theory]
    [InlineData(1073741824, 1.0)]
    [InlineData(2147483648, 2.0)]
    [InlineData(536870912, 0.5)]
    [InlineData(0, 0.0)]
    public void ToGibibytes_ConvertsUsingBinaryUnits(long bytes, double expected)
        => Assert.Equal(expected, bytes.ToGibibytes());

    [Fact]
    public void ByteConversions_SupportNegativeValues()
    {
        Assert.Equal(-1.0, (-1024L).ToKibibytes());
        Assert.Equal(-1.0, (-1048576L).ToMebibytes());
        Assert.Equal(-1.0, (-1073741824L).ToGibibytes());
    }
}
