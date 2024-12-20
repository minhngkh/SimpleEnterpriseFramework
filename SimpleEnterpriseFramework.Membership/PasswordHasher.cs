using System.Security.Cryptography;

namespace SimpleEnterpriseFramework.Membership;

public class PasswordHasher
{
    private const int SaltSize = 16; // Size of the salt in bytes
    private const int HashSize = 32; // Size of the hash in bytes
    private const int Iterations = 10000;
    
    public string HashPassword(string password)
    {
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations,
                   HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(HashSize);

            // Combine the salt and hash
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            // Return the combined result as a Base64 string
            return Convert.ToBase64String(hashBytes);
        }
    }
    public bool VerifyPassword(string password, string hashedPassword)
    {
        // Decode the stored hash from Base64
        byte[] hashBytes = Convert.FromBase64String(hashedPassword);

        // Extract the salt
        byte[] salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        // Extract the stored hash
        byte[] storedHash = new byte[HashSize];
        Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);

        // Compute the hash of the provided password using the same salt
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] computedHash = pbkdf2.GetBytes(HashSize);

            // Compare the computed hash with the stored hash
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
    }

}