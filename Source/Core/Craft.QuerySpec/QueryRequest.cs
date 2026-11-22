using System.Text.Json;
using System.Text.Json.Serialization;

namespace Craft.QuerySpec;

public enum FilterOperator
{
    Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual,
    Contains, StartsWith, EndsWith, In, NotIn, IsNull, IsNotNull
}

public enum FilterLogic { And, Or }

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record QueryFilter(string Field, FilterOperator Operator, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] JsonElement Value = default);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record QuerySort(string Field, bool Descending = false);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FilterGroup
{
    public FilterLogic Logic { get; init; }

    public IReadOnlyList<QueryFilter> Filters { get; init; } = [];

    public IReadOnlyList<FilterGroup> Groups { get; init; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record QueryRequest
{
    public FilterGroup? Filter { get; init; }

    public IReadOnlyList<QuerySort> Sort { get; init; } = [];

    public long Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool IncludeDetails { get; init; }
}

public sealed record QueryLimits
{
    public int MaximumDepth { get; init; } = 5;

    public int MaximumNodes { get; init; } = 50;

    public int MaximumSortFields { get; init; } = 5;

    public int MaximumValues { get; init; } = 100;

    public int MaximumValueBytes { get; init; } = 4096;

    public int MaximumPageSize { get; init; } = 200;
}
