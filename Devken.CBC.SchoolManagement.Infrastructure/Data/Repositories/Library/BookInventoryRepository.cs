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
    public class BookInventoryRepository : RepositoryBase<BookInventory, Guid>, IBookInventoryRepository
    {
        public BookInventoryRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<BookInventory>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Author)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Category)
                .OrderBy(i => i.Book.Title)
                .ToListAsync();

        public async Task<IEnumerable<BookInventory>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(i => i.TenantId == schoolId, trackChanges)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Author)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Category)
                .OrderBy(i => i.Book.Title)
                .ToListAsync();

        public async Task<BookInventory?> GetByBookIdAsync(Guid bookId, bool trackChanges) =>
            await FindByCondition(i => i.BookId == bookId, trackChanges)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Author)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Category)
                .FirstOrDefaultAsync();

        public async Task<BookInventory?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(i => i.Id == id, trackChanges)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Author)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Category)
                .Include(i => i.Book)
                    .ThenInclude(b => b.Publisher)
                .FirstOrDefaultAsync();

        public Task<bool> ExistsByBookIdAsync(Guid bookId) =>
            FindByCondition(i => i.BookId == bookId, false).AnyAsync();
    }
}