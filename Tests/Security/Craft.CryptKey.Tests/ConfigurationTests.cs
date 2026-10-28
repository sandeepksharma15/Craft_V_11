using Craft.CryptKey;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Craft.CryptKey.Tests;

public class ConfigurationTests
{
    public static TheoryData<Action<HashKeyOptions>> InvalidOptions => new()
    {
        o => o.Salt = null!, o => o.Salt = "", o => o.Salt = " ", o => o.Salt = " padded ",
        o => o.MinHashLength = -1, o => o.MaxHashLength = 0, o => o.MaxHashLength = 1,
        o => o.Alphabet = null!, o => o.Alphabet = "", o => o.Alphabet += "/",
        o => o.Alphabet += o.Alphabet[0], o => o.Separators = null!, o => o.Separators = "",
        o => o.Separators = "!", o => o.Separators = "cc",
        o => { o.Alphabet = "abcdef"; o.Separators = "a"; },
        o => o.Separators = o.Alphabet
    };

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public void InvalidConfiguration_FailsBothDirectAndDiConstruction(Action<HashKeyOptions> configure)
    {
        var options = HashKeysTests.Configuration();
        configure(options);
        Assert.Throws<OptionsValidationException>(() => new HashKeys(options));
        var services = new ServiceCollection();
        services.AddHashKeys(o => { o.Salt = "test"; configure(o); });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IHashKeys>());
    }

    [Fact]
    public void ConstructorsAndRegistration_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new HashKeys(null!));
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddHashKeys(null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddHashKeys(null!));
    }

    [Fact]
    public void Registration_IsIdempotentAndUsesOneSingleton()
    {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddHashKeys(o => o.Salt = "application"));
        services.AddHashKeys();
        using var provider = services.BuildServiceProvider();
        var keys = provider.GetRequiredService<IHashKeys>();
        Assert.Same(keys, provider.GetRequiredService<IHashKeys>());
        Assert.Single(provider.GetServices<IHashKeys>());
        Assert.Single(provider.GetServices<IValidateOptions<HashKeyOptions>>());
        Assert.Equal(42, keys.Decode<int>(keys.Encode(42)));
    }

    [Fact]
    public void Registration_RespectsAnExistingImplementation()
    {
        var custom = new HashKeys(HashKeysTests.Configuration());
        var services = new ServiceCollection();
        services.AddSingleton<IHashKeys>(custom);
        services.AddHashKeys(o => o.Salt = "application");
        using var provider = services.BuildServiceProvider();
        Assert.Same(custom, provider.GetRequiredService<IHashKeys>());
    }

    [Fact]
    public async Task MissingSalt_FailsAtHostStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddHashKeys();
        using var host = builder.Build();
        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ValidConfiguration_StartsAndStopsHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddHashKeys().Configure(o => o.Salt = "host-application");
        using var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        var keys = host.Services.GetRequiredService<IHashKeys>();
        Assert.Equal(1, keys.Decode<int>(keys.Encode(1)));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void CustomUrlSafeAlphabetAndSeparators_Work()
    {
        var options = HashKeysTests.Configuration();
        options.Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789-_";
        options.Separators = "abc";
        options.MinHashLength = 0;
        var keys = new HashKeys(options);
        var token = keys.Encode(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.All(token, c => Assert.Contains(c, options.Alphabet));
        Assert.Equal(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"), keys.Decode<Guid>(token));
    }
}