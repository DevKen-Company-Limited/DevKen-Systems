using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Library
{
    public class LibraryMemberRepository
        : RepositoryBase<LibraryMember, Guid>, ILibraryMemberRepository
    {
        public LibraryMemberRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        // ── All members ───────────────────────────────────────────────────────
        // User is NOT included here — load it separately per member in the service
        // when needed (same pattern as UserActivity), or use GetByIdWithDetailsAsync
        // for single-record lookups. This avoids EF Core warning 10622 caused by
        // User having a global tenant query filter.
        public async Task<IEnumerable<LibraryMember>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderBy(m => m.MemberNumber)
                .ToListAsync();

        // ── By school ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<LibraryMember>> GetBySchoolIdAsync(
            Guid schoolId, bool trackChanges) =>
            await FindByCondition(m => m.TenantId == schoolId, trackChanges)
                .OrderBy(m => m.MemberNumber)
                .ToListAsync();

        // ── By ID with full details ───────────────────────────────────────────
        // User is NOT included — LibraryMember has no User navigation property.
        // User data is resolved separately in the service via IUserRepository.GetByIdsAsync.
        public async Task<LibraryMember?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(m => m.Id == id, trackChanges)
                .Include(m => m.BorrowTransactions)
                .FirstOrDefaultAsync();

        // ── By MemberNumber ───────────────────────────────────────────────────
        public async Task<LibraryMember?> GetByMemberNumberAsync(
            string memberNumber, Guid schoolId) =>
            await FindByCondition(
                    m => m.MemberNumber == memberNumber && m.TenantId == schoolId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        // ── By UserId ─────────────────────────────────────────────────────────
        public async Task<LibraryMember?> GetByUserIdAsync(Guid userId, Guid schoolId) =>
            await FindByCondition(
                    m => m.UserId == userId && m.TenantId == schoolId,
                    trackChanges: false)
                .FirstOrDefaultAsync();
    }
}