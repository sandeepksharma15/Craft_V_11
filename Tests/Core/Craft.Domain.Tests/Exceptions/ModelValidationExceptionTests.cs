using Craft.Domain.Exceptions;

namespace Craft.Domain.Tests.Exceptions;

public class ModelValidationExceptionTests
{
    [Fact]
    public void Constructor_ValidationErrors_DeepCopiesAndFlattensInput()
    {
        string[] nameErrors = ["Required", "Too long"];
        Dictionary<string, string[]> source = new() { ["Name"] = nameErrors, ["Age"] = ["Invalid"] };
        Exception cause = new("Cause");
        ModelValidationException exception = new(source, "Validation failed", cause);
        nameErrors[0] = "Changed";
        source.Clear();

        Assert.Equal(400, exception.StatusCodeValue);
        Assert.Same(cause, exception.InnerException);
        Assert.Equal(["Required", "Too long", "Invalid"], exception.Errors);
        Assert.Equal(["Required", "Too long"], exception.ValidationErrors["Name"]);
        Assert.Equal(["Invalid"], exception.ValidationErrors["Age"]);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)exception.ValidationErrors["Name"])[0] = "Changed");
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, IReadOnlyList<string>>)exception.ValidationErrors).Clear());
    }

    [Fact]
    public void Constructor_EmptyDictionary_ReturnsEmptyCollections()
    {
        ModelValidationException exception = new(new Dictionary<string, string[]>());
        Assert.Equal("One or more validation failures have occurred.", exception.Message);
        Assert.Empty(exception.ValidationErrors);
        Assert.Empty(exception.Errors);
        Assert.Empty(new ModelValidationException().ValidationErrors);
    }

    [Fact]
    public void Constructor_CaseDistinctAndModelLevelKeys_PreservesAllKeys()
    {
        ModelValidationException exception = new(new Dictionary<string, string[]>
        {
            ["Name"] = ["One"], ["name"] = ["Two"], [""] = ["Model error"]
        });
        Assert.Equal(3, exception.ValidationErrors.Count);
        Assert.Equal(["Model error"], exception.ValidationErrors[""]);
    }

    [Fact]
    public void Constructor_NullDictionary_RejectsInput()
        => Assert.Throws<ArgumentNullException>(() => new ModelValidationException(validationErrors: null!));

    [Fact]
    public void Constructor_NullErrorArray_RejectsInput()
        => Assert.Throws<ArgumentNullException>(() => new ModelValidationException(new Dictionary<string, string[]> { ["Name"] = null! }));

    [Fact]
    public void Constructor_EmptyPropertyErrors_AreSupported()
    {
        ModelValidationException exception = new(new Dictionary<string, string[]> { ["Name"] = [] });
        Assert.Empty(exception.Errors);
        Assert.Empty(exception.ValidationErrors["Name"]);
    }
}