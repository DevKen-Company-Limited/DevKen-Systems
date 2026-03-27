// Infrastructure/Data/Repositories/Library/LibrarySettingsRepository.cs
using System;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Library
{
    public class LibrarySettingsRepository
        : RepositoryBase<LibrarySettings, Guid>, ILibrarySettingsRepository
    {
        public LibrarySettingsRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext) { }

        public async Task<LibrarySettings?> GetBySchoolIdAsync(Guid schoolId, bool trackChanges) =>
            await FindByCondition(s => s.TenantId == schoolId, trackChanges)
                .FirstOrDefaultAsync();
    }
}