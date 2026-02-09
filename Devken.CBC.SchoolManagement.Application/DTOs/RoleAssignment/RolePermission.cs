using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Application.DTOs.RolePermission
{

    public class UserWithPermissionDto
    {
        /// <summary>
        /// User ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's full name
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// List of role names that grant this permission to the user
        /// </summary>
        public List<string> RoleNames { get; set; } = new List<string>();
    }
    /// <summary>
    /// Permission data transfer object
    /// </summary>
    public class PermissionDto
    {
        /// <summary>
        /// Permission ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Permission key (unique identifier)
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the permission
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Group name for organizing permissions
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// Description of what the permission allows
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indicates if this permission is currently assigned to a role (context-dependent)
        /// </summary>
        public bool IsAssigned { get; set; }

        /// <summary>
        /// Number of users who have this permission (through their roles)
        /// </summary>
        public int UserCount { get; set; }
    }

    /// <summary>
    /// Role with its assigned permissions
    /// </summary>
    public class RoleWithPermissionsDto
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public Guid? TenantId { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new();
        public int TotalPermissions { get; set; }
    }

    /// <summary>
    /// Result of role permission operations
    /// </summary>
    public class RolePermissionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public RoleWithPermissionsDto? Data { get; set; }

        public static RolePermissionResult Successful(string message, RoleWithPermissionsDto? data = null)
        {
            return new RolePermissionResult
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static RolePermissionResult Failed(string message)
        {
            return new RolePermissionResult
            {
                Success = false,
                Message = message,
                Errors = new List<string> { message }
            };
        }

        public static RolePermissionResult Failed(List<string> errors)
        {
            return new RolePermissionResult
            {
                Success = false,
                Message = "Operation failed",
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Request to update role permissions
    /// </summary>
    public class UpdateRolePermissionsRequest
    {
        public Guid RoleId { get; set; }
        public List<Guid> PermissionIds { get; set; } = new();
        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// Request to add permissions to role
    /// </summary>
    public class AddPermissionsRequest
    {
        public Guid RoleId { get; set; }
        public List<Guid> PermissionIds { get; set; } = new();
        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// Request to remove permission from role
    /// </summary>
    public class RemovePermissionRequest
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// Request to clone permissions between roles
    /// </summary>
    public class ClonePermissionsRequest
    {
        public Guid SourceRoleId { get; set; }
        public Guid TargetRoleId { get; set; }
        public Guid? TenantId { get; set; }
    }
}