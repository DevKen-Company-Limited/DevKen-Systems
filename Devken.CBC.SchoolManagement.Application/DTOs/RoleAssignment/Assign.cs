using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment
{
    /// <summary>
    /// Request to assign a role to a user
    /// </summary>
    public class AssignRoleRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }
    }

    /// <summary>
    /// Request to assign multiple roles to a user
    /// </summary>
    public class AssignMultipleRolesRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one role must be specified")]
        public List<Guid> RoleIds { get; set; } = new();
    }

    /// <summary>
    /// Request to remove a role from a user
    /// </summary>
    public class RemoveRoleRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }
    }

    /// <summary>
    /// Request to update all roles for a user (replace existing)
    /// </summary>
    public class UpdateUserRolesRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public List<Guid> RoleIds { get; set; } = new();
    }

    /// <summary>
    /// Response containing user role information
    /// </summary>
    public class UserRoleResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? RoleDescription { get; set; }
        public bool IsSystemRole { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response containing complete user with roles
    /// </summary>
    public class UserWithRolesResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public Guid? TenantId { get; set; }
        public List<RoleInfoDto> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    /// <summary>
    /// Role information DTO
    /// </summary>
    public class RoleInfoDto
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public int PermissionCount { get; set; }
    }

    /// <summary>
    /// Response for role assignment operation
    /// </summary>
    public class RoleAssignmentResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UserWithRolesResponse? User { get; set; }
        public List<string>? Errors { get; set; }
    }

    /// <summary>
    /// Request to get users by role
    /// </summary>
    public class GetUsersByRoleRequest
    {
        [Required]
        public Guid RoleId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Response containing users with a specific role
    /// </summary>
    public class UsersInRoleResponse
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public List<UserBasicInfo> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>
    /// Basic user information
    /// </summary>
    public class UserBasicInfo
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
