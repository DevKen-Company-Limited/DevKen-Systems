using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Assessments
{
    // ─────────────────────────────────────────────────────────────────────────
    // FORMATIVE ASSESSMENT REPOSITORY
    // ─────────────────────────────────────────────────────────────────────────
    public class FormativeAssessmentRepository
        : RepositoryBase<FormativeAssessment, Guid>, IFormativeAssessmentRepository
    {
        public FormativeAssessmentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<FormativeAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId, Guid? teacherId,
            bool? isPublished, bool trackChanges = false)
        {
            var query = FindAll(trackChanges)
                .Include(f => f.Teacher)
                .Include(f => f.Subject)
                .Include(f => f.Class)
                .Include(f => f.Term)
                .Include(f => f.AcademicYear)
                .AsQueryable();

            if (classId.HasValue) query = query.Where(f => f.ClassId == classId);
            if (termId.HasValue) query = query.Where(f => f.TermId == termId);
            if (subjectId.HasValue) query = query.Where(f => f.SubjectId == subjectId);
            if (teacherId.HasValue) query = query.Where(f => f.TeacherId == teacherId);
            if (isPublished.HasValue) query = query.Where(f => f.IsPublished == isPublished);

            return await query
                .OrderByDescending(f => f.AssessmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Standard tenant-filtered fetch. Use for regular tenant users.
        /// </summary>
        public async Task<FormativeAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false)
            => await FindByCondition(f => f.Id == id, trackChanges)
                .Include(f => f.Teacher)
                .Include(f => f.Subject)
                .Include(f => f.Class)
                .Include(f => f.Term)
                .Include(f => f.AcademicYear)
                .Include(f => f.LearningOutcome)
                .Include(f => f.Scores).ThenInclude(s => s.Student)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Bypasses the global tenant query filter. Safe to use for SuperAdmin reads
        /// where TenantContext may not match the entity's TenantId.
        /// </summary>
        public async Task<FormativeAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false)
        {
            var query = _context.Set<FormativeAssessment>()
                .IgnoreQueryFilters()
                .Include(f => f.Teacher)
                .Include(f => f.Subject)
                .Include(f => f.Class)
                .Include(f => f.Term)
                .Include(f => f.AcademicYear)
                .Include(f => f.LearningOutcome)
                .Include(f => f.Scores).ThenInclude(s => s.Student)
                .Where(f => f.Id == id);

            return trackChanges
                ? await query.FirstOrDefaultAsync()
                : await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<FormativeAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false)
            => await FindByCondition(
                    f => f.ClassId == classId && f.TermId == termId, trackChanges)
                .Include(f => f.Subject)
                .Include(f => f.Teacher)
                .OrderByDescending(f => f.AssessmentDate)
                .ToListAsync();

        public async Task<bool> IsPublishedAsync(Guid id)
            => await FindByCondition(f => f.Id == id, false)
                .Select(f => f.IsPublished)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Loads all navigation properties directly on the tracked entity,
        /// bypassing the tenant query filter. Safe to call after Create + SaveAsync
        /// when the TenantContext may not match the entity's TenantId (e.g. SuperAdmin).
        /// </summary>
        public async Task LoadNavigationsAsync(FormativeAssessment entity)
        {
            var entry = _context.Entry(entity);
            await entry.Reference(e => e.Teacher).LoadAsync();
            await entry.Reference(e => e.Subject).LoadAsync();
            await entry.Reference(e => e.Class).LoadAsync();
            await entry.Reference(e => e.Term).LoadAsync();
            await entry.Reference(e => e.AcademicYear).LoadAsync();
            await entry.Reference(e => e.LearningOutcome).LoadAsync();
            // Scores is empty on a brand-new assessment — no need to load
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SUMMATIVE ASSESSMENT REPOSITORY
    // ─────────────────────────────────────────────────────────────────────────
    public class SummativeAssessmentRepository
        : RepositoryBase<SummativeAssessment, Guid>, ISummativeAssessmentRepository
    {
        public SummativeAssessmentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<SummativeAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId, Guid? teacherId,
            bool? isPublished, bool trackChanges = false)
        {
            var query = FindAll(trackChanges)
                .Include(s => s.Teacher)
                .Include(s => s.Subject)
                .Include(s => s.Class)
                .Include(s => s.Term)
                .Include(s => s.AcademicYear)
                .AsQueryable();

            if (classId.HasValue) query = query.Where(s => s.ClassId == classId);
            if (termId.HasValue) query = query.Where(s => s.TermId == termId);
            if (subjectId.HasValue) query = query.Where(s => s.SubjectId == subjectId);
            if (teacherId.HasValue) query = query.Where(s => s.TeacherId == teacherId);
            if (isPublished.HasValue) query = query.Where(s => s.IsPublished == isPublished);

            return await query
                .OrderByDescending(s => s.AssessmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Standard tenant-filtered fetch. Use for regular tenant users.
        /// </summary>
        public async Task<SummativeAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false)
            => await FindByCondition(s => s.Id == id, trackChanges)
                .Include(s => s.Teacher)
                .Include(s => s.Subject)
                .Include(s => s.Class)
                .Include(s => s.Term)
                .Include(s => s.AcademicYear)
                .Include(s => s.Scores).ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Bypasses the global tenant query filter. Safe to use for SuperAdmin reads
        /// where TenantContext may not match the entity's TenantId.
        /// </summary>
        public async Task<SummativeAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false)
        {
            var query = _context.Set<SummativeAssessment>()
                .IgnoreQueryFilters()
                .Include(s => s.Teacher)
                .Include(s => s.Subject)
                .Include(s => s.Class)
                .Include(s => s.Term)
                .Include(s => s.AcademicYear)
                .Include(s => s.Scores).ThenInclude(sc => sc.Student)
                .Where(s => s.Id == id);

            return trackChanges
                ? await query.FirstOrDefaultAsync()
                : await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SummativeAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false)
            => await FindByCondition(
                    s => s.ClassId == classId && s.TermId == termId, trackChanges)
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .OrderByDescending(s => s.AssessmentDate)
                .ToListAsync();

        public async Task<bool> IsPublishedAsync(Guid id)
            => await FindByCondition(s => s.Id == id, false)
                .Select(s => s.IsPublished)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Loads all navigation properties directly on the tracked entity,
        /// bypassing the tenant query filter. Safe to call after Create + SaveAsync
        /// when the TenantContext may not match the entity's TenantId (e.g. SuperAdmin).
        /// </summary>
        public async Task LoadNavigationsAsync(SummativeAssessment entity)
        {
            var entry = _context.Entry(entity);
            await entry.Reference(e => e.Teacher).LoadAsync();
            await entry.Reference(e => e.Subject).LoadAsync();
            await entry.Reference(e => e.Class).LoadAsync();
            await entry.Reference(e => e.Term).LoadAsync();
            await entry.Reference(e => e.AcademicYear).LoadAsync();
            // Scores is empty on a brand-new assessment — no need to load
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMPETENCY ASSESSMENT REPOSITORY
    // ─────────────────────────────────────────────────────────────────────────
    public class CompetencyAssessmentRepository
        : RepositoryBase<CompetencyAssessment, Guid>, ICompetencyAssessmentRepository
    {
        public CompetencyAssessmentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<CompetencyAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId, Guid? teacherId,
            bool? isPublished, bool trackChanges = false)
        {
            var query = FindAll(trackChanges)
                .Include(c => c.Teacher)
                .Include(c => c.Subject)
                .Include(c => c.Class)
                .Include(c => c.Term)
                .Include(c => c.AcademicYear)
                .AsQueryable();

            if (classId.HasValue) query = query.Where(c => c.ClassId == classId);
            if (termId.HasValue) query = query.Where(c => c.TermId == termId);
            if (subjectId.HasValue) query = query.Where(c => c.SubjectId == subjectId);
            if (teacherId.HasValue) query = query.Where(c => c.TeacherId == teacherId);
            if (isPublished.HasValue) query = query.Where(c => c.IsPublished == isPublished);

            return await query
                .OrderByDescending(c => c.AssessmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Standard tenant-filtered fetch. Use for regular tenant users.
        /// </summary>
        public async Task<CompetencyAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false)
            => await FindByCondition(c => c.Id == id, trackChanges)
                .Include(c => c.Teacher)
                .Include(c => c.Subject)
                .Include(c => c.Class)
                .Include(c => c.Term)
                .Include(c => c.AcademicYear)
                .Include(c => c.Scores).ThenInclude(s => s.Student)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Bypasses the global tenant query filter. Safe to use for SuperAdmin reads
        /// where TenantContext may not match the entity's TenantId.
        /// </summary>
        public async Task<CompetencyAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false)
        {
            var query = _context.Set<CompetencyAssessment>()
                .IgnoreQueryFilters()
                .Include(c => c.Teacher)
                .Include(c => c.Subject)
                .Include(c => c.Class)
                .Include(c => c.Term)
                .Include(c => c.AcademicYear)
                .Include(c => c.Scores).ThenInclude(s => s.Student)
                .Where(c => c.Id == id);

            return trackChanges
                ? await query.FirstOrDefaultAsync()
                : await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CompetencyAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false)
            => await FindByCondition(
                    c => c.ClassId == classId && c.TermId == termId, trackChanges)
                .Include(c => c.Subject)
                .Include(c => c.Teacher)
                .OrderByDescending(c => c.AssessmentDate)
                .ToListAsync();

        public async Task<bool> IsPublishedAsync(Guid id)
            => await FindByCondition(c => c.Id == id, false)
                .Select(c => c.IsPublished)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Loads all navigation properties directly on the tracked entity,
        /// bypassing the tenant query filter. Safe to call after Create + SaveAsync
        /// when the TenantContext may not match the entity's TenantId (e.g. SuperAdmin).
        /// </summary>
        public async Task LoadNavigationsAsync(CompetencyAssessment entity)
        {
            var entry = _context.Entry(entity);
            await entry.Reference(e => e.Teacher).LoadAsync();
            await entry.Reference(e => e.Subject).LoadAsync();
            await entry.Reference(e => e.Class).LoadAsync();
            await entry.Reference(e => e.Term).LoadAsync();
            await entry.Reference(e => e.AcademicYear).LoadAsync();
            // Scores is empty on a brand-new assessment — no need to load
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE REPOSITORIES
    // ─────────────────────────────────────────────────────────────────────────
    public class FormativeAssessmentScoreRepository
        : RepositoryBase<FormativeAssessmentScore, Guid>, IFormativeAssessmentScoreRepository
    {
        public FormativeAssessmentScoreRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<FormativeAssessmentScore>> GetByAssessmentAsync(
            Guid assessmentId, bool trackChanges = false)
            => await FindByCondition(s => s.FormativeAssessmentId == assessmentId, trackChanges)
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .OrderBy(s => s.Student.LastName)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, Guid? termId = null, bool trackChanges = false)
        {
            var query = FindByCondition(s => s.StudentId == studentId, trackChanges)
                .Include(s => s.FormativeAssessment)
                .AsQueryable();

            if (termId.HasValue)
                query = query.Where(s => s.FormativeAssessment.TermId == termId);

            return await query
                .OrderByDescending(s => s.FormativeAssessment.AssessmentDate)
                .ToListAsync();
        }

        public async Task<FormativeAssessmentScore?> GetByAssessmentAndStudentAsync(
            Guid assessmentId, Guid studentId, bool trackChanges = false)
            => await FindByCondition(
                    s => s.FormativeAssessmentId == assessmentId && s.StudentId == studentId,
                    trackChanges)
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .FirstOrDefaultAsync();
    }

    public class SummativeAssessmentScoreRepository
        : RepositoryBase<SummativeAssessmentScore, Guid>, ISummativeAssessmentScoreRepository
    {
        public SummativeAssessmentScoreRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<SummativeAssessmentScore>> GetByAssessmentAsync(
            Guid assessmentId, bool trackChanges = false)
            => await FindByCondition(s => s.SummativeAssessmentId == assessmentId, trackChanges)
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .OrderBy(s => s.PositionInClass)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, Guid? termId = null, bool trackChanges = false)
        {
            var query = FindByCondition(s => s.StudentId == studentId, trackChanges)
                .Include(s => s.SummativeAssessment)
                .AsQueryable();

            if (termId.HasValue)
                query = query.Where(s => s.SummativeAssessment.TermId == termId);

            return await query
                .OrderByDescending(s => s.SummativeAssessment.AssessmentDate)
                .ToListAsync();
        }

        public async Task<SummativeAssessmentScore?> GetByAssessmentAndStudentAsync(
            Guid assessmentId, Guid studentId, bool trackChanges = false)
            => await FindByCondition(
                    s => s.SummativeAssessmentId == assessmentId && s.StudentId == studentId,
                    trackChanges)
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .FirstOrDefaultAsync();
    }

    public class CompetencyAssessmentScoreRepository
        : RepositoryBase<CompetencyAssessmentScore, Guid>, ICompetencyAssessmentScoreRepository
    {
        public CompetencyAssessmentScoreRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetByAssessmentAsync(
            Guid assessmentId, bool trackChanges = false)
            => await FindByCondition(s => s.CompetencyAssessmentId == assessmentId, trackChanges)
                .Include(s => s.Student)
                .Include(s => s.Assessor)
                .OrderBy(s => s.Student.LastName)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetByStudentAsync(
            Guid studentId, Guid? termId = null, bool trackChanges = false)
        {
            var query = FindByCondition(s => s.StudentId == studentId, trackChanges)
                .Include(s => s.CompetencyAssessment)
                .AsQueryable();

            if (termId.HasValue)
                query = query.Where(s => s.CompetencyAssessment.TermId == termId);

            return await query
                .OrderByDescending(s => s.CompetencyAssessment.AssessmentDate)
                .ToListAsync();
        }

        public async Task<CompetencyAssessmentScore?> GetByAssessmentAndStudentAsync(
            Guid assessmentId, Guid studentId, bool trackChanges = false)
            => await FindByCondition(
                    s => s.CompetencyAssessmentId == assessmentId && s.StudentId == studentId,
                    trackChanges)
                .Include(s => s.Student)
                .Include(s => s.Assessor)
                .FirstOrDefaultAsync();
    }
}