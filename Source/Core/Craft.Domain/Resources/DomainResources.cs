using System.Globalization;
using System.Resources;

namespace Craft.Domain.Resources;

public static class DomainResources
{
    private static readonly ResourceManager _resourceManager = new(
        "Craft.Domain.Resources.DomainResources", typeof(DomainResources).Assembly);

    public static string AfterError => GetString(nameof(AfterError));

    public static string RequiredError => GetString(nameof(RequiredError));

    public static string StartDateGreaterError => GetString(nameof(StartDateGreaterError));

    public static string RangeError => GetString(nameof(RangeError));

    public static string MinLengthError => GetString(nameof(MinLengthError));

    public static string MaxLengthError => GetString(nameof(MaxLengthError));

    public static string AlphabetAndSpecialCharError => GetString(nameof(AlphabetAndSpecialCharError));

    public static string AlphaNumericError => GetString(nameof(AlphaNumericError));

    public static string CapitalAlphabetError => GetString(nameof(CapitalAlphabetError));

    public static string OnlyDigitError => GetString(nameof(OnlyDigitError));

    public static string OnlyNumberError => GetString(nameof(OnlyNumberError));

    public static string AlphaNumCodeWithHyphenRegExError => GetString(nameof(AlphaNumCodeWithHyphenRegExError));

    public static string FormatError => GetString(nameof(FormatError));

    public static string DuplicateError => GetString(nameof(DuplicateError));

    public static string InvalidEmail => GetString(nameof(InvalidEmail));

    public static string InvalidPhone => GetString(nameof(InvalidPhone));

    public static string ExactLengthError => GetString(nameof(ExactLengthError));

    /// <summary>Looks up a template using the supplied culture or CurrentUICulture.</summary>
    public static string GetString(string name, CultureInfo? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _resourceManager.GetString(name, culture)
            ?? throw new KeyNotFoundException($"Resource '{name}' was not found.");
    }

    /// <summary>Looks up using CurrentUICulture and formats using CurrentCulture.</summary>
    public static string Format(string name, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return string.Format(CultureInfo.CurrentCulture, GetString(name), arguments);
    }

    /// <summary>Uses the supplied culture for both lookup and formatting.</summary>
    public static string Format(CultureInfo culture, string name, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(arguments);
        return string.Format(culture, GetString(name, culture), arguments);
    }

    public static string FormatAfterError(string fieldName, string comparisonFieldName)
        => Format(nameof(AfterError), fieldName, comparisonFieldName);

    public static string FormatRequiredError(string fieldName)
        => Format(nameof(RequiredError), fieldName);

    public static string FormatRangeError(string fieldName, object minValue, object maxValue)
        => Format(nameof(RangeError), fieldName, minValue, maxValue);

    public static string FormatMinLengthError(string fieldName, int minLength)
        => Format(nameof(MinLengthError), fieldName, minLength);

    public static string FormatMaxLengthError(string fieldName, int maxLength)
        => Format(nameof(MaxLengthError), fieldName, maxLength);

    public static string FormatExactLengthError(string fieldName, int length)
        => Format(nameof(ExactLengthError), fieldName, length);
}