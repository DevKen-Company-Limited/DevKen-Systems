namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    /// <summary>
    /// Service for hashing and verifying passwords
    /// This ensures consistent password hashing across all services
    /// </summary>
    public interface IPasswordHashingService
    {
        /// <summary>
        /// Hashes a plain text password
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a plain text password against a hashed password
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="hashedPassword">Hashed password to verify against</param>
        /// <returns>True if password matches, false otherwise</returns>
        bool VerifyPassword(string password, string hashedPassword);
    }
}