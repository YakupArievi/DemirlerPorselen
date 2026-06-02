using System.Security.Cryptography;
using Toptanci.Application.Common.Abstractions;

namespace Toptanci.Infrastructure.Security;

/// <summary>
/// PBKDF2 (SHA-256) tabanlı parola özetleme. Format: "{salt}.{hash}" (base64).
/// Harici paket gerektirmez; .NET kriptografi API'leri kullanılır.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 2);
        if (parts.Length != 2)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);
            var input = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, hash.Length);
            return CryptographicOperations.FixedTimeEquals(input, hash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
