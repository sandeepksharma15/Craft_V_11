using System.Linq.Expressions;
using Craft.Expressions.Extensions;
using Craft.Expressions.Tests.Fixtures;

namespace Craft.Expressions.Tests.Extensions;

public class PredicateCompositionExtensionsTests
{
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
}
