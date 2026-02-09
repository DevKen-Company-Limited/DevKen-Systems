using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Devken.CBC.SchoolManagement.Application.Dtos
{

    public class PasswordResetResultDto
    {
        /// <summary>
        /// The user whose password was reset
        /// </summary>
        public UserDto User { get; set; } = null!;

        /// <summary>
        /// The generated temporary password
        /// </summary>
        public string TemporaryPassword { get; set; } = string.Empty;

        /// <summary>
        /// Instructions or message for the user
        /// </summary>
        public string Message { get; set; } = "Password has been reset. User must change password on next login.";

        /// <summary>
        /// When the password was reset
        /// </summary>
        public DateTime ResetAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who performed the reset
        /// </summary>
        public Guid ResetBy { get; set; }
    }
    // ── USER DTO ────────────────────────────────────────────
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public Guid SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool RequirePasswordChange { get; set; }
        public string? TemporaryPassword { get; set; }
        public List<string> RoleNames { get; set; } = new();
        public List<string> Permissions { get; set; } = new(); // Added for permissions support
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public Guid TenantId { get; set; }

        // Default constructor for object initialization
        public UserDto() { }

        // Constructor for backward compatibility
        public UserDto(
            Guid id,
            string email,
            string fullName,
            Guid schoolId,
            string schoolName,
            string[] roles,
            string[] permissions,
            bool requirePasswordChange)
        {
            Id = id;
            Email = email;

            // Split full name into first and last names
            if (!string.IsNullOrEmpty(fullName))
            {
                var nameParts = fullName.Split(' ', 2);
                FirstName = nameParts[0];
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
            }

            SchoolId = schoolId;
            SchoolName = schoolName;
            TenantId = schoolId; // Assuming SchoolId and TenantId are the same
            RequirePasswordChange = requirePasswordChange;
            RoleNames = roles?.ToList() ?? new List<string>();
            Permissions = permissions?.ToList() ?? new List<string>(); // FIX: Now storing permissions
            IsActive = true;
            IsEmailVerified = true;
            CreatedOn = DateTime.UtcNow;
            UpdatedOn = DateTime.UtcNow;
        }

        // Convenience property for FullName
        [System.Text.Json.Serialization.JsonIgnore]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class PaginatedUsersResponse
    {
        public List<UserDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // ── CREATE USER ───────────────────────────────────────
    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Required for SuperAdmin, ignored for regular admins
        /// </summary>
        public Guid? SchoolId { get; set; }

        /// <summary>
        /// List of role IDs to assign to the user
        /// </summary>
        public List<Guid>? RoleIds { get; set; }

        /// <summary>
        /// If true, user must change password on first login
        /// </summary>
        public bool RequirePasswordChange { get; set; } = true;

        /// <summary>
        /// If not provided, a temporary password will be generated
        /// </summary>
        [MinLength(8)]
        public string? TemporaryPassword { get; set; }
    }

    // Alternative CreateUserDto (record version - simpler, for backward compatibility)
    public record CreateUserDto(
        string Email,
        string? FirstName,
        string? LastName,
        string TemporaryPassword,
        Guid? RoleId = null
    );

    // ── UPDATE USER ───────────────────────────────────────
    public class UpdateUserRequest
    {
        [EmailAddress]
        [MaxLength(256)]
        public string? Email { get; set; }

        [MinLength(2)]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MinLength(2)]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public List<string> RoleIds { get; set; } = new();
        public string? ProfileImageUrl { get; set; }
        public bool? IsActive { get; set; }
    }

    // ── ASSIGN ROLES ──────────────────────────────────────
    public class AssignRolesRequest
    {
        [Required]
        [MinLength(1)]
        public List<Guid> RoleIds { get; set; } = new();
    }

    // ── USER MANAGEMENT DTOs ──────────────────────────────
    public class UserManagementDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public Guid TenantId { get; set; }
        public string? SchoolName { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool RequirePasswordChange { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockedUntil { get; set; }
        public List<RoleDto> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class UserListDto
    {
        public List<UserManagementDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class CreateUserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string TemporaryPassword { get; set; } = null!;
        public bool RequirePasswordChange { get; set; }
    }

    public class ResetPasswordResponseDto
    {
        public string TemporaryPassword { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    // ── REGISTER SCHOOL ────────────────────────────────────
    public record RegisterSchoolRequest(
        string SchoolName,
        string SchoolSlug,
        string SchoolEmail,
        string SchoolPhone,
        string SchoolAddress,
        string AdminEmail,
        string AdminPassword,
        string AdminFullName,
        string? AdminPhone = null
    );

    public record RegisterSchoolResponse(
        Guid SchoolId,
        string AccessToken,
        string RefreshToken,
        UserDto User
    );

    public class RegisterSchoolResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public int ExpiresInSeconds { get; set; }
        public string RefreshToken { get; set; } = default!;
        public UserDto User { get; set; } = default!;
    }

    // ── LOGIN ───────────────────────────────────────────────
    public record LoginRequest(
        string? TenantSlug,
        string Email,
        string Password
    );

    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int AccessTokenExpiresInSeconds,
        UserDto User,
        string[]? Permissions = null // Added for explicit permissions
    );

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public int ExpiresInSeconds { get; set; }
        public string RefreshToken { get; set; } = default!;
        public UserDto User { get; set; } = default!;
        public string Message { get; set; } = default!;
    }

    // ── USER INFO ─────────────────────────────────────────
    public record UserInfo(
        Guid Id,
        Guid TenantId,
        string Email,
        string FullName,
        string[] Roles,
        string[] Permissions,
        bool RequirePasswordChange
    );

    // ── REFRESH TOKEN ─────────────────────────────────────
    public record RefreshTokenRequest(string RefreshToken);

    public record RefreshTokenResponse(
        string AccessToken,
        string RefreshToken,
        int AccessTokenExpiresInSeconds
    );

    public class RefreshTokenRequestDto
    {
        public string Token { get; set; } = default!;
    }

    // ── CHANGE PASSWORD ───────────────────────────────────
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    // ── SUPER ADMIN ───────────────────────────────────────
    public record SuperAdminLoginRequest(string Email, string Password);

    public record SuperAdminLoginResponse(
        string AccessToken,
        int AccessTokenExpiresInSeconds,
        SuperAdminDto User,
        string[] Roles,
        string[] Permissions,
        string RefreshToken
    );

    public record SuperAdminDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName
    );

    // ── SERVICE RESULT ────────────────────────────────────
    //public class ServiceResult<T>
    //{
    //    public bool Success { get; set; }
    //    public T? Data { get; set; }
    //    public string? Error { get; set; }

    //    public static ServiceResult<T> SuccessResult(T data) => new()
    //    {
    //        Success = true,
    //        Data = data
    //    };

    //    public static ServiceResult<T> FailureResult(string error) => new()
    //    {
    //        Success = false,
    //        Error = error
    //    };
    //}

    // ── AUTH RESULT ───────────────────────────────────────
    public record AuthResult(bool Success, string? Error = null);
}