using System.Security.Cryptography;

namespace Craft.Utilities.Passwords;

public static class PasswordGenerator
{
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string NumericChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+[]{}|;:,.<>?";
    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string AllChars = UppercaseChars + LowercaseChars + NumericChars + SpecialChars;

    /// <summary>Generates a cryptographically random password with all four character categories.</summary>
    /// <param name="length">The password length. Must be at least 6; defaults to 8 for compatibility.</param>
    /// <exception cref="ArgumentOutOfRangeException">The length is less than 6.</exception>
    public static string GeneratePassword(int length = 8)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 6);

        return string.Create(length, AllChars, static (password, pool) =>
        {
            password[0] = GetRandomChar(UppercaseChars);
            password[1] = GetRandomChar(LowercaseChars);
            password[2] = GetRandomChar(NumericChars);
            password[3] = GetRandomChar(SpecialChars);
            RandomNumberGenerator.GetItems(pool.AsSpan(), password[4..]);
            RandomNumberGenerator.Shuffle(password);
        });
    }

    private static char GetRandomChar(string pool)
        => pool[RandomNumberGenerator.GetInt32(pool.Length)];
}
