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

    #endregion Models
}
