using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Application.Dtos
{
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
        UserInfo User
    );

    // ── USER INFO / DTO ───────────────────────────────────
    public record UserInfo(
        Guid Id,                        // User unique identifier
        Guid TenantId,                  // Tenant (school) identifier
        string Email,                   // User email
        string FullName,                // User full name
        string[] Roles,                 // User roles
        string[] Permissions,           // User permissions
        bool RequirePasswordChange      // Indicates if password must be changed
    );

    public record UserDto(
        Guid Id,
        string Email,
        string FullName,
        Guid TenantId,
        string SchoolName,
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

    // ── CHANGE PASSWORD ────────────────────────────────────
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

    public class RegisterSchoolResponseDto
    {
        public string AccessToken { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string RefreshToken { get; set; }
        public UserDto User { get; set; }
    }

    public class LoginResponseDto
    {
        public string AccessToken { get; set; }
        public int ExpiresInSeconds { get; set; }
        public string RefreshToken { get; set; }
        public UserDto User { get; set; }
        public string Message { get; set; }
    }


    public record SuperAdminDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName
    );

    // ── CREATE USER ───────────────────────────────────────
    public record CreateUserDto(
        string Email,
        string? FirstName,
        string? LastName,
        string TemporaryPassword,
        Guid? RoleId = null
    );

    // ── RESULT WRAPPER ────────────────────────────────────
    public record AuthResult(bool Success, string? Error = null);
}
