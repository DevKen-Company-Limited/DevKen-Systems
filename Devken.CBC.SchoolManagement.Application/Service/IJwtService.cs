using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    /// <summary>
    /// Contract for JWT access & refresh token operations
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT access token for a user.
        /// Automatically aggregates permissions from all user roles and removes duplicates.
        /// </summary>
        /// <param name="user">Authenticated user</param>
        /// <param name="roles">User roles (will fetch and combine permissions from all roles)</param>
        /// <param name="tenantId">Optional tenant/school ID (overrides user's default tenant)</param>
        Task<string> GenerateTokenAsync(
            User user,
            IList<string> roles,
            Guid? tenantId = null);

        /// <summary>
        /// Generates a JWT access token with explicit permissions (legacy method).
        /// Use GenerateTokenAsync for automatic permission aggregation from roles.
        /// </summary>
        /// <param name="user">Authenticated user</param>
        /// <param name="roles">User roles</param>
        /// <param name="permissions">Explicit permissions list</param>
        /// <param name="tenantId">Optional tenant/school ID</param>
        string GenerateToken(
            User user,
            IList<string> roles,
            IList<string> permissions,
            Guid? tenantId = null);

        /// <summary>
        /// Generates a cryptographically secure refresh token
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Extracts claims from an expired access token
        /// (used during refresh token flow)
        /// </summary>
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

        /// <summary>
        /// Validates an active access token and returns its claims
        /// </summary>
        ClaimsPrincipal? ValidateToken(string token);
    }
}