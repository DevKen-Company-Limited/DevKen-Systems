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
    public class LibraryBranchRepository : RepositoryBase<LibraryBranch, Guid>, ILibraryBranchRepository
    {
        public LibraryBranchRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<IEnumerable<LibraryBranch>> GetAllAsync(bool trackChanges) =>
            await FindAll(trackChanges)
                .Include(lb => lb.BookCopies)
                .OrderBy(lb => lb.Name)
                .ToListAsync();

        public async Task<IEnumerable<LibraryBranch>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(lb => lb.TenantId == schoolId, trackChanges)
                .Include(lb => lb.BookCopies)
                .OrderBy(lb => lb.Name)
                .ToListAsync();

        public async Task<LibraryBranch?> GetByIdWithDetailsAsync(Guid id, bool trackChanges) =>
            await FindByCondition(lb => lb.Id == id, trackChanges)
                .Include(lb => lb.BookCopies)
                    .ThenInclude(bc => bc.Book)
                .FirstOrDefaultAsync();

        public async Task<LibraryBranch?> GetByNameAsync(string name, Guid schoolId) =>
            await FindByCondition(
                    lb => lb.Name == name && lb.TenantId == schoolId,
                    trackChanges: false)
                .FirstOrDefaultAsync();
    }
}