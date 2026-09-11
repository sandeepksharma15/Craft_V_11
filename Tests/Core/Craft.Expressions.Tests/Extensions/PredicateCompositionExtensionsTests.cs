using System.Linq.Expressions;
using Craft.Expressions.Extensions;
using Craft.Expressions.Tests.Fixtures;

namespace Craft.Expressions.Tests.Extensions;

public class PredicateCompositionExtensionsTests
{
    [Fact]
    public void And_WithNullExpression_ThrowsArgumentNullException()
    {
        Expression<Func<TestEntity, bool>> expression = null!;
        Expression<Func<TestEntity, bool>> other = x => x.Name == "Test";

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => expression.And(other));

        Assert.Equal("expression", exception.ParamName);
    }

    [Fact]
    public void And_WithNullOther_ThrowsArgumentNullException()
    {
        Expression<Func<TestEntity, bool>> expression = x => x.Name.Length > 3;
        Expression<Func<TestEntity, bool>> other = null!;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => expression.And(other));

        Assert.Equal("other", exception.ParamName);
    }

    [Fact]
    public void And_ReturnsCombinedExpression()
    {
        Expression<Func<TestEntity, bool>> expr1 = x => x.Name.Length > 3;
        Expression<Func<TestEntity, bool>> expr2 = x => x.Name == "Test";

        var andExpression = expr1.And(expr2);
        var compiled = andExpression.Compile();
        var obj1 = new TestEntity { Name = "Test" };
        var obj2 = new TestEntity { Name = "Hi" };

        Assert.NotNull(andExpression);
        Assert.True(compiled(obj1));
        Assert.False(compiled(obj2));
        Assert.DoesNotContain("Invoke", andExpression.Body.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void And_WithDifferentParameterNames_RebindsToSingleParameterAndUsesAndAlso()
    {
        Expression<Func<TestEntity, bool>> expr1 = entity => entity.Name.StartsWith("T");
        Expression<Func<TestEntity, bool>> expr2 = item => item.Name.EndsWith("t");

        Expression<Func<TestEntity, bool>> andExpression = expr1.And(expr2);
        Func<TestEntity, bool> compiled = andExpression.Compile();

        Assert.Single(andExpression.Parameters);
        Assert.Equal(ExpressionType.AndAlso, andExpression.Body.NodeType);
        Assert.True(compiled(new TestEntity { Name = "Test" }));
        Assert.False(compiled(new TestEntity { Name = "Team" }));
    }

    [Fact]
    public void Or_WithNullExpression_ThrowsArgumentNullException()
    {
        Expression<Func<TestEntity, bool>> expression = null!;
        Expression<Func<TestEntity, bool>> other = x => x.Name == "Test";

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => expression.Or(other));

        Assert.Equal("expression", exception.ParamName);
    }

    [Fact]
    public void Or_WithNullOther_ThrowsArgumentNullException()
    {
        Expression<Func<TestEntity, bool>> expression = x => x.Name.Length > 3;
        Expression<Func<TestEntity, bool>> other = null!;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => expression.Or(other));

        Assert.Equal("other", exception.ParamName);
    }

    [Fact]
    public void Or_ReturnsCombinedExpression()
    {
        Expression<Func<TestEntity, bool>> expr1 = x => x.Name.Length > 3;
        Expression<Func<TestEntity, bool>> expr2 = x => x.Name == "Test";

        var orExpression = expr1.Or(expr2);
        var compiled = orExpression.Compile();
        var obj1 = new TestEntity { Name = "Hello" };
        var obj2 = new TestEntity { Name = "Test" };
        var obj3 = new TestEntity { Name = "No" };

        Assert.NotNull(orExpression);
        Assert.True(compiled(obj1));
        Assert.True(compiled(obj2));
        Assert.False(compiled(obj3));
        Assert.DoesNotContain("Invoke", orExpression.Body.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Or_WithDifferentParameterNames_RebindsToSingleParameterAndUsesOrElse()
    {
        Expression<Func<TestEntity, bool>> expr1 = source => source.Name == "Hi";
        Expression<Func<TestEntity, bool>> expr2 = candidate => candidate.Name.Length > 4;

        Expression<Func<TestEntity, bool>> orExpression = expr1.Or(expr2);
        Func<TestEntity, bool> compiled = orExpression.Compile();

        Assert.Single(orExpression.Parameters);
        Assert.Equal(ExpressionType.OrElse, orExpression.Body.NodeType);
        Assert.True(compiled(new TestEntity { Name = "Hi" }));
        Assert.True(compiled(new TestEntity { Name = "Hello" }));
        Assert.False(compiled(new TestEntity { Name = "No" }));
    }
}
