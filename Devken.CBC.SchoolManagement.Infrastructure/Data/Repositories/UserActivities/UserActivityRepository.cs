using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.UserActivities1;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Infrastructure.Data;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Devken.CBC.SchoolManagement.Infrastructure.RepositoryManagers.UserActivities
{
    public class UserActivityRepository
        : RepositoryBase<UserActivity, Guid>, IUserActivityRepository
    {
        public UserActivityRepository(
            AppDbContext context,
            TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<UserActivity>> GetByUserIdAsync(
            Guid userId, int page, int pageSize)
        {
            return await FindByCondition(a => a.UserId == userId, false)
                .OrderByDescending(a => a.CreatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserActivity>> GetByTenantAsync(
            Guid tenantId, int page, int pageSize)
        {
            return await FindByCondition(a => a.TenantId == tenantId, false)
                .OrderByDescending(a => a.CreatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserActivity>> GetAllPagedAsync(
            int page, int pageSize)
        {
            return await FindAll(false)
                .OrderByDescending(a => a.CreatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public Task<int> CountByUserAsync(Guid userId) =>
            FindByCondition(a => a.UserId == userId, false).CountAsync();

        public Task<int> CountByTenantAsync(Guid tenantId) =>
            FindByCondition(a => a.TenantId == tenantId, false).CountAsync();

        public Task<int> CountAllAsync() =>
            FindAll(false).CountAsync();

        // 🔥 NEW METHODS

        public Task<int> CountByConditionAsync(
            Expression<Func<UserActivity, bool>> expression) =>
            FindByCondition(expression, false).CountAsync();

        public Task<int> CountDistinctUsersAsync() =>
            FindAll(false)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();
    }
}
