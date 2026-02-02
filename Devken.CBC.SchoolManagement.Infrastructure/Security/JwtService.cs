using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _settings;
        private readonly SigningCredentials _signingCredentials;

        public JwtService(JwtSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string GenerateAccessToken(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var claims = new List<Claim>
            {
                new Claim("user_id", user.Id.ToString()),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty)
            };

            // Add roles
            if (user.UserRoles != null)
            {
                foreach (var ur in user.UserRoles)
                {
                    if (!string.IsNullOrWhiteSpace(ur.Role?.Name))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));
                    }
                }
            }

            // Add permissions
            if (user.UserRoles != null)
            {
                var permissions = user.UserRoles
                    .Where(ur => ur.Role?.RolePermissions != null)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Where(rp => !string.IsNullOrWhiteSpace(rp.Permission?.Key))
                    .Select(rp => rp.Permission.Key)
                    .Distinct()
                    .ToList();

                foreach (var perm in permissions)
                {
                    claims.Add(new Claim("permissions", perm));
                }
            }

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenLifetimeMinutes),
                signingCredentials: _signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateSuperAdminAccessToken(SuperAdmin admin)
        {
            if (admin == null)
                throw new ArgumentNullException(nameof(admin));

            var claims = new List<Claim>
            {
                new Claim("user_id", admin.Id.ToString()),
                new Claim(ClaimTypes.Email, admin.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, admin.FirstName ?? string.Empty),
                new Claim(ClaimTypes.Role, "SuperAdmin"),
                new Claim("is_super_admin", "true")
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenLifetimeMinutes),
                signingCredentials: _signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshTokenString()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }
    }
}