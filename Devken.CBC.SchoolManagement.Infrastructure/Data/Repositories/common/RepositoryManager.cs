using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Curriculum;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Payments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Tenant;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.UserActivities1;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academics;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Curriculum;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.NumberSeries;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Payments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Tenant;
using Devken.CBC.SchoolManagement.Infrastructure.RepositoryManagers.UserActivities;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Common
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly AppDbContext _context;
        private readonly TenantContext _tenantContext;

        // ── Academic ─────────────────────────────────────────────────────────
        private readonly Lazy<IStudentRepository> _studentRepository;
        private readonly Lazy<ITeacherRepository> _teacherRepository;
        private readonly Lazy<ISchoolRepository> _schoolRepository;
        private readonly Lazy<IAcademicYearRepository> _academicYearRepository;
        private readonly Lazy<ITermRepository> _termRepository;
        private readonly Lazy<IClassRepository> _classRepository;
        private readonly Lazy<ISubjectRepository> _subjectRepository;
        private readonly Lazy<IUserActivityRepository> _userActivityRepository;

        // ── CBC Curriculum ───────────────────────────────────────────────────
        private readonly Lazy<ILearningAreaRepository> _learningAreaRepository;
        private readonly Lazy<IStrandRepository> _strandRepository;
        private readonly Lazy<ISubStrandRepository> _subStrandRepository;
        private readonly Lazy<ILearningOutcomeRepository> _learningOutcomeRepository;

        // ── Assessments ──────────────────────────────────────────────────────
        private readonly Lazy<IFormativeAssessmentRepository> _formativeAssessmentRepository;
        private readonly Lazy<ISummativeAssessmentRepository> _summativeAssessmentRepository;
        private readonly Lazy<ICompetencyAssessmentRepository> _competencyAssessmentRepository;
        private readonly Lazy<IFormativeAssessmentScoreRepository> _formativeScoreRepository;
        private readonly Lazy<ISummativeAssessmentScoreRepository> _summativeScoreRepository;
        private readonly Lazy<ICompetencyAssessmentScoreRepository> _competencyScoreRepository;

        // ── Identity ─────────────────────────────────────────────────────────
        private readonly Lazy<IUserRepository> _userRepository;
        private readonly Lazy<IRoleRepository> _roleRepository;
        private readonly Lazy<IPermissionRepository> _permissionRepository;
        private readonly Lazy<IRolePermissionRepository> _rolePermissionRepository;
        private readonly Lazy<IUserRoleRepository> _userRoleRepository;
        private readonly Lazy<IRefreshTokenRepository> _refreshTokenRepository;
        private readonly Lazy<ISuperAdminRepository> _superAdminRepository;

        // ── Number Series ────────────────────────────────────────────────────
        private readonly Lazy<IDocumentNumberSeriesRepository> _documentNumberSeriesRepository;

        // ── Payments ─────────────────────────────────────────────────────────
        private readonly Lazy<IMpesaPaymentRepository> _mpesaPaymentRepository;

        public RepositoryManager(AppDbContext context, TenantContext tenantContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));

            // Academic
            _studentRepository = new Lazy<IStudentRepository>(() => new StudentRepository(_context, _tenantContext));
            _teacherRepository = new Lazy<ITeacherRepository>(() => new TeacherRepository(_context, _tenantContext));
            _schoolRepository = new Lazy<ISchoolRepository>(() => new SchoolRepository(_context, _tenantContext));
            _academicYearRepository = new Lazy<IAcademicYearRepository>(() => new AcademicYearRepository(_context, _tenantContext));
            _termRepository = new Lazy<ITermRepository>(() => new TermRepository(_context, _tenantContext));
            _classRepository = new Lazy<IClassRepository>(() => new ClassRepository(_context, _tenantContext));
            _subjectRepository = new Lazy<ISubjectRepository>(() => new SubjectRepository(_context, _tenantContext));
            _userActivityRepository = new Lazy<IUserActivityRepository>(() => new UserActivityRepository(_context, _tenantContext));

            // CBC Curriculum
            _learningAreaRepository = new Lazy<ILearningAreaRepository>(() => new LearningAreaRepository(_context, _tenantContext));
            _strandRepository = new Lazy<IStrandRepository>(() => new StrandRepository(_context, _tenantContext));
            _subStrandRepository = new Lazy<ISubStrandRepository>(() => new SubStrandRepository(_context, _tenantContext));
            _learningOutcomeRepository = new Lazy<ILearningOutcomeRepository>(() => new LearningOutcomeRepository(_context, _tenantContext));

            // Assessments
            _formativeAssessmentRepository = new Lazy<IFormativeAssessmentRepository>(() => new FormativeAssessmentRepository(_context, _tenantContext));
            _summativeAssessmentRepository = new Lazy<ISummativeAssessmentRepository>(() => new SummativeAssessmentRepository(_context, _tenantContext));
            _competencyAssessmentRepository = new Lazy<ICompetencyAssessmentRepository>(() => new CompetencyAssessmentRepository(_context, _tenantContext));
            _formativeScoreRepository = new Lazy<IFormativeAssessmentScoreRepository>(() => new FormativeAssessmentScoreRepository(_context, _tenantContext));
            _summativeScoreRepository = new Lazy<ISummativeAssessmentScoreRepository>(() => new SummativeAssessmentScoreRepository(_context, _tenantContext));
            _competencyScoreRepository = new Lazy<ICompetencyAssessmentScoreRepository>(() => new CompetencyAssessmentScoreRepository(_context, _tenantContext));

            // Identity
            _userRepository = new Lazy<IUserRepository>(() => new UserRepository(_context, _tenantContext));
            _roleRepository = new Lazy<IRoleRepository>(() => new RoleRepository(_context, _tenantContext));
            _permissionRepository = new Lazy<IPermissionRepository>(() => new PermissionRepository(_context, _tenantContext));
            _rolePermissionRepository = new Lazy<IRolePermissionRepository>(() => new RolePermissionRepository(_context, _tenantContext));
            _userRoleRepository = new Lazy<IUserRoleRepository>(() => new UserRoleRepository(_context, _tenantContext));
            _refreshTokenRepository = new Lazy<IRefreshTokenRepository>(() => new RefreshTokenRepository(_context, _tenantContext));
            _superAdminRepository = new Lazy<ISuperAdminRepository>(() => new SuperAdminRepository(_context, _tenantContext));

            // Number Series
            _documentNumberSeriesRepository = new Lazy<IDocumentNumberSeriesRepository>(() => new DocumentNumberService(_context, _tenantContext, this));

            // Payments
            _mpesaPaymentRepository = new Lazy<IMpesaPaymentRepository>(() => new MpesaPaymentRepository(_context, _tenantContext));
        }

        // ── Academic Properties ──────────────────────────────────────────────
        public IStudentRepository Student => _studentRepository.Value;
        public ITeacherRepository Teacher => _teacherRepository.Value;
        public ISchoolRepository School => _schoolRepository.Value;
        public IAcademicYearRepository AcademicYear => _academicYearRepository.Value;
        public ITermRepository Term => _termRepository.Value;
        public IClassRepository Class => _classRepository.Value;
        public ISubjectRepository Subject => _subjectRepository.Value;
        public IUserActivityRepository UserActivity => _userActivityRepository.Value;
        public DbContext Context => _context;

        // ── CBC Curriculum Properties ────────────────────────────────────────
        public ILearningAreaRepository LearningArea => _learningAreaRepository.Value;
        public IStrandRepository Strand => _strandRepository.Value;
        public ISubStrandRepository SubStrand => _subStrandRepository.Value;
        public ILearningOutcomeRepository LearningOutcome => _learningOutcomeRepository.Value;

        // ── Assessment Properties ────────────────────────────────────────────
        public IFormativeAssessmentRepository FormativeAssessment => _formativeAssessmentRepository.Value;
        public ISummativeAssessmentRepository SummativeAssessment => _summativeAssessmentRepository.Value;
        public ICompetencyAssessmentRepository CompetencyAssessment => _competencyAssessmentRepository.Value;
        public IFormativeAssessmentScoreRepository FormativeAssessmentScore => _formativeScoreRepository.Value;
        public ISummativeAssessmentScoreRepository SummativeAssessmentScore => _summativeScoreRepository.Value;
        public ICompetencyAssessmentScoreRepository CompetencyAssessmentScore => _competencyScoreRepository.Value;

        // ── Identity Properties ──────────────────────────────────────────────
        public IUserRepository User => _userRepository.Value;
        public IRoleRepository Role => _roleRepository.Value;
        public IPermissionRepository Permission => _permissionRepository.Value;
        public IRolePermissionRepository RolePermission => _rolePermissionRepository.Value;
        public IUserRoleRepository UserRole => _userRoleRepository.Value;
        public IRefreshTokenRepository RefreshToken => _refreshTokenRepository.Value;
        public ISuperAdminRepository SuperAdmin => _superAdminRepository.Value;

        // ── Payment Properties ───────────────────────────────────────────────
        public IMpesaPaymentRepository MpesaPayment => _mpesaPaymentRepository.Value;

        // ── Number Series Properties ─────────────────────────────────────────
        public IDocumentNumberSeriesRepository DocumentNumberSeries => _documentNumberSeriesRepository.Value;

        // ── Unit of Work ─────────────────────────────────────────────────────
        public async Task SaveAsync() => await _context.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync() =>
            await _context.Database.BeginTransactionAsync();
    }
}