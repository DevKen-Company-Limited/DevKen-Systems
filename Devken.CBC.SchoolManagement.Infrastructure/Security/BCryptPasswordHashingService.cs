using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System.Security.Cryptography;
using System.Text;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    /// <summary>
    /// BCrypt password hashing service with support for migrating from legacy hash formats
    /// </summary>
    public class BCryptPasswordHashingService : IPasswordHashingService
    {
        private const int WorkFactor = 11;
        private readonly IPasswordHasher<User> _legacyPasswordHasher;

        public BCryptPasswordHashingService()
        {
            _legacyPasswordHasher = new PasswordHasher<User>();
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Always hash new passwords with BCrypt
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            // Try BCrypt verification first
            if (IsBCryptHash(hashedPassword))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
                }
                catch (SaltParseException)
                {
                    // Fall through to legacy verification
                }
            }

            // Try ASP.NET Core Identity hash verification
            if (IsIdentityHash(hashedPassword))
            {
                var tempUser = new User { PasswordHash = hashedPassword };
                var result = _legacyPasswordHasher.VerifyHashedPassword(tempUser, hashedPassword, password);
                return result == PasswordVerificationResult.Success ||
                       result == PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Try SHA256 hash verification (old format)
            if (IsSHA256Hash(hashedPassword))
            {
                return VerifySHA256Password(password, hashedPassword);
            }

            return false;
        }

        /// <summary>
        /// Checks if a hash needs to be upgraded to BCrypt
        /// </summary>
        public bool NeedsRehash(string hashedPassword)
        {
            return !IsBCryptHash(hashedPassword);
        }

        #region Private Helper Methods

        private static bool IsBCryptHash(string hash)
        {
            // BCrypt hashes start with $2a$, $2b$, or $2y$ and are 60 characters long
            return hash != null &&
                   hash.Length == 60 &&
                   (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"));
        }

        private static bool IsIdentityHash(string hash)
        {
            // ASP.NET Core Identity hashes are base64 encoded and typically start with specific bytes
            // They're usually around 84-88 characters
            return hash != null &&
                   hash.Length >= 84 &&
                   hash.Length <= 100 &&
                   IsBase64String(hash);
        }

        private static bool IsSHA256Hash(string hash)
        {
            // SHA256 produces 32 bytes, which becomes 44 characters in base64
            return hash != null && hash.Length == 44 && IsBase64String(hash);
        }

        private static bool IsBase64String(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            try
            {
                Convert.FromBase64String(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool VerifySHA256Password(string password, string hashedPassword)
        {
            try
            {
                var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                var computedHash = Convert.ToBase64String(hashedBytes);
                return computedHash == hashedPassword;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}