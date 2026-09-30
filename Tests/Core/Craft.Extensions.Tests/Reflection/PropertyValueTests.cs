using System.Reflection;
using static Craft.Testing.Reflection.ReflectionTestModels;

namespace Craft.Extensions.Tests.Reflection;

public class PropertyValueTests
{
    [Fact]
    public void NestedPaths_ReadWriteAndAliasesAgree()
    {
        var model = new Simple { Nested = new Nested { Deep = new DeepNested() } };
        model.SetValue("Nested.Deep.DoubleProp", 2.5);
        Assert.Equal(2.5, model.GetValue<double>("Nested.Deep.DoubleProp"));
        model.SetPropertyValue(propertyName: "Nested.Deep.DoubleProp", value: 4.5);
        Assert.Equal(4.5, model.GetPropertyValue(propertyName: "Nested.Deep.DoubleProp"));
        Assert.Null(model.GetValue("StringProp"));
        Assert.Null(model.GetValue<string>("StringProp"));
        Assert.Throws<InvalidCastException>(() => model.GetValue<int>("StringProp"));
        Assert.Throws<InvalidCastException>(() => model.GetValue<string>("IntProp"));
        Assert.Throws<InvalidCastException>(() => model.GetValue<long>("IntProp"));
        Assert.Equal(0, model.GetValue<int?>("IntProp"));
        Assert.Equal(0, ((object)new PropertyPoint()).GetValue<int>("X"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".IntProp")]
    [InlineData("IntProp.")]
    [InlineData("Nested..Deep")]
    [InlineData("Nested. .Deep")]
    [InlineData("intProp")]
    [InlineData(" IntProp")]
    [InlineData("Missing")]
    [InlineData("Field")]
    [InlineData("StaticProp")]
    public void InvalidPathsAndNonProperties_AreRejected(string path)
    {
        var model = new Simple();
        Assert.Throws<ArgumentException>(() => model.GetValue(path));
        Assert.Throws<ArgumentException>(() => model.SetValue(path, 1));
    }

    [Fact]
    public void NullArgumentsAndIntermediateValues_AreRejected()
    {
        var model = new Simple();
        Assert.Throws<ArgumentNullException>(() => model.GetValue(null!));
        Assert.Throws<ArgumentNullException>(() => model.SetValue(null!, 1));
        Assert.Throws<ArgumentNullException>(() => ((object)null!).GetValue("IntProp"));
        Assert.Throws<ArgumentNullException>(() => ((object)null!).SetValue("IntProp", 1));
        Assert.Throws<InvalidOperationException>(() => model.GetValue("Nested.Deep.DoubleProp"));
        Assert.Throws<InvalidOperationException>(() => model.SetValue("Nested.Deep.DoubleProp", 1d));
        model.Nested = new Nested();
        Assert.Throws<InvalidOperationException>(() => model.GetValue("Nested.Deep.DoubleProp"));
        Assert.Throws<InvalidOperationException>(() => model.SetValue("Nested.Deep.DoubleProp", 1d));
        var error = Assert.Throws<ArgumentException>(() => model.GetValue("Nested.Missing"));
        Assert.Contains("Missing", error.Message);
        Assert.Contains("Nested.Missing", error.Message);
    }

    [Fact]
    public void AccessorVisibility_IsEnforcedAndCanBeOptedInto()
    {
        var model = new PropertyAccessModel();
        Assert.Equal(3, model.GetValue<int>("PrivateSetter"));
        Assert.Throws<ArgumentException>(() => model.SetValue("PrivateSetter", 7));
        model.SetValue("PrivateSetter", 7, true);
        Assert.Equal(7, model.PrivateSetter);
        model.SetValue("PrivateGetter", 9);
        Assert.Throws<ArgumentException>(() => model.GetValue("PrivateGetter"));
        Assert.Equal(9, model.GetValue<int>("PrivateGetter", true));
        model.SetValue("WriteOnly", 11);
        Assert.Equal(11, model.Written);
        Assert.Throws<ArgumentException>(() => model.GetValue("WriteOnly", true));
        Assert.Throws<ArgumentException>(() => model.SetValue("Written", 12, true));
        Assert.Throws<ArgumentException>(() => model.SetValue("InitOnly", 12));
        Assert.Throws<ArgumentException>(() => model.SetValue("InitOnly", 12, true));

        var simple = new Simple();
        Assert.Throws<ArgumentException>(() => simple.GetValue("PrivateProp"));
        Assert.Throws<ArgumentException>(() => simple.SetValue("PrivateProp", 1));
        simple.SetValue("PrivateProp", 5, true);
        Assert.Equal(5, simple.GetValue<int>("PrivateProp", true));
        var derived = new Derived();
        derived.SetValue("PrivateBaseProp", 6, true);
        Assert.Equal(6, derived.GetValue<int>("PrivateBaseProp", true));
        derived.SetValue("BaseProp", 8);
        Assert.Equal(8, derived.GetValue<int>("BaseProp"));
    }

    [Fact]
    public void Values_RequireAssignmentCompatibilityWithoutConversion()
    {
        var model = new Simple { IntProp = 10 };
        Assert.Throws<ArgumentException>(() => model.SetValue("IntProp", null));
        Assert.Throws<ArgumentException>(() => model.SetValue("IntProp", 1L));
        Assert.Throws<ArgumentException>(() => model.SetValue("IntProp", "1"));
        Assert.Equal(10, model.IntProp);
        model.SetValue("StringProp", null);
        model.SetValue("StringProp", "text");
        Assert.Equal("text", model.StringProp);
        var holder = new NullableHolder();
        Assert.Null(holder.GetValue<int?>("NullableInt"));
        holder.SetValue("NullableInt", 12);
        Assert.Equal(12, holder.GetValue<int?>("NullableInt"));
        holder.SetValue("NullableInt", null);
        Assert.Null(holder.NullableInt);
        var clone = new CustomClone();
        var child = new SpecialClone();
        clone.SetValue("Child", child);
        Assert.Same(child, clone.Child);
    }

    [Fact]
    public void NestedStructs_WriteBackThroughMultipleLevelsAndNullableProperties()
    {
        var model = new PropertyAccessModel();
        model.SetValue("Envelope.Point.X", 17);
        Assert.Equal(17, model.Envelope.Point.X);
        Assert.Equal(17, model.GetValue<int>("Envelope.Point.X"));
        model.SetValue("NullablePoint.X", 19);
        Assert.Equal(19, model.NullablePoint?.X);
        Assert.Throws<ArgumentException>(() => model.SetValue("PrivatePoint.X", 21));
        model.SetValue("PrivatePoint.X", 21, true);
        Assert.Equal(21, model.PrivatePoint.X);
        model.NullablePoint = null;
        Assert.Throws<InvalidOperationException>(() => model.SetValue("NullablePoint.X", 1));
        Assert.Throws<ArgumentException>(() => ((object)new PropertyPoint()).SetValue("X", 1));
    }

    [Fact]
    public void ReferenceBoundaries_DoNotRequireWriteBackButReadOnlyStructsDo()
    {
        var model = new PropertyAccessModel();
        Assert.Throws<ArgumentException>(() => model.SetValue("ReadOnlyPoint.X", 1));
        Assert.Equal(0, model.ReadOnlyPoint.X);
        model.SetValue("ReadOnlyPoint.Child.IntProp", 23);
        Assert.Equal(23, model.ReadOnlyPoint.Child?.IntProp);
        model.SetValue("ReadOnlyChild.IntProp", 25);
        Assert.Equal(25, model.ReadOnlyChild.IntProp);
        model.SetValue("BoxedPoint.X", 27);
        Assert.Equal(27, ((PropertyPoint)model.BoxedPoint).X);
    }

    [Theory]
    [InlineData("Item")]
    [InlineData("Span")]
    public void UnsupportedPropertyShapes_AreRejected(string path)
    {
        var model = new PropertyAccessModel();
        Assert.Throws<ArgumentException>(() => model.GetValue(path, true));
        Assert.Throws<ArgumentException>(() => model.SetValue(path, null, true));
    }

    [Fact]
    public void UserAccessorExceptions_ArePreserved()
    {
        var model = new PropertyAccessModel();
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => model.GetValue("Throwing")).InnerException);
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => model.SetValue("Throwing", 1)).InnerException);
    }
}
