using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace FestConnect.Security;

/// <summary>
/// Argon2id password hasher implementation following OWASP recommendations.
/// Memory: 64MB, Iterations: 3, Parallelism: 4
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemorySize = 65536; // 64 MB
    private const int Iterations = 3;
    private const int Parallelism = 4;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPasswordInternal(password, salt);

        // Format: $argon2id$v=19$m=65536,t=3,p=4$<base64-salt>$<base64-hash>
        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hash);

        try
        {
            var parts = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || parts[0] != "argon2id")
            {
                return false;
            }

            // Parse parameters (for future-proofing if params change)
            var salt = Convert.FromBase64String(parts[3]);
            var storedHash = Convert.FromBase64String(parts[4]);

            var computedHash = HashPasswordInternal(password, salt);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] HashPasswordInternal(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        return argon2.GetBytes(HashSize);
    }
}
