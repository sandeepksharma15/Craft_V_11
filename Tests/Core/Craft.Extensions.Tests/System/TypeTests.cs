using System.Reflection;

namespace Craft.Extensions.Tests.System;

public class TypeTests
{
    [Theory]
    [InlineData(typeof(BaseClass), true)]
    [InlineData(typeof(DerivedClass), false)]
    [InlineData(typeof(IInterface), false)]
    public void HasAttribute_ShouldReturnExpectedResult(Type type, bool expected)
        => Assert.Equal(expected, type.HasAttribute<TestAttribute>());

    [Fact]
    public void HasAttribute_NullType_ShouldReturnFalse()
    {
        Type? type = null;
        Assert.False(type.HasAttribute<TestAttribute>());
    }

    [Theory]
    [InlineData(typeof(int), typeof(int))]
    [InlineData(typeof(int?), typeof(int))]
    [InlineData(typeof(string), typeof(string))]
    [InlineData(typeof(double?), typeof(double))]
    [InlineData(null, null)]
    public void GetNonNullableType_ShouldReturnExpectedType(Type? type, Type? expected)
        => Assert.Equal(expected, type.GetNonNullableType());

    [Theory]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(string), false)]
    [InlineData(null, false)]
    public void IsNullable_ShouldReturnExpectedResult(Type? type, bool expected)
        => Assert.Equal(expected, type.IsNullable());

    [Theory]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(double?), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(bool), false)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(null, false)]
    public void IsNumeric_ShouldReturnExpectedResult(Type? type, bool expected)
        => Assert.Equal(expected, type.IsNumeric());

    [Theory]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(ulong?), true)]
    [InlineData(typeof(float), false)]
    [InlineData(typeof(decimal?), false)]
    [InlineData(typeof(string), false)]
    [InlineData(null, false)]
    public void IsIntegral_ShouldReturnExpectedResult(Type? type, bool expected)
        => Assert.Equal(expected, type.IsIntegral());

    [Theory]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(double?), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(int?), false)]
    [InlineData(typeof(string), false)]
    [InlineData(null, false)]
    public void IsFloating_ShouldReturnExpectedResult(Type? type, bool expected)
        => Assert.Equal(expected, type.IsFloating());

    [Theory]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(DateTime?), true)]
    [InlineData(typeof(int), false)]
    [InlineData(null, false)]
    public void IsDateTime_ShouldReturnExpectedResult(Type? type, bool expected)
        => Assert.Equal(expected, type.IsDateTime());

    [Theory]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(bool?), true)]
    [InlineData(typeof(int), false)]
    [InlineData(null, false)]
    public void IsBoolean_ShouldReturnExpectedResult(Type? type, bool expected)
        => Assert.Equal(expected, type.IsBoolean());

    [Theory]
    [InlineData(typeof(SimpleEnum), true)]
    [InlineData(typeof(SimpleEnum?), true)]
    [InlineData(typeof(int), false)]
    [InlineData(null, false)]
    public void IsEnumType_ShouldReturnExpectedResult(Type? type, bool expected)
        => Assert.Equal(expected, type.IsEnumType());

    [Theory]
    [InlineData(typeof(BaseClass), typeof(DerivedClass), true)]
    [InlineData(typeof(BaseClass), typeof(BaseClass), true)]
    [InlineData(typeof(DerivedClass), typeof(BaseClass), false)]
    [InlineData(typeof(IInterface), typeof(DerivedClass), true)]
    public void IsDerivedFromClass_ShouldReturnExpectedResult(Type baseType, Type derivedType, bool expected)
        => Assert.Equal(expected, derivedType.IsDerivedFromClass(baseType));

    [Fact]
    public void IsDerivedFromClass_NullArgument_ShouldThrow()
    {
        Type? derived = null;
        Assert.Throws<ArgumentNullException>(() => derived.IsDerivedFromClass(typeof(BaseClass)));
    }

    [Theory]
    [InlineData(typeof(DerivedClass), typeof(IInterface), true)]
    [InlineData(typeof(DerivedClass), typeof(INonGenericInterface), false)]
    [InlineData(typeof(DerivedClass), typeof(BaseClass), false)]
    [InlineData(null, typeof(IInterface), false)]
    [InlineData(typeof(DerivedClass), null, false)]
    public void HasImplementedInterface_ShouldReturnExpectedResult(Type? type, Type? interfaceType, bool expected)
        => Assert.Equal(expected, type.HasImplementedInterface(interfaceType));

    [Theory]
    [InlineData(typeof(BaseClass), typeof(DerivedClass), true)]
    [InlineData(typeof(DerivedClass), typeof(BaseClass), true)]
    [InlineData(typeof(BaseClass), typeof(BaseClass), true)]
    [InlineData(typeof(string), typeof(int), false)]
    public void IsCompatibleWith_ShouldReturnExpectedResult(Type type, Type otherType, bool expected)
        => Assert.Equal(expected, type.IsCompatibleWith(otherType));

    [Theory]
    [InlineData(typeof(BaseClass), typeof(DerivedClass), false)]
    [InlineData(typeof(string), typeof(int), true)]
    public void IsNotCompatibleWith_ShouldReturnExpectedResult(Type type, Type otherType, bool expected)
        => Assert.Equal(expected, type.IsNotCompatibleWith(otherType));

    [Theory]
    [InlineData(typeof(BaseClass), "TypeTests+BaseClass")]
    [InlineData(typeof(DerivedClass), "TypeTests+DerivedClass")]
    [InlineData(typeof(int), "Int32")]
    [InlineData(typeof(string), "String")]
    public void GetClassName_ShouldReturnExpectedResult(Type type, string expected)
        => Assert.Equal(expected, type.GetClassName());

    [Fact]
    public void GetClassName_NullType_ShouldReturnNull()
    {
        Type? type = null;
        Assert.Null(type.GetClassName());
    }

    [Fact]
    public void GetMemberUnderlyingType_ShouldReturnFieldType()
    {
        var member = typeof(MyTestClass).GetField("_myField", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(typeof(int), member.GetMemberUnderlyingType());
    }

    [Fact]
    public void GetMemberUnderlyingType_ShouldReturnPropertyType()
    {
        var member = typeof(MyTestClass).GetProperty(nameof(MyTestClass.MyProperty));
        Assert.Equal(typeof(string), member.GetMemberUnderlyingType());
    }

    [Fact]
    public void GetMemberUnderlyingType_ShouldReturnEventType()
    {
        var member = typeof(MyTestClass).GetEvent(nameof(MyTestClass.MyEvent));
        Assert.Equal(typeof(EventHandler), member.GetMemberUnderlyingType());
    }

    [Fact]
    public void GetMemberUnderlyingType_Null_ShouldReturnNull()
    {
        MemberInfo? member = null;
        Assert.Null(member.GetMemberUnderlyingType());
    }

    [Fact]
    public void GetMemberUnderlyingType_UnsupportedMember_ShouldThrow()
        => Assert.Throws<ArgumentException>(() => typeof(MyTestClass).GetMethod(nameof(MyTestClass.Method)).GetMemberUnderlyingType());

    [Fact]
    public void GetClassesWithAttribute_ShouldReturnMatchingClasses()
    {
        var result = typeof(BaseClass).GetClassesWithAttribute<TestAttribute>();

        Assert.Contains(nameof(ClassA), result);
        Assert.Contains(nameof(DerivedClass), result);
        Assert.DoesNotContain(nameof(ClassB), result);
    }

    [Fact]
    public void GetClassesWithoutAttribute_ShouldReturnMatchingClasses()
    {
        var result = typeof(BaseClass).GetClassesWithoutAttribute<AnotherAttribute>();

        Assert.Contains(nameof(BaseClass), result);
        Assert.Contains(nameof(DerivedClass), result);
        Assert.Contains(nameof(ClassA), result);
        Assert.DoesNotContain(nameof(ClassB), result);
    }

    [Fact]
    public void GetInheritedClasses_ShouldReturnConcreteDerivedClasses()
    {
        var result = typeof(BaseClass).GetInheritedClasses();

        Assert.Contains(nameof(DerivedClass), result);
        Assert.Contains(nameof(ClassA), result);
        Assert.Contains(nameof(ClassB), result);
        Assert.DoesNotContain(nameof(BaseClass), result);
    }

    [Fact]
    public void TypeDiscovery_NullType_ShouldReturnEmptyLists()
    {
        Type? type = null;

        Assert.Empty(type.GetClassesWithAttribute<TestAttribute>());
        Assert.Empty(type.GetClassesWithoutAttribute<TestAttribute>());
        Assert.Empty(type.GetInheritedClasses());
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    private sealed class TestAttribute : Attribute;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    private sealed class AnotherAttribute : Attribute;

    private class BaseClass;

    [Test]
    private class DerivedClass : BaseClass, IInterface;

    [Test]
    private class ClassA : BaseClass;

    [Another]
    private class ClassB : BaseClass;

    private interface IInterface;

    private interface INonGenericInterface;

    private enum SimpleEnum
    {
        None,
        Value
    }

#pragma warning disable CS0169, CS0067
    private class MyTestClass
    {
        private readonly int _myField;
        public string? MyProperty { get; set; }
        public event EventHandler? MyEvent;
        public void Method() { }
    }
#pragma warning restore CS0169, CS0067
}
