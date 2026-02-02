using System;
using System.Security.Cryptography;
using System.Text;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    public static class PasswordHelper
    {
        private const int SaltLength = 16; // bytes
        private const int KeyLength = 64;  // bytes
        private const int Iterations = 100_000;

        public static string HashPassword(string plainText)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltLength);
            var key = DeriveKey(plainText, salt);
            return Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(key);
        }

        public static bool VerifyPassword(string plainText, string storedHash)
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedKey = Convert.FromBase64String(parts[1]);
            var computedKey = DeriveKey(plainText, salt);

            return CryptographicOperations.FixedTimeEquals(storedKey, computedKey);
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA512
            );
            return pbkdf2.GetBytes(KeyLength);
        }
    }
}
