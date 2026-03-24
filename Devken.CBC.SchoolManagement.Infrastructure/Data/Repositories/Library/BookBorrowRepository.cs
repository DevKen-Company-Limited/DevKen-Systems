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
    public class BookBorrowRepository : RepositoryBase<BookBorrow, Guid>, IBookBorrowRepository
    {
        public BookBorrowRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<BookBorrow?> GetByIdWithDetailsAsync(Guid id)
        {
            return await FindByCondition(b => b.Id == id, trackChanges: false)
                .Include(b => b.Member)
                .Include(b => b.Items)
                    .ThenInclude(i => i.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .Include(b => b.Items)
                    .ThenInclude(i => i.Fines)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<BookBorrow>> GetByMemberIdAsync(Guid memberId)
        {
            return await FindByCondition(b => b.MemberId == memberId, trackChanges: false)
                .Include(b => b.Items)
                    .ThenInclude(i => i.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .Include(b => b.Items)
                    .ThenInclude(i => i.Fines)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookBorrow>> GetOverdueBorrowsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await FindByCondition(
                b => b.BStatus == BorrowStatus.Borrowed && b.DueDate < today,
                trackChanges: false)
                .Include(b => b.Member)
                .Include(b => b.Items)
                    .ThenInclude(i => i.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .OrderBy(b => b.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookBorrow>> GetActiveBorrowsAsync()
        {
            return await FindByCondition(
                b => b.BStatus == BorrowStatus.Borrowed,
                trackChanges: false)
                .Include(b => b.Member)
                .Include(b => b.Items)
                    .ThenInclude(i => i.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookBorrow>> GetBorrowsByStatusAsync(BorrowStatus status)
        {
            return await FindByCondition(b => b.BStatus == status, trackChanges: false)
                .Include(b => b.Member)
                .Include(b => b.Items)
                    .ThenInclude(i => i.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();
        }

        public async Task<bool> HasActiveBorrowsAsync(Guid memberId)
        {
            return await FindByCondition(
                b => b.MemberId == memberId && b.BStatus == BorrowStatus.Borrowed,
                trackChanges: false)
                .AnyAsync();
        }

        public async Task<int> GetActiveBorrowCountAsync(Guid memberId)
        {
            return await FindByCondition(
                b => b.MemberId == memberId && b.BStatus == BorrowStatus.Borrowed,
                trackChanges: false)
                .CountAsync();
        }
    }
}