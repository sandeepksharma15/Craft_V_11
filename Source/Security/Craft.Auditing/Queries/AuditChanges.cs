using System.Text.Json;
using Craft.Auditing.Model;

namespace Craft.Auditing.Queries;

public static class AuditChanges
{
    extension(AuditTrail audit)
    {
        public IReadOnlyList<AuditDifference> GetDifferences()
        {
            ArgumentNullException.ThrowIfNull(audit);

            var oldValues = Read(audit.OldValues);
            var newValues = Read(audit.NewValues);

            return [.. oldValues.Keys.Union(newValues.Keys).Order(StringComparer.Ordinal)
                .Select(name => new AuditDifference(name,
                    oldValues.TryGetValue(name, out var oldValue) ? oldValue : null,
                    newValues.TryGetValue(name, out var newValue) ? newValue : null))];
        }
    }

    #region Private Methods

    private static Dictionary<string, JsonElement> Read(string? json)
        => json is null ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? throw new JsonException("Audit values must be JSON objects.");

    #endregion Private Methods
}
