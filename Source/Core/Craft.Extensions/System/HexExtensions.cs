#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class HexExtensions
{
        extension(byte[]? bytes)
        {
            public string? BytesToHex()
                => bytes is null ? null : Convert.ToHexString(bytes);
        }
    
        extension(string? hex)
        {
            public byte[] HexToBytes()
            {
                if (string.IsNullOrWhiteSpace(hex))
                    return [];
    
                hex = hex.Trim();
    
                if (hex.Length % 2 != 0)
                    throw new FormatException("Hex string must have an even number of characters.");
    
                try
                {
                    return Convert.FromHexString(hex);
                }
                catch (FormatException ex)
                {
                    throw new FormatException("The string is not a valid hexadecimal value.", ex);
                }
            }
        }
}
