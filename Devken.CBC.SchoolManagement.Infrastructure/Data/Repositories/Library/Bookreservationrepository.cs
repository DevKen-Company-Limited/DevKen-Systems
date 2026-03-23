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
    public class BookReservationRepository
        : RepositoryBase<BookReservation, Guid>, IBookReservationRepository
    {
        public BookReservationRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        // ── All reservations (SuperAdmin) ─────────────────────────────────────
        public async Task<IEnumerable<BookReservation>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(r => r.Book)
                .Include(r => r.Member)
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

        // ── By school ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<BookReservation>> GetBySchoolIdAsync(
            Guid schoolId, bool trackChanges) =>
            await FindByCondition(r => r.TenantId == schoolId, trackChanges)
                .Include(r => r.Book)
                .Include(r => r.Member)
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

        // ── By book ──────────────────────────────────────────────────────────
        public async Task<IEnumerable<BookReservation>> GetByBookIdAsync(
            Guid bookId, bool trackChanges) =>
            await FindByCondition(r => r.BookId == bookId, trackChanges)
                .Include(r => r.Book)
                .Include(r => r.Member)
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

        // ── By member ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<BookReservation>> GetByMemberIdAsync(
            Guid memberId, bool trackChanges) =>
            await FindByCondition(r => r.MemberId == memberId, trackChanges)
                .Include(r => r.Book)
                .Include(r => r.Member)
                .OrderByDescending(r => r.ReservedOn)
                .ToListAsync();

        // ── By ID with details ────────────────────────────────────────────────
        public async Task<BookReservation?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(r => r.Id == id, trackChanges)
                .Include(r => r.Book)
                .Include(r => r.Member)
                .FirstOrDefaultAsync();

        // ── Pending duplicate guard ───────────────────────────────────────────
        public async Task<BookReservation?> GetPendingReservationAsync(
            Guid bookId, Guid memberId) =>
            await FindByCondition(
                    r => r.BookId == bookId &&
                         r.MemberId == memberId &&
                         !r.IsFulfilled,
                    trackChanges: false)
                .FirstOrDefaultAsync();
    }
}