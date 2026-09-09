namespace Craft.Testing.Fixtures;

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
    public string Name { get; set; } = string.Empty;
}

public sealed class TestUser
{
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public string UserName { get; set; } = string.Empty;
}
