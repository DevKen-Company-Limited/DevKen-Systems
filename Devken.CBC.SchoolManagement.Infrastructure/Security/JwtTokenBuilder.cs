using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Devken.CBC.SchoolManagement.Infrastructure.Security
{
    public static class JwtTokenBuilder
    {
        public static string BuildToken(
            JwtSettings settings,
            IEnumerable<Claim> claims)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(settings.SecretKey));

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(settings.AccessTokenLifetimeMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
