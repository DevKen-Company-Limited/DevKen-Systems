using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
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
        /// Returns all non-deleted parents for a tenant.
        /// StudentCount is populated via a separate lightweight COUNT query
        /// to avoid pulling in the full Students collection, which has a
        /// different Status enum type (StudentStatus vs EntityStatus) and
        /// causes EF materialisation failures on Include.
        /// </summary>
        public async Task<IEnumerable<Parent>> GetByTenantIdAsync(
            Guid tenantId, bool trackChanges)
        {
            var parents = await FindByCondition(
                    p => p.TenantId == tenantId && p.Status != EntityStatus.Deleted,
                    trackChanges)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            // Populate student counts via a separate query — avoids the
            // Student.Status string/int conversion conflict on Include
            var parentIds = parents.Select(p => (Guid)p.Id!).ToList();

            var countMap = await _context.Set<Student>()
                .Where(s => s.ParentId.HasValue && parentIds.Contains(s.ParentId.Value))
                .GroupBy(s => s.ParentId!.Value)
                .Select(g => new { ParentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ParentId, x => x.Count);

            foreach (var parent in parents)
            {
                var count = countMap.GetValueOrDefault((Guid)parent.Id!);
                for (var i = 0; i < count; i++)
                    parent.Students.Add(new Student());
            }

            return parents;
        }

        /// <summary>
        /// Returns a single non-deleted parent with Students eagerly loaded.
        /// Used for detail views — full collection is safe here since it's
        /// a single record and StudentCount is derived from Students.Count.
        /// </summary>
        public async Task<Parent?> GetWithStudentsAsync(
            Guid id, bool trackChanges) =>
            await FindByCondition(
                    p => p.Id == id && p.Status != EntityStatus.Deleted,
                    trackChanges)
                .Include(p => p.Students)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Returns all parents linked to a specific student within a tenant.
        /// Filter on Students.Any must come before Include so EF translates
        /// it to SQL rather than evaluating client-side.
        /// </summary>
        public async Task<IEnumerable<Parent>> GetByStudentIdAsync(
            Guid studentId, Guid tenantId, bool trackChanges) =>
            await FindByCondition(
                    p => p.TenantId == tenantId &&
                         p.Status != EntityStatus.Deleted &&
                         p.Students.Any(s => s.Id == studentId),
                    trackChanges)
                .Include(p => p.Students)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

        /// <summary>
        /// Checks whether a National ID is already in use within a tenant,
        /// optionally excluding the parent being updated.
        /// </summary>
        public async Task<bool> NationalIdExistsAsync(
            string nationalId, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    p => p.TenantId == tenantId &&
                         p.Status != EntityStatus.Deleted &&
                         p.NationalIdNumber != null &&
                         p.NationalIdNumber == nationalId &&
                         (excludeId == null || p.Id != excludeId.Value),
                    trackChanges: false)
                .AnyAsync();
    }
}