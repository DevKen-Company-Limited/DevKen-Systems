using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Finance
{
    /// <summary>
    /// EF Core implementation of <see cref="IFeeStructureRepository"/>.
    /// </summary>
    public class FeeStructureRepository : RepositoryBase<FeeStructure, Guid>, IFeeStructureRepository
    {
        public FeeStructureRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        // ── Private helper ────────────────────────────────────────────────────
        /// <summary>
        /// Base query that always eager-loads the three navigation properties.
        /// </summary>
        private IQueryable<FeeStructure> WithDetails(bool trackChanges)
        {
            var query = trackChanges
                ? _context.Set<FeeStructure>()
                : _context.Set<FeeStructure>().AsNoTracking();

            return query
                .Include(fs => fs.FeeItem)
                .Include(fs => fs.AcademicYear)
                .Include(fs => fs.Term);
        }

        // ── IFeeStructureRepository ──────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<IEnumerable<FeeStructure>> GetAllAsync(
            Guid? tenantId,
            bool trackChanges = false)
        {
            var query = WithDetails(trackChanges);

            if (tenantId.HasValue)
                query = query.Where(fs => fs.TenantId == tenantId.Value);

            return await query
                .OrderBy(fs => fs.AcademicYear.Name)
                .ThenBy(fs => fs.FeeItem.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<FeeStructure?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false)
        {
            return await WithDetails(trackChanges)
                .FirstOrDefaultAsync(fs => fs.Id == id);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FeeStructure>> GetByFeeItemAsync(
            Guid feeItemId,
            Guid? tenantId,
            bool trackChanges = false)
        {
            var query = WithDetails(trackChanges)
                .Where(fs => fs.FeeItemId == feeItemId);

            if (tenantId.HasValue)
                query = query.Where(fs => fs.TenantId == tenantId.Value);

            return await query
                .OrderBy(fs => fs.Level)
                .ThenBy(fs => fs.ApplicableTo)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FeeStructure>> GetByAcademicYearAsync(
            Guid academicYearId,
            Guid? tenantId,
            bool trackChanges = false)
        {
            var query = WithDetails(trackChanges)
                .Where(fs => fs.AcademicYearId == academicYearId);

            if (tenantId.HasValue)
                query = query.Where(fs => fs.TenantId == tenantId.Value);

            return await query
                .OrderBy(fs => fs.FeeItem.Name)
                .ThenBy(fs => fs.Level)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FeeStructure>> GetByTermAsync(
            Guid termId,
            Guid? tenantId,
            bool trackChanges = false)
        {
            var query = WithDetails(trackChanges)
                .Where(fs => fs.TermId == termId);

            if (tenantId.HasValue)
                query = query.Where(fs => fs.TenantId == tenantId.Value);

            return await query
                .OrderBy(fs => fs.FeeItem.Name)
                .ThenBy(fs => fs.Level)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FeeStructure>> GetByLevelAsync(
            CBCLevel level,
            Guid? tenantId,
            bool trackChanges = false)
        {
            var query = WithDetails(trackChanges)
                .Where(fs => fs.Level == null || fs.Level == level);

            if (tenantId.HasValue)
                query = query.Where(fs => fs.TenantId == tenantId.Value);

            return await query
                .OrderBy(fs => fs.FeeItem.Name)
                .ThenBy(fs => fs.AcademicYear.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsDuplicateAsync(
            Guid tenantId,
            Guid feeItemId,
            Guid academicYearId,
            Guid? termId,
            CBCLevel? level,
            ApplicableTo applicableTo,
            Guid? excludeId = null)
        {
            var query = _context.Set<FeeStructure>()
                .AsNoTracking()
                .Where(fs =>
                    fs.TenantId == tenantId &&
                    fs.FeeItemId == feeItemId &&
                    fs.AcademicYearId == academicYearId &&
                    fs.TermId == termId &&
                    fs.Level == level &&
                    fs.ApplicableTo == applicableTo);

            if (excludeId.HasValue)
                query = query.Where(fs => fs.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}