using Craft.CryptKey.Abstractions;
using Craft.CryptKey.Configuration;
using Craft.CryptKey.Encoding;
using Craft.CryptKey.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Craft.CryptKey.Tests;

public class ConfigurationTests
{
    #region Public Properties

    public static TheoryData<string> InvalidOptionScenarios => new()
    {
        "NullSalt", "EmptySalt", "WhitespaceSalt", "PaddedSalt",
        "NegativeMinHashLength", "ZeroMaxHashLength", "ShortMaxHashLength",
        "NullAlphabet", "EmptyAlphabet", "AlphabetContainsSlash",
        "AlphabetContainsDuplicateCharacter", "NullSeparators", "EmptySeparators",
        "SeparatorsContainUnknownCharacter", "SeparatorsContainDuplicateCharacter",
        "SeparatorsOverlapAlphabet", "SeparatorsEqualAlphabet"
    };

    #endregion Public Properties

    #region Public Methods

    [Fact]
    public void ConstructorsAndRegistration_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new HashKeys(null!));
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddHashKeys(null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddHashKeys(null!));
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

    [Theory]
    [MemberData(nameof(InvalidOptionScenarios))]
    public void InvalidConfiguration_FailsBothDirectAndDiConstruction(string scenario)
    {
        var options = HashKeysTests.Configuration();
        ApplyInvalidOptionScenario(options, scenario);
        Assert.Throws<OptionsValidationException>(() => new HashKeys(options));
        var services = new ServiceCollection();
        services.AddHashKeys(o =>
        {
            o.Salt = "test";
            ApplyInvalidOptionScenario(o, scenario);
        });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IHashKeys>());
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

    #endregion Public Methods

    #region Private Methods

    private static void ApplyInvalidOptionScenario(HashKeyOptions options, string scenario)
    {
        switch (scenario)
        {
            case "NullSalt":
                options.Salt = null!;
                break;
            case "EmptySalt":
                options.Salt = "";
                break;
            case "WhitespaceSalt":
                options.Salt = " ";
                break;
            case "PaddedSalt":
                options.Salt = " padded ";
                break;
            case "NegativeMinHashLength":
                options.MinHashLength = -1;
                break;
            case "ZeroMaxHashLength":
                options.MaxHashLength = 0;
                break;
            case "ShortMaxHashLength":
                options.MaxHashLength = 1;
                break;
            case "NullAlphabet":
                options.Alphabet = null!;
                break;
            case "EmptyAlphabet":
                options.Alphabet = "";
                break;
            case "AlphabetContainsSlash":
                options.Alphabet += "/";
                break;
            case "AlphabetContainsDuplicateCharacter":
                options.Alphabet += options.Alphabet[0];
                break;
            case "NullSeparators":
                options.Separators = null!;
                break;
            case "EmptySeparators":
                options.Separators = "";
                break;
            case "SeparatorsContainUnknownCharacter":
                options.Separators = "!";
                break;
            case "SeparatorsContainDuplicateCharacter":
                options.Separators = "cc";
                break;
            case "SeparatorsOverlapAlphabet":
                options.Alphabet = "abcdef";
                options.Separators = "a";
                break;
            case "SeparatorsEqualAlphabet":
                options.Separators = options.Alphabet;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown invalid option scenario.");
        }
    }

    #endregion Private Methods
}
