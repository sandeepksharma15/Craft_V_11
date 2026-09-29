using Craft.Utilities.Builders;

namespace Craft.Utilities.Tests.Builders;

public class StyleBuilderTests
{
    [Fact]
    public void Build_DefaultInstances_ReturnEmptyString()
    {
        Assert.Equal(string.Empty, default(StyleBuilder).Build());
        Assert.Equal(string.Empty, new StyleBuilder().ToString());
        Assert.Null(default(StyleBuilder).NullIfEmpty());
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" \t\r\n ", "")]
    [InlineData("color:red", "color:red;")]
    [InlineData("color:red;", "color:red;;")]
    public void Default_StyleFragment_PreservesExistingSemicolonBehavior(string? style, string expected)
    {
        StyleBuilder builder = StyleBuilder.Default(style);

        Assert.Equal(expected, builder.Build());
        Assert.Equal(expected, builder.ToString());
        Assert.Equal(expected.Length == 0 ? null : expected, builder.NullIfEmpty());
    }

    [Fact]
    public void AddStyle_DefaultValue_CanBeMutated()
    {
        StyleBuilder builder = default;

        builder.AddStyle("color", "red");
        builder.AddStyle("padding", "0");

        Assert.Equal("color:red;padding:0;", builder.Build());
    }

    [Fact]
    public void AddStyle_CopiesAndNestedBuilders_AreIndependent()
    {
        StyleBuilder original = new("color", "red");
        StyleBuilder copy = original;
        StyleBuilder parent = StyleBuilder.Empty().AddStyle(original);

        copy.AddStyle("padding", "0");
        original.AddStyle("margin", "0");

        Assert.Equal("color:red;margin:0;", original.Build());
        Assert.Equal("color:red;padding:0;", copy.Build());
        Assert.Equal("color:red;", parent.Build());
        Assert.Equal("color:red;", parent.AddStyle(default(StyleBuilder)).Build());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddStyle_ValueFactoryAndBoolean_EvaluatesOnlyWhenEnabled(bool when)
    {
        StyleBuilder builder = new("color", "red");
        int calls = 0;

        builder.AddStyle("padding", () =>
        {
            calls++;
            return "0";
        }, when);

        Assert.Equal(when ? 1 : 0, calls);
        Assert.Equal(when ? "color:red;padding:0;" : "color:red;", builder.Build());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void AddStyle_StringAndPredicate_AddsOnlyWhenTrue(bool? condition)
    {
        StyleBuilder builder = StyleBuilder.Empty();
        int calls = 0;
        Func<bool>? when = condition.HasValue ? () => { calls++; return condition.Value; } : null;

        builder.AddStyle("color", "red", when);

        Assert.Equal(condition.HasValue ? 1 : 0, calls);
        Assert.Equal(condition == true ? "color:red;" : "", builder.Build());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void AddStyle_ValueFactoryAndPredicate_EvaluatesConditionBeforeValue(bool? condition)
    {
        StyleBuilder builder = StyleBuilder.Empty();
        List<string> calls = [];
        Func<bool>? when = condition.HasValue
            ? () => { calls.Add("condition"); return condition.Value; }
            : null;

        builder.AddStyle("color", () =>
        {
            calls.Add("value");
            return "red";
        }, when);

        string[] expectedCalls = condition switch
        {
            true => ["condition", "value"],
            false => ["condition"],
            null => []
        };
        Assert.Equal(expectedCalls, calls);
        Assert.Equal(condition == true ? "color:red;" : "", builder.Build());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddStyle_NestedBuilderAndBoolean_AddsOnlyWhenTrue(bool when)
    {
        StyleBuilder builder = new("color", "red");
        StyleBuilder child = new("padding", "0");

        builder.AddStyle(child, when);

        Assert.Equal(when ? "color:red;padding:0;" : "color:red;", builder.Build());
        Assert.Equal("padding:0;", child.Build());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void AddStyle_NestedBuilderAndPredicate_AddsOnlyWhenTrue(bool? condition)
    {
        StyleBuilder builder = StyleBuilder.Empty();
        StyleBuilder child = new("color", "red");
        int calls = 0;
        Func<bool>? when = condition.HasValue ? () => { calls++; return condition.Value; } : null;

        builder.AddStyle(child, when);

        Assert.Equal(condition.HasValue ? 1 : 0, calls);
        Assert.Equal(condition == true ? "color:red;" : "", builder.Build());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AddStyle_ValueBuilder_InvokesOnlyWhenEnabledAndOmitsEmptyValues(bool when, bool addValue)
    {
        StyleBuilder builder = new("color", "red");
        int calls = 0;

        builder.AddStyle("text-decoration", values =>
        {
            calls++;
            values.AddValue("underline", addValue);
        }, when);

        Assert.Equal(when ? 1 : 0, calls);
        Assert.Equal(when && addValue ? "color:red;text-decoration:underline;" : "color:red;", builder.Build());
    }

    [Fact]
    public void AddStyle_NullCallbacks_ThrowOnlyWhenEnabled()
    {
        StyleBuilder builder = StyleBuilder.Empty();

        Assert.Throws<ArgumentNullException>("value", () => builder.AddStyle("color", (Func<string>)null!));
        Assert.Throws<ArgumentNullException>("builder", () => builder.AddStyle("color", (Action<ValueBuilder>)null!));
        builder.AddStyle("color", (Func<string>)null!, false);
        builder.AddStyle("color", (Action<ValueBuilder>)null!, false);

        Assert.Equal(string.Empty, builder.Build());
    }

    [Fact]
    public void AddStyle_ThrowingCallback_PropagatesExceptionWithoutAddingDeclaration()
    {
        StyleBuilder builder = new("color", "red");
        InvalidOperationException error = new("callback failed");

        Assert.Same(error, Assert.Throws<InvalidOperationException>(() =>
            builder.AddStyle("padding", () => throw error, true)));
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() =>
            builder.AddStyle("padding", "0", () => throw error)));
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() =>
            builder.AddStyle("padding", (ValueBuilder _) => throw error)));
        Assert.Equal("color:red;", builder.Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void AddStyleFromAttributes_EmptyStyle_DoesNotChangeBuilder(string? style)
    {
        StyleBuilder builder = new("color", "red");
        Dictionary<string, object> attributes = new() { ["style"] = style! };

        builder.AddStyleFromAttributes(attributes);

        Assert.Equal("color:red;", builder.Build());
    }

    [Fact]
    public void AddStyleFromAttributes_NullOrMissingAttribute_DoesNotChangeBuilder()
    {
        StyleBuilder builder = new("color", "red");

        builder.AddStyleFromAttributes(null);
        builder.AddStyleFromAttributes(new Dictionary<string, object> { ["class"] = "example" });

        Assert.Equal("color:red;", builder.Build());
    }

    [Theory]
    [InlineData("color:red")]
    [InlineData("color:red;")]
    [InlineData("  color:red; \t")]
    public void AddStyleFromAttributes_WithFollowingDeclaration_EnsuresSeparator(string style)
    {
        Dictionary<string, object> attributes = new() { ["style"] = style };

        string result = StyleBuilder.Empty().AddStyleFromAttributes(attributes).AddStyle("padding", "0").Build();

        Assert.Equal("color:red;padding:0;", result);
        Assert.Equal(style, attributes["style"]);
    }

    [Fact]
    public void AddStyleFromAttributes_ObjectValue_UsesToString()
    {
        Dictionary<string, object> attributes = new() { ["style"] = new StyleBuilder("color", "red") };

        string result = StyleBuilder.Empty().AddStyleFromAttributes(attributes).Build();

        Assert.Equal("color:red;", result);
    }

    [Fact]
    public void Build_ComplexCssValues_PreservesContentAndDeclarationOrder()
    {
        string result = StyleBuilder.Default("--accent", "var(--brand, red)")
            .AddStyle("background-image", "url('data:image/svg+xml;base64,PHN2Zz4=')")
            .AddStyle("content", "'a;b:c'")
            .AddStyle("--accent", "blue !important")
            .Build();

        Assert.Equal("--accent:var(--brand, red);background-image:url('data:image/svg+xml;base64,PHN2Zz4=');content:'a;b:c';--accent:blue !important;", result);
    }

    [Fact]
    public void ShouldBulidConditionalInlineStyles()
    {
        // Arrange
        var hasBorder = true;
        var isOnTop = false;
        var top = 2;
        var bottom = 10;
        var left = 4;
        var right = 20;

        // Act
        var ClassToRender = new StyleBuilder("background-color", "DodgerBlue")
                        .AddStyle("border-width", $"{top}px {right}px {bottom}px {left}px", when: hasBorder)
                        .AddStyle("z-index", "999", when: isOnTop)
                        .AddStyle("z-index", "-1", when: !isOnTop)
                        .AddStyle("padding", "35px")
                        .Build();
        // Assert
        Assert.Equal("background-color:DodgerBlue;border-width:2px 20px 10px 4px;z-index:-1;padding:35px;", ClassToRender);
    }

    [Fact]
    public void ShouldBulidConditionalInlineStylesFromAttributes()
    {

        // Arrange
        var hasBorder = true;
        var isOnTop = false;
        var top = 2;
        var bottom = 10;
        var left = 4;
        var right = 20;

        // Act
        var StyleToRender = new StyleBuilder("background-color", "DodgerBlue")
                        .AddStyle("border-width", $"{top}px {right}px {bottom}px {left}px", when: hasBorder)
                        .AddStyle("z-index", "999", when: isOnTop)
                        .AddStyle("z-index", "-1", when: !isOnTop)
                        .AddStyle("padding", "35px")
                        .Build();

        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object> { { "style", StyleToRender } };

        var ClassToRender = new StyleBuilder().AddStyleFromAttributes(attributes).Build();

        // Assert
        Assert.Equal("background-color:DodgerBlue;border-width:2px 20px 10px 4px;z-index:-1;padding:35px;", ClassToRender);
    }

    [Fact]
    public void ShouldAddExistingStyle()
    {
        var StyleToRender = StyleBuilder.Empty()
            .AddStyle("background-color:DodgerBlue;")
            .AddStyle("padding", "35px")
            .Build();

        var StyleToRenderFromDefaultConstructor = StyleBuilder.Default(StyleToRender).Build();

        /// Double ;; is valid HTML.
        /// The CSS syntax allows for empty declarations, which means that you can add leading and trailing semicolons as you like. For instance, this is valid CSS
        /// .foo { ;;;display:none;;;color:black;;; }
        /// Trimming is possible, but is it worth the operations for a non-issue?
        Assert.Equal("background-color:DodgerBlue;;padding:35px;", StyleToRender);
        Assert.Equal("background-color:DodgerBlue;;padding:35px;;", StyleToRenderFromDefaultConstructor);

    }

    [Fact]
    public void ShouldNotAddEmptyStyle()
    {
        // Arrange & Act
        var StyleToRender = StyleBuilder.Empty().AddStyle("");

        Assert.Null(StyleToRender.NullIfEmpty());
    }

    [Fact]
    public void ShouldAddNestedStyles()
    {


        var Child = StyleBuilder.Empty()
            .AddStyle("background-color", "DodgerBlue")
            .AddStyle("padding", "35px");

        var StyleToRender = StyleBuilder.Empty()
            .AddStyle(Child)
            .AddStyle("z-index", "-1")
            .Build();

        /// Double ;; is valid HTML.
        /// The CSS syntax allows for empty declarations, which means that you can add leading and trailing semicolons as you like. For instance, this is valid CSS
        /// .foo { ;;;display:none;;;color:black;;; }
        /// Trimming is possible, but is it worth the operations for a non-issue?
        Assert.Equal("background-color:DodgerBlue;padding:35px;z-index:-1;", StyleToRender);
    }

    [Fact]
    public void ShouldAddComplexStyles()
    {
        var StyleToRender = StyleBuilder.Empty()
            .AddStyle("text-decoration", v => v
                        .AddValue("underline", true)
                        .AddValue("overline", false)
                        .AddValue("line-through", true),
                        when: true)
            .AddStyle("z-index", "-1")
            .Build();

        /// Double ;; is valid HTML.
        /// The CSS syntax allows for empty declarations, which means that you can add leading and trailing semicolons as you like. For instance, this is valid CSS
        /// .foo { ;;;display:none;;;color:black;;; }
        /// Trimming is possible, but is it worth the operations for a non-issue?
        Assert.Equal("text-decoration:underline line-through;z-index:-1;", StyleToRender);

    }

    [Fact]
    public void ShouldBuildStyleWithFunc()
    {
        {
            // Arrange
            // Simulates Razor Components attribute splatting feature
            IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object> { { "class", "my-custom-class-1" } };

            // Act
            var StyleToRender = StyleBuilder.Empty()
                            .AddStyle("background-color", () => attributes["style"].ToString()!, when: attributes.ContainsKey("style"))
                            .AddStyle("background-color", "black")
                            .Build();
            // Assert
            Assert.Equal("background-color:black;", StyleToRender);
        }
    }

    [Fact]
    public void AddStyle_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var builder = StyleBuilder.Default("color", "red");

        // Act
        builder.AddStyle("background-color", "blue");
        var result = builder.Build();

        // Assert
        Assert.Equal("color:red;background-color:blue;", result);
    }

    [Fact]
    public void AddStyle_Conditional_WhenConditionTrue_AddsStyle()
    {
        // Arrange
        var builder = StyleBuilder.Empty();

        // Act
        builder.AddStyle("margin", "10px", true);
        var result = builder.Build();

        // Assert
        Assert.Equal("margin:10px;", result);
    }

    [Fact]
    public void AddStyle_Conditional_WhenConditionFalse_DoesNotAddStyle()
    {
        // Arrange
        var builder = StyleBuilder.Empty();

        // Act
        builder.AddStyle("padding", "5px", false);
        var result = builder.Build();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Build_WithNoStyles_ReturnsEmptyString()
    {
        // Arrange
        var builder = StyleBuilder.Empty();

        // Act
        var result = builder.Build();

        // Assert
        Assert.Equal(string.Empty, result);
    }
}

