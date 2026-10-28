using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using Craft.Domain.Helpers;
using Craft.Domain.Resources;

namespace Craft.Domain.Tests.Resources;

public class DomainResourcesTests
{
    #region Private Classes

    private sealed class Model
    {
        #region Public Properties

        [Range(18, 65, ErrorMessageResourceType = typeof(DomainResources), ErrorMessageResourceName = nameof(DomainResources.RangeError))]
        public int Age { get; init; }

        [Display(Name = "Full name")]
        [Required(ErrorMessageResourceType = typeof(DomainResources), ErrorMessageResourceName = nameof(DomainResources.RequiredError))]
        [MinLength(2, ErrorMessageResourceType = typeof(DomainResources), ErrorMessageResourceName = nameof(DomainResources.MinLengthError))]
        [MaxLength(5, ErrorMessageResourceType = typeof(DomainResources), ErrorMessageResourceName = nameof(DomainResources.MaxLengthError))]
        public string? Name { get; init; }

        #endregion Public Properties
    }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public async Task AmbientCultures_AreIsolatedAcrossAsyncOperations()
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        static async Task<string> FormatInCulture(string name)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
                await Task.Yield();
                return DomainResources.FormatRangeError("Price", 1.5m, 2.5m);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }
        var results = await Task.WhenAll(FormatInCulture("fr-FR"), FormatInCulture("en-US"));
        Assert.Equal(["Price must be between 1,5 and 2,5", "Price must be between 1.5 and 2.5"], results);
        Assert.Same(original, CultureInfo.CurrentCulture);
        Assert.Same(originalUi, CultureInfo.CurrentUICulture);
    }

    [Fact]
    public void Catalog_ContainsExactlyThePublicTemplates_WithoutMalformedOrRedundantText()
    {
        var manager = new ResourceManager("Craft.Domain.Resources.DomainResources", typeof(DomainResources).Assembly);
        using var resources = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var entries = resources.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
        var properties = typeof(DomainResources).GetProperties(BindingFlags.Public | BindingFlags.Static);
        Assert.Equal(17, entries.Count);
        Assert.Equal(entries.Keys.Order(), properties.Select(p => p.Name).Order());
        Assert.Equal(entries.Count, entries.Values.Distinct().Count());
        foreach (var property in properties)
        {
            var template = entries[property.Name];
            Assert.False(string.IsNullOrWhiteSpace(template));
            Assert.Equal(template.Trim(), template);
            Assert.Equal(template, property.GetValue(null));
            var count = property.Name switch
            {
                "StartDateGreaterError" or "DuplicateError" => 0,
                "RangeError" => 3,
                "AfterError" or "MinLengthError" or "MaxLengthError" or "ExactLengthError" => 2,
                _ => 1
            };
            Assert.Equal(count, CompositeFormat.Parse(template).MinimumArgumentCount);
        }
        Assert.Equal("en", typeof(DomainResources).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>()!.CultureName);
    }

    [Fact]
    public void DataAnnotations_AcceptsValidValues()
    {
        var model = new Model { Name = "Alice", Age = 30 };
        Assert.True(Validator.TryValidateObject(model, new ValidationContext(model), [], true));
    }

    [Theory]
    [InlineData(null, 20, "Full name is required")]
    [InlineData("A", 20, "Full name must be at least 2 characters")]
    [InlineData("Too long", 20, "Full name must not exceed 5 characters")]
    [InlineData("Alice", 17, "Age must be between 18 and 65")]
    public void DataAnnotations_ResolvesResourceProperties(string? name, int age, string expected)
    {
        var model = new Model { Name = name, Age = age };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.Equal(expected, Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void ExplicitCulture_FormatsWithoutChangingAmbientCulture()
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        Assert.Equal("Price must be between 1,5 and 2,5", DomainResources.Format(CultureInfo.GetCultureInfo("fr-FR"), nameof(DomainResources.RangeError), "Price", 1.5m, 2.5m));
        Assert.Same(original, CultureInfo.CurrentCulture);
        Assert.Same(originalUi, CultureInfo.CurrentUICulture);
    }

    [Fact]
    public void FluentValidationTemplates_PreservePropertyTokens()
    {
        Assert.Equal("{PropertyName} is required", DomainConstants.Fv.Required);
        Assert.Equal("{PropertyName} must start with an uppercase letter and contain only letters, spaces, periods, apostrophes and parentheses", DomainConstants.Fv.NameFormat);
        Assert.Equal("{PropertyName} must start with an uppercase letter and contain only letters and digits", DomainConstants.Fv.AlphaNumCode);
        Assert.Equal("{PropertyName} is not a valid email address", DomainConstants.Fv.InvalidEmail);
        Assert.Equal("{PropertyName} is not a valid phone number", DomainConstants.Fv.InvalidPhone);
        Assert.Equal("{PropertyName} must not exceed 10 characters", DomainConstants.Fv.MaxLength(10));
        Assert.Equal("{PropertyName} must be between 1 and 10", DomainConstants.Fv.InclusiveBetween(1, 10));
        Assert.Equal("{PropertyName} must be exactly 6 characters", DomainConstants.Fv.ExactLength(6));
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("{PropertyName} must be between 1,5 and 2,5", DomainConstants.Fv.InclusiveBetween(1.5m, 2.5m));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Format_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => DomainResources.Format(nameof(DomainResources.RequiredError), (object?[])null!));
        Assert.Throws<ArgumentNullException>(() => DomainResources.Format(null!, nameof(DomainResources.RequiredError), "Name"));
        Assert.Throws<ArgumentNullException>(() => DomainResources.Format(CultureInfo.InvariantCulture, nameof(DomainResources.RequiredError), (object?[])null!));
        Assert.Throws<FormatException>(() => DomainResources.Format(nameof(DomainResources.RangeError), "Age"));
        Assert.Throws<FormatException>(() => DomainResources.Format(CultureInfo.InvariantCulture, nameof(DomainResources.RangeError), "Age"));
        Assert.Throws<KeyNotFoundException>(() => DomainResources.Format("Missing"));
        Assert.Throws<KeyNotFoundException>(() => DomainResources.Format(CultureInfo.InvariantCulture, "Missing"));
    }

    [Fact]
    public void Lookup_FallsBackToEnglish()
        => Assert.Equal("{0} is required", DomainResources.GetString(nameof(DomainResources.RequiredError), CultureInfo.GetCultureInfo("fr-FR")));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void Lookup_RejectsMissingNames(string? name)
        => Assert.ThrowsAny<ArgumentException>(() => DomainResources.GetString(name!));

    [Theory]
    [InlineData("Unknown")]
    [InlineData("requirederror")]
    public void Lookup_RejectsUnknownOrIncorrectlyCasedNames(string name)
        => Assert.Throws<KeyNotFoundException>(() => DomainResources.GetString(name));

    [Fact]
    public void TypedFormatters_InsertArguments()
    {
        Assert.Equal("End must be after Start", DomainResources.FormatAfterError("End", "Start"));
        Assert.Equal("Name is required", DomainResources.FormatRequiredError("Name"));
        Assert.Equal("Age must be between 18 and 65", DomainResources.FormatRangeError("Age", 18, 65));
        Assert.Equal("Name must be at least 2 characters", DomainResources.FormatMinLengthError("Name", 2));
        Assert.Equal("Name must not exceed 20 characters", DomainResources.FormatMaxLengthError("Name", 20));
        Assert.Equal("Code must be exactly 6 characters", DomainResources.FormatExactLengthError("Code", 6));
    }

    #endregion Public Methods
}
