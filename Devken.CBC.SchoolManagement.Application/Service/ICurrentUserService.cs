using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    /// <summary>
    /// Service to get current user information from JWT claims
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the current user ID from "user_id" claim
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Gets the current user's email
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Gets the current user's full name
        /// </summary>
        string? FullName { get; }

        /// <summary>
        /// Gets the current tenant ID from "tenant_id" claim
        /// </summary>
        Guid? TenantId { get; }

        /// <summary>
        /// Gets the current school ID (same as TenantId for school users)
        /// </summary>
        Guid? SchoolId { get; }

        /// <summary>
        /// Gets the current user's roles from Role claims
        /// </summary>
        IEnumerable<string> Roles { get; }

        /// <summary>
        /// Gets the current user's permissions from "permissions" claim
        /// </summary>
        IEnumerable<string> Permissions { get; }

        /// <summary>
        /// Gets the current user's claims
        /// </summary>
        ClaimsPrincipal? ClaimsPrincipal { get; }

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Checks if the current user is a super admin
        /// </summary>
        bool IsSuperAdmin { get; }

        /// <summary>
        /// Checks if the current user is a school admin
        /// </summary>
        bool IsSchoolAdmin { get; }

        /// <summary>
        /// Checks if the current user is a teacher
        /// </summary>
        bool IsTeacher { get; }

        /// <summary>
        /// Checks if the current user is a parent
        /// </summary>
        bool IsParent { get; }

        /// <summary>
        /// Checks if the current user has a specific role
        /// </summary>
        bool IsInRole(string role);

        /// <summary>
        /// Checks if the current user has a specific permission
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// Checks if the current user has any of the specified permissions
        /// </summary>
        bool HasAnyPermission(params string[] permissions);

        /// <summary>
        /// Checks if the current user has all of the specified permissions
        /// </summary>
        bool HasAllPermissions(params string[] permissions);

        /// <summary>
        /// Gets a specific claim value
        /// </summary>
        string? GetClaimValue(string claimType);
    }
}
