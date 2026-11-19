using System.Text.Json;

namespace Craft.HttpServices;

public sealed class HttpServiceOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaximumResponseBytes { get; set; } = 4 * 1024 * 1024;

    public JsonSerializerOptions JsonOptions { get; set; } = new(JsonSerializerDefaults.Web);
}
