using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Craft.Authentication.Tests;

public class ConfigurationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public void InvalidConfiguration_IsRejectedByRegistrationAndDirectConstruction(int mutation)
    {
        var settings = new JwtOptions { Issuer = "issuer", Audience = "api", ActiveKeyId = "key", SigningKeys = new() { ["key"] = Convert.ToBase64String(new byte[32]) } };
        switch (mutation)
        {
            case 0: settings.Issuer = " "; break;
            case 1: settings.Audience = " "; break;
            case 2: settings.ActiveKeyId = " "; break;
            case 3: settings.Lifetime = TimeSpan.Zero; break;
            case 4: settings.ClockSkew = TimeSpan.FromSeconds(-1); break;
            case 5: settings.MaximumTokenSize = 1; break;
            case 6: settings.SigningKeys = null!; break;
            case 7: settings.SigningKeys.Clear(); break;
            case 8: settings.SigningKeys[" "] = Convert.ToBase64String(new byte[32]); break;
            case 9: settings.SigningKeys["key"] = null!; break;
            case 10: settings.SigningKeys["key"] = "invalid"; break;
            case 11: settings.SigningKeys["key"] = Convert.ToBase64String(new byte[1]); break;
        }
        var clock = new Clock();
        Assert.Throws<OptionsValidationException>(() => new JwtTokenService(Options.Create(settings), clock, new InMemoryTokenRevocationStore(clock)));
        var services = new ServiceCollection();
        services.AddCraftAuthentication(o => { o.Issuer = settings.Issuer; o.Audience = settings.Audience; o.ActiveKeyId = settings.ActiveKeyId; o.SigningKeys = settings.SigningKeys; o.Lifetime = settings.Lifetime; o.ClockSkew = settings.ClockSkew; o.MaximumTokenSize = settings.MaximumTokenSize; });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtOptions>>().Value);
    }
}
