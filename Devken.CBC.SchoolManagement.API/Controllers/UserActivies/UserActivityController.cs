using Devken.CBC.SchoolManagement.Application.DTOs.userActivities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.API.Controllers.UserActivies
{
    [ApiController]
    [Route("api/user-activity")]
    [Authorize]
    public class UserActivityController : ControllerBase
    {
        private readonly IUserActivityService1 _activityService;

        public UserActivityController(IUserActivityService1 activityService)
        {
            _activityService = activityService;
        }

        // ── GET /api/user-activity?page=1&pageSize=20 ─────────────────────────────
        /// <summary>
        /// Returns all user activities paged. SuperAdmin only.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _activityService.GetAllAsync(page, pageSize);

            return Ok(ApiResponse<PagedResult<UserActivityDto>>.SuccessResult(result));
        }

        // ── GET /api/user-activity/summary ───────────────────────────────────────
        /// <summary>
        /// Returns aggregate stats for the dashboard.
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _activityService.GetSummaryAsync();
            return Ok(ApiResponse<ActivitySummaryDto>.SuccessResult(result));
        }

        // ── GET /api/user-activity/user/{userId}?page=1&pageSize=20 ──────────────
        /// <summary>
        /// Returns activities for a specific user.
        /// </summary>
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _activityService.GetByUserAsync(userId, page, pageSize);
            return Ok(ApiResponse<PagedResult<UserActivityDto>>.SuccessResult(result));
        }

        // ── GET /api/user-activity/tenant/{tenantId}?page=1&pageSize=20 ──────────
        /// <summary>
        /// Returns activities scoped to a tenant (school). SuperAdmin only.
        /// </summary>
        [HttpGet("tenant/{tenantId:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetByTenant(
            Guid tenantId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _activityService.GetByTenantAsync(tenantId, page, pageSize);
            return Ok(ApiResponse<PagedResult<UserActivityDto>>.SuccessResult(result));
        }
    }

    // ── Generic API response wrapper (matches existing pattern) ──────────────────

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResult(T data, string message = "") =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
