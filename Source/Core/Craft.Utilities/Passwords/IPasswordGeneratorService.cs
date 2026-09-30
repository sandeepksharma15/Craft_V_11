namespace Craft.Utilities.Passwords;

/// <summary>Generates passwords containing uppercase, lowercase, numeric, and special characters.</summary>
public interface IPasswordGeneratorService
{
    /// <summary>Generates a password of the requested length, which must be at least 6.</summary>
    string GeneratePassword(int length = 8);
}

