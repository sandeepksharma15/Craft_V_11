namespace Craft.Utilities.Builders;

// Adapted from https://github.com/EdCharbeneau/BlazorComponentUtilities
/// <summary>Builds a space-separated list of CSS classes.</summary>
public readonly struct CssBuilder(string? value)
{
    private readonly string? _value = value;

    public static CssBuilder Default(string? value) => new(value);

    public CssBuilder AddClass(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return this;

        var current = Build();
        var className = value.Trim();
        return new CssBuilder(current.Length == 0 ? className : $"{current} {className}");
    }

    public CssBuilder AddClass(string value, bool when = true)
        => when ? AddClass(value) : this;

    public CssBuilder AddClass(string value, Func<bool>? when = null)
        => AddClass(value, when?.Invoke() == true);

    public CssBuilder AddClass(Func<string> value, bool when = true)
        => when ? AddClass(value()) : this;

    public CssBuilder AddClass(Func<string> value, Func<bool>? when = null)
        => AddClass(value, when?.Invoke() == true);

    public CssBuilder AddClass(CssBuilder builder, bool when = true)
        => when ? AddClass(builder.Build()) : this;

    public CssBuilder AddClass(CssBuilder builder, Func<bool>? when = null)
        => AddClass(builder, when?.Invoke() == true);

    public CssBuilder AddClassFromAttributes(IReadOnlyDictionary<string, object>? additionalAttributes)
        => additionalAttributes?.TryGetValue("class", out var value) == true
            ? AddClass(value?.ToString() ?? string.Empty)
            : this;

    public CssBuilder AddValue(string? value)
        => new((_value ?? string.Empty) + value);

    public string Build() => _value?.Trim() ?? string.Empty;

    public override string ToString() => Build();
}

public static class CssBuilderExtensions
{
    public static string? NullIfEmpty(this CssBuilder cssBuilder)
    {
        var value = cssBuilder.Build();
        return value.Length == 0 ? null : value;
    }
}
