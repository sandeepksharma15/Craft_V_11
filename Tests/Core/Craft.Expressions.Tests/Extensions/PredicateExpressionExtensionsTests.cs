using System.Linq.Expressions;
using Craft.Expressions.Extensions;

namespace Craft.Expressions.Tests.Extensions;

public class PredicateExpressionExtensionsTests
{
    [Fact]
    public void RemoveCondition_WithDifferentCondition_ReturnsOriginalPredicate()
    {
        Expression<Func<int, bool>> original = x => x > 5;
        Expression<Func<int, bool>> condition = value => value < 10;

        Expression<Func<int, bool>>? result = original.RemoveCondition(condition);

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

        Expression<Func<int, bool>>? result = original.RemoveCondition(condition);

        Assert.NotNull(result);
        Assert.True(result!.Compile()(6));
        Assert.False(result.Compile()(4));
        Assert.True(result.IsSemanticallyEquivalentTo((Expression<Func<int, bool>>)(x => x > 5)));
    }

    [Fact]
    public void RemoveCondition_WhenLeftConditionMatches_VisitsRightBranchRecursively()
    {
        Expression<Func<int, bool>> original = x => x > 5 && (x > 5 && x < 10);
        Expression<Func<int, bool>> condition = value => value > 5;

        Expression<Func<int, bool>>? result = original.RemoveCondition(condition);

        Assert.NotNull(result);
        Assert.True(result!.IsSemanticallyEquivalentTo((Expression<Func<int, bool>>)(x => x < 10)));
    }

    [Fact]
    public void RemoveConditions_RemovesMultipleConditions()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10 && x != 7;

        Expression<Func<int, bool>>? result = original.RemoveConditions(value => value < 10, number => number != 7);

        Assert.NotNull(result);
        Assert.True(result!.Compile()(6));
        Assert.False(result.Compile()(5));
        Assert.True(result.IsSemanticallyEquivalentTo((Expression<Func<int, bool>>)(x => x > 5)));
    }

    [Fact]
    public void RemoveConditions_WithNullOrEmptyConditions_ReturnsOriginalPredicate()
    {
        Expression<Func<int, bool>> original = x => x > 5;
        Expression<Func<int, bool>>[]? nullConditions = null;

        Expression<Func<int, bool>>? empty = original.RemoveConditions();
        Expression<Func<int, bool>>? nullArray = original.RemoveConditions(nullConditions!);

        Assert.Same(original, empty);
        Assert.Same(original, nullArray);
    }

    [Fact]
    public void RemoveConditions_WithEmptyArray_ReturnsOriginalPredicate()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        Expression<Func<int, bool>>? result = original.RemoveConditions([]);

        Assert.Same(original, result);
    }

    [Fact]
    public void RemoveConditions_WithNullArray_ReturnsOriginalPredicate()
    {
        Expression<Func<int, bool>> original = x => x > 5;
        Expression<Func<int, bool>>[]? conditions = null;

        Expression<Func<int, bool>>? result = original.RemoveConditions(conditions!);

        Assert.Same(original, result);
    }

    [Fact]
    public void RemoveConditions_WithNonEmptyNonMatchingConditions_ReturnsEquivalentPredicate()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        Expression<Func<int, bool>>? result = original.RemoveConditions(value => value < 10);

        Assert.NotNull(result);
        Assert.NotSame(original, result);
        Assert.True(result!.IsSemanticallyEquivalentTo(original));
    }

    [Fact]
    public void RemoveConditions_WithEquivalentSingleCondition_ReturnsNull()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        Expression<Func<int, bool>>? result = original.RemoveConditions(value => value > 5);

        Assert.Null(result);
    }

    [Fact]
    public void RemoveConditions_WithEquivalentFirstCondition_ReturnsNull()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        Expression<Func<int, bool>>? result = original.RemoveConditions(number => number > 5, number => number < 100);

        Assert.Null(result);
    }

    [Fact]
    public void RemoveConditions_WhenSubsequentConditionMatchesUpdatedBody_ReturnsNull()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10;

        Expression<Func<int, bool>>? result = original.RemoveConditions(value => value < 10, number => number > 5);

        Assert.Null(result);
    }

    [Fact]
    public void RemoveConditions_WithNullConditionItem_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        Assert.Throws<ArgumentNullException>(() => original.RemoveConditions((Expression<Func<int, bool>>)null!));
    }

    [Fact]
    public void ReplaceCondition_ReplacesMatchingConditionAndCompiles()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10;
        Expression<Func<int, bool>> oldCondition = value => value < 10;
        Expression<Func<int, bool>> newCondition = candidate => candidate < 20;

        Expression<Func<int, bool>> result = original.ReplaceCondition(oldCondition, newCondition);
        Func<int, bool> compiled = result.Compile();

        Assert.True(compiled(15));
        Assert.False(compiled(25));
        Assert.True(compiled(6));
    }

    [Fact]
    public void ReplaceCondition_DoesNothingWhenConditionIsNotPresent()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10;

        Expression<Func<int, bool>> result = original.ReplaceCondition(value => value != 0, candidate => candidate < 20);

        Assert.True(result.IsSemanticallyEquivalentTo(original));
    }

    [Fact]
    public void ReplaceCondition_ReplacesMatchingLeftCondition()
    {
        Expression<Func<int, bool>> original = x => x > 5 && x < 10;

        Expression<Func<int, bool>> result = original.ReplaceCondition(value => value > 5, candidate => candidate > 7);

        Assert.True(result.IsSemanticallyEquivalentTo((Expression<Func<int, bool>>)(x => x > 7 && x < 10)));
    }

    [Fact]
    public void ReplaceCondition_WhenLeftConditionMatches_VisitsRightBranchRecursively()
    {
        Expression<Func<int, bool>> original = x => x > 5 && (x > 5 && x < 10);

        Expression<Func<int, bool>> result = original.ReplaceCondition(value => value > 5, candidate => candidate > 7);

        Assert.True(result.IsSemanticallyEquivalentTo((Expression<Func<int, bool>>)(x => x > 7 && (x > 7 && x < 10))));
    }

    [Fact]
    public void ReplaceCondition_WithNullExpression_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>>? original = null;

        Assert.Throws<ArgumentNullException>(() => original!.ReplaceCondition(value => value > 5, candidate => candidate < 20));
    }

    [Fact]
    public void ReplaceCondition_WithNullOldCondition_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        Assert.Throws<ArgumentNullException>(() => original.ReplaceCondition(null!, candidate => candidate < 20));
    }

    [Fact]
    public void ReplaceCondition_WithNullNewCondition_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>> original = x => x > 5;

        Assert.Throws<ArgumentNullException>(() => original.ReplaceCondition(value => value > 5, null!));
    }

    [Fact]
    public void IsSemanticallyEquivalentTo_ReturnsTrueForEquivalentBooleanExpressions()
    {
        Expression<Func<TestEntity, bool>> left = entity => entity.IsActive;
        Expression<Func<TestEntity, bool>> right = item => item.IsActive == true;

        Assert.True(left.IsSemanticallyEquivalentTo(right));
    }

    [Fact]
    public void IsSemanticallyEquivalentTo_ReturnsTrueForEquivalentExpressionsWithDifferentParameterNames()
    {
        Expression<Func<TestEntity, bool>> left = entity => entity.Id == 5 && entity.IsActive;
        Expression<Func<TestEntity, bool>> right = record => record.IsActive && record.Id == 5;

        Assert.True(left.IsSemanticallyEquivalentTo(right));
    }

    [Fact]
    public void IsSemanticallyEquivalentTo_ReturnsFalseForDifferentExpressions()
    {
        Expression<Func<TestEntity, bool>> left = entity => entity.IsActive;
        Expression<Func<TestEntity, bool>> right = entity => entity.IsDeleted;

        Assert.False(left.IsSemanticallyEquivalentTo(right));
    }

}
