using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Library
{
    public class BookCategoryRepository : RepositoryBase<BookCategory, Guid>, IBookCategoryRepository
    {
        public BookCategoryRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<BookCategory>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<IEnumerable<BookCategory>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(c => c.TenantId == tenantId, trackChanges)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    c => c.Name.ToLower() == name.ToLower() &&
                         c.TenantId == tenantId &&
                         (excludeId == null || c.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();
    }
}
