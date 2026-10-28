using Craft.Domain.Resources;

namespace Craft.Domain.Helpers;

public static class DomainConstants
{
    #region Regular Expressions

    /// <summary>
    /// Regular expression for validating names (first character uppercase, allows alphabets and special characters like .'()).
    /// </summary>
    public const string NameRegExpr = @"^[A-Z][A-Za-z\.\'\(\) ]*";

    /// <summary>
    /// Regular expression for validating uppercase alphabetic codes.
    /// </summary>
    public const string AlphaCodeRegEx = "^[A-Z]+";

    /// <summary>
    /// Regular expression for validating alphanumeric codes (first character uppercase).
    /// </summary>
    public const string AlphaNumCodeRegEx = "^[A-Z][A-Z0-9]*";

    /// <summary>
    /// Regular expression for validating digit-only strings.
    /// </summary>
    public const string DigitsRegEx = "^[0-9]+";

    /// <summary>
    /// Regular expression for validating positive integers (no leading zeros).
    /// </summary>
    public const string DigitsGreaterThanZero = "^[1-9]+";

    /// <summary>
    /// Regular expression for validating numbers (integers and decimals).
    /// </summary>
    public const string NumberRegEx = "(^[0-9]*[.])?[0-9]+";

    /// <summary>
    /// Regular expression for validating alphanumeric codes with hyphens.
    /// </summary>
    public const string AlphaNumCodeWithHyphenRegEx = @"[A-Z]*[0-9\-]*";

    /// <summary>
    /// Regular expression for validating email addresses according to RFC standards.
    /// </summary>
    public const string EmailRegExpr = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

    /// <summary>
    /// Regular expression for validating phone numbers (allows +, digits, spaces, hyphens, and parentheses).
    /// </summary>
    public const string PhoneRegExpr = @"^\+?[\d\s\-\(\)]+$";

    /// <summary>
    /// Regular expression for validating URLs (HTTP/HTTPS).
    /// </summary>
    public const string UrlRegExpr = @"^https?://[^\s]+$";

    #endregion

    /// <summary>Resource-backed templates with FluentValidation's property-name placeholder.</summary>
    public static class Fv
    {
        public static string Required => DomainResources.Format(nameof(DomainResources.RequiredError), "{PropertyName}");

        public static string NameFormat => DomainResources.Format(nameof(DomainResources.AlphabetAndSpecialCharError), "{PropertyName}");

        public static string AlphaNumCode => DomainResources.Format(nameof(DomainResources.AlphaNumericError), "{PropertyName}");

        public static string InvalidEmail => DomainResources.Format(nameof(DomainResources.InvalidEmail), "{PropertyName}");

        public static string InvalidPhone => DomainResources.Format(nameof(DomainResources.InvalidPhone), "{PropertyName}");

        public static string MaxLength(int max)
            => DomainResources.Format(nameof(DomainResources.MaxLengthError), "{PropertyName}", max);

        public static string InclusiveBetween(int min, int max)
            => DomainResources.Format(nameof(DomainResources.RangeError), "{PropertyName}", min, max);

        public static string InclusiveBetween(decimal min, decimal max)
            => DomainResources.Format(nameof(DomainResources.RangeError), "{PropertyName}", min, max);

        public static string ExactLength(int length)
            => DomainResources.Format(nameof(DomainResources.ExactLengthError), "{PropertyName}", length);
    }
}