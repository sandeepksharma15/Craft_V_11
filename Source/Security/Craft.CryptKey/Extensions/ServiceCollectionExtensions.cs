using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Craft.CryptKey;

public static class ServiceCollectionExtensions
{
    public static OptionsBuilder<HashKeyOptions> AddHashKeys(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<HashKeyOptions>, HashKeyOptionsValidator>());
        services.TryAddSingleton<IHashKeys>(provider => new HashKeys(provider.GetRequiredService<IOptions<HashKeyOptions>>().Value));
        return services.AddOptions<HashKeyOptions>().ValidateOnStart();
    }

    public static IServiceCollection AddHashKeys(this IServiceCollection services, Action<HashKeyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddHashKeys().Configure(configure);
        return services;
    }
}