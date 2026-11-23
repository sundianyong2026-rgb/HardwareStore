using System.Security.Cryptography;
using System.Text;

namespace HardwareStore.Data;

/// <summary>
/// Demo password hasher (SHA-256). The graduation project stores hashes only,
/// never plain-text passwords. Replace with ASP.NET Core Identity hashing
/// if stronger credentials are required later.
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(string password, string storedHash)
    {
        return Hash(password) == storedHash;
    }
}
