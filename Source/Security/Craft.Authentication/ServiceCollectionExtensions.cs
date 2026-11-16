using Craft.Core.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Craft.Authentication;

public static class ServiceCollectionExtensions
{
    public const string Scheme = "CraftBearer";

    public static IServiceCollection AddCraftAuthentication(this IServiceCollection services, Action<JwtOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<JwtOptions>().Configure(configure).Validate(JwtOptions.IsValid, "Invalid JWT configuration.").ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<JwtTokenService>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        services.AddAuthentication().AddJwtBearer(Scheme);
        services.AddOptions<JwtBearerOptions>(Scheme).Configure<JwtTokenService>((options, tokens) =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.SaveToken = false;
            options.TokenValidationParameters = tokens.CreateValidationParameters();
            options.TokenHandlers.Clear();
            options.TokenHandlers.Add(new JsonWebTokenHandler { MaximumTokenSizeInBytes = tokens.MaximumTokenSize, MapInboundClaims = false });
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (!await tokens.AcceptPrincipalAsync(context.Principal!, context.HttpContext.RequestAborted).ConfigureAwait(false))
                        context.Fail("Invalid or revoked token.");
                }
            };
        });
        return services;
    }

    public static IServiceCollection AddInMemoryTokenRevocation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ITokenRevocationStore, InMemoryTokenRevocationStore>();
        return services;
    }
}
