using System.Text.Json;
using Craft.Core.Results;

namespace Craft.QuerySpec.Tests;

public sealed class PolicyTests
{
    public sealed record Row(int Id, string? Name, int? Age = null)
    {
        public int[] Values { get; init; } = [1];
    }

    private static QueryPolicy<Row> Policy(QueryLimits? limits = null)
        => new QueryPolicy<Row>(limits)
            .Allow("id", row => row.Id, true, Enum.GetValues<FilterOperator>())
            .Allow("name", row => row.Name, true, Enum.GetValues<FilterOperator>())
            .Allow("age", row => row.Age, false, Enum.GetValues<FilterOperator>());

    private static QueryRequest Request(string field, FilterOperator operation, string json = "null")
        => new() { Filter = new FilterGroup { Filters = [new(field, operation, JsonSerializer.Deserialize<JsonElement>(json))] } };

    [Theory]
    [InlineData("id", FilterOperator.Equal, "2", true)]
    [InlineData("id", FilterOperator.NotEqual, "2", false)]
    [InlineData("id", FilterOperator.GreaterThan, "1", true)]
    [InlineData("id", FilterOperator.GreaterThanOrEqual, "2", true)]
    [InlineData("id", FilterOperator.LessThan, "3", true)]
    [InlineData("id", FilterOperator.LessThanOrEqual, "2", true)]
    [InlineData("name", FilterOperator.Contains, "\"ell\"", true)]
    [InlineData("name", FilterOperator.StartsWith, "\"he\"", true)]
    [InlineData("name", FilterOperator.EndsWith, "\"lo\"", true)]
    [InlineData("id", FilterOperator.In, "[1,2]", true)]
    [InlineData("id", FilterOperator.NotIn, "[1,2]", false)]
    [InlineData("age", FilterOperator.IsNull, "null", true)]
    [InlineData("age", FilterOperator.IsNotNull, "null", false)]
    [InlineData("name", FilterOperator.IsNull, "null", false)]
    public void Operators_ProduceTypedPredicates(string field, FilterOperator operation, string value, bool matches)
    {
        Result<Query<Row>> result = Policy().Build(Request(field, operation, value));
        Assert.True(result.IsSuccess);
        Assert.Equal(matches, result.Value.ApplyFilters(new[] { new Row(2, "hello") }.AsQueryable()).Any());
    }

    [Theory]
    [InlineData("id", FilterOperator.Equal, "\"bad\"")]
    [InlineData("id", FilterOperator.IsNull, "null")]
    [InlineData("id", FilterOperator.Contains, "1")]
    [InlineData("name", FilterOperator.Contains, "null")]
    [InlineData("name", FilterOperator.GreaterThan, "\"a\"")]
    [InlineData("id", FilterOperator.In, "1")]
    [InlineData("id", FilterOperator.In, "[1,2,3]")]
    [InlineData("secret", FilterOperator.Equal, "1")]
    public void InvalidFilters_AreValidationResults(string field, FilterOperator operation, string value)
    {
        Result<Query<Row>> result = Policy(new QueryLimits { MaximumValues = 2 }).Build(Request(field, operation, value));
        Assert.Equal(ErrorKind.Validation, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void Groups_SortingAndSpecificationsAreReusable()
    {
        QueryRequest request = new()
        {
            Filter = new FilterGroup
            {
                Logic = FilterLogic.Or,
                Groups = [Request("id", FilterOperator.Equal, "1").Filter!, Request("id", FilterOperator.Equal, "2").Filter!]
            },
            Sort = [new("name"), new("id", true)],
            IncludeDetails = true
        };
        Query<Row> query = Policy().Build(request).Value;
        Row[] rows = [new(1, "same"), new(2, "same"), new(3, "other")];
        Assert.Equal([2, 1], query.ApplyOrdering(query.ApplyFilters(rows.AsQueryable()), row => row.Id).Select(row => row.Id));
        Assert.True(query.IncludeDetails);
        Assert.Equal(2, query.ApplyFilters(rows.AsQueryable()).Count());
        Query<Row> original = new();
        Query<Row> changed = original.Where(row => row.Id > 1).Include("Details").WithDetails();
        Assert.Empty(original.Predicates);
        Assert.Empty(original.Includes);
        Assert.Single(changed.Includes);
        Assert.Equal([1, 2, 3], original.ApplyOrdering(rows.AsQueryable(), row => row.Id).Select(row => row.Id));
        Assert.Equal([3, 2, 1], original.OrderBy(row => row.Id, true).OrderBy(row => row.Name).ApplyOrdering(rows.AsQueryable(), row => row.Id).Select(row => row.Id));
        QueryPolicy<Row> nested = new QueryPolicy<Row>().Allow("positive", row => row.Values.Any(value => value > 0), false, FilterOperator.Equal);
        Assert.True(nested.Build(Request("positive", FilterOperator.Equal, "true")).Value.ApplyFilters(rows.AsQueryable()).Any());
    }

    public static IEnumerable<object[]> InvalidRequests()
    {
        yield return [new QueryRequest { Page = 0 }];
        yield return [new QueryRequest { PageSize = 0 }];
        yield return [new QueryRequest { PageSize = 201 }];
        yield return [new QueryRequest { Page = long.MaxValue }];
        yield return [new QueryRequest { Sort = null! }];
        yield return [new QueryRequest { Sort = [null!] }];
        yield return [new QueryRequest { Sort = [new(null!)] }];
        yield return [new QueryRequest { Sort = [new("unknown")] }];
        yield return [new QueryRequest { Sort = [new("age")] }];
        yield return [new QueryRequest { Sort = [new("id"), new("id")] }];
        yield return [new QueryRequest { Sort = Enumerable.Repeat(new QuerySort("id"), 6).ToArray() }];
        yield return [new QueryRequest { Filter = new FilterGroup { Logic = (FilterLogic)99 } }];
        yield return [new QueryRequest { Filter = new FilterGroup { Filters = null! } }];
        yield return [new QueryRequest { Filter = new FilterGroup { Groups = null! } }];
        yield return [new QueryRequest { Filter = new FilterGroup { Filters = [null!] } }];
        yield return [new QueryRequest { Filter = new FilterGroup { Filters = [new(null!, FilterOperator.Equal)] } }];
        yield return [new QueryRequest { Filter = new FilterGroup { Filters = [new("id", (FilterOperator)99)] } }];
        yield return [new QueryRequest { Filter = new FilterGroup { Filters = [new("id", FilterOperator.Equal)] } }];
        yield return [new QueryRequest { Filter = new FilterGroup { Groups = [null!] } }];
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidRequests_FailClosed(QueryRequest request)
        => Assert.True(Policy().Build(request).IsFailure);

    [Fact]
    public void ComplexityAndValueLimits_AreEnforced()
    {
        QueryRequest request = Request("id", FilterOperator.Equal, "1");
        QueryRequest nested = new() { Filter = new FilterGroup { Groups = [request.Filter!] } };
        Assert.True(Policy(new QueryLimits { MaximumDepth = 1 }).Build(nested).IsFailure);
        Assert.True(Policy(new QueryLimits { MaximumNodes = 1 }).Build(request).IsFailure);
        FilterGroup deeper = new() { Groups = [nested.Filter!, nested.Filter!] };
        Assert.True(Policy(new QueryLimits { MaximumNodes = 5 }).Build(new QueryRequest { Filter = deeper }).IsFailure);
        Assert.True(Policy(new QueryLimits { MaximumValueBytes = 1 }).Build(Request("name", FilterOperator.Equal, "\"long\"")).IsFailure);
        Assert.True(Policy().Build(new QueryRequest()).IsSuccess);
    }

    public static IEnumerable<object[]> InvalidLimits()
    {
        yield return [new QueryLimits { MaximumDepth = 0 }];
        yield return [new QueryLimits { MaximumDepth = 33 }];
        yield return [new QueryLimits { MaximumNodes = 0 }];
        yield return [new QueryLimits { MaximumSortFields = 0 }];
        yield return [new QueryLimits { MaximumValues = 0 }];
        yield return [new QueryLimits { MaximumValueBytes = 0 }];
        yield return [new QueryLimits { MaximumPageSize = 0 }];
    }

    [Theory]
    [MemberData(nameof(InvalidLimits))]
    public void InvalidConfiguration_Throws(QueryLimits limits)
        => Assert.Throws<ArgumentException>(() => Policy(limits));

    [Fact]
    public void ProgrammingErrors_AreNotValidationResults()
    {
        QueryPolicy<Row> policy = Policy();
        Assert.Throws<ArgumentNullException>(() => policy.Build(null!));
        Assert.Throws<ArgumentException>(() => policy.Allow("", row => row.Id));
        Assert.Throws<ArgumentNullException>(() => policy.Allow<int>("x", null!));
        Assert.Throws<ArgumentNullException>(() => policy.Allow("x", row => row.Id, false, null!));
        Assert.Throws<ArgumentException>(() => policy.Allow("x", row => row.Id, false, (FilterOperator)99));
        Assert.Throws<ArgumentException>(() => policy.Allow("id", row => row.Id));
        Query<Row> query = new();
        Assert.Throws<ArgumentNullException>(() => query.Where(null!));
        Assert.Throws<ArgumentNullException>(() => query.OrderBy<int>(null!));
        Assert.Throws<ArgumentException>(() => query.Include(""));
        Assert.Throws<ArgumentNullException>(() => query.ApplyFilters(null!));
        Assert.Throws<ArgumentNullException>(() => query.ApplyOrdering(null!, row => row.Id));
        Assert.Throws<ArgumentNullException>(() => query.ApplyOrdering<int>(Array.Empty<Row>().AsQueryable(), null!));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<QueryRequest>("{\"IgnoreQueryFilters\":true}"));
    }
}
