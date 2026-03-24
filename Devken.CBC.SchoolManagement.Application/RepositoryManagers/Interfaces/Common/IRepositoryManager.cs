using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments;
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
        IParentRepository Parent { get; }
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
        IFeeStructureRepository FeeStructure { get; }
        IInvoiceRepository Invoice { get; }
        IInvoiceItemRepository InvoiceItem { get; }

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
        // ================= LIBRARY =================
        IBookAuthorRepository BookAuthor { get; }
        IBookCategoryRepository BookCategory { get; }
        IBookPublisherRepository BookPublisher { get; }
        IBookRepository Book { get; }
        ILibraryBranchRepository LibraryBranch { get; }
        IBookCopyRepository BookCopy { get; }
        IBookInventoryRepository BookInventory { get; }
        IBookReservationRepository BookReservation { get; }
        ILibraryMemberRepository LibraryMember { get; }
        IBookBorrowRepository BookBorrow { get; }
        IBookBorrowItemRepository BookBorrowItem { get; }
        ILibraryFineRepository LibraryFine { get; }

        // ================= PAYMENTS =================
        IMpesaPaymentRepository MpesaPayment { get; }
        IPaymentRepository Payment { get; }

        // ================= NUMBER SERIES =================
        IDocumentNumberSeriesRepository DocumentNumberSeries { get; }

        // ================= ADVANCED =================
        DbContext Context { get; }

        // ================= UNIT OF WORK =================
        Task SaveAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}