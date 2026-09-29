namespace Craft.Utilities.Builders;

// Adapted from https://github.com/EdCharbeneau/BlazorComponentUtilities
/// <summary>Builds inline CSS declarations. Adding a style mutates this value and returns it for chaining.</summary>
public struct StyleBuilder
{
    private string? _stringBuffer;

    public static StyleBuilder Default(string prop, string value) => new(prop, value);

    public static StyleBuilder Default(string? style) => Empty().AddStyle(style);

    public static StyleBuilder Empty() => new();

    public StyleBuilder(string prop, string value)
        => _stringBuffer = $"{prop}:{value};";

    /// <summary>Appends a style fragment followed by a semicolon, preserving any existing semicolons.</summary>
    public StyleBuilder AddStyle(string? style)
        => !string.IsNullOrWhiteSpace(style) ? AddRaw($"{style};") : this;

    private StyleBuilder AddRaw(string style)
    {
        _stringBuffer += style;
        return this;
    }

    public StyleBuilder AddStyle(string prop, string value)
        => AddRaw($"{prop}:{value};");

    public StyleBuilder AddStyle(string prop, string value, bool when = true)
        => when ? AddStyle(prop, value) : this;

    public StyleBuilder AddStyle(string prop, Func<string> value, bool when = true)
    {
        if (!when)
            return this;

        ArgumentNullException.ThrowIfNull(value);
        return AddStyle(prop, value());
    }

    /// <summary>Adds a declaration only when the predicate returns true. A null predicate skips it.</summary>
    public StyleBuilder AddStyle(string prop, string value, Func<bool>? when = null)
        => AddStyle(prop, value, when?.Invoke() == true);

    public StyleBuilder AddStyle(string prop, Func<string> value, Func<bool>? when = null)
        => AddStyle(prop, value, when?.Invoke() == true);

    public StyleBuilder AddStyle(StyleBuilder builder) => AddRaw(builder.Build());

    public StyleBuilder AddStyle(StyleBuilder builder, bool when = true)
        => when ? AddStyle(builder) : this;

    public StyleBuilder AddStyle(StyleBuilder builder, Func<bool>? when = null)
        => AddStyle(builder, when?.Invoke() == true);

    /// <summary>Invokes the value builder only when enabled, and omits declarations with no values.</summary>
    public StyleBuilder AddStyle(string prop, Action<ValueBuilder> builder, bool when = true)
    {
        if (!when)
            return this;

        ArgumentNullException.ThrowIfNull(builder);
        ValueBuilder values = new();
        builder(values);
        return AddStyle(prop, values.ToString(), values.HasValue);
    }

    /// <summary>Appends the style attribute, ensuring a separator before subsequent declarations.</summary>
    public StyleBuilder AddStyleFromAttributes(IReadOnlyDictionary<string, object>? additionalAttributes)
    {
        if (additionalAttributes is null || !additionalAttributes.TryGetValue("style", out object? value))
            return this;

        string? style = value?.ToString();
        if (string.IsNullOrWhiteSpace(style))
            return this;

        style = style.Trim();
        return AddRaw(style.EndsWith(';') ? style : $"{style};");
    }

    public readonly string Build() => _stringBuffer?.Trim() ?? string.Empty;

    public override readonly string ToString() => Build();
}

public static class StyleBuilderExtensions
{
    extension(StyleBuilder styleBuilder)
    {
        public string? NullIfEmpty()
        {
            string value = styleBuilder.Build();
            return value.Length == 0 ? null : value;
        }
    }
}
