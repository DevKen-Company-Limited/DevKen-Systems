using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity
{
    public class UserRepository : RepositoryBase<User, Guid>, IUserRepository
    {
        public UserRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<User?> GetByEmailAsync(string email, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await FindByCondition(
                    u => u.Email == email && u.TenantId == tenantId,
                    trackChanges: false)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailWithRolesAsync(string email, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await FindByCondition(
                    u => u.Email == email && u.TenantId == tenantId,
                    trackChanges: false)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await FindByCondition(
                    u => u.Email == email && u.TenantId == tenantId,
                    trackChanges: false)
                .AnyAsync();
        }
    }
}