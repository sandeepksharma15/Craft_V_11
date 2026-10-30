using System.Text.Json;

namespace Craft.Auditing.Model;

public sealed record AuditDifference(string PropertyName, JsonElement? OldValue, JsonElement? NewValue)
{
    public bool HasChanged => OldValue is { } oldValue
        ? NewValue is not { } newValue || !JsonElement.DeepEquals(oldValue, newValue)
        : NewValue is not null;
}
