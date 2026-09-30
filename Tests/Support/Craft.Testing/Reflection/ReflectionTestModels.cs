namespace Craft.Testing.Reflection;

public static class ReflectionTestModels
{
    #region Models

    public class Base
    {
        #region Private Properties

        private int PrivateBaseProp { get; set; }

        #endregion Private Properties

        #region Public Properties

        public int BaseProp { get; set; }

        #endregion Public Properties
    }

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
    public class CustomClone
    {
        #region Public Properties

        public CustomClone? Child { get; set; }
        public DateTime Created { get; set; }
        public string? Name { get; set; }
        public object? Polymorphic { get; set; }
        public int ReadOnlyValue => 10;
        public CustomClone? SecondChild { get; set; }
        public Uri? Uri { get; set; }

        #endregion Public Properties
    }

    public class DeepNested
    {
        #region Public Properties

        public double DoubleProp { get; set; }

        #endregion Public Properties
    }

    public class Derived : Base
    {
        #region Public Properties

        public int DerivedProp { get; set; }

        #endregion Public Properties
    }

    public class Nested
    {
        #region Public Properties

        public DeepNested? Deep { get; set; }

        #endregion Public Properties
    }

    public class NullableHolder
    {
        #region Public Properties

        public int? NullableInt { get; set; }

        #endregion Public Properties
    }

    public class PrivateConstructorClone
    {
        #region Private Constructors

        private PrivateConstructorClone()
        {
        }

        #endregion Private Constructors

        #region Public Properties

        public string? Name { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static PrivateConstructorClone Create(string? name)
        {
            return new PrivateConstructorClone { Name = name };
        }

        #endregion Public Methods
    }

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
    public class Simple
    {
        #region Private Properties

        private int PrivateProp { get; set; }

        #endregion Private Properties

        #region Public Fields

        public int Field = 1;

        #endregion Public Fields

        #region Public Properties

        public static int StaticProp { get; set; }
        public int IntProp { get; set; }
        public Nested? Nested { get; set; }
        public int ReadOnlyProp => 10;
        public string? StringProp { get; set; }

        #endregion Public Properties

        #region Public Methods

        public int GetPrivateProp() => PrivateProp;

        public void SetPrivateProp(int value) => PrivateProp = value;

        #endregion Public Methods
    }

    public class SpecialClone : CustomClone
    {
        #region Public Properties

        public int Code { get; set; }

        #endregion Public Properties
    }

    public struct PropertyPoint
    {
        public int X { get; set; }
        public Simple? Child { get; set; }
    }

    public struct PropertyEnvelope
    {
        public PropertyPoint Point { get; set; }
    }

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Reflection accessor fixtures require instance properties.")]
    public class PropertyAccessModel
    {
        private int _written;
        public int PrivateSetter { get; private set; } = 3;
        public int PrivateGetter { private get; set; }
        public int WriteOnly { set => _written = value; }
        public int Written => _written;
        public int InitOnly { get; init; }
        public int this[int index] { get => index; set => _written = value; }
        public int Throwing { get => throw new InvalidOperationException("getter"); set => throw new InvalidOperationException("setter"); }
        public Span<int> Span => default;
        public PropertyEnvelope Envelope { get; set; }
        public PropertyPoint? NullablePoint { get; set; } = new PropertyPoint();
        public PropertyPoint ReadOnlyPoint { get; } = new() { Child = new Simple() };
        public PropertyPoint PrivatePoint { get; private set; }
        public object BoxedPoint { get; } = new PropertyPoint();
        public Simple ReadOnlyChild { get; } = new();
    }

    #endregion Models
}
