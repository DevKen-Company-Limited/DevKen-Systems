using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
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

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings?.Value
                ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        // =========================================================
        // ACCESS TOKEN GENERATION
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
                // Identity
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("user_id", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Profile
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name,
                    $"{user.FirstName} {user.LastName}".Trim()),

                new Claim("email", user.Email ?? string.Empty),
                new Claim("full_name",
                    $"{user.FirstName} {user.LastName}".Trim()),

                // SuperAdmin flag
                new Claim("is_super_admin",
                    isSuperAdmin.ToString().ToLowerInvariant())
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
            // Roles
            // -----------------------------------------------------

            foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // -----------------------------------------------------
            // Permissions
            // -----------------------------------------------------

            foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("permission", permission));
            }

            // Optional JSON blob (frontend convenience)
            if (permissions.Any())
            {
                claims.Add(new Claim(
                    "permissions",
                    JsonSerializer.Serialize(permissions)));
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

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
    }
}
