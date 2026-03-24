using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
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
    public class LibraryFineRepository : RepositoryBase<LibraryFine, Guid>, ILibraryFineRepository
    {
        public LibraryFineRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<LibraryFine?> GetByIdWithDetailsAsync(Guid id)
        {
            return await FindByCondition(f => f.Id == id, trackChanges: false)
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.Borrow)
                        .ThenInclude(b => b.Member)
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<LibraryFine>> GetByBorrowItemIdAsync(Guid borrowItemId)
        {
            return await FindByCondition(f => f.BorrowItemId == borrowItemId, trackChanges: false)
                .OrderByDescending(f => f.IssuedOn)
                .ToListAsync();
        }

        public async Task<IEnumerable<LibraryFine>> GetUnpaidFinesAsync()
        {
            return await FindByCondition(f => !f.IsPaid, trackChanges: false)
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.Borrow)
                        .ThenInclude(b => b.Member)
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .OrderBy(f => f.IssuedOn)
                .ToListAsync();
        }

        public async Task<IEnumerable<LibraryFine>> GetFinesByMemberIdAsync(Guid memberId)
        {
            return await FindByCondition(f => f.BorrowItem.Borrow.MemberId == memberId, trackChanges: false)
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.Borrow)
                .Include(f => f.BorrowItem)
                    .ThenInclude(bi => bi.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .OrderByDescending(f => f.IssuedOn)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidFinesForMemberAsync(Guid memberId)
        {
            return await FindByCondition(
                f => f.BorrowItem.Borrow.MemberId == memberId && !f.IsPaid,
                trackChanges: false)
                .SumAsync(f => f.Amount);
        }

        public async Task<decimal> GetTotalPaidFinesForMemberAsync(Guid memberId)
        {
            return await FindByCondition(
                f => f.BorrowItem.Borrow.MemberId == memberId && f.IsPaid,
                trackChanges: false)
                .SumAsync(f => f.Amount);
        }
    }
}