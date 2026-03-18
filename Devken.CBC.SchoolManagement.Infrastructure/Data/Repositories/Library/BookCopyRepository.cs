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
    public class BookCopyRepository : RepositoryBase<BookCopy, Guid>, IBookCopyRepository
    {
        public BookCopyRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<BookCopy>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Author)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Category)
                .Include(bc => bc.LibraryBranch)
                .OrderBy(bc => bc.Book.Title)
                .ThenBy(bc => bc.AccessionNumber)
                .ToListAsync();

        public async Task<IEnumerable<BookCopy>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(bc => bc.TenantId == schoolId, trackChanges)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Author)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Category)
                .Include(bc => bc.LibraryBranch)
                .OrderBy(bc => bc.Book.Title)
                .ThenBy(bc => bc.AccessionNumber)
                .ToListAsync();

        public async Task<IEnumerable<BookCopy>> GetByBookIdAsync(Guid bookId, bool trackChanges) =>
            await FindByCondition(bc => bc.BookId == bookId, trackChanges)
                .Include(bc => bc.Book)
                .Include(bc => bc.LibraryBranch)
                .OrderBy(bc => bc.AccessionNumber)
                .ToListAsync();

        public async Task<IEnumerable<BookCopy>> GetByBranchIdAsync(Guid branchId, bool trackChanges) =>
            await FindByCondition(bc => bc.LibraryBranchId == branchId, trackChanges)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Author)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Category)
                .Include(bc => bc.LibraryBranch)
                .OrderBy(bc => bc.Book.Title)
                .ThenBy(bc => bc.AccessionNumber)
                .ToListAsync();

        public async Task<BookCopy?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(bc => bc.Id == id, trackChanges)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Author)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Category)
                .Include(bc => bc.Book)
                    .ThenInclude(b => b.Publisher)
                .Include(bc => bc.LibraryBranch)
                .FirstOrDefaultAsync();

        public async Task<BookCopy?> GetByAccessionNumberAsync(string accessionNumber, Guid schoolId) =>
            await FindByCondition(
                    bc => bc.AccessionNumber == accessionNumber && bc.TenantId == schoolId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<BookCopy?> GetByBarcodeAsync(string barcode, Guid schoolId) =>
            await FindByCondition(
                    bc => bc.Barcode == barcode && bc.TenantId == schoolId,
                    trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<BookCopy>> GetAvailableCopiesByBookAsync(Guid bookId) =>
            await FindByCondition(
                    bc => bc.BookId == bookId && bc.IsAvailable && !bc.IsLost && !bc.IsDamaged,
                    trackChanges: false)
                .Include(bc => bc.LibraryBranch)
                .OrderBy(bc => bc.AccessionNumber)
                .ToListAsync();

        public Task<int> CountByBookIdAsync(Guid bookId) =>
            FindByCondition(bc => bc.BookId == bookId, false).CountAsync();

        public Task<int> CountAvailableByBookIdAsync(Guid bookId) =>
            FindByCondition(bc => bc.BookId == bookId && bc.IsAvailable && !bc.IsLost && !bc.IsDamaged, false).CountAsync();

        public Task<int> CountLostByBookIdAsync(Guid bookId) =>
            FindByCondition(bc => bc.BookId == bookId && bc.IsLost, false).CountAsync();

        public Task<int> CountDamagedByBookIdAsync(Guid bookId) =>
            FindByCondition(bc => bc.BookId == bookId && bc.IsDamaged, false).CountAsync();
    }
}