namespace Craft.Utilities.Passwords;

/// <summary>Provides stateless password generation for dependency injection.</summary>
public class PasswordGeneratorService : IPasswordGeneratorService
{
    public string GeneratePassword(int length = 8)
        => PasswordGenerator.GeneratePassword(length);
}

