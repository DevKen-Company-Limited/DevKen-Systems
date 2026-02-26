// ─────────────────────────────────────────────────────────────────────────────
// DIFF / ADDITION for IRepositoryManager.cs
// Add the following property to the "FINANCE" section:
// ─────────────────────────────────────────────────────────────────────────────
//
//   // ================= FINANCE =================
//   IFeeItemRepository      FeeItem      { get; }
//   IFeeStructureRepository FeeStructure { get; }   // ← ADD THIS LINE
//
// Full updated interface shown below for reference.
// ─────────────────────────────────────────────────────────────────────────────

using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
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
        IGradeRepository Grade { get; }

        // ================= CBC CURRICULUM =================
        ILearningAreaRepository LearningArea { get; }
        IStrandRepository Strand { get; }
        ISubStrandRepository SubStrand { get; }
        ILearningOutcomeRepository LearningOutcome { get; }

        // ================= FINANCE =================
        IFeeItemRepository FeeItem { get; }
        IFeeStructureRepository FeeStructure { get; }   // ← NEW

        // ================= ASSESSMENTS =================
        IFormativeAssessmentRepository FormativeAssessment { get; }
        ISummativeAssessmentRepository SummativeAssessment { get; }
        ICompetencyAssessmentRepository CompetencyAssessment { get; }
        IFormativeAssessmentScoreRepository FormativeAssessmentScore { get; }
        ISummativeAssessmentScoreRepository SummativeAssessmentScore { get; }
        ICompetencyAssessmentScoreRepository CompetencyAssessmentScore { get; }

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

        // ================= ADVANCED =================
        /// <summary>Exposes the underlying DbContext for advanced scenarios.</summary>
        DbContext Context { get; }

        // ================= UNIT OF WORK =================
        Task SaveAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}