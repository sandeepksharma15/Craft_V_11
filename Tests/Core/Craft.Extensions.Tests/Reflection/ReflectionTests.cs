using System.Linq.Expressions;
using System.Reflection;

namespace Craft.Extensions.Tests.Reflection;

public class ReflectionExtensionsTests
{
    [Fact]
    public void GetMemberByName_ReturnsDescriptor_ForSimpleProperty()
    {
        var descriptor = typeof(Simple).GetMemberByName("IntProp");

        Assert.NotNull(descriptor);
        Assert.Equal("IntProp", descriptor.Name);
    }

    [Fact]
    public void GetMemberByName_ReturnsDescriptor_ForNestedProperty()
    {
        var descriptor = typeof(Simple).GetMemberByName("Nested.Deep.DoubleProp");

        Assert.NotNull(descriptor);
        Assert.Equal("DoubleProp", descriptor.Name);
    }

    [Fact]
    public void GetMemberByName_ReturnsNull_WhenPropertyNotFound()
    {
        Assert.Null(typeof(Simple).GetMemberByName("NotExist"));
        Assert.Null(typeof(Simple).GetMemberByName("Nested.NotExist"));
        Assert.Null(typeof(Simple).GetMemberByName("NotExist.Value"));
    }

    [Fact]
    public void GetMemberByName_ThrowsOnInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ReflectionExtensions.GetMemberByName(null!, "IntProp"));
        Assert.Throws<ArgumentNullException>(() => typeof(Simple).GetMemberByName(null!));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetMemberByName(""));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetMemberByName("   "));
    }

    [Fact]
    public void GetAllProperties_ReturnsAllInstancePropertiesIncludingBase()
    {
        var properties = typeof(Derived).GetAllProperties();

        Assert.Contains(properties, p => p.Name == "BaseProp");
        Assert.Contains(properties, p => p.Name == "DerivedProp");
        Assert.Contains(properties, p => p.Name == "PrivateBaseProp");
        Assert.DoesNotContain(properties, p => p.Name == "StaticProp");
    }

    [Fact]
    public void GetAllProperties_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => ReflectionExtensions.GetAllProperties(null));
    }

    [Fact]
    public void GetPropertyInfo_ByTypeAndName_ReturnsProperty()
    {
        var property = typeof(Simple).GetPropertyInfo("IntProp");

        Assert.Equal("IntProp", property.Name);
    }

    [Fact]
    public void GetPropertyInfo_ByTypeAndName_FindsNonPublicAndStaticProperties()
    {
        Assert.Equal("PrivateProp", typeof(Simple).GetPropertyInfo("PrivateProp").Name);
        Assert.Equal("StaticProp", typeof(Simple).GetPropertyInfo("StaticProp").Name);
    }

    [Fact]
    public void GetPropertyInfo_ByTypeAndName_ThrowsWhenNotFoundOrInvalid()
    {
        Assert.Throws<ArgumentNullException>(() => ReflectionExtensions.GetPropertyInfo(null!, "IntProp"));
        Assert.Throws<ArgumentNullException>(() => typeof(Simple).GetPropertyInfo(null!));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetPropertyInfo(""));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetPropertyInfo("   "));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetPropertyInfo("NotExist"));
    }

    [Fact]
    public void GetPropertyInfo_FromExpression_ReturnsProperty()
    {
        Expression<Func<Simple, int>> expression = s => s.IntProp;

        Assert.Equal("IntProp", expression.GetPropertyInfo().Name);
    }

    [Fact]
    public void GetPropertyInfo_FromObjectExpression_ReturnsProperty()
    {
        Expression<Func<Simple, object>> expression = s => s.StringProp!;

        Assert.Equal("StringProp", expression.GetPropertyInfo().Name);
    }

    [Fact]
    public void GetPropertyInfo_FromLambdaExpression_ReturnsProperty()
    {
        LambdaExpression expression = (Expression<Func<Simple, int>>)(s => s.IntProp);

        Assert.Equal("IntProp", expression.GetPropertyInfo().Name);
    }

    [Fact]
    public void GetPropertyInfo_RejectsNonPropertyExpressions()
    {
        Expression<Func<Simple, int>> calculated = s => s.IntProp + 1;
        Expression<Func<Simple, int>> method = s => s.GetHashCode();
        Expression<Func<Simple, int>> field = s => s.Field;

        Assert.Throws<ArgumentException>(() => calculated.GetPropertyInfo());
        Assert.Throws<ArgumentException>(() => method.GetPropertyInfo());
        Assert.Throws<ArgumentException>(() => field.GetPropertyInfo());
    }

    [Fact]
    public void GetPropertyInfo_ThrowsOnNullExpression()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ReflectionExtensions.GetPropertyInfo((LambdaExpression)null!));
    }

    [Fact]
    public void GetMemberName_ReturnsPropertyName()
    {
        Expression<Func<Simple, int>> expression = s => s.IntProp;

        Assert.Equal("IntProp", expression.GetMemberName());
    }

    [Fact]
    public void GetMemberType_ReturnsPropertyType()
    {
        Expression<Func<Simple, int>> expression = s => s.IntProp;
        Expression<Func<NullableHolder, int?>> nullableExpression = s => s.NullableInt;

        Assert.Equal(typeof(int), expression.GetMemberType());
        Assert.Equal(typeof(int), nullableExpression.GetMemberType());
    }

    [Fact]
    public void SetPropertyValue_SetsPublicPrivateAndNullValues()
    {
        var obj = new Simple();

        obj.SetPropertyValue("IntProp", 42);
        obj.SetPropertyValue("PrivateProp", 99);
        obj.SetPropertyValue("StringProp", null);

        Assert.Equal(42, obj.IntProp);
        Assert.Equal(99, obj.GetPrivateProp());
        Assert.Null(obj.StringProp);
    }

    [Fact]
    public void SetPropertyValue_ThrowsWhenPropertyIsMissingReadOnlyOrInvalid()
    {
        var obj = new Simple();

        Assert.Throws<ArgumentNullException>(() => obj.SetPropertyValue(null!, 1));
        Assert.Throws<ArgumentException>(() => obj.SetPropertyValue("", 1));
        Assert.Throws<ArgumentException>(() => obj.SetPropertyValue("NotExist", 1));
        Assert.Throws<ArgumentException>(() => obj.SetPropertyValue("ReadOnlyProp", 1));
        Assert.Throws<NullReferenceException>(() =>
            ((object)null!).SetPropertyValue("IntProp", 1));
    }

    [Fact]
    public void GetPropertyValue_ReturnsPublicAndPrivateValues()
    {
        var obj = new Simple { IntProp = 123 };
        obj.SetPrivateProp(55);

        Assert.Equal(123, obj.GetPropertyValue("IntProp"));
        Assert.Equal(55, obj.GetPropertyValue("PrivateProp"));
    }

    [Fact]
    public void GetPropertyValue_ThrowsWhenPropertyIsMissingOrInvalid()
    {
        var obj = new Simple();

        Assert.Throws<ArgumentNullException>(() => obj.GetPropertyValue(null!));
        Assert.Throws<ArgumentException>(() => obj.GetPropertyValue(""));
        Assert.Throws<ArgumentException>(() => obj.GetPropertyValue("NotExist"));
        Assert.Throws<NullReferenceException>(() =>
            ((object)null!).GetPropertyValue("IntProp"));
    }

    [Fact]
    public void GetClone_DeepClonesObject()
    {
        var original = new CustomClone
        {
            Name = "A",
            Child = new CustomClone { Name = "B" }
        };

        var clone = original.GetClone();

        Assert.NotSame(original, clone);
        Assert.NotSame(original.Child, clone.Child);
        Assert.Equal("A", clone.Name);
        Assert.Equal("B", clone.Child?.Name);
    }

    [Fact]
    public void GetClone_PreservesSystemAndValueTypeValues()
    {
        var created = DateTime.UtcNow;
        var original = new CustomClone
        {
            Name = "A",
            Created = created,
            Uri = new Uri("https://example.com")
        };

        var clone = original.GetClone();

        Assert.Equal(created, clone.Created);
        Assert.Same(original.Uri, clone.Uri);
    }

    [Fact]
    public void GetClone_PreservesCyclesAndSharedReferences()
    {
        var shared = new CustomClone { Name = "Shared" };
        var original = new CustomClone
        {
            Name = "Root",
            Child = shared,
            SecondChild = shared
        };
        shared.Child = original;

        var clone = original.GetClone();

        Assert.NotSame(original, clone);
        Assert.Same(clone, clone.Child?.Child);
        Assert.Same(clone.Child, clone.SecondChild);
    }

    [Fact]
    public void GetClone_PreservesRuntimeTypeForPolymorphicProperty()
    {
        var original = new CustomClone { Polymorphic = new SpecialClone { Name = "Special", Code = 7 } };

        var clone = original.GetClone();

        var special = Assert.IsType<SpecialClone>(clone.Polymorphic);
        Assert.Equal(7, special.Code);
    }

    [Fact]
    public void GetClone_SkipsReadOnlyProperties()
    {
        var original = new CustomClone { Name = "A" };

        var clone = original.GetClone();

        Assert.Equal(10, clone.ReadOnlyValue);
    }

    [Fact]
    public void GetClone_SupportsPrivateParameterlessConstructors()
    {
        var original = new PrivateConstructorClone { Name = "A" };

        var clone = original.GetClone();

        Assert.NotSame(original, clone);
        Assert.Equal("A", clone.Name);
    }

    [Fact]
    public void GetClone_ReturnsValueTypesAndStringsUnchanged()
    {
        const int number = 42;
        const string text = "hello";

        Assert.Equal(number, number.GetClone());
        Assert.Same(text, text.GetClone());
    }

    [Fact]
    public void GetClone_ThrowsOnNull()
    {
        CustomClone obj = null!;

        Assert.Throws<ArgumentNullException>(() => obj.GetClone());
    }

    private class Simple
    {
        public int IntProp { get; set; }
        public string? StringProp { get; set; }
        public Nested? Nested { get; set; }
        public int ReadOnlyProp => 10;
        public static int StaticProp { get; set; }
        public int Field;
        private int PrivateProp { get; set; }

        public int GetPrivateProp() => PrivateProp;
        public void SetPrivateProp(int value) => PrivateProp = value;
    }

    private class Nested
    {
        public DeepNested? Deep { get; set; }
    }

    private class DeepNested
    {
        public double DoubleProp { get; set; }
    }

    private class NullableHolder
    {
        public int? NullableInt { get; set; }
    }

    private class Base
    {
        public int BaseProp { get; set; }
        private int PrivateBaseProp { get; set; }
    }

    private class Derived : Base
    {
        public int DerivedProp { get; set; }
    }

    private class CustomClone
    {
        public string? Name { get; set; }
        public DateTime Created { get; set; }
        public Uri? Uri { get; set; }
        public CustomClone? Child { get; set; }
        public CustomClone? SecondChild { get; set; }
        public object? Polymorphic { get; set; }
        public int ReadOnlyValue => 10;

    }

    private class SpecialClone : CustomClone
    {
        public int Code { get; set; }
    }

    private class PrivateConstructorClone
    {
        public string? Name { get; set; }

        private PrivateConstructorClone()
        {
        }
    }
}
