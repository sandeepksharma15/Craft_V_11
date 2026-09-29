namespace Craft.Testing.Models;

public sealed class Company
{
    public long CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class Store
{
    public string City { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public Company? Company { get; set; }
}

public sealed class TestEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string Name { get; set; } = string.Empty;
    public TestLocation Location { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = [];
}

public sealed class TestLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TestCountry Country { get; set; } = new();
}

public sealed class TestCountry
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class TestUser
{
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public sealed class TestClass
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public double Price { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public TestClass? Child { get; set; }
    public int ScoreField;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "<Pending>")]
public sealed class MyClass
{
    public int AnotherProperty { get; set; }
    public string? PropertyName { get; set; }
    public static int StaticField;
    private int _privateField;

    public void SetPrivateField(int value) => _privateField = value;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
public sealed class MethodHost
{
    public string Echo(string value) => value;
    public string TakesObject(object value) => value.ToString() ?? string.Empty;
    public double DoubleInput(double value) => value;
    public int NeedsInt(int value) => value;
    public string Ambiguous(string? value) => value ?? string.Empty;
    public string Ambiguous(Uri? value) => value?.ToString() ?? string.Empty;
}

public sealed class MethodTargetContainer
{
    public MethodHost Target { get; set; } = new();
}
