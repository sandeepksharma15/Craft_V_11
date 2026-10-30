using Craft.Auditing.Capture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Craft.Auditing.Configuration;

public static class ServiceCollectionExtensions
{
    #region Public Methods

    public static OptionsBuilder<AuditOptions> AddCraftAuditing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<AuditTrailFactory>();

        return services.AddOptions<AuditOptions>()
            .Validate(o => o.RetentionPeriod is not { } retention || retention > TimeSpan.Zero, "RetentionPeriod must be positive.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.RedactionText), "RedactionText is required.")
            .ValidateOnStart();
    }

    #endregion Public Methods
}
