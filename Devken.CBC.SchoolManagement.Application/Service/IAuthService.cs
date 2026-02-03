using Devken.CBC.SchoolManagement.Application.Dtos;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IAuthService
    {
        // School Registration & Login
        Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request);
        Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null);
        Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> LogoutAsync(string refreshToken);

        // ✅ FIXED: Single ChangePassword method with nullable Guid? for tenantId
        Task<AuthResult> ChangePasswordAsync(Guid userId, Guid? tenantId, ChangePasswordRequest request);

        // Super Admin
        Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest request);
        Task<RefreshTokenResponse?> SuperAdminRefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> SuperAdminLogoutAsync(string refreshToken);
    }
}