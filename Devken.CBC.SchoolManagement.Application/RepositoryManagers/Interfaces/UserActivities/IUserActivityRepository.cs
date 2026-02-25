using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.UserActivities1
{
    public interface IUserActivityRepository : IRepositoryBase<UserActivity, Guid>
    {
        Task<IEnumerable<UserActivity>> GetByUserIdAsync(Guid userId, int page, int pageSize);
        Task<IEnumerable<UserActivity>> GetByTenantAsync(Guid tenantId, int page, int pageSize);
        Task<IEnumerable<UserActivity>> GetAllPagedAsync(int page, int pageSize);
        Task<int> CountByUserAsync(Guid userId);
        Task<int> CountByTenantAsync(Guid tenantId);
        Task<int> CountAllAsync();
        Task<int> CountByConditionAsync(
        Expression<Func<UserActivity, bool>> expression);

        Task<int> CountDistinctUsersAsync();
    }
}
