using Craft.Utilities.Passwords;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Craft.Utilities.Tests.Passwords;

public class PasswordTests
{
    #region Private Fields

    private const string AllowedChars = UppercaseChars + LowercaseChars + NumericChars + SpecialChars;
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string NumericChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+[]{}|;:,.<>?";
    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    #endregion Private Fields

    #region Private Methods

    private static void AssertPassword(string password, int length)
    {
        Assert.Equal(length, password.Length);
        Assert.All(password, c => Assert.Contains(c, AllowedChars));
        Assert.Contains(password, c => UppercaseChars.Contains(c));
        Assert.Contains(password, c => LowercaseChars.Contains(c));
        Assert.Contains(password, c => NumericChars.Contains(c));
        Assert.Contains(password, c => SpecialChars.Contains(c));
    }

    #endregion Private Methods

    #region Public Methods

    [Fact]
    public void AddPasswordGeneratorService_CustomRegistration_PreservesExistingImplementation()
    {
        IPasswordGeneratorService custom = Mock.Of<IPasswordGeneratorService>();
        ServiceCollection services = new();
        services.AddSingleton(custom);

        services.AddPasswordGeneratorService();

        Assert.Single(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(custom, provider.GetRequiredService<IPasswordGeneratorService>());
    }

    [Fact]
    public void AddPasswordGeneratorService_DefaultRegistration_ReturnsCollectionAndResolvesSingleton()
    {
        ServiceCollection services = new();

        Assert.Same(services, services.AddPasswordGeneratorService());
        ServiceDescriptor descriptor = Assert.Single(services);
        Assert.Equal(typeof(IPasswordGeneratorService), descriptor.ServiceType);
        Assert.Equal(typeof(PasswordGeneratorService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        using ServiceProvider provider = services.BuildServiceProvider();
        IPasswordGeneratorService first = provider.GetRequiredService<IPasswordGeneratorService>();
        IPasswordGeneratorService second = provider.GetRequiredService<IPasswordGeneratorService>();

        Assert.IsType<PasswordGeneratorService>(first);
        Assert.Same(first, second);
        AssertPassword(first.GeneratePassword(16), 16);
    }

    [Fact]
    public void AddPasswordGeneratorService_NullCollection_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>("services", () => services.AddPasswordGeneratorService());
    }

    [Fact]
    public void AddPasswordGeneratorService_RepeatedRegistration_DoesNotDuplicateService()
    {
        ServiceCollection services = new();

        services.AddPasswordGeneratorService().AddPasswordGeneratorService();

        Assert.Single(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Single(provider.GetServices<IPasswordGeneratorService>());
    }

    [Fact]
    public void GeneratePassword_DefaultLength_ReturnsValidPassword()
    {
        string password = PasswordGenerator.GeneratePassword();

        AssertPassword(password, 8);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void GeneratePassword_LengthBelowMinimum_ThrowsWithParameterAndValue(int length)
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            nameof(length), () => PasswordGenerator.GeneratePassword(length));

        Assert.Equal(length, error.ActualValue);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(16)]
    public void GeneratePassword_RepeatedCalls_AlwaysMeetContract(int length)
    {
        for (int i = 0; i < 50; i++)
            AssertPassword(PasswordGenerator.GeneratePassword(length), length);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(1024)]
    public void GeneratePassword_ValidLength_ReturnsExactLengthAndAllCategories(int length)
    {
        string password = PasswordGenerator.GeneratePassword(length);

        AssertPassword(password, length);
    }

    [Fact]
    public void GeneratePasswordService_ConcreteDefault_ReturnsValidPassword()
    {
        PasswordGeneratorService service = new();

        AssertPassword(service.GeneratePassword(), 8);
    }

    [Fact]
    public async Task GeneratePasswordService_ConcurrentCalls_ReturnValidPasswords()
    {
        ServiceCollection services = new();
        services.AddPasswordGeneratorService();
        using ServiceProvider provider = services.BuildServiceProvider();
        IPasswordGeneratorService service = provider.GetRequiredService<IPasswordGeneratorService>();

        string[] passwords = await Task.WhenAll(Enumerable.Range(0, 64)
            .Select(_ => Task.Run(() => service.GeneratePassword(24))));

        Assert.All(passwords, password => AssertPassword(password, 24));
    }

    [Fact]
    public void GeneratePasswordService_InterfaceDefault_ReturnsValidPassword()
    {
        IPasswordGeneratorService service = new PasswordGeneratorService();

        AssertPassword(service.GeneratePassword(), 8);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void GeneratePasswordService_InvalidLength_PreservesValidation(int length)
    {
        IPasswordGeneratorService service = new PasswordGeneratorService();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            nameof(length), () => service.GeneratePassword(length));

        Assert.Equal(length, error.ActualValue);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(1024)]
    public void GeneratePasswordService_ValidLength_ReturnsValidPassword(int length)
    {
        IPasswordGeneratorService service = new PasswordGeneratorService();

        AssertPassword(service.GeneratePassword(length), length);
    }

    #endregion Public Methods
}
