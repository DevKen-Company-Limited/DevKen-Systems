using Devken.CBC.SchoolManagement.Application.Dtos;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IAuthService
    {
        // =========================================================
        // SCHOOL REGISTRATION & AUTH
        // =========================================================

        Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request);

        Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            string? ipAddress = null);

        /// <summary>
        /// Refresh access token using refresh token.
        /// MUST rebuild roles, permissions and tenant (school) context from DB.
        /// </summary>
        Task<RefreshTokenResponse?> RefreshTokenAsync(
            RefreshTokenRequest request);

        Task<bool> LogoutAsync(string refreshToken);

        // =========================================================
        // PASSWORD MANAGEMENT
        // =========================================================

        /// <summary>
        /// Change password for a user.
        /// tenantId == schoolId (nullable for SuperAdmin).
        /// </summary>
        Task<AuthResult> ChangePasswordAsync(
            Guid userId,
            Guid? tenantId,
            ChangePasswordRequest request);

        // =========================================================
        // SUPER ADMIN AUTH
        // =========================================================

        Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(
            SuperAdminLoginRequest request);

        /// <summary>
        /// Refresh token for SuperAdmin.
        /// MUST NOT inject tenant_id into JWT.
        /// </summary>
        Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(
            RefreshTokenRequest request);

        Task<bool> SuperAdminLogoutAsync(string refreshToken);
    }
}
