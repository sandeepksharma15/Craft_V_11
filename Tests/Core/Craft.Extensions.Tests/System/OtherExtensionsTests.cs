using System.Globalization;

namespace Craft.Extensions.Tests.System;

public class OtherExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(new byte[0], "")]
    [InlineData(new byte[] { 0x01, 0xAB, 0xFF }, "01ABFF")]
    [InlineData(new byte[] { 0x00, 0x0F, 0xFF }, "000FFF")]
    public void BytesToHex_ReturnsExpectedResult(byte[]? input, string? expected)
        => Assert.Equal(expected, input.BytesToHex());

    [Theory]
    [InlineData("48656C6C6F", new byte[] { 72, 101, 108, 108, 111 })]
    [InlineData("010203", new byte[] { 1, 2, 3 })]
    [InlineData("abcdef", new byte[] { 0xAB, 0xCD, 0xEF })]
    [InlineData("  01ABFF  ", new byte[] { 0x01, 0xAB, 0xFF })]
    public void HexToBytes_ReturnsExpectedResult(string input, byte[] expected)
        => Assert.Equal(expected, input.HexToBytes());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void HexToBytes_EmptyOrWhitespace_ReturnsEmptyArray(string? input)
        => Assert.Empty(input.HexToBytes());

    [Theory]
    [InlineData("12345")]
    [InlineData("InvalidHex")]
    [InlineData("ABCDEF012G")]
    public void HexToBytes_InvalidInput_ThrowsFormatException(string input)
        => Assert.Throws<FormatException>(() => input.HexToBytes());

    [Fact]
    public void HexToBytes_InvalidInput_PreservesInnerException()
    {
        var exception = Assert.Throws<FormatException>(() => "GG".HexToBytes());

        Assert.NotNull(exception.InnerException);
    }

    [Theory]
    [InlineData(0, "0%")]
    [InlineData(0.1234, "12.34%")]
    [InlineData(-0.1234, "-12.34%")]
    [InlineData(1, "100%")]
    [InlineData(-1, "-100%")]
    [InlineData(0.005, "0.5%")]
    [InlineData(123.456, "12345.6%")]
    [InlineData(-123.456, "-12345.6%")]
    [InlineData(0.9999, "99.99%")]
    [InlineData(0.995, "99.5%")]
    public void ToPercentage_Decimal_ReturnsExpected(decimal input, string expected)
        => Assert.Equal(expected, input.ToPercentage());

    [Theory]
    [InlineData(0d, "0%")]
    [InlineData(0.1234d, "12.34%")]
    [InlineData(-0.1234d, "-12.34%")]
    [InlineData(1d, "100%")]
    [InlineData(-1d, "-100%")]
    [InlineData(0.005d, "0.5%")]
    [InlineData(123.456d, "12345.6%")]
    [InlineData(-123.456d, "-12345.6%")]
    public void ToPercentage_Double_ReturnsExpected(double input, string expected)
        => Assert.Equal(expected, input.ToPercentage());

    [Theory]
    [InlineData(0f, "0%")]
    [InlineData(0.1234f, "12.34%")]
    [InlineData(-0.1234f, "-12.34%")]
    [InlineData(1f, "100%")]
    [InlineData(-1f, "-100%")]
    [InlineData(0.005f, "0.5%")]
    [InlineData(123.456f, "12345.6%")]
    [InlineData(-123.456f, "-12345.6%")]
    public void ToPercentage_Float_ReturnsExpected(float input, string expected)
        => Assert.Equal(expected, input.ToPercentage());

    [Fact]
    public void ToPercentage_FormatsSpecialDoubleValues()
    {
        Assert.Equal("NaN%", double.NaN.ToPercentage());
        Assert.Equal($"{double.PositiveInfinity.ToString(CultureInfo.CurrentCulture)}%", double.PositiveInfinity.ToPercentage());
        Assert.Equal($"{double.NegativeInfinity.ToString(CultureInfo.CurrentCulture)}%", double.NegativeInfinity.ToPercentage());
    }
}
