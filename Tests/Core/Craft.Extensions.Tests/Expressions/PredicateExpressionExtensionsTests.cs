using System.Linq.Expressions;
using Craft.Extensions.Expressions;

namespace Craft.Extensions.Tests.Expressions;

public class PredicateExpressionExtensionsTests
{
    [Fact]
    public void RemoveCondition_WithDifferentCondition_ReturnsOriginalPredicate()
    {
        Expression<Func<int, bool>> original = x => x > 5;
        Expression<Func<int, bool>> condition = value => value < 10;

        var result = original.RemoveCondition(condition);

        Assert.NotNull(result);
        Assert.Equal(original.Compile()(6), result!.Compile()(6));
        Assert.Equal(original.Compile()(4), result.Compile()(4));
    }

    [Fact]
    public void RemoveCondition_WithEquivalentCondition_ReturnsNull()
    {
        Expression<Func<int, bool>> original = x => x > 5;
        Expression<Func<int, bool>> condition = value => value > 5;

        Assert.Null(original.RemoveCondition(condition));
    }

    [Fact]
    public void RemoveCondition_RemovesMatchingConditionEvenWhenParameterNamesDiffer()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10;
        Expression<Func<int, bool>> condition = value => value < 10;

        var result = original.RemoveCondition(condition);

        Assert.NotNull(result);
        Assert.True(result!.Compile()(6));
        Assert.False(result.Compile()(4));
        Assert.True(result.IsSemanticallyEquivalentTo((Expression<Func<int, bool>>)(x => x > 5)));
    }

    [Fact]
    public void RemoveConditions_RemovesMultipleConditions()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10 && x != 7;

        var result = original.RemoveConditions(value => value < 10, number => number != 7);

        Assert.NotNull(result);
        Assert.True(result!.Compile()(6));
        Assert.False(result.Compile()(5));
        Assert.True(result.IsSemanticallyEquivalentTo((Expression<Func<int, bool>>)(x => x > 5)));
    }

    [Fact]
    public void RemoveConditions_WithNullOrEmptyConditions_ReturnsOriginalPredicate()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        var empty = original.RemoveConditions();
        var nullArray = original.RemoveConditions(null!);

        Assert.Same(original, empty);
        Assert.Same(original, nullArray);
    }

    [Fact]
    public void ReplaceCondition_ReplacesMatchingConditionAndCompiles()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10;
        Expression<Func<int, bool>> oldCondition = value => value < 10;
        Expression<Func<int, bool>> newCondition = candidate => candidate < 20;

        var result = original.ReplaceCondition(oldCondition, newCondition);
        var compiled = result.Compile();

        Assert.True(compiled(15));
        Assert.False(compiled(25));
        Assert.True(compiled(6));
    }

    [Fact]
    public void ReplaceCondition_DoesNothingWhenConditionIsNotPresent()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10;

        var result = original.ReplaceCondition(value => value != 0, candidate => candidate < 20);

        Assert.True(result.IsSemanticallyEquivalentTo(original));
    }

    [Fact]
    public void IsSemanticallyEquivalentTo_ReturnsTrueForEquivalentBooleanExpressions()
    {
        Expression<Func<Entity, bool>> left = entity => entity.IsActive;
        Expression<Func<Entity, bool>> right = item => item.IsActive == true;

        Assert.True(left.IsSemanticallyEquivalentTo(right));
    }

    [Fact]
    public void IsSemanticallyEquivalentTo_ReturnsTrueForEquivalentExpressionsWithDifferentParameterNames()
    {
        Expression<Func<Entity, bool>> left = entity => entity.Id == 5 && entity.IsActive;
        Expression<Func<Entity, bool>> right = record => record.IsActive && record.Id == 5;

        Assert.True(left.IsSemanticallyEquivalentTo(right));
    }

    [Fact]
    public void IsSemanticallyEquivalentTo_ReturnsFalseForDifferentExpressions()
    {
        Expression<Func<Entity, bool>> left = entity => entity.IsActive;
        Expression<Func<Entity, bool>> right = entity => entity.IsDeleted;

        Assert.False(left.IsSemanticallyEquivalentTo(right));
    }

    private sealed class Entity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
