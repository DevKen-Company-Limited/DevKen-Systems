using Devken.CBC.SchoolManagement.Application.Service;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    /// <summary>
    /// Provides access to current authenticated user information via JWT claims
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // =============================
        // Core Context
        // =============================

        public ClaimsPrincipal? ClaimsPrincipal =>
            _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated =>
            ClaimsPrincipal?.Identity?.IsAuthenticated ?? false;

        // =============================
        // Identity
        // =============================

        public Guid? UserId
        {
            get
            {
                var value =
                    GetClaimValue(JwtRegisteredClaimNames.Sub) ??
                    GetClaimValue("user_id");

                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public string? Email =>
            GetClaimValue(ClaimTypes.Email) ??
            GetClaimValue(JwtRegisteredClaimNames.Email);

        public string? FullName =>
            GetClaimValue(ClaimTypes.Name) ??
            GetClaimValue("full_name");

        // =============================
        // Tenant / School
        // =============================

        public Guid? TenantId
        {
            get
            {
                var value = GetClaimValue("tenant_id");
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public Guid? SchoolId => TenantId;

        // =============================
        // Roles & Permissions
        // =============================

        public IEnumerable<string> Roles =>
            ClaimsPrincipal?
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct()
            ?? Enumerable.Empty<string>();

        public IEnumerable<string> Permissions =>
            ClaimsPrincipal?
                .FindAll("permission")
                .Select(c => c.Value)
                .Distinct()
            ?? Enumerable.Empty<string>();

        // =============================
        // Role Checks
        // =============================

        public bool IsSuperAdmin =>
            IsInRole("SuperAdmin");

        public bool IsSchoolAdmin =>
            IsInRole("SchoolAdmin");

        public bool IsTeacher =>
            IsInRole("Teacher");

        public bool IsParent =>
            IsInRole("Parent");

        public bool IsInRole(string role) =>
            Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

        // =============================
        // Permission Checks
        // =============================

        public bool HasPermission(string permission) =>
            Permissions.Any(p =>
                p.Equals(permission, StringComparison.OrdinalIgnoreCase));

        public bool HasAnyPermission(params string[] permissions) =>
            permissions.Any(HasPermission);

        public bool HasAllPermissions(params string[] permissions) =>
            permissions.All(HasPermission);

        // =============================
        // Claim Helpers
        // =============================

        public string? GetClaimValue(string claimType) =>
            ClaimsPrincipal?.FindFirst(claimType)?.Value;
    }
}
