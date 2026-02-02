using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Identity;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;       // <-- correct AppDbContext
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;                            // <-- EF Core only
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Identity
{
    internal class UserRepository
        : RepositoryBase<User, Guid>, IUserRepository
    {
        public UserRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId) =>
            await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.TenantId == tenantId &&
                    u.Status != EntityStatus.Deleted);

        public async Task<User?> GetWithRolesAndPermissionsAsync(Guid userId, Guid tenantId) =>
            await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u =>
                    u.Id == userId &&
                    u.TenantId == tenantId &&
                    u.Status != EntityStatus.Deleted);

        public Task<bool> EmailExistsInTenantAsync(string email, Guid tenantId) =>
            _context.Users.AsNoTracking()
                .AnyAsync(u =>
                    u.Email == email &&
                    u.TenantId == tenantId &&
                    u.Status != EntityStatus.Deleted);
    }
}
 