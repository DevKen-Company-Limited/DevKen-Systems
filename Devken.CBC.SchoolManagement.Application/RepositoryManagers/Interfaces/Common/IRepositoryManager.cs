using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Payments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common
{
    public interface IRepositoryManager
    {
        // ================= ACADEMIC =================
        IStudentRepository Student { get; }
        ITeacherRepository Teacher { get; }        // ← NEW
        ISchoolRepository School { get; }
        IAcademicYearRepository AcademicYear { get; }
        IClassRepository Class { get; }


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