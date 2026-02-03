// ──────────────────────────────────────────────────────────────
// DTOs for Authentication & School Management
// ──────────────────────────────────────────────────────────────

using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Application.Dtos
{
    // ── LOGIN ───────────────────────────────────────────────
    public record LoginRequest(
        string TenantSlug,      // identifies the school
        string Email,
        string Password
    );

    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int AccessTokenExpiresInSeconds,
        UserInfo User
    );

    public record UserInfo(
        Guid Id,
        Guid TenantId,
        string Email,
        string FullName,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        bool RequirePasswordChange // ✨ added this field
    );

    // ── REFRESH TOKEN ─────────────────────────────────────
    public record RefreshTokenRequest(string RefreshToken);

    public record RefreshTokenResponse(
        string AccessToken,
        string RefreshToken,
        int AccessTokenExpiresInSeconds
    );

    // ── REGISTER SCHOOL (First-time setup) ────────────────
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

    public record UserDto(
        Guid Id,
        string Email,
        string FullName,
        Guid SchoolId,
        School School1,
        string SchoolName,
        IEnumerable<string> Roles,
        IEnumerable<string> Permissions,
        bool RequirePasswordChange // ✨ added this field
    );

    public class RefreshTokenRequestDto
    {
        public string Token { get; set; } = default!;
    }

    // ── SUPER ADMIN LOGIN ───────────────────────────────
    public record SuperAdminLoginRequest(string Email, string Password);

    public record SuperAdminLoginResponse(
        string AccessToken,
        int AccessTokenExpiresInSeconds,
        SuperAdminDto User,
        string[] Roles,
        List<string> Permissions,
        string RefreshToken
    );

    public record SuperAdminDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName
    );

    // ── CHANGE PASSWORD ────────────────────────────────
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    // ── RESULT WRAPPER ────────────────────────────────
    public record AuthResult(bool Success, string? Error = null);

    // ── CREATE USER ───────────────────────────────────
    public record CreateUserDto(
        string Email,
        string? FirstName,
        string? LastName,
        string TemporaryPassword,
        Guid? RoleId = null
    );
}
