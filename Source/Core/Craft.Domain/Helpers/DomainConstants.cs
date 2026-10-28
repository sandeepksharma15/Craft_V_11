using Craft.Domain.Resources;

namespace Craft.Domain.Helpers;

public static class DomainConstants
{
    // Whole-input .NET patterns; callers choose regex options and a finite match timeout.
    /// <summary>ASCII name starting uppercase; allows letters, spaces, periods, apostrophes and parentheses.</summary>
    public const string NameRegExpr = @"\A[A-Z][A-Za-z.'() ]*\z";

    /// <summary>One or more uppercase ASCII letters.</summary>
    public const string AlphaCodeRegEx = @"\A[A-Z]+\z";

    /// <summary>Uppercase ASCII letters and digits, starting with a letter.</summary>
    public const string AlphaNumCodeRegEx = @"\A[A-Z][A-Z0-9]*\z";

    /// <summary>One or more ASCII digits, including leading zeros.</summary>
    public const string DigitsRegEx = @"\A[0-9]+\z";

    /// <summary>Positive ASCII integer without leading zeros.</summary>
    public const string DigitsGreaterThanZero = @"\A[1-9][0-9]*\z";

    /// <summary>Unsigned ASCII integer or decimal using a period; allows <c>.5</c> but not <c>5.</c>.</summary>
    public const string NumberRegEx = @"\A(?:[0-9]+|[0-9]*\.[0-9]+)\z";

    /// <summary>Nonempty uppercase ASCII letters, digits and hyphens in any order.</summary>
    public const string AlphaNumCodeWithHyphenRegEx = @"\A[A-Z0-9-]+\z";

    /// <summary>Basic ASCII email format; not complete RFC validation or a deliverability check.</summary>
    public const string EmailRegExpr = @"\A[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*\z";

    /// <summary>Basic phone format: optional leading +, Unicode digits, spaces, hyphens and parentheses; requires a digit.</summary>
    public const string PhoneRegExpr = @"\A\+?[ ()-]*\d[\d ()-]*\z";

    /// <summary>Lowercase HTTP/HTTPS prefix with a nonempty remainder without whitespace or control characters; not URI validation.</summary>
    public const string UrlRegExpr = @"\Ahttps?://[^\s\p{Cc}]+\z";

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