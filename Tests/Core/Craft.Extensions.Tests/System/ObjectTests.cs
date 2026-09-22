namespace Craft.Extensions.Tests.System;

public class ObjectTests
{
    [Fact]
    public void If_WithFunc_True_AppliesFunction()
        => Assert.Equal(10, 5.If(true, x => x * 2));

    [Fact]
    public void If_WithFunc_False_ReturnsOriginalValue()
        => Assert.Equal(5, 5.If(false, x => x * 2));

    [Fact]
    public void If_WithAction_True_PerformsActionAndReturnsOriginalValue()
    {
        var observed = 0;

        var result = 5.If(true, (Action<int>)(x => observed = x * 2));

        Assert.Equal(10, observed);
        Assert.Equal(5, result);
    }

    [Fact]
    public void If_WithAction_False_DoesNotPerformAction()
    {
        var called = false;

        var result = 5.If(false, _ => called = true);

        Assert.False(called);
        Assert.Equal(5, result);
    }

    [Fact]
    public void If_WithFunc_NullDelegate_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => 5.If(true, (Func<int, int>)null!));

    [Fact]
    public void If_WithAction_NullDelegate_Throws()
        => Assert.Throws<ArgumentNullException>(
            () => 5.If(true, (Action<int>)null!));

    [Fact]
    public void If_WithFunc_SupportsNullReferenceValue()
    {
        string? value = null;

        Assert.Equal("default", value.If(true, x => x ?? "default"));
    }

    [Fact]
    public void If_WithAction_SupportsNullReferenceValue()
    {
        string? value = null;
        var called = false;

        var result = value.If(true, x => called = x is null);

        Assert.True(called);
        Assert.Null(result);
    }

    [Fact]
    public void ToValue_ConvertsGuid()
    {
        var input = "6F9619FF-8B86-D011-B42D-00C04FC964FF";

        Assert.Equal(Guid.Parse(input), input.ToValue<Guid>());
    }

    [Fact]
    public void ToValue_ConvertsValue()
        => Assert.Equal(123, "123".ToValue<int>());

    [Fact]
    public void ToValue_ConvertsBoxedValue()
        => Assert.Equal(42, ((object)42).ToValue<int>());

    [Fact]
    public void ToValue_ReturnsDefaultForNull()
    {
        object? input = null;

        Assert.Equal(default, input.ToValue<int>());
    }

    [Fact]
    public void ToValue_ReturnsDefaultForInvalidConversion()
        => Assert.Equal(default, "InvalidNumber".ToValue<int>());

    [Fact]
    public void ToValue_ReturnsDefaultForInvalidGuid()
        => Assert.Equal(default, "not-a-guid".ToValue<Guid>());

    [Fact]
    public void ToValue_ReturnsDefaultForNonConvertibleValue()
        => Assert.Equal(default, new object().ToValue<int>());

    [Fact]
    public void TryToValue_ReportsSuccessfulConversion()
    {
        Assert.True("123".TryToValue<int>(out var result));
        Assert.Equal(123, result);
    }

    [Fact]
    public void TryToValue_ReportsFailureWithoutThrowing()
    {
        Assert.False("InvalidNumber".TryToValue<int>(out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryToValue_ReportsNullAsFailure()
    {
        object? input = null;

        Assert.False(input.TryToValue<int>(out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryToValue_ReportsInvalidGuidAsFailure()
        => Assert.False("not-a-guid".TryToValue<Guid>(out _));

    [Fact]
    public void TryToValue_ReportsNonConvertibleValueAsFailure()
        => Assert.False(new object().TryToValue<int>(out _));
    [Fact]
    public void TryToValue_ReturnsFalseForInvalidCast()
        => Assert.False(Guid.Empty.TryToValue<int>(out _));

    [Fact]
    public void TryToValue_ReturnsFalseForOverflow()
        => Assert.False(long.MaxValue.TryToValue<int>(out _));

    [Fact]
    public void TryToValue_ReturnsFalseForArgumentException()
        => Assert.False(new ArgumentThrowingConvertible().TryToValue<int>(out _));

    private sealed class ArgumentThrowingConvertible : IConvertible
    {
        public TypeCode GetTypeCode() => TypeCode.Object;
        public bool ToBoolean(IFormatProvider? provider) => throw new NotSupportedException();
        public byte ToByte(IFormatProvider? provider) => throw new NotSupportedException();
        public char ToChar(IFormatProvider? provider) => throw new NotSupportedException();
        public DateTime ToDateTime(IFormatProvider? provider) => throw new NotSupportedException();
        public decimal ToDecimal(IFormatProvider? provider) => throw new NotSupportedException();
        public double ToDouble(IFormatProvider? provider) => throw new NotSupportedException();
        public short ToInt16(IFormatProvider? provider) => throw new NotSupportedException();
        public int ToInt32(IFormatProvider? provider) => throw new NotSupportedException();
        public long ToInt64(IFormatProvider? provider) => throw new NotSupportedException();
        public sbyte ToSByte(IFormatProvider? provider) => throw new NotSupportedException();
        public float ToSingle(IFormatProvider? provider) => throw new NotSupportedException();
        public string ToString(IFormatProvider? provider) => throw new NotSupportedException();
        public ushort ToUInt16(IFormatProvider? provider) => throw new NotSupportedException();
        public uint ToUInt32(IFormatProvider? provider) => throw new NotSupportedException();
        public ulong ToUInt64(IFormatProvider? provider) => throw new NotSupportedException();
        public object ToType(Type conversionType, IFormatProvider? provider) => throw new ArgumentException();
    }

}
