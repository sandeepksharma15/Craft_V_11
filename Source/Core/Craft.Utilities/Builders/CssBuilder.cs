/// <summary>
/// A builder for constructing CSS class strings. This class allows for fluent construction of CSS
/// class names, supporting conditional addition of classes.
/// </summary>
/// The original code is from https://github.com/EdCharbeneau/BlazorComponentUtilities

namespace Craft.Utilities.Builders;

// Adapted from https://github.com/EdCharbeneau/BlazorComponentUtilities
/// <summary>
/// Builds a space-separated list of CSS classes.
/// </summary>
public readonly struct CssBuilder(string? value)
{
    #region Private Fields

    private readonly string? _value = value;

    #endregion Private Fields

    #region Public Methods

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

    #endregion Public Methods
}

public static class CssBuilderExtensions
{
    extension(CssBuilder cssBuilder)
    {
        public string? NullIfEmpty()
        {
            var value = cssBuilder.Build();
            return value.Length == 0 ? null : value;
        }
    }
}
