using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academic
{
    public class ParentRepository : RepositoryBase<Parent, Guid>, IParentRepository
    {
        public ParentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        /// <summary>
        /// Returns all parents for a tenant with Students eagerly loaded.
        /// No filtering or sorting here — that lives in ParentService,
        /// matching the SubjectRepository pattern.
        /// </summary>
        public async Task<IEnumerable<Parent>> GetByTenantIdAsync(
    Guid tenantId, bool trackChanges) =>
    await FindByCondition(p => p.TenantId == tenantId && p.Status != EntityStatus.Deleted, trackChanges)
        .Include(p => p.Students)
        .OrderBy(p => p.LastName)
        .ThenBy(p => p.FirstName)
        .ToListAsync();

        /// <summary>
        /// Returns a single parent with their linked Students eagerly loaded.
        /// Tenant is enforced by the global query filter on the context;
        /// the service validates tenant ownership after loading.
        /// </summary>
        public async Task<Parent?> GetWithStudentsAsync(
             Guid id, bool trackChanges) =>
             await FindByCondition(p => p.Id == id && p.Status != EntityStatus.Deleted, trackChanges)
                 .Include(p => p.Students)
                 .FirstOrDefaultAsync();

        /// <summary>
        /// Returns all parents linked to a specific student within a tenant.
        /// Uses the Students join collection rather than a separate FK,
        /// matching how the Parent–Student many-to-many is modelled.
        /// </summary>
        public async Task<IEnumerable<Parent>> GetByStudentIdAsync(
            Guid studentId, Guid tenantId, bool trackChanges) =>
            await FindByCondition(
                    p => p.TenantId == tenantId && p.Status != EntityStatus.Deleted, trackChanges)
                .Include(p => p.Students)
                .Where(p => p.Students.Any(s => s.Id == studentId))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

        /// <summary>
        /// Checks whether a National ID is already in use within a tenant,
        /// optionally excluding the parent being updated (to allow keeping the same value).
        /// </summary>
        public async Task<bool> NationalIdExistsAsync(
            string nationalId, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    p => p.TenantId == tenantId &&
                            p.Status != EntityStatus.Deleted &&   // ← add this
                            p.NationalIdNumber != null &&
                            p.NationalIdNumber == nationalId &&
                            (excludeId == null || p.Id != excludeId.Value),
                    trackChanges: false)
                .AnyAsync();
    }
}