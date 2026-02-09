using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IRepositoryManager _repository;

        public JwtService(
            IOptions<JwtSettings> jwtSettings,
            IRepositoryManager repository)
        {
            _jwtSettings = jwtSettings?.Value
                ?? throw new ArgumentNullException(nameof(jwtSettings));
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        // =========================================================
        // ACCESS TOKEN GENERATION (WITH AUTOMATIC PERMISSION AGGREGATION)
        // =========================================================

        /// <summary>
        /// Generates token with automatic permission aggregation from all user roles
        /// </summary>
        public async Task<string> GenerateTokenAsync(
            User user,
            IList<string> roles,
            Guid? tenantId = null)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            roles ??= new List<string>();

            // -----------------------------------------------------
            // SuperAdmin detection
            // -----------------------------------------------------

            var isSuperAdmin = roles.Any(r =>
                r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));

            // -----------------------------------------------------
            // Get combined permissions from all roles
            // -----------------------------------------------------

            List<string> permissions;

            if (isSuperAdmin)
            {
                // SuperAdmin gets ALL permissions
                permissions = PermissionKeys.AllPermissions.ToList();
            }
            else
            {
                // Aggregate permissions from all user's roles
                permissions = await GetCombinedPermissionsFromRolesAsync(
                    user.Id,
                    roles,
                    tenantId ?? user.TenantId);
            }

            // Generate token with aggregated permissions
            return GenerateToken(user, roles, permissions, tenantId);
        }

        // =========================================================
        // ACCESS TOKEN GENERATION (WITH EXPLICIT PERMISSIONS)
        // =========================================================

        public string GenerateToken(
            User user,
            IList<string> roles,
            IList<string> permissions,
            Guid? tenantId = null)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            roles ??= new List<string>();
            permissions ??= new List<string>();

            // -----------------------------------------------------
            // SuperAdmin detection (authoritative)
            // -----------------------------------------------------

            var isSuperAdmin =
                roles.Any(r => r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));

            // -----------------------------------------------------
            // Normalize SuperAdmin context
            // -----------------------------------------------------

            if (isSuperAdmin)
            {
                roles = new List<string> { "SuperAdmin" };

                // 🔥 SuperAdmin must ALWAYS have all permissions
                permissions = PermissionKeys.AllPermissions.ToList();

                // 🔥 SuperAdmin must NEVER be tenant-bound
                tenantId = null;
            }

            // -----------------------------------------------------
            // Base claims
            // -----------------------------------------------------

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // standard
                new Claim("user_id", user.Id.ToString()),                   // our main NameClaim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Profile
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()), // full name
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", user.Email ?? string.Empty),
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", $"{user.FirstName} {user.LastName}".Trim()),

                new Claim("email", user.Email ?? string.Empty),
                new Claim("full_name", $"{user.FirstName} {user.LastName}".Trim()),

                // SuperAdmin flag
                new Claim("is_super_admin", isSuperAdmin.ToString().ToLowerInvariant())
            };

            if (!string.IsNullOrWhiteSpace(user.FirstName))
                claims.Add(new Claim("first_name", user.FirstName));

            if (!string.IsNullOrWhiteSpace(user.LastName))
                claims.Add(new Claim("last_name", user.LastName));

            // -----------------------------------------------------
            // Tenant context (school users ONLY)
            // -----------------------------------------------------

            if (!isSuperAdmin)
            {
                var effectiveTenantId =
                    tenantId ??
                    (user.TenantId != Guid.Empty ? user.TenantId : (Guid?)null);

                if (effectiveTenantId.HasValue)
                {
                    claims.Add(new Claim("tenant_id", effectiveTenantId.Value.ToString()));
                    claims.Add(new Claim("school_id", effectiveTenantId.Value.ToString()));
                }
            }

            // -----------------------------------------------------
            // Roles (remove duplicates, case-insensitive)
            // -----------------------------------------------------

            var uniqueRoles = roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var role in uniqueRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role));
            }

            // -----------------------------------------------------
            // Permissions (remove duplicates, case-insensitive)
            // -----------------------------------------------------

            var uniquePermissions = permissions
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p) // Optional: alphabetical order
                .ToList();

            foreach (var permission in uniquePermissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            // Optional JSON blob (frontend convenience)
            if (uniquePermissions.Any())
            {
                claims.Add(new Claim(
                    "permissions",
                    JsonSerializer.Serialize(uniquePermissions)));
            }

            // -----------------------------------------------------
            // Token creation
            // -----------------------------------------------------

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(_jwtSettings.AccessTokenLifetime),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================================================
        // PERMISSION AGGREGATION HELPER
        // =========================================================

        /// <summary>
        /// Combines permissions from multiple roles and removes duplicates.
        /// Fetches permissions from the database for all provided role names.
        /// </summary>
        private async Task<List<string>> GetCombinedPermissionsFromRolesAsync(
            Guid userId,
            IList<string> roleNames,
            Guid? tenantId)
        {
            if (roleNames == null || !roleNames.Any())
                return new List<string>();

            // Get role IDs for the given role names
            var roleIds = await _repository.Role
                .FindByCondition(r => roleNames.Contains(r.Name ?? string.Empty), false)
                .Select(r => r.Id)
                .ToListAsync();

            if (!roleIds.Any())
                return new List<string>();

            // Get all permissions from all roles in one query
            var permissions = await _repository.RolePermission
                .FindByCondition(rp => roleIds.Contains(rp.RoleId), false)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission != null && !string.IsNullOrWhiteSpace(rp.Permission.Key))
                .Select(rp => rp.Permission!.Key!)
                .Distinct() // Remove duplicates at database level
                .OrderBy(p => p) // Optional: order alphabetically
                .ToListAsync();

            return permissions;
        }

        // =========================================================
        // REFRESH TOKEN
        // =========================================================

        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        // =========================================================
        // TOKEN VALIDATION (EXPIRED – REFRESH FLOW)
        // =========================================================

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            var parameters = GetTokenValidationParameters(validateLifetime: false);

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        // =========================================================
        // TOKEN VALIDATION (ACTIVE TOKEN)
        // =========================================================

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(
                    token,
                    GetTokenValidationParameters(validateLifetime: true),
                    out var securityToken);

                if (securityToken is not JwtSecurityToken jwt ||
                    !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // SHARED VALIDATION CONFIG
        // =========================================================

        private TokenValidationParameters GetTokenValidationParameters(bool validateLifetime) =>
         new()
         {
             ValidateIssuerSigningKey = true,
             IssuerSigningKey = new SymmetricSecurityKey(
                 Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
             ValidateIssuer = true,
             ValidIssuer = _jwtSettings.Issuer,
             ValidateAudience = true,
             ValidAudience = _jwtSettings.Audience,
             ValidateLifetime = validateLifetime,
             ClockSkew = TimeSpan.Zero,
             NameClaimType = "user_id",  // <-- Use the actual GUID claim
             RoleClaimType = ClaimTypes.Role
         };
    }
}