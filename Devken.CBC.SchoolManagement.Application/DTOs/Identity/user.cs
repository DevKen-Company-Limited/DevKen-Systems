using System;

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
        UserDto User
    );

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public int ExpiresInSeconds { get; set; }
        public string RefreshToken { get; set; } = default!;
        public UserDto User { get; set; } = default!;
        public string Message { get; set; } = default!;
    }

    // ── USER INFO / DTO ───────────────────────────────────
    public record UserInfo(
        Guid Id,
        Guid TenantId,
        string Email,
        string FullName,
        string[] Roles,
        string[] Permissions,
        bool RequirePasswordChange
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

    // ── CREATE USER ───────────────────────────────────────
    public record CreateUserDto(
        string Email,
        string? FirstName,
        string? LastName,
        string TemporaryPassword,
        Guid? RoleId = null
    );

    // ── RESULT WRAPPER ───────────────────────────────────
    public record AuthResult(bool Success, string? Error = null);
}
