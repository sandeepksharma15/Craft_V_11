using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Craft.Core.Results;

namespace Craft.QuerySpec;

/// <summary>Configure allowed aliases once before sharing a policy between requests.</summary>
public sealed class QueryPolicy<T> where T : class
{
    #region Fields

    private readonly Dictionary<string, Field> _fields = new(StringComparer.Ordinal);
    private readonly QueryLimits _limits;

    #endregion

    #region Constructors

    public QueryPolicy(QueryLimits? limits = null)
    {
        _limits = limits ?? new QueryLimits();
        if (_limits.MaximumDepth < 1 || _limits.MaximumDepth > 32 || _limits.MaximumNodes < 1
            || _limits.MaximumSortFields < 1 || _limits.MaximumValues < 1
            || _limits.MaximumValueBytes < 1 || _limits.MaximumPageSize < 1)
            throw new ArgumentException("Query limits must be positive; depth cannot exceed 32.", nameof(limits));
    }

    #endregion

    #region Methods

    public QueryPolicy<T> Allow<TValue>(string alias, Expression<Func<T, TValue>> selector,
        bool sortable = false, params FilterOperator[] operators)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(operators);
        if (operators.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Unknown filter operator.", nameof(operators));

        _fields.Add(alias, new Field<TValue>(selector, sortable, operators.ToHashSet()));
        return this;
    }

    public Result<Query<T>> Build(QueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            if (request.Page < 1 || request.PageSize < 1 || request.PageSize > _limits.MaximumPageSize
                || request.Page - 1 > int.MaxValue / request.PageSize)
                throw Invalid("Invalid page or page size.");
            if (request.Sort is null || request.Sort.Count > _limits.MaximumSortFields)
                throw Invalid("Invalid sort count.");

            Query<T> query = new();
            int remaining = _limits.MaximumNodes;
            if (request.Filter is not null)
                query = query.Where(BuildGroup(request.Filter, 1, ref remaining));

            HashSet<string> sorted = new(StringComparer.Ordinal);
            foreach (QuerySort sort in request.Sort)
            {
                if (sort is null || sort.Field is null || !sorted.Add(sort.Field)
                    || !_fields.TryGetValue(sort.Field, out Field? field) || !field.Sortable)
                    throw Invalid("Unknown, duplicate or disallowed sort field.");
                query = field.Sort(query, sort.Descending);
            }
            return Result<Query<T>>.Success(query.WithDetails(request.IncludeDetails));
        }
        catch (QueryValidationException error)
        {
            return Result<Query<T>>.Failure(new Error("query.invalid", error.Message, ErrorKind.Validation));
        }
    }

    private Expression<Func<T, bool>> BuildGroup(FilterGroup group, int depth, ref int remaining)
    {
        if (group is null || depth > _limits.MaximumDepth || --remaining < 0 || !Enum.IsDefined(group.Logic)
            || group.Filters is null || group.Groups is null
            || (long)group.Filters.Count + group.Groups.Count > remaining)
            throw Invalid("Invalid filter structure or complexity limit exceeded.");

        ParameterExpression parameter = Expression.Parameter(typeof(T), "entity");
        Expression body = Expression.Constant(group.Logic == FilterLogic.And);
        foreach (QueryFilter filter in group.Filters)
        {
            remaining--;
            if (filter is null || filter.Field is null || !_fields.TryGetValue(filter.Field, out Field? field)
                || !field.Operators.Contains(filter.Operator))
                throw Invalid("Unknown or disallowed filter field/operator.");
            if (filter.Value.ValueKind != JsonValueKind.Undefined
                && Encoding.UTF8.GetByteCount(filter.Value.GetRawText()) > _limits.MaximumValueBytes)
                throw Invalid("Filter value is too large.");

            body = Combine(body, field.Filter(filter, _limits.MaximumValues), parameter, group.Logic);
        }
        foreach (FilterGroup child in group.Groups)
            body = Combine(body, BuildGroup(child, depth + 1, ref remaining), parameter, group.Logic);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression Combine(Expression body, Expression<Func<T, bool>> predicate,
        ParameterExpression parameter, FilterLogic logic)
    {
        Expression right = new ReplaceParameter(predicate.Parameters[0], parameter).Visit(predicate.Body);
        return logic == FilterLogic.And ? Expression.AndAlso(body, right) : Expression.OrElse(body, right);
    }

    private static QueryValidationException Invalid(string message) => new(message);

    #endregion

    #region Nested Types

    private sealed class QueryValidationException(string message) : Exception(message);

    private sealed class ReplaceParameter(ParameterExpression original, ParameterExpression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == original ? replacement : node;
    }

    private abstract class Field(bool sortable, HashSet<FilterOperator> operators)
    {
        public bool Sortable { get; } = sortable;
        public HashSet<FilterOperator> Operators { get; } = operators;
        public abstract Query<T> Sort(Query<T> query, bool descending);
        public abstract Expression<Func<T, bool>> Filter(QueryFilter filter, int maximumValues);
    }

    private sealed class Field<TValue>(Expression<Func<T, TValue>> selector, bool sortable,
        HashSet<FilterOperator> operators) : Field(sortable, operators)
    {
        public override Query<T> Sort(Query<T> query, bool descending) => query.OrderBy(selector, descending);

        public override Expression<Func<T, bool>> Filter(QueryFilter filter, int maximumValues)
        {
            try
            {
                Expression member = selector.Body;
                Expression body;
                if (filter.Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
                {
                    if (typeof(TValue).IsValueType && Nullable.GetUnderlyingType(typeof(TValue)) is null)
                        throw Invalid("Null checks require a nullable field.");
                    Expression empty = Expression.Constant(null, typeof(TValue));
                    body = filter.Operator == FilterOperator.IsNull ? Expression.Equal(member, empty) : Expression.NotEqual(member, empty);
                }
                else if (filter.Operator is FilterOperator.In or FilterOperator.NotIn)
                {
                    if (filter.Value.ValueKind != JsonValueKind.Array || filter.Value.GetArrayLength() > maximumValues)
                        throw Invalid("Membership values must be a bounded array.");
                    TValue[] values = filter.Value.Deserialize<TValue[]>()!;
                    body = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(TValue)], Expression.Constant(values), member);
                    if (filter.Operator == FilterOperator.NotIn)
                        body = Expression.Not(body);
                }
                else
                {
                    TValue? value = filter.Value.Deserialize<TValue>();
                    Expression constant = Expression.Constant(value, typeof(TValue));
                    body = filter.Operator switch
                    {
                        FilterOperator.Equal => Expression.Equal(member, constant),
                        FilterOperator.NotEqual => Expression.NotEqual(member, constant),
                        FilterOperator.GreaterThan => Expression.GreaterThan(member, constant),
                        FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, constant),
                        FilterOperator.LessThan => Expression.LessThan(member, constant),
                        FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(member, constant),
                        _ => StringMatch(member, constant, filter.Operator)
                    };
                }
                return Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
            }
            catch (Exception error) when (error is JsonException or InvalidOperationException or ArgumentException or NotSupportedException)
            {
                throw Invalid("The value or operator is incompatible with the field.");
            }
        }

        private static Expression StringMatch(Expression member, Expression value, FilterOperator operation)
        {
            if (typeof(TValue) != typeof(string) || value is ConstantExpression { Value: null })
                throw Invalid("Text matching requires a string field and non-null value.");
            string method = operation switch
            {
                FilterOperator.Contains => nameof(string.Contains),
                FilterOperator.StartsWith => nameof(string.StartsWith),
                _ => nameof(string.EndsWith)
            };
            return Expression.AndAlso(Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
                Expression.Call(member, method, Type.EmptyTypes, value));
        }
    }

    #endregion
}
