using System.Text;

namespace Craft.Utilities.Builders;

// Adapted from https://github.com/EdCharbeneau/BlazorComponentUtilities
/// <summary>Builds a space-separated list of CSS values.</summary>
public class ValueBuilder
{
    private readonly StringBuilder _buffer = new();

    public bool HasValue => _buffer.Length > 0;

    /// <summary>Adds a value when enabled, ignoring blank values and trimming their outer whitespace.</summary>
    public ValueBuilder AddValue(string? value, bool when = true)
    {
        if (!when || string.IsNullOrWhiteSpace(value))
            return this;

        if (HasValue)
            _buffer.Append(' ');

        _buffer.Append(value.Trim());
        return this;
    }

    /// <summary>Evaluates the value factory once, only when enabled.</summary>
    public ValueBuilder AddValue(Func<string?> value, bool when = true)
    {
        if (!when)
            return this;

        ArgumentNullException.ThrowIfNull(value);
        return AddValue(value());
    }

    public string Build() => _buffer.ToString();

    public override string ToString() => Build();
}
