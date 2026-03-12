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
    public class BookPublisherRepository : RepositoryBase<BookPublisher, Guid>, IBookPublisherRepository
    {
        public BookPublisherRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<BookPublisher>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<IEnumerable<BookPublisher>> GetByTenantIdAsync(Guid tenantId, bool trackChanges) =>
            await FindByCondition(p => p.TenantId == tenantId, trackChanges)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null) =>
            await FindByCondition(
                    p => p.Name.ToLower() == name.ToLower() &&
                         p.TenantId == tenantId &&
                         (excludeId == null || p.Id != excludeId),
                    trackChanges: false)
                .AnyAsync();
    }
}
