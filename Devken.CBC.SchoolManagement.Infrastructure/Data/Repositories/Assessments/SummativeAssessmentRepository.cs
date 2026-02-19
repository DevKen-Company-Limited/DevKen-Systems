using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Repositories.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT — Repository Implementation
    //
    // SummativeAssessment is a TPH-derived type of Assessment1.
    // AppDbContext exposes only  DbSet<Assessment1> Assessments  (the root).
    // We reach SummativeAssessment rows via  .OfType<SummativeAssessment>()
    // which EF translates to  WHERE AssessmentType = 'Summative'  in SQL.
    //
    // Write operations use  _context.Assessments  (the root DbSet);
    // EF automatically writes the correct discriminator value.
    // ═══════════════════════════════════════════════════════════════════

    public class SummativeAssessmentRepository : ISummativeAssessmentRepository
    {
        private readonly AppDbContext _context;

        public SummativeAssessmentRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Queryable helpers ───────────────────────────────────────────

        /// <summary>
        /// Base IQueryable scoped to SummativeAssessment rows,
        /// with all standard navigation properties eagerly loaded.
        /// </summary>
        private IQueryable<SummativeAssessment> BaseQuery(bool trackChanges)
        {
            var query = _context.Assessments
                .OfType<SummativeAssessment>();

            if (!trackChanges)
                query = query.AsNoTracking();

            return query
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear);
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<SummativeAssessment>> GetAllAsync(bool trackChanges)
            => await BaseQuery(trackChanges)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessment>> GetBySchoolAsync(
            Guid schoolId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(a => a.TenantId == schoolId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<SummativeAssessment?> GetByIdAsync(Guid id, bool trackChanges)
            => await BaseQuery(trackChanges)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<SummativeAssessment?> GetWithScoresAsync(Guid id, bool trackChanges)
        {
            // Separate query so we can chain the extra score ThenIncludes
            // without affecting the lean BaseQuery used everywhere else.
            var query = _context.Assessments
                .OfType<SummativeAssessment>();

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .Include(a => a.Scores)
                    .ThenInclude(s => s.Student)
                .Include(a => a.Scores)
                    .ThenInclude(s => s.GradedBy)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<SummativeAssessment>> GetByClassAsync(
            Guid classId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(a => a.ClassId == classId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessment>> GetByTeacherAsync(
            Guid teacherId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(a => a.TeacherId == teacherId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessment>> GetByTermAsync(
            Guid termId, Guid academicYearId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(a => a.TermId == termId && a.AcademicYearId == academicYearId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessment>> GetByExamTypeAsync(
            string examType, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(a => a.ExamType != null &&
                            a.ExamType.ToLower() == examType.ToLower())
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessment>> GetPublishedAsync(
            Guid classId, Guid termId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(a => a.ClassId == classId
                         && a.TermId == termId
                         && a.IsPublished)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        // ── Write ───────────────────────────────────────────────────────
        // Use the root DbSet<Assessment1>; EF Core automatically writes
        // AssessmentType = 'Summative' because it knows the concrete type.

        public void Create(SummativeAssessment assessment)
            => _context.Assessments.Add(assessment);

        public void Update(SummativeAssessment assessment)
            => _context.Assessments.Update(assessment);

        public void Delete(SummativeAssessment assessment)
            => _context.Assessments.Remove(assessment);
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT SCORE — Repository Implementation
    //
    // SummativeAssessmentScore is NOT part of the TPH Assessment hierarchy.
    // It is stored in its own table and accessed via
    // DbSet<SummativeAssessmentScore> SummativeAssessmentScores on AppDbContext.
    // ═══════════════════════════════════════════════════════════════════

    public class SummativeAssessmentScoreRepository : ISummativeAssessmentScoreRepository
    {
        private readonly AppDbContext _context;

        public SummativeAssessmentScoreRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Queryable helper ────────────────────────────────────────────

        /// <summary>
        /// Base query with all standard navigation properties included.
        /// </summary>
        private IQueryable<SummativeAssessmentScore> BaseQuery(bool trackChanges)
        {
            var query = trackChanges
                ? _context.SummativeAssessmentScores
                : _context.SummativeAssessmentScores.AsNoTracking();

            return query
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .Include(s => s.SummativeAssessment);
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<SummativeAssessmentScore>> GetAllByAssessmentAsync(
            Guid assessmentId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(s => s.SummativeAssessmentId == assessmentId)
                .OrderBy(s => s.PositionInClass)
                    .ThenBy(s => s.Student.LastName)
                    .ThenBy(s => s.Student.FirstName)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.SummativeAssessment!.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<SummativeAssessmentScore>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .Where(s => s.StudentId == studentId
                         && s.SummativeAssessment != null
                         && s.SummativeAssessment.TermId == termId)
                .OrderByDescending(s => s.SummativeAssessment!.AssessmentDate)
                .ToListAsync();

        public async Task<SummativeAssessmentScore?> GetByIdAsync(Guid id, bool trackChanges)
            => await BaseQuery(trackChanges)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<SummativeAssessmentScore?> GetByStudentAndAssessmentAsync(
            Guid studentId, Guid assessmentId, bool trackChanges)
            => await BaseQuery(trackChanges)
                .FirstOrDefaultAsync(s => s.StudentId == studentId
                                       && s.SummativeAssessmentId == assessmentId);

        // ── Write ───────────────────────────────────────────────────────

        public void Create(SummativeAssessmentScore score)
            => _context.SummativeAssessmentScores.Add(score);

        public void Update(SummativeAssessmentScore score)
            => _context.SummativeAssessmentScores.Update(score);

        public void Delete(SummativeAssessmentScore score)
            => _context.SummativeAssessmentScores.Remove(score);

        public void DeleteRange(IEnumerable<SummativeAssessmentScore> scores)
            => _context.SummativeAssessmentScores.RemoveRange(scores);
    }
}