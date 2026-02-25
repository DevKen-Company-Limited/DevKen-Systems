using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academics
{
    public class SubjectRepository : RepositoryBase<Subject, Guid>, ISubjectRepository
    {
        public SubjectRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        /// <summary>
        /// Returns all subjects across all tenants (SuperAdmin only).
        /// No School navigation property exists on Subject — school name
        /// is resolved separately in the service layer.
        /// </summary>
        public async Task<IEnumerable<Subject>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderBy(s => s.Name)
                .ToListAsync();

        /// <summary>
        /// Returns all subjects belonging to a specific tenant/school.
        /// </summary>
        public async Task<IEnumerable<Subject>> GetByTenantIdAsync(
            Guid tenantId, bool trackChanges) =>
            await FindByCondition(s => s.TenantId == tenantId, trackChanges)
                .OrderBy(s => s.Name)
                .ToListAsync();

        /// <summary>
        /// Finds a subject by its auto-generated code within a specific tenant.
        /// </summary>
        public async Task<Subject?> GetByCodeAsync(string code, Guid tenantId) =>
            await FindByCondition(
                    s => s.Code == code && s.TenantId == tenantId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Checks whether a subject with the given name already exists for a tenant,
        /// optionally excluding a specific subject ID (used during update to allow
        /// keeping the same name).
        /// </summary>
        public async Task<bool> ExistsByNameAsync(
            string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    s => s.Name.ToLower() == name.ToLower() &&
                         s.TenantId == tenantId &&
                         (excludeId == null || s.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();

        /// <summary>
        /// Returns a subject by ID with its related Classes, Teachers and Grades
        /// eagerly loaded. School is NOT included — Subject has no School navigation
        /// property; the service layer fetches the school name via IRepositoryManager.School.
        /// </summary>
        public async Task<Subject?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(s => s.Id == id, trackChanges)
                .Include(s => s.Classes)
                .Include(s => s.Teachers)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync();
    }
}
