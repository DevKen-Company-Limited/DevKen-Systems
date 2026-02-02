
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
            _settings = settings;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new("tenant_id", user.TenantId.ToString()),
                new("user_id", user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email)
            };

            foreach (var ur in user.UserRoles)
                claims.Add(new Claim(ClaimTypes.Role, ur.Role?.Name ?? ""));

            var permissionKeys = user.UserRoles
                .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
                .Select(rp => rp.Permission?.Key)
                .Where(k => k != null)
                .Distinct();

            foreach (var perm in permissionKeys)
                claims.Add(new Claim("permission", perm!));

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
            var claims = new List<Claim>
            {
                new("user_id", admin.Id.ToString()),
                new(ClaimTypes.Email, admin.Email),
                new(ClaimTypes.Role, "SuperAdmin"),
                new("is_super_admin", "true")
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

        public string GenerateRefreshTokenString() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
