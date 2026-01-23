namespace FestConnect.Security;

/// <summary>
/// Interface for password hashing operations using Argon2id.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using Argon2id.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <returns>The hashed password with embedded parameters.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <param name="hash">The stored hash.</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    bool VerifyPassword(string password, string hash);
}
