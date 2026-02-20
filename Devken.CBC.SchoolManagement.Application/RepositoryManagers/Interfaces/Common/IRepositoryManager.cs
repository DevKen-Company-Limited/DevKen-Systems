using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Payments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.UserActivities1;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common
{
    public interface IRepositoryManager
    {
        // ================= ACADEMIC =================
        IStudentRepository Student { get; }
        ITeacherRepository Teacher { get; }
        ISchoolRepository School { get; }
        IAcademicYearRepository AcademicYear { get; }
        ITermRepository Term { get; }
        IClassRepository Class { get; }
        ISubjectRepository Subject { get; }
        IUserActivityRepository UserActivity { get; }

        /// <summary>
        /// Exposes the underlying DbContext for advanced scenarios like execution strategy.
        /// </summary>
        DbContext Context { get; }

        // ================= ASSESSMENTS =================
        IAssessmentRepository Assessment { get; }

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

        // ================= NUMBER SERIES =================
        IDocumentNumberSeriesRepository DocumentNumberSeries { get; }

        // ================= UNIT OF WORK =================
        Task SaveAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}