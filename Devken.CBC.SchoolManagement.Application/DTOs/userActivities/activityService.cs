using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.userActivities
{

    public class UserActivityDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public Guid? TenantId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string ActivityDetails { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? SchoolName { get; internal set; }
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class ActivitySummaryDto
    {
        public int TotalActivities { get; set; }
        public int TodayActivities { get; set; }
        public int LoginCount { get; set; }
        public int UniqueUsers { get; set; }
    }

    // ── Interface ─────────────────────────────────────────────────────────────────

    public interface IUserActivityService1
    {
        Task LogAsync(Guid userId, string activityType, string details = "", Guid? tenantId = null);
        Task<PagedResult<UserActivityDto>> GetAllAsync(int page, int pageSize);
        Task<PagedResult<UserActivityDto>> GetByUserAsync(Guid userId, int page, int pageSize);
        Task<PagedResult<UserActivityDto>> GetByTenantAsync(Guid tenantId, int page, int pageSize);
        Task<ActivitySummaryDto> GetSummaryAsync();
    }

    // ── Implementation ────────────────────────────────────────────────────────────

}
