
using Devken.CBC.SchoolManagement.Application.Service;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // generate a 128-bit salt using a cryptographically strong random sequence
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            // store salt + hash together
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = parts[1];

            var computedHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return storedHash == computedHash;
        }
    }
}