using System.ComponentModel;

namespace Craft.Extensions.Tests.System;

public class EnumExtensionsTests
{
    [Fact]
    public void GetOrderedEnumValues_ReturnsUnderlyingOrder()
        => Assert.Equal(
            [SimpleEnum.Zero, SimpleEnum.One, SimpleEnum.Two],
            SimpleEnum.GetOrderedEnumValues());

    [Fact]
    public void GetHighestAndLowestEnumValue_ReturnExtremes()
    {
        Assert.Equal(SimpleEnum.Two, SimpleEnum.GetHighestEnumValue());
        Assert.Equal(SimpleEnum.Zero, SimpleEnum.GetLowestEnumValue());
    }

    [Fact]
    public void GetDescriptions_ReturnsDescriptionOrName()
    {
        var result = DescEnum.GetDescriptions();

        Assert.Equal("Alpha Desc", result[DescEnum.Alpha]);
        Assert.Equal("Beta Desc", result[DescEnum.Beta]);
        Assert.Equal("Gamma", result[DescEnum.Gamma]);
    }

    [Fact]
    public void GetNames_ReturnsNames()
        => Assert.Equal(
            "Two",
            SimpleEnum.GetNames()[SimpleEnum.Two]);

    [Fact]
    public void GetValues_ReturnsAllValues()
        => Assert.Equal(
            [SimpleEnum.Zero, SimpleEnum.One, SimpleEnum.Two],
            SimpleEnum.GetValues());

    [Fact]
    public void GetNextEnumValue_WrapsAtEnd()
    {
        Assert.Equal(SimpleEnum.One, SimpleEnum.Zero.GetNextEnumValue());
        Assert.Equal(SimpleEnum.Zero, SimpleEnum.Two.GetNextEnumValue());
    }

    [Fact]
    public void GetPrevEnumValue_WrapsAtBeginning()
    {
        Assert.Equal(SimpleEnum.Zero, SimpleEnum.One.GetPrevEnumValue());
        Assert.Equal(SimpleEnum.Two, SimpleEnum.Zero.GetPrevEnumValue());
    }

    [Fact]
    public void GetDescription_ReturnsDescriptionOrName()
    {
        Assert.Equal("Alpha Desc", DescEnum.Alpha.GetDescription());
        Assert.Equal("Gamma", DescEnum.Gamma.GetDescription());
    }

    [Fact]
    public void GetDescription_UnknownValue_ReturnsNumericText()
        => Assert.Equal("999", ((SimpleEnum)999).GetDescription());

    [Fact]
    public void GetName_UsesEnumFormatting()
    {
        Assert.Equal("One", TestFlags.One.GetName());
        Assert.Equal("One, Two", (TestFlags.One | TestFlags.Two).GetName());
    }

    [Fact]
    public void TryGetSingleDescription_RejectsCompositeValue()
    {
        Assert.True(DescEnum.Beta.TryGetSingleDescription(out var description));
        Assert.Equal("Beta Desc", description);

        Assert.False((TestFlags.One | TestFlags.Two).TryGetSingleDescription(out description));
        Assert.Null(description);
    }

    [Fact]
    public void TryGetSingleName_RejectsCompositeValue()
    {
        Assert.True(DescEnum.Beta.TryGetSingleName(out var name));
        Assert.Equal("Beta", name);

        Assert.False((TestFlags.One | TestFlags.Two).TryGetSingleName(out name));
        Assert.Null(name);
    }

    [Fact]
    public void GetFlags_ReturnsOnlyIndividualSetFlags()
    {
        var flags = (TestFlags.One | TestFlags.Two).GetFlags().ToArray();

        Assert.Equal([TestFlags.One, TestFlags.Two], flags);
    }

    [Fact]
    public void GetFlags_ReturnsEmptyForNonFlagsEnum()
        => Assert.Empty(SimpleEnum.One.GetFlags());

    [Fact]
    public void GetFlags_ReturnsEmptyForNoFlags()
        => Assert.Empty(TestFlags.None.GetFlags());

    [Fact]
    public void IsSet_ReturnsTrueOnlyWhenAllRequestedBitsAreSet()
    {
        var value = TestFlags.One | TestFlags.Two;

        Assert.True(value.IsSet(TestFlags.One));
        Assert.True(value.IsSet(TestFlags.One | TestFlags.Two));
        Assert.False(value.IsSet(TestFlags.Four));
    }

    [Fact]
    public void IsSet_ZeroFlags_ReturnsTrue()
        => Assert.True(TestFlags.One.IsSet(TestFlags.None));

    [Fact]
    public void ToStringInvariant_UsesEnumName()
        => Assert.Equal("Alpha", DescEnum.Alpha.ToStringInvariant());

    [Fact]
    public void ToEnum_String_ParsesNameAndNumericValue()
    {
        Assert.Equal(SimpleEnum.Two, "Two".ToEnum<SimpleEnum>());
        Assert.Equal(SimpleEnum.One, "1".ToEnum<SimpleEnum>());
        Assert.Equal(SimpleEnum.Two, "two".ToEnum<SimpleEnum>());
    }

    [Fact]
    public void ToEnum_String_ThrowsForEmpty()
        => Assert.Throws<ArgumentException>(() => "".ToEnum<SimpleEnum>());

    [Fact]
    public void ToEnum_Int_ConvertsValue()
        => Assert.Equal(SimpleEnum.One, 1.ToEnum<SimpleEnum>());

    [Fact]
    public void Contains_FindsSingleAndCompositeFlags()
    {
        Assert.True("One,Two".Contains(TestFlags.One));
        Assert.True("two".Contains(TestFlags.One | TestFlags.Two));
        Assert.False("Four".Contains(TestFlags.One));
        Assert.False("Three".Contains(TestFlags.One | TestFlags.Two));
    }

    [Fact]
    public void Contains_HandlesEmptyInput()
        => Assert.False("".Contains(TestFlags.One));

    [Fact]
    public void GetEnumNameValuePairs_ReturnsValuesForEnumAndNullableEnum()
    {
        var values = typeof(SimpleEnum).GetEnumNameValuePairs();
        var nullableValues = typeof(SimpleEnum?).GetEnumNameValuePairs();

        Assert.Equal(values, nullableValues);
        Assert.Equal(3, values.Count);
        Assert.Equal(("One", (object)SimpleEnum.One), values[1]);
    }

    [Fact]
    public void GetEnumNameValuePairs_ReturnsEmptyForNonEnum()
        => Assert.Empty(typeof(string).GetEnumNameValuePairs());

    [Flags]
    private enum TestFlags
    {
        None = 0,
        One = 1,
        Two = 2,
        Four = 4
    }

    private enum SimpleEnum
    {
        Zero = 0,
        One = 1,
        Two = 2
    }

    private enum DescEnum
    {
        [Description("Alpha Desc")]
        Alpha = 1,
        [Description("Beta Desc")]
        Beta = 2,
        Gamma = 3
    }
}
