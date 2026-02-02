using Devken.CBC.SchoolManagement.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IAuthService
    {
        Task<RegisterSchoolResponse?> RegisterSchoolAsync(RegisterSchoolRequest request);
        Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null);
        Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> LogoutAsync(string refreshToken);
        Task<AuthResult> ChangePasswordAsync(Guid userId, Guid tenantId, ChangePasswordRequest request);
        Task<SuperAdminLoginResponse?> SuperAdminLoginAsync(SuperAdminLoginRequest request);
    }
}
