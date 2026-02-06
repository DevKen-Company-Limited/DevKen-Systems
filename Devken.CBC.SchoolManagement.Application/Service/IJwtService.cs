using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    /// <summary>
    /// Contract for JWT access & refresh token operations
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT access token for a user, including roles,
        /// permissions, and optional tenant/school context.
        /// </summary>
        /// <param name="user">Authenticated user</param>
        /// <param name="roles">User roles</param>
        /// <param name="permissions">User permissions</param>
        /// <param name="tenantId">
        /// Optional tenant/school ID (overrides user's default tenant)
        /// </param>
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
