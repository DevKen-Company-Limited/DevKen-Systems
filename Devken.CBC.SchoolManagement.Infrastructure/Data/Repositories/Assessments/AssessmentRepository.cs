using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Assessments
{
    /// <summary>
    /// Concrete implementation of <see cref="IAssessmentRepository"/>.
    /// Inherits generic CRUD from <see cref="RepositoryBase{T,TId}"/> and adds
    /// assessment-specific query methods.
    /// </summary>
    public class AssessmentRepository : RepositoryBase<Assessment1, Guid>, IAssessmentRepository
    {
        public AssessmentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        // ── Custom queries ────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Returns every non-deleted assessment regardless of tenant.
        /// Intended for SuperAdmin use only — the controller enforces that restriction.
        /// </remarks>
        public async Task<IEnumerable<Assessment1>> GetAllAsync(bool trackChanges = false)
        {
            var query = FindAll(trackChanges)
                .Include(a => a.Subject)
                .Include(a => a.Teacher)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .OrderByDescending(a => a.AssessmentDate);

            return await query.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Assessment1>> GetBySchoolAsync(
            Guid schoolId, bool trackChanges = false)
        {
            var query = FindByCondition(a => a.TenantId == schoolId, trackChanges)
                .Include(a => a.Subject)
                .Include(a => a.Teacher)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .OrderByDescending(a => a.AssessmentDate);

            return await query.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Assessment1>> GetByClassAsync(
            Guid classId, bool trackChanges = false)
        {
            var query = FindByCondition(a => a.ClassId == classId, trackChanges)
                .Include(a => a.Subject)
                .Include(a => a.Teacher)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .OrderByDescending(a => a.AssessmentDate);

            return await query.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Assessment1>> GetByTeacherAsync(
            Guid teacherId, bool trackChanges = false)
        {
            var query = FindByCondition(a => a.TeacherId == teacherId, trackChanges)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear)
                .OrderByDescending(a => a.AssessmentDate);

            return await query.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Assessment1>> GetByTermAsync(
            Guid termId, Guid academicYearId, bool trackChanges = false)
        {
            var query = FindByCondition(
                    a => a.TermId == termId && a.AcademicYearId == academicYearId,
                    trackChanges)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Teacher)
                .OrderByDescending(a => a.AssessmentDate);

            return await query.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Assessment1?> GetWithGradesAsync(
            Guid assessmentId, bool trackChanges = false)
        {
            var query = FindByCondition(a => a.Id == assessmentId, trackChanges)
                .Include(a => a.Grades)
                .Include(a => a.Subject)
                .Include(a => a.Teacher)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .Include(a => a.AcademicYear);

            return await query.FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Assessment1>> GetPublishedAsync(
            Guid classId, Guid termId, bool trackChanges = false)
        {
            var query = FindByCondition(
                    a => a.ClassId == classId
                      && a.TermId == termId
                      && a.IsPublished,
                    trackChanges)
                .Include(a => a.Subject)
                .Include(a => a.Teacher)
                .Include(a => a.AcademicYear)
                .OrderByDescending(a => a.PublishedDate);

            return await query.ToListAsync();
        }
    }
}