namespace Craft.Testing.Models;

public sealed class TestItem
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public override string ToString() => $"TestItem:{Id}:{Name}";
}
