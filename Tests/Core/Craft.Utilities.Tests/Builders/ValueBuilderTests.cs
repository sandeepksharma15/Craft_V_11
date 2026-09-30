using Craft.Utilities.Builders;

namespace Craft.Utilities.Tests.Builders;

public class ValueBuilderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void AddValue_BlankString_DoesNotInsertSeparators(string? value)
    {
        ValueBuilder builder = new();

        builder.AddValue(value).AddValue("first").AddValue(value).AddValue("second").AddValue(value);

        Assert.True(builder.HasValue);
        Assert.Equal("first second", builder.Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void AddValue_FactoryReturningBlank_DoesNotInsertSeparators(string? value)
    {
        ValueBuilder builder = new();

        builder.AddValue("first").AddValue(() => value).AddValue("second");

        Assert.Equal("first second", builder.Build());
    }

    [Fact]
    public void AddValue_OuterWhitespace_TrimsEachValueAndPreservesInnerWhitespace()
    {
        ValueBuilder builder = new();

        builder.AddValue("  calc(100% - 2rem)  ").AddValue(" \tvar(--gap,  0)\r\n");

        Assert.Equal("calc(100% - 2rem) var(--gap,  0)", builder.Build());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddValue_Factory_EvaluatesOnlyWhenEnabled(bool when)
    {
        ValueBuilder builder = new();
        int calls = 0;

        ValueBuilder result = builder.AddValue(() =>
        {
            calls++;
            return "value";
        }, when);

        Assert.Same(builder, result);
        Assert.Equal(when ? 1 : 0, calls);
        Assert.Equal(when ? "value" : "", builder.Build());
        Assert.Equal(when, builder.HasValue);
    }

    [Fact]
    public void AddValue_NullFactory_ThrowsOnlyWhenEnabled()
    {
        ValueBuilder builder = new();

        Assert.Throws<ArgumentNullException>("value", () => builder.AddValue((Func<string?>)null!));
        Assert.Same(builder, builder.AddValue((Func<string?>)null!, false));
        Assert.False(builder.HasValue);
        Assert.Equal(string.Empty, builder.Build());
    }

    [Fact]
    public void AddValue_ThrowingFactory_PropagatesExceptionWithoutChangingValues()
    {
        ValueBuilder builder = new();
        builder.AddValue("first");
        InvalidOperationException error = new("factory failed");

        Assert.Same(error, Assert.Throws<InvalidOperationException>(() =>
            builder.AddValue(() => throw error)));
        Assert.Same(builder, builder.AddValue(() => throw error, false));
        Assert.True(builder.HasValue);
        Assert.Equal("first", builder.Build());
    }

    [Fact]
    public void AddValue_EnabledAndDisabledCalls_ReturnSameInstance()
    {
        ValueBuilder builder = new();

        Assert.Same(builder, builder.AddValue("first"));
        Assert.Same(builder, builder.AddValue("ignored", false));
        Assert.Same(builder, builder.AddValue((string?)null));
        Assert.Same(builder, builder.AddValue(() => "second"));

        Assert.Equal("first second", builder.Build());
    }

    [Fact]
    public void Build_RepeatedCalls_ReturnSnapshotsWithoutChangingState()
    {
        ValueBuilder builder = new();
        Assert.Equal(string.Empty, builder.Build());
        builder.AddValue("first");

        string snapshot = builder.Build();
        Assert.Equal(snapshot, builder.Build());
        Assert.Equal(snapshot, builder.ToString());
        builder.AddValue("second");

        Assert.Equal("first", snapshot);
        Assert.Equal("first second", builder.Build());
    }

    [Fact]
    public void AddValue_ComplexCss_PreservesContentOrderAndDuplicates()
    {
        ValueBuilder builder = new();

        builder.AddValue("'a;b:c'")
            .AddValue("url('data:image/svg+xml;base64,PHN2Zz4=')")
            .AddValue("var(--Accent, Red)")
            .AddValue("'a;b:c'");

        Assert.Equal("'a;b:c' url('data:image/svg+xml;base64,PHN2Zz4=') var(--Accent, Red) 'a;b:c'", builder.Build());
    }

    [Fact]
    public void AddValue_InsideStyleBuilder_OmitsBlanksAndSeparatesValues()
    {
        string result = StyleBuilder.Empty()
            .AddStyle("padding", values => values.AddValue(" 1px ").AddValue("").AddValue(() => " 2px "))
            .Build();

        Assert.Equal("padding:1px 2px;", result);
    }

    [Fact]
    public void Default_HasNoValue()
    {
        // Arrange & Act
        var builder = new ValueBuilder();

        // Assert
        Assert.False(builder.HasValue);
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void AddValue_String_WhenTrue_AppendsValue()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue("foo", true);

        // Assert
        Assert.True(builder.HasValue);
        Assert.Equal("foo", builder.ToString());
    }

    [Fact]
    public void AddValue_String_WhenFalse_DoesNotAppend()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue("foo", false);

        // Assert
        Assert.False(builder.HasValue);
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void AddValue_Func_WhenTrue_AppendsValue()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue(() => "bar", true);

        // Assert
        Assert.True(builder.HasValue);
        Assert.Equal("bar", builder.ToString());
    }

    [Fact]
    public void AddValue_Func_WhenFalse_DoesNotAppend()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue(() => "bar", false);

        // Assert
        Assert.False(builder.HasValue);
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void AddValue_Chaining_AppendsMultipleValues()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue("foo").AddValue("bar");

        // Assert
        Assert.True(builder.HasValue);
        Assert.Equal("foo bar", builder.ToString());
    }

    [Fact]
    public void AddValue_MixedChaining_AppendsAllValues()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue("foo").AddValue(() => "bar").AddValue("baz", false);

        // Assert
        Assert.True(builder.HasValue);
        Assert.Equal("foo bar", builder.ToString());
    }

    [Fact]
    public void ToString_TrimmedResult()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue("  foo  ");

        // Assert
        Assert.Equal("foo", builder.ToString());
    }

    [Fact]
    public void AddValue_EmptyString_DoesNotAffectHasValue()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue("");

        // Assert
        Assert.False(builder.HasValue);
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void AddValue_NullString_DoesNotThrowOrAffectHasValue()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue((string)null!);

        // Assert
        Assert.False(builder.HasValue);
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void AddValue_FuncReturnsNull_DoesNotThrowOrAffectHasValue()
    {
        // Arrange
        var builder = new ValueBuilder();

        // Act
        builder.AddValue(() => null!);

        // Assert
        Assert.False(builder.HasValue);
        Assert.Equal(string.Empty, builder.ToString());
    }
}

