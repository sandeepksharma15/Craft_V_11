using System.Linq.Expressions;

namespace Craft.Expressions.Tests;

public class PredicateCompositionExtensionsTests
{
    [Fact]
    public void And_ReturnsCombinedExpression()
    {
        Expression<Func<MyClass, bool>> expr1 = x => x.AnotherProperty > 10;
        Expression<Func<MyClass, bool>> expr2 = x => x.PropertyName == "Test";

        var andExpression = expr1.And(expr2);
        var compiled = andExpression.Compile();
        var obj1 = new MyClass { AnotherProperty = 20, PropertyName = "Test" };
        var obj2 = new MyClass { AnotherProperty = 5, PropertyName = "Test" };

        Assert.NotNull(andExpression);
        Assert.True(compiled(obj1));
        Assert.False(compiled(obj2));
        Assert.DoesNotContain("Invoke", andExpression.Body.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Or_ReturnsCombinedExpression()
    {
        Expression<Func<MyClass, bool>> expr1 = x => x.AnotherProperty > 10;
        Expression<Func<MyClass, bool>> expr2 = x => x.PropertyName == "Test";

        var orExpression = expr1.Or(expr2);
        var compiled = orExpression.Compile();
        var obj1 = new MyClass { AnotherProperty = 20, PropertyName = "No" };
        var obj2 = new MyClass { AnotherProperty = 5, PropertyName = "Test" };
        var obj3 = new MyClass { AnotherProperty = 5, PropertyName = "No" };

        Assert.NotNull(orExpression);
        Assert.True(compiled(obj1));
        Assert.True(compiled(obj2));
        Assert.False(compiled(obj3));
        Assert.DoesNotContain("Invoke", orExpression.Body.ToString(), StringComparison.Ordinal);
    }

    private sealed class MyClass
    {
        public int AnotherProperty { get; set; }
        public string? PropertyName { get; set; }
    }
}
