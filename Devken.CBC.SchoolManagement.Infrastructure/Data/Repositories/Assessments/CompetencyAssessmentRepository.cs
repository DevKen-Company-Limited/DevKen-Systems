using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Repositories.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT — Repository Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class CompetencyAssessmentRepository : ICompetencyAssessmentRepository
    {
        private readonly AppDbContext _context;

        public CompetencyAssessmentRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Base query with standard navigations ────────────────────────
        private IQueryable<CompetencyAssessment> Query(bool trackChanges)
        {
            var q = trackChanges
                ? _context.Set<CompetencyAssessment>()
                : _context.Set<CompetencyAssessment>().AsNoTracking();

            return q
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear);
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<CompetencyAssessment>> GetAllAsync(bool trackChanges)
            => await Query(trackChanges)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessment>> GetBySchoolAsync(
            Guid schoolId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TenantId == schoolId)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<CompetencyAssessment?> GetByIdAsync(Guid id, bool trackChanges)
            => await Query(trackChanges)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<CompetencyAssessment?> GetWithScoresAsync(Guid id, bool trackChanges)
        {
            var q = trackChanges
                ? _context.Set<CompetencyAssessment>()
                : _context.Set<CompetencyAssessment>().AsNoTracking();

            return await q
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .Include(a => a.Scores)
                    .ThenInclude(s => s.Student)
                .Include(a => a.Scores)
                    .ThenInclude(s => s.Assessor)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<CompetencyAssessment>> GetByClassAsync(
            Guid classId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.ClassId == classId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessment>> GetByTeacherAsync(
            Guid teacherId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TeacherId == teacherId)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessment>> GetByTermAsync(
            Guid termId, Guid academicYearId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TermId == termId && a.AcademicYearId == academicYearId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessment>> GetByCompetencyNameAsync(
            string competencyName, Guid schoolId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TenantId == schoolId
                         && a.CompetencyName.ToLower().Contains(competencyName.ToLower()))
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessment>> GetByTargetLevelAsync(
            CBCLevel level, Guid schoolId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TenantId == schoolId && a.TargetLevel == level)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessment>> GetByStrandAsync(
            string strand, Guid schoolId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.TenantId == schoolId
                         && a.Strand != null
                         && a.Strand.ToLower() == strand.ToLower())
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessment>> GetPublishedAsync(
            Guid classId, Guid termId, bool trackChanges)
            => await Query(trackChanges)
                .Where(a => a.ClassId == classId && a.TermId == termId && a.IsPublished)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

        // ── Write ───────────────────────────────────────────────────────

        public void Create(CompetencyAssessment assessment)
            => _context.Set<CompetencyAssessment>().Add(assessment);

        public void Update(CompetencyAssessment assessment)
            => _context.Set<CompetencyAssessment>().Update(assessment);

        public void Delete(CompetencyAssessment assessment)
            => _context.Set<CompetencyAssessment>().Remove(assessment);
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT SCORE — Repository Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class CompetencyAssessmentScoreRepository : ICompetencyAssessmentScoreRepository
    {
        private readonly AppDbContext _context;

        public CompetencyAssessmentScoreRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ── Base query ──────────────────────────────────────────────────
        private IQueryable<CompetencyAssessmentScore> Query(bool trackChanges)
        {
            var q = trackChanges
                ? _context.CompetencyAssessmentScores
                : _context.CompetencyAssessmentScores.AsNoTracking();

            return q
                .Include(s => s.Student)
                .Include(s => s.Assessor)
                .Include(s => s.CompetencyAssessment);
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetAllByAssessmentAsync(
            Guid assessmentId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.CompetencyAssessmentId == assessmentId)
                .OrderBy(s => s.Student.LastName)
                .ThenBy(s => s.Student.FirstName)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetByStudentAsync(
            Guid studentId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.AssessmentDate)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.StudentId == studentId
                         && s.CompetencyAssessment != null
                         && s.CompetencyAssessment.TermId == termId)
                .OrderByDescending(s => s.AssessmentDate)
                .ToListAsync();

        public async Task<CompetencyAssessmentScore?> GetByIdAsync(Guid id, bool trackChanges)
            => await Query(trackChanges)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<CompetencyAssessmentScore?> GetByStudentAndAssessmentAsync(
            Guid studentId, Guid assessmentId, bool trackChanges)
            => await Query(trackChanges)
                .FirstOrDefaultAsync(s => s.StudentId == studentId
                                       && s.CompetencyAssessmentId == assessmentId);

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetFinalizedByAssessmentAsync(
            Guid assessmentId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.CompetencyAssessmentId == assessmentId && s.IsFinalized)
                .OrderBy(s => s.Student.LastName)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetByRatingAsync(
            Guid assessmentId, string rating, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.CompetencyAssessmentId == assessmentId
                         && s.Rating.ToLower() == rating.ToLower())
                .OrderBy(s => s.Student.LastName)
                .ToListAsync();

        public async Task<IEnumerable<CompetencyAssessmentScore>> GetByAssessorAsync(
            Guid assessorId, bool trackChanges)
            => await Query(trackChanges)
                .Where(s => s.AssessorId == assessorId)
                .OrderByDescending(s => s.AssessmentDate)
                .ToListAsync();

        // ── Write ───────────────────────────────────────────────────────

        public void Create(CompetencyAssessmentScore score)
            => _context.CompetencyAssessmentScores.Add(score);

        public void Update(CompetencyAssessmentScore score)
            => _context.CompetencyAssessmentScores.Update(score);

        public void Delete(CompetencyAssessmentScore score)
            => _context.CompetencyAssessmentScores.Remove(score);

        public void DeleteRange(IEnumerable<CompetencyAssessmentScore> scores)
            => _context.CompetencyAssessmentScores.RemoveRange(scores);
    }
}