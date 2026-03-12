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
    public class BookAuthorRepository : RepositoryBase<BookAuthor, Guid>, IBookAuthorRepository
    {
        public BookAuthorRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<BookAuthor>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderBy(a => a.Name)
                .ToListAsync();

        public async Task<IEnumerable<BookAuthor>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(a => a.TenantId == tenantId, trackChanges)
                .OrderBy(a => a.Name)
                .ToListAsync();

        public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    a => a.Name.ToLower() == name.ToLower() &&
                         a.TenantId == tenantId &&
                         (excludeId == null || a.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();
    }
}
