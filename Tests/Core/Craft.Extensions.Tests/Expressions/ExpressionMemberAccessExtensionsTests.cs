using System.Linq.Expressions;
using Craft.Extensions.Expressions;

namespace Craft.Extensions.Tests.Expressions;

public class ExpressionMemberAccessExtensionsTests
{
    [Fact]
    public void CreateMemberExpression_NonexistentProperty_ShouldThrowArgumentException()
    {
        const string propertyName = "NonexistentProperty";

        Assert.Throws<ArgumentException>(() => propertyName.CreateMemberExpression<MyClass>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateMemberExpression_NullOrEmptyPropertyName_ShouldThrowArgumentException(string? propertyName)
    {
        if (propertyName is null)
            Assert.Throws<ArgumentNullException>(() => propertyName!.CreateMemberExpression<MyClass>());
        else
            Assert.Throws<ArgumentException>(() => propertyName!.CreateMemberExpression<MyClass>());
    }

    [Fact]
    public void CreateMemberExpression_ValidProperty_ReturnsExpression()
    {
        var obj = new MyClass { PropertyName = "Test", AnotherProperty = 42 };
        const string propertyName = "PropertyName";

        var expression = propertyName.CreateMemberExpression<MyClass>();
        var compiled = expression.Compile();
        var value = compiled.DynamicInvoke(obj);

        Assert.NotNull(expression);
        Assert.Equal("Test", value);
    }

    [Fact]
    public void CreateMemberExpression_ValidField_ReturnsExpression()
    {
        var type = typeof(MyClass);
        var fieldName = nameof(MyClass.StaticField);
        MyClass.StaticField = 123;

        var expression = type.CreateMemberExpression(fieldName);
        var compiled = expression.Compile();
        var result = compiled.DynamicInvoke();

        Assert.NotNull(expression);
        Assert.Equal(123, result);
    }

    [Fact]
    public void CreateMemberExpression_PrivateField_ReturnsExpression()
    {
        var type = typeof(MyClass);
        var expression = type.CreateMemberExpression("_privateField");
        var obj = new MyClass();
        obj.SetPrivateField(77);

        var compiled = expression.Compile();
        var value = compiled.DynamicInvoke(obj);

        Assert.NotNull(expression);
        Assert.Equal(77, value);
    }

    [Fact]
    public void CreateMemberExpression_WithInvalidProperty_ShouldThrowArgumentException()
    {
        var type = typeof(MyClass);

        Assert.Throws<ArgumentException>(() => type.CreateMemberExpression("InvalidProperty"));
    }

    [Fact]
    public void CreateMemberExpression_WithNullType_ShouldThrowArgumentNullException()
    {
        Type type = null!;

        Assert.Throws<ArgumentNullException>(() => type.CreateMemberExpression("PropertyName"));
    }

    [Theory]
    [InlineData("PropertyName")]
    [InlineData("AnotherProperty")]
    public void CreateMemberExpression_ValidProperty_ShouldNotThrowException(string propertyName)
    {
        var exception = Record.Exception(() => propertyName.CreateMemberExpression<MyClass>());

        Assert.Null(exception);
    }

    [Fact]
    public void CreateMemberExpression_StronglyTypedProperty_ReturnsExpression()
    {
        var obj = new MyClass { AnotherProperty = 99 };
        var expression = "AnotherProperty".CreateMemberExpression<MyClass, int>();
        var compiled = expression.Compile();

        Assert.NotNull(expression);
        Assert.Equal(99, compiled(obj));
    }

    [Fact]
    public void CreateMemberExpression_NullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => ((string?)null)!.CreateMemberExpression<MyClass, string>());
        Assert.Throws<ArgumentException>(() => "".CreateMemberExpression<MyClass, string>());
        Assert.Throws<ArgumentException>(() => "   ".CreateMemberExpression<MyClass, string>());
    }

    [Fact]
    public void CreateStaticMemberExpression_ValidStaticField_ReturnsCorrectExpression()
    {
        MyClass.StaticField = 42;

        var expression = typeof(MyClass).CreateStaticMemberExpression<int>("StaticField");
        var result = expression.Compile()();

        Assert.Equal(42, result);
    }

    [Fact]
    public void CreateStaticMemberExpression_InvalidMemberName_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            typeof(MyClass).CreateStaticMemberExpression<int>("NonExistentField"));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void CreateStaticMemberExpression_NullMemberName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => typeof(MyClass).CreateStaticMemberExpression<int>(null!));
    }

    [Fact]
    public void CreateStaticMemberExpression_InstanceField_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => typeof(MyClass).CreateStaticMemberExpression<int>("AnotherProperty"));
    }

    [Fact]
    public void CreateStaticMemberExpression_WrongResultType_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            typeof(MyClass).CreateStaticMemberExpression<string>("StaticField"));

        Assert.Contains("cannot be assigned", exception.Message);
    }

    [Fact]
    public void CreateMemberExpression_NonExistentMember_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            "NonExistent".CreateMemberExpression<MyClass, string>());

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void CreateMemberExpression_StronglyTyped_InvalidProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() => "NotExist".CreateMemberExpression<MyClass, int>());
    }

    [Fact]
    public void CreateMemberExpression_PrivateField_ReturnsCorrectExpression()
    {
        var expression = "_privateField".CreateMemberExpression<MyClass, int>();
        var func = expression.Compile();
        var instance = new MyClass();
        instance.SetPrivateField(55);

        Assert.NotNull(expression);
        Assert.Equal(55, func(instance));
    }

    [Fact]
    public void CreateMemberExpression_StaticField_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            "StaticField".CreateMemberExpression<MyClass, int>());

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void CreateMemberExpression_TypeMismatch_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            "AnotherProperty".CreateMemberExpression<MyClass, string>());

        Assert.Contains("cannot be assigned", exception.Message);
    }

    private sealed class MyClass
    {
        public int AnotherProperty { get; set; }
        public string? PropertyName { get; set; }
        public static int StaticField;
        private int _privateField;

        public void SetPrivateField(int value) => _privateField = value;
    }
}
