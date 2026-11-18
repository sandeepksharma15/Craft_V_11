using System.Security.Cryptography;
using System.Text;

namespace Craft.Identity.Sessions;

public static class RefreshTokenSecret
{
    #region Public Methods

    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    #endregion Public Methods
}
