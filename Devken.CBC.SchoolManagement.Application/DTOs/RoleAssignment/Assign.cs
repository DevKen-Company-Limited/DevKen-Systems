using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Application.DTOs.RoleAssignment
{
    #region ===================== Result DTOs =====================

    public class RoleAssignmentResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UserWithRolesDto? User { get; set; }

        public static RoleAssignmentResult Failed(string message) =>
            new() { Success = false, Message = message };

        public static RoleAssignmentResult Successful(string message, UserWithRolesDto? user = null) =>
            new() { Success = true, Message = message, User = user };
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;

        public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 20) =>
            new() { PageNumber = pageNumber, PageSize = pageSize, Items = new List<T>(), TotalCount = 0 };
    }

    #endregion

    #region ===================== User & Role DTOs =====================

    public class UserWithRolesDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid? TenantId { get; set; }
        public List<UserRoleDto> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public bool RequirePasswordChange { get; set; }
        public bool IsSuperAdmin { get; set; }
    }

    public class UserRoleDto
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PermissionCount { get; set; }
        public bool IsSystemRole { get; set; }
        public List<RolePermissionDto>? Permissions { get; set; }
    }

    public class RolePermissionDto
    {
        public Guid PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UserSearchResultDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
    }

    #endregion

    #region ===================== Requests =====================

    public class AssignRoleRequest
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }

    public class AssignMultipleRolesRequest
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; } = new();
    }

    public class RemoveRoleRequest
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }

    public class UpdateUserRolesRequest
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; } = new();
    }

    public class GetUsersByRoleRequest
    {
        [Required]
        public Guid RoleId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    #endregion

    #region ===================== Response DTOs =====================

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

    public class UserWithRolesResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public Guid? TenantId { get; set; }
        public List<RoleInfoDto> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public class RoleInfoDto
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public int PermissionCount { get; set; }
    }

    public class UsersInRoleResponse
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public List<UserBasicInfo> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class UserBasicInfo
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    #endregion
}
