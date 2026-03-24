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
    public class BookBorrowItemRepository : RepositoryBase<BookBorrowItem, Guid>, IBookBorrowItemRepository
    {
        public BookBorrowItemRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<BookBorrowItem?> GetByIdWithDetailsAsync(Guid id)
        {
            return await FindByCondition(bi => bi.Id == id, trackChanges: false)
                .Include(bi => bi.Borrow)
                    .ThenInclude(b => b.Member)
                .Include(bi => bi.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .Include(bi => bi.Fines)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<BookBorrowItem>> GetByBorrowIdAsync(Guid borrowId)
        {
            return await FindByCondition(bi => bi.BorrowId == borrowId, trackChanges: false)
                .Include(bi => bi.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .Include(bi => bi.Fines)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookBorrowItem>> GetUnreturnedItemsAsync()
        {
            return await FindByCondition(bi => bi.ReturnedOn == null, trackChanges: false)
                .Include(bi => bi.Borrow)
                    .ThenInclude(b => b.Member)
                .Include(bi => bi.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .Include(bi => bi.Fines)
                .OrderBy(bi => bi.Borrow.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookBorrowItem>> GetOverdueItemsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await FindByCondition(
                bi => bi.ReturnedOn == null && bi.IsOverdue,
                trackChanges: false)
                .Include(bi => bi.Borrow)
                    .ThenInclude(b => b.Member)
                .Include(bi => bi.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .Include(bi => bi.Fines)
                .OrderBy(bi => bi.Borrow.DueDate)
                .ToListAsync();
        }

        public async Task<BookBorrowItem?> GetByBookCopyIdAsync(Guid bookCopyId)
        {
            return await FindByCondition(
                bi => bi.BookCopyId == bookCopyId && bi.ReturnedOn == null,
                trackChanges: false)
                .Include(bi => bi.Borrow)
                    .ThenInclude(b => b.Member)
                .Include(bi => bi.BookCopy)
                    .ThenInclude(bc => bc.Book)
                .Include(bi => bi.Fines)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsBookCopyBorrowedAsync(Guid bookCopyId)
        {
            return await FindByCondition(
                bi => bi.BookCopyId == bookCopyId && bi.ReturnedOn == null,
                trackChanges: false)
                .AnyAsync();
        }
    }
}