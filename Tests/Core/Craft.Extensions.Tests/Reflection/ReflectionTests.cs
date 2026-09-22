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
        var descriptor = typeof(Simple).GetMemberByName("Nested.DoubleProp");

        Assert.NotNull(descriptor);
        Assert.Equal("DoubleProp", descriptor.Name);
    }

    [Fact]
    public void GetMemberByName_ReturnsNull_WhenSimplePropertyNotFound()
    {
        Assert.Null(typeof(Simple).GetMemberByName("NotExist"));
    }

    [Fact]
    public void GetMemberByName_ReturnsNull_WhenNestedPropertyNotFound()
    {
        Assert.Null(typeof(Simple).GetMemberByName("Nested.NotExist"));
    }

    [Fact]
    public void GetMemberByName_ReturnsNull_WhenTopLevelMemberNotFound()
    {
        Assert.Null(typeof(Simple).GetMemberByName("NotExist.Value"));
    }

    [Fact]
    public void GetMemberByName_ThrowsOnNullType()
    {
        Assert.Throws<ArgumentNullException>(() => ReflectionExtensions.GetMemberByName(null!, "IntProp"));
    }

    [Fact]
    public void GetMemberByName_ThrowsOnNullOrEmptyName()
    {
        Assert.Throws<ArgumentNullException>(() => typeof(Simple).GetMemberByName(null!));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetMemberByName(""));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetMemberByName("   "));
    }

    [Fact]
    public void GetMemberName_ReturnsPropertyName()
    {
        Expression<Func<Simple, int>> expression = s => s.IntProp;

        Assert.Equal("IntProp", expression.GetMemberName());
    }

    [Fact]
    public void GetMemberType_ReturnsUnderlyingType()
    {
        Expression<Func<Simple, int>> expression = s => s.IntProp;

        Assert.Equal(typeof(int), expression.GetMemberType());
    }

    [Fact]
    public void GetMemberType_ReturnsNonNullableType()
    {
        Expression<Func<NullableHolder, int?>> expression = s => s.NullableInt;

        Assert.Equal(typeof(int), expression.GetMemberType());
    }

    [Fact]
    public void GetPropertyInfo_ByTypeAndName_ReturnsPropertyInfo()
    {
        var property = typeof(Simple).GetPropertyInfo("IntProp");

        Assert.NotNull(property);
        Assert.Equal("IntProp", property.Name);
    }

    [Fact]
    public void GetPropertyInfo_ByTypeAndName_ReturnsNonPublicProperty()
    {
        var property = typeof(Simple).GetPropertyInfo("PrivateProp");

        Assert.NotNull(property);
        Assert.Equal("PrivateProp", property.Name);
    }

    [Fact]
    public void GetPropertyInfo_ByTypeAndName_ThrowsIfNotFound()
    {
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetPropertyInfo("NotExist"));
    }

    [Fact]
    public void GetPropertyInfo_ByTypeAndName_ThrowsOnNulls()
    {
        Assert.Throws<ArgumentNullException>(() => ReflectionExtensions.GetPropertyInfo(null!, "IntProp"));
        Assert.Throws<ArgumentNullException>(() => typeof(Simple).GetPropertyInfo(null!));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetPropertyInfo(""));
        Assert.Throws<ArgumentException>(() => typeof(Simple).GetPropertyInfo("   "));
    }

    [Fact]
    public void GetPropertyInfo_FromExpression_ReturnsPropertyInfo()
    {
        Expression<Func<Simple, int>> expression = s => s.IntProp;

        var property = expression.GetPropertyInfo();

        Assert.NotNull(property);
        Assert.Equal("IntProp", property.Name);
    }

    [Fact]
    public void GetPropertyInfo_FromObjectExpression_ReturnsPropertyInfo()
    {
        Expression<Func<Simple, object>> expression = s => s.StringProp!;

        var property = expression.GetPropertyInfo();

        Assert.NotNull(property);
        Assert.Equal("StringProp", property.Name);
    }

    [Fact]
    public void GetPropertyInfo_FromLambdaExpression_ReturnsPropertyInfo()
    {
        LambdaExpression expression = (Expression<Func<Simple, int>>)(s => s.IntProp);

        var property = expression.GetPropertyInfo();

        Assert.NotNull(property);
        Assert.Equal("IntProp", property.Name);
    }

    [Fact]
    public void GetPropertyInfo_ThrowsOnInvalidExpression()
    {
        Expression<Func<Simple, int>> expression = s => s.IntProp + 1;

        Assert.Throws<ArgumentException>(() => expression.GetPropertyInfo());
    }

    [Fact]
    public void GetPropertyInfo_ThrowsOnMemberThatIsNotAProperty()
    {
        Expression<Func<Simple, int>> expression = s => s.GetHashCode();

        Assert.Throws<InvalidCastException>(() => expression.GetPropertyInfo());
    }

    [Fact]
    public void GetAllProperties_ReturnsAllIncludingBase()
    {
        var properties = typeof(Derived).GetAllProperties();

        Assert.Contains(properties, p => p.Name == "BaseProp");
        Assert.Contains(properties, p => p.Name == "DerivedProp");
        Assert.Contains(properties, p => p.Name == "PrivateBaseProp");
    }

    [Fact]
    public void GetAllProperties_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => ReflectionExtensions.GetAllProperties(null));
    }

    [Fact]
    public void SetPropertyValue_SetsPublicProperty()
    {
        var obj = new Simple();

        obj.SetPropertyValue("IntProp", 42);

        Assert.Equal(42, obj.IntProp);
    }

    [Fact]
    public void SetPropertyValue_SetsPrivateProperty()
    {
        var obj = new Simple();

        obj.SetPropertyValue("PrivateProp", 99);

        Assert.Equal(99, obj.GetPrivateProp());
    }

    [Fact]
    public void SetPropertyValue_ThrowsIfNotFoundOrNotWritable()
    {
        var obj = new Simple();

        Assert.Throws<ArgumentException>(() => obj.SetPropertyValue("NotExist", 1));
        Assert.Throws<ArgumentException>(() => obj.SetPropertyValue("ReadOnlyProp", 1));
    }

    [Fact]
    public void SetPropertyValue_ThrowsOnNullObject()
    {
        object obj = null!;

        Assert.Throws<NullReferenceException>(() => obj.SetPropertyValue("IntProp", 1));
    }

    [Fact]
    public void GetPropertyValue_GetsPublicProperty()
    {
        var obj = new Simple { IntProp = 123 };

        Assert.Equal(123, obj.GetPropertyValue("IntProp"));
    }

    [Fact]
    public void GetPropertyValue_GetsPrivateProperty()
    {
        var obj = new Simple();
        obj.SetPrivateProp(55);

        Assert.Equal(55, obj.GetPropertyValue("PrivateProp"));
    }

    [Fact]
    public void GetPropertyValue_ThrowsIfNotFound()
    {
        var obj = new Simple();

        Assert.Throws<ArgumentException>(() => obj.GetPropertyValue("NotExist"));
    }

    [Fact]
    public void GetPropertyValue_ThrowsOnNullObject()
    {
        object obj = null!;

        Assert.Throws<NullReferenceException>(() => obj.GetPropertyValue("IntProp"));
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
    public void GetClone_PreservesSystemReferences()
    {
        var original = new CustomClone
        {
            Name = "A",
            Created = DateTime.UtcNow
        };

        var clone = original.GetClone();

        Assert.Equal(original.Created, clone.Created);
    }

    [Fact]
    public void GetClone_PreservesCycles()
    {
        var original = new CustomClone { Name = "Root" };
        original.Child = original;

        var clone = original.GetClone();

        Assert.NotSame(original, clone);
        Assert.Same(clone, clone.Child);
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
        private int PrivateProp { get; set; }

        public int GetPrivateProp() => PrivateProp;
        public void SetPrivateProp(int value) => PrivateProp = value;
    }

    private class CustomClone
    {
        public string? Name { get; set; }
        public DateTime Created { get; set; }
        public CustomClone? Child { get; set; }
    }

    private class Nested
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

        public void SetPrivateBaseProp(int value) => PrivateBaseProp = value;
    }

    private class Derived : Base
    {
        public int DerivedProp { get; set; }

        public Derived()
        {
            SetPrivateBaseProp(7);
        }
    }
}
