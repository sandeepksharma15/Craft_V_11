namespace Craft.Authentication;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string ActiveKeyId { get; set; } = "";
    public Dictionary<string, string> SigningKeys { get; set; } = [];
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(30);
    public int MaximumTokenSize { get; set; } = 16384;

    internal static bool IsValid(JwtOptions value)
    {
        if (string.IsNullOrWhiteSpace(value.Issuer) || string.IsNullOrWhiteSpace(value.Audience) ||
            string.IsNullOrWhiteSpace(value.ActiveKeyId) || value.Lifetime <= TimeSpan.Zero ||
            value.ClockSkew < TimeSpan.Zero || value.MaximumTokenSize < 256 || value.SigningKeys is null ||
            !value.SigningKeys.ContainsKey(value.ActiveKeyId)) return false;
        foreach (var pair in value.SigningKeys)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null) return false;
            try { if (Convert.FromBase64String(pair.Value).Length < 32) return false; }
            catch (FormatException) { return false; }
        }
        return true;
    }
}
