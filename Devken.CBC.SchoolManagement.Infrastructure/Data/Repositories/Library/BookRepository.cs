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
    public class BookRepository : RepositoryBase<Book, Guid>, IBookRepository
    {
        public BookRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<Book>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Copies)
                .OrderBy(b => b.Title)
                .ToListAsync();

        public async Task<IEnumerable<Book>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(b => b.TenantId == schoolId, trackChanges)
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Copies)
                .OrderBy(b => b.Title)
                .ToListAsync();

        public async Task<Book?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(b => b.Id == id, trackChanges)
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Copies)
                    .ThenInclude(c => c.LibraryBranch)
                .FirstOrDefaultAsync();

        public async Task<Book?> GetByISBNAsync(string isbn, Guid schoolId) =>
            await FindByCondition(b => b.ISBN == isbn && b.TenantId == schoolId, trackChanges: false)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<Book>> GetByCategoryAsync(Guid categoryId, Guid schoolId, bool trackChanges) =>
            await FindByCondition(b => b.CategoryId == categoryId && b.TenantId == schoolId, trackChanges)
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Copies)
                .OrderBy(b => b.Title)
                .ToListAsync();

        public async Task<IEnumerable<Book>> GetByAuthorAsync(Guid authorId, Guid schoolId, bool trackChanges) =>
            await FindByCondition(b => b.AuthorId == authorId && b.TenantId == schoolId, trackChanges)
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.Copies)
                .OrderBy(b => b.Title)
                .ToListAsync();
    }
}