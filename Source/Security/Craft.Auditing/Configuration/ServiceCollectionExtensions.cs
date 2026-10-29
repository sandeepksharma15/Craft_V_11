using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Craft.Auditing;

public static class ServiceCollectionExtensions
{
    public static OptionsBuilder<AuditOptions> AddCraftAuditing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<AuditTrailFactory>();
        return services.AddOptions<AuditOptions>()
            .Validate(o => o.RetentionPeriod is null || o.RetentionPeriod > TimeSpan.Zero, "RetentionPeriod must be positive.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.RedactionText), "RedactionText is required.")
            .ValidateOnStart();
    }
}