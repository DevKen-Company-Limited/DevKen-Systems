using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Library
{
    public class LibraryFeeRepository
        : RepositoryBase<LibraryFee, Guid>, ILibraryFeeRepository
    {
        public LibraryFeeRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        // ── All fees ──────────────────────────────────────────────────────────
        public async Task<IEnumerable<LibraryFee>> GetAllAsync(bool trackChanges)
        {
            var query = FindAll(trackChanges);

            // This forces the join at the database level specifically for these properties
            return await query
                 .Include(f => f.Member)
                    .ThenInclude(m => m.User)
                .Include(f => f.School)
                .OrderByDescending(f => f.FeeDate)
                .ToListAsync();
        }


        // ── By school ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<LibraryFee>> GetBySchoolIdAsync(
            Guid schoolId, bool trackChanges) {
            var query = FindAll(trackChanges);

            // This forces the join at the database level specifically for these properties
            return await FindByCondition(f => f.TenantId == schoolId, trackChanges)
                .Include(f => f.Member)
                    .ThenInclude(m => m.User)
                .Include(f => f.School)
                .OrderByDescending(f => f.FeeDate)
                .ToListAsync();
        }


        // ── By member ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<LibraryFee>> GetByMemberIdAsync(
            Guid memberId, Guid schoolId, bool trackChanges)
        { var query = FindAll(trackChanges);
            return await FindByCondition(
                    f => f.MemberId == memberId && f.TenantId == schoolId, trackChanges)
                .Include(f => f.Member)
                  .ThenInclude(m => m.User)
                .Include(f => f.School)
                .OrderByDescending(f => f.FeeDate)
                .ToListAsync();
        }


        // ── By borrow transaction ─────────────────────────────────────────────
        public async Task<IEnumerable<LibraryFee>> GetByBorrowIdAsync(
            Guid borrowId, bool trackChanges) {
            var query = FindAll(trackChanges);
            return await FindByCondition(f => f.BookBorrowId == borrowId, trackChanges)
               .Include(f => f.Member)
                       .ThenInclude(m => m.User)
               .Include(f => f.School)
               .OrderByDescending(f => f.FeeDate)
               .ToListAsync();
        }

        // ── By ID with full details ───────────────────────────────────────────
        public async Task<LibraryFee?> GetByIdWithDetailsAsync(Guid id, bool trackChanges)
        {
            var query = FindAll(trackChanges);
            return await FindByCondition(f => f.Id == id, trackChanges)
                .Include(f => f.Member)
                    .ThenInclude(m => m.User)
                .Include(f => f.School)
                .Include(f => f.BookBorrow)
                .FirstOrDefaultAsync();
        }


        // ── By status ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<LibraryFee>> GetByStatusAsync(
            Guid schoolId, LibraryFeeStatus status, bool trackChanges) {

            var query = FindAll(trackChanges);
            return await FindByCondition(
                    f => f.TenantId == schoolId && f.FeeStatus == status, trackChanges)
                .Include(f => f.Member)
                    .ThenInclude(m => m.User)
                .Include(f => f.School)
                .OrderByDescending(f => f.FeeDate)
                .ToListAsync();

        }


        // ── Filtered query ────────────────────────────────────────────────────
        public async Task<IEnumerable<LibraryFee>> GetFilteredAsync(
            Guid? schoolId,
            Guid? memberId,
            LibraryFeeStatus? status,
            LibraryFeeType? feeType,
            DateTime? fromDate,
            DateTime? toDate,
            bool trackChanges)
        {
            var query = schoolId.HasValue
                ? FindByCondition(f => f.TenantId == schoolId.Value, trackChanges)
                : FindAll(trackChanges);

            if (memberId.HasValue)
                query = query.Where(f => f.MemberId == memberId.Value);

            if (status.HasValue)
                query = query.Where(f => f.FeeStatus == status.Value);

            if (feeType.HasValue)
                query = query.Where(f => f.FeeType == feeType.Value);

            if (fromDate.HasValue)
                query = query.Where(f => f.FeeDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(f => f.FeeDate <= toDate.Value);

            return await query
                .Include(f => f.Member)
                    .ThenInclude(m => m.User)
                .Include(f => f.School)
                .OrderByDescending(f => f.FeeDate)
                .ToListAsync();
        }

        // ── Outstanding balance ───────────────────────────────────────────────
        public async Task<decimal> GetOutstandingBalanceAsync(Guid memberId, Guid schoolId) 
           {
            var query = FindAll(trackChanges: false);
            return await FindByCondition(
                    f => f.MemberId == memberId &&
                         f.TenantId == schoolId &&
                         (f.FeeStatus == LibraryFeeStatus.Unpaid ||
                          f.FeeStatus == LibraryFeeStatus.PartiallyPaid),
                    trackChanges: false)
                .SumAsync(f => f.Amount - f.AmountPaid);
    }
    }
}