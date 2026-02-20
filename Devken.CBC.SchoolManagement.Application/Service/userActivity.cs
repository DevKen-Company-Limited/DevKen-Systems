using Devken.CBC.SchoolManagement.Application.DTOs.userActivities;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public class UserActivityService1 : IUserActivityService1
    {
        private readonly IRepositoryManager _repo;

        public UserActivityService1(IRepositoryManager repo)
        {
            _repo = repo;
        }

        // ─────────────────────────────────────────────────────────────
        // Log Activity
        // ─────────────────────────────────────────────────────────────
        public async Task LogAsync(
            Guid userId,
            string activityType,
            string details = "",
            Guid? tenantId = null)
        {
            var activity = new UserActivity
            {
                UserId = userId,
                TenantId = tenantId,
                ActivityType = activityType,
                ActivityDetails = details,
                CreatedOn = DateTime.UtcNow
            };

            _repo.UserActivity.Create(activity);
            await _repo.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // Get All (Paged)
        // ─────────────────────────────────────────────────────────────
        public async Task<PagedResult<UserActivityDto>> GetAllAsync(int page, int pageSize)
        {
            var activities = await _repo.UserActivity.GetAllPagedAsync(page, pageSize);
            var total = await _repo.UserActivity.CountAllAsync();
            var dtos = await MapToDtoAsync(activities);

            return new PagedResult<UserActivityDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Get By User (Paged)
        // ─────────────────────────────────────────────────────────────
        public async Task<PagedResult<UserActivityDto>> GetByUserAsync(
            Guid userId, int page, int pageSize)
        {
            var activities = await _repo.UserActivity
                .GetByUserIdAsync(userId, page, pageSize);
            var total = await _repo.UserActivity.CountByUserAsync(userId);
            var dtos = await MapToDtoAsync(activities);

            return new PagedResult<UserActivityDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Get By Tenant (Paged)
        // ─────────────────────────────────────────────────────────────
        public async Task<PagedResult<UserActivityDto>> GetByTenantAsync(
            Guid tenantId, int page, int pageSize)
        {
            var activities = await _repo.UserActivity
                .GetByTenantAsync(tenantId, page, pageSize);
            var total = await _repo.UserActivity.CountByTenantAsync(tenantId);
            var dtos = await MapToDtoAsync(activities);

            return new PagedResult<UserActivityDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Get Summary
        // ─────────────────────────────────────────────────────────────
        public async Task<ActivitySummaryDto> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;

            var total = await _repo.UserActivity.CountAllAsync();
            var todayCount = await _repo.UserActivity
                .CountByConditionAsync(a => a.CreatedOn.Date == today);
            var loginCount = await _repo.UserActivity
                .CountByConditionAsync(a => a.ActivityType == "Login");
            var uniqueUsers = await _repo.UserActivity
                .CountDistinctUsersAsync();

            return new ActivitySummaryDto
            {
                TotalActivities = total,
                TodayActivities = todayCount,
                LoginCount = loginCount,
                UniqueUsers = uniqueUsers
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Map to DTO (with school name)
        // ─────────────────────────────────────────────────────────────
        private async Task<IEnumerable<UserActivityDto>> MapToDtoAsync(
            IEnumerable<UserActivity> activities)
        {
            var userIds = activities.Select(a => a.UserId).Distinct().ToList();

            var users = await _repo.User.GetByIdsAsync(userIds);
            var userDictionary = users.ToDictionary(u => u.Id, u => u);

            var tenantIds = activities
                .Where(a => a.TenantId.HasValue)
                .Select(a => a.TenantId!.Value)
                .Distinct()
                .ToList();

            var tenants = await _repo.School.GetByIdsAsync(tenantIds);
            var tenantDictionary = tenants.ToDictionary(t => t.Id, t => t);

            return activities.Select(a =>
            {
                userDictionary.TryGetValue(a.UserId, out var user);
                School? tenant = null;
                if (a.TenantId.HasValue)
                {
                    tenantDictionary.TryGetValue(a.TenantId.Value, out tenant);
                }

                return new UserActivityDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserFullName = user != null
                        ? $"{user.FirstName} {user.LastName}".Trim()
                        : "Unknown User",
                    UserEmail = user?.Email ?? string.Empty,
                    TenantId = a.TenantId,
                    SchoolName = tenant?.Name ?? string.Empty,
                    ActivityType = a.ActivityType,
                    ActivityDetails = a.ActivityDetails,
                    CreatedOn = a.CreatedOn
                };
            });
        }
    }
}
