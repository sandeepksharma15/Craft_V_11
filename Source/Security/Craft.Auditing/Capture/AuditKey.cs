using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Craft.Auditing.Capture;

public static class AuditKey
{
    #region Internal Methods

    internal static string Fingerprint(string json) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

    #endregion Internal Methods

    #region Public Methods

    public static string Serialize(IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
            throw new ArgumentException("At least one key property is required.", nameof(values));

        var sorted = new SortedDictionary<string, object?>(StringComparer.Ordinal);

        foreach (var pair in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);

            if (pair.Value is null)
                throw new ArgumentException("Key values cannot be null.", nameof(values));

            sorted.Add(pair.Key, pair.Value);
        }

        return JsonSerializer.Serialize(sorted);
    }

    #endregion Public Methods
}
