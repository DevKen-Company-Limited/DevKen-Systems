using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
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
    // FORMATIVE ASSESSMENT — Repository Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class FormativeAssessmentRepository : IFormativeAssessmentRepository
    {
        private readonly AppDbContext _context;

        public FormativeAssessmentRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Base query with all navigations eagerly loaded ──────────────
        private IQueryable<FormativeAssessment> Query(bool trackChanges)
        {
            var q = trackChanges
                ? _context.Set<FormativeAssessment>()
                : _context.Set<FormativeAssessment>().AsNoTracking();

            return q
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .Include(a => a.LearningOutcome);
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<FormativeAssessment>> GetAllAsync(bool trackChanges)
            => await Query(trackChanges)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessment>> GetBySchoolAsync(
            Guid schoolId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TenantId == schoolId)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<FormativeAssessment?> GetByIdAsync(Guid id, bool trackChanges)
            => await Query(trackChanges)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<FormativeAssessment?> GetWithScoresAsync(Guid id, bool trackChanges)
        {
            var q = trackChanges
                ? _context.Set<FormativeAssessment>()
                : _context.Set<FormativeAssessment>().AsNoTracking();

            return await q
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .Include(a => a.LearningOutcome)
                .Include(a => a.Scores)
                    .ThenInclude(s => s.Student)
                .Include(a => a.Scores)
                    .ThenInclude(s => s.GradedBy)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<FormativeAssessment>> GetByClassAsync(
            Guid classId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.ClassId == classId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessment>> GetByTeacherAsync(
            Guid teacherId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TeacherId == teacherId)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessment>> GetByTermAsync(
            Guid termId, Guid academicYearId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TermId == termId && a.AcademicYearId == academicYearId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessment>> GetByLearningOutcomeAsync(
            Guid learningOutcomeId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.LearningOutcomeId == learningOutcomeId)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessment>> GetPublishedAsync(
            Guid classId, Guid termId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.ClassId == classId && a.TermId == termId && a.IsPublished)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        // ── Write ───────────────────────────────────────────────────────

        public void Create(FormativeAssessment assessment)
            => _context.Set<FormativeAssessment>().Add(assessment);

        public void Update(FormativeAssessment assessment)
            => _context.Set<FormativeAssessment>().Update(assessment);

        public void Delete(FormativeAssessment assessment)
            => _context.Set<FormativeAssessment>().Remove(assessment);
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT SCORE — Repository Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class FormativeAssessmentScoreRepository : IFormativeAssessmentScoreRepository
    {
        private readonly AppDbContext _context;

        public FormativeAssessmentScoreRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Base query ──────────────────────────────────────────────────
        private IQueryable<FormativeAssessmentScore> Query(bool trackChanges)
        {
            var q = trackChanges
                ? _context.FormativeAssessmentScores
                : _context.FormativeAssessmentScores.AsNoTracking();

            return q
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .Include(s => s.FormativeAssessment);
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<FormativeAssessmentScore>> GetAllByAssessmentAsync(
            Guid assessmentId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.FormativeAssessmentId == assessmentId)
                .OrderBy(s => s.Student.LastName)
                .ThenBy(s => s.Student.FirstName)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessmentScore>> GetByStudentAsync(
            Guid studentId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<FormativeAssessmentScore>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.StudentId == studentId
                         && s.FormativeAssessment != null
                         && s.FormativeAssessment.TermId == termId)
                .OrderByDescending(s => s.CreatedOn)
                .ToListAsync();

        public async Task<FormativeAssessmentScore?> GetByIdAsync(Guid id, bool trackChanges)
            => await Query(trackChanges)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<FormativeAssessmentScore?> GetByStudentAndAssessmentAsync(
            Guid studentId, Guid assessmentId, bool trackChanges)
            => await Query(trackChanges)
                .FirstOrDefaultAsync(s => s.StudentId == studentId
                                       && s.FormativeAssessmentId == assessmentId);

        public async Task<IEnumerable<FormativeAssessmentScore>> GetSubmittedByAssessmentAsync(
            Guid assessmentId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.FormativeAssessmentId == assessmentId && s.IsSubmitted)
                .OrderBy(s => s.Student.LastName)
                .ToListAsync();

        // ── Write ───────────────────────────────────────────────────────

        public void Create(FormativeAssessmentScore score)
            => _context.FormativeAssessmentScores.Add(score);

        public void Update(FormativeAssessmentScore score)
            => _context.FormativeAssessmentScores.Update(score);

        public void Delete(FormativeAssessmentScore score)
            => _context.FormativeAssessmentScores.Remove(score);

        public void DeleteRange(IEnumerable<FormativeAssessmentScore> scores)
            => _context.FormativeAssessmentScores.RemoveRange(scores);
    }
}