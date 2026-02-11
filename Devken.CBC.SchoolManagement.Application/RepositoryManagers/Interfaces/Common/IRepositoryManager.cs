using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Payments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common
{
    public interface IRepositoryManager
    {
        // ================= ACADEMIC =================
        IStudentRepository Student { get; }
        ISchoolRepository School { get; }
        // In IRepositoryManager.cs - add this property:
        IAcademicYearRepository AcademicYear { get; }

        // ================= IDENTITY =================
        IUserRepository User { get; }
        IRoleRepository Role { get; }
        IPermissionRepository Permission { get; }
        IRolePermissionRepository RolePermission { get; }
        IUserRoleRepository UserRole { get; }
        IRefreshTokenRepository RefreshToken { get; }
        ISuperAdminRepository SuperAdmin { get; }

        // ================= PAYMENTS =================
        IMpesaPaymentRepository MpesaPayment { get; }

        // ================= UNIT OF WORK =================
        Task SaveAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
