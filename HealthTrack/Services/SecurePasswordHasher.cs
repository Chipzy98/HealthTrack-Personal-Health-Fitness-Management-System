using System.Security.Cryptography;

namespace HealthTrack.Services
{
    /// <summary>
    /// Salted PBKDF2 (SHA-256, 100,000 iterations) password hashing.
    /// Stored format: "{iterations}.{base64 salt}.{base64 hash}".
    /// </summary>
    public static class SecurePasswordHasher
    {
        private const int SaltSize = 16;          // 128-bit salt
        private const int KeySize = 32;           // 256-bit derived key
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;

            var parts = storedHash.Split('.', 3);
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations)) return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expectedKey = Convert.FromBase64String(parts[2]);
                byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedKey.Length);

                // Constant-time comparison prevents timing attacks.
                return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
