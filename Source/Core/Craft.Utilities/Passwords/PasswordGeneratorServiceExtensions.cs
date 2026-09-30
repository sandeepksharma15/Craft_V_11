using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Craft.Utilities.Passwords;

public static class PasswordGeneratorServiceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the default singleton password generator if no implementation is registered.
        /// </summary>
        public IServiceCollection AddPasswordGeneratorService()
        {
            ArgumentNullException.ThrowIfNull(services);
            services.TryAddSingleton<IPasswordGeneratorService, PasswordGeneratorService>();

            return services;
        }
    }
}
