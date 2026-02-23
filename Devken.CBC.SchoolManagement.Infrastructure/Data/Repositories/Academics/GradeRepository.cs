using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academics
{
    public class GradeRepository : RepositoryBase<Grade, Guid>, IGradeRepository
    {
        public GradeRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        /// <summary>Returns all grades across all tenants (SuperAdmin only).</summary>
        public async Task<IEnumerable<Grade>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .Include(g => g.Term)
                .OrderByDescending(g => g.AssessmentDate)
                .ToListAsync();

        /// <summary>Returns all grades for a specific tenant/school.</summary>
        public async Task<IEnumerable<Grade>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(g => g.TenantId == tenantId, trackChanges)
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .Include(g => g.Term)
                .OrderByDescending(g => g.AssessmentDate)
                .ToListAsync();

        /// <summary>Returns all grades for a specific student within a tenant.</summary>
        public async Task<IEnumerable<Grade>> GetByStudentIdAsync(
            Guid studentId, Guid tenantId, bool trackChanges) =>
            await FindByCondition(
                    g => g.StudentId == studentId && g.TenantId == tenantId, trackChanges)
                .Include(g => g.Subject)
                .Include(g => g.Term)
                .OrderByDescending(g => g.AssessmentDate)
                .ToListAsync();

        /// <summary>Returns all grades for a specific subject within a tenant.</summary>
        public async Task<IEnumerable<Grade>> GetBySubjectIdAsync(
            Guid subjectId, Guid tenantId, bool trackChanges) =>
            await FindByCondition(
                    g => g.SubjectId == subjectId && g.TenantId == tenantId, trackChanges)
                .Include(g => g.Student)
                .Include(g => g.Term)
                .OrderByDescending(g => g.AssessmentDate)
                .ToListAsync();

        /// <summary>Returns all grades for a specific term within a tenant.</summary>
        public async Task<IEnumerable<Grade>> GetByTermIdAsync(
            Guid termId, Guid tenantId, bool trackChanges) =>
            await FindByCondition(
                    g => g.TermId == termId && g.TenantId == tenantId, trackChanges)
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .OrderByDescending(g => g.AssessmentDate)
                .ToListAsync();

        /// <summary>Returns a grade by ID with all navigation properties loaded.</summary>
        public async Task<Grade?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(g => g.Id == id, trackChanges)
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .Include(g => g.Term)
                .Include(g => g.Assessment)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Checks whether a grade already exists for the same student/subject/term combination,
        /// optionally excluding a specific grade ID (used during update).
        /// </summary>
        public async Task<bool> ExistsByStudentSubjectTermAsync(
            Guid studentId, Guid subjectId, Guid? termId, Guid? excludeId = null) =>
            await FindByCondition(
                    g => g.StudentId == studentId &&
                         g.SubjectId == subjectId &&
                         g.TermId == termId &&
                         (excludeId == null || g.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();
    }
}