//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;

//namespace Devken.CBC.SchoolManagement.Application.Dtos
//{
//    #region Create User

//    public class CreateUserRequest
//    {
//        [Required]
//        [EmailAddress]
//        [MaxLength(256)]
//        public string Email { get; set; } = null!;

//        [Required]
//        [MinLength(2)]
//        [MaxLength(100)]
//        public string FirstName { get; set; } = null!;

//        [Required]
//        [MinLength(2)]
//        [MaxLength(100)]
//        public string LastName { get; set; } = null!;

//        [Phone]
//        [MaxLength(20)]
//        public string? PhoneNumber { get; set; }

//        /// <summary>
//        /// Required for SuperAdmin, ignored for regular admins
//        /// </summary>
//        public Guid? SchoolId { get; set; }

//        /// <summary>
//        /// List of role IDs to assign to the user
//        /// </summary>
//        public List<Guid>? RoleIds { get; set; }

//        /// <summary>
//        /// If true, user must change password on first login
//        /// </summary>
//        public bool RequirePasswordChange { get; set; } = true;

//        /// <summary>
//        /// If not provided, a temporary password will be generated
//        /// </summary>
//        [MinLength(8)]
//        public string? TemporaryPassword { get; set; }
//    }

//    #endregion

//    #region Update User

//    public class UpdateUserRequest
//    {
//        [EmailAddress]
//        [MaxLength(256)]
//        public string? Email { get; set; }

//        [MinLength(2)]
//        [MaxLength(100)]
//        public string? FirstName { get; set; }

//        [MinLength(2)]
//        [MaxLength(100)]
//        public string? LastName { get; set; }

//        [Phone]
//        [MaxLength(20)]
//        public string? PhoneNumber { get; set; }
//        public List<string> RoleIds { get; set; } = new();
//        public string? ProfileImageUrl { get; set; }

//        public bool? IsActive { get; set; }
//    }

//    #endregion

//    #region Assign Roles

//    public class AssignRolesRequest
//    {
//        [Required]
//        [MinLength(1)]
//        public List<Guid> RoleIds { get; set; } = new();

//    }

//    #endregion

//    #region User Response DTOs

//    public class UserManagementDto
//    {
//        public Guid Id { get; set; }
//        public string Email { get; set; } = null!;
//        public string FirstName { get; set; } = null!;
//        public string LastName { get; set; } = null!;
//        public string FullName { get; set; } = null!;
//        public string? PhoneNumber { get; set; }
//        public string? ProfileImageUrl { get; set; }
//        public Guid TenantId { get; set; }
//        public string? SchoolName { get; set; }
//        public bool IsActive { get; set; }
//        public bool IsEmailVerified { get; set; }
//        public bool RequirePasswordChange { get; set; }
//        public bool IsLockedOut { get; set; }
//        public DateTime? LockedUntil { get; set; }
//        public List<RoleDto> Roles { get; set; } = new();
//        public List<string> Permissions { get; set; } = new();
//        public DateTime CreatedOn { get; set; }
//        public DateTime UpdatedOn { get; set; }
//    }

//    public class UserListDto
//    {
//        public List<UserManagementDto> Users { get; set; } = new();
//        public int TotalCount { get; set; }
//        public int Page { get; set; }
//        public int PageSize { get; set; }
//        public int TotalPages { get; set; }
//    }

//    public class RoleDto
//    {
//        public Guid Id { get; set; }
//        public string Name { get; set; } = null!;
//        public string? Description { get; set; }
//    }

//    public class CreateUserResponseDto
//    {
//        public Guid Id { get; set; }
//        public string Email { get; set; } = null!;
//        public string FullName { get; set; } = null!;
//        public string TemporaryPassword { get; set; } = null!;
//        public bool RequirePasswordChange { get; set; }
//    }

//    public class ResetPasswordResponseDto
//    {
//        public string TemporaryPassword { get; set; } = null!;
//        public string Message { get; set; } = null!;
//    }

//    #endregion

//    #region Service Result

//    public class ServiceResult<T>
//    {
//        public bool Success { get; set; }
//        public T? Data { get; set; }
//        public string? Error { get; set; }

//        public static ServiceResult<T> SuccessResult(T data) => new()
//        {
//            Success = true,
//            Data = data
//        };

//        public static ServiceResult<T> FailureResult(string error) => new()
//        {
//            Success = false,
//            Error = error
//        };
//    }

//    #endregion
//}