using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Dashboard;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Dashboard;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Dashboard
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : BaseApiController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            IDashboardService dashboardService,
            IUserActivityService? activityService = null,
            ILogger<DashboardController>? logger = null)
            : base(activityService, logger)
        {
            _dashboardService = dashboardService
                ?? throw new ArgumentNullException(nameof(dashboardService));
        }

        [HttpGet]
        [Authorize(Policy = PermissionKeys.DashboardView)]
        public async Task<IActionResult> GetDashboard([FromQuery] DashboardQueryParams query)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var userId = GetCurrentUserId();
                var permissions = BuildPermissions();

                if (!IsSuperAdmin)
                {
                    query.SchoolId = userSchoolId;
                }
                else
                {
                    if (query.SchoolId == null || query.SchoolId == Guid.Empty)
                        return ValidationErrorResponse("SuperAdmin must supply a schoolId query parameter.");
                }

                var dashboard = await _dashboardService.GetDashboardAsync(
                    query, permissions, userId, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "dashboard.view",
                    $"Viewed School Overview dashboard for school {query.SchoolId}, level '{query.Level ?? "All"}'");

                return SuccessResponse(dashboard);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("stats")]
        [Authorize(Policy = PermissionKeys.DashboardView)]
        public async Task<IActionResult> GetStats([FromQuery] DashboardQueryParams query)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) query.SchoolId = userSchoolId;

                var permissions = BuildPermissions();

                if (!permissions.CanViewStats)
                    return ForbiddenResponse("You do not have permission to view dashboard statistics.");

                var stats = await _dashboardService.GetStatsAsync(query, permissions, userSchoolId, IsSuperAdmin);
                return SuccessResponse(stats);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("class-performance")]
        [Authorize(Policy = PermissionKeys.DashboardClassPerformance)]
        public async Task<IActionResult> GetClassPerformance([FromQuery] DashboardQueryParams query)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) query.SchoolId = userSchoolId;

                var section = await _dashboardService.GetClassPerformanceAsync(
                    query, userSchoolId, IsSuperAdmin, IsClassTeacher, GetCurrentUserId());

                return SuccessResponse(section);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("competency")]
        [Authorize(Policy = PermissionKeys.DashboardCompetency)]
        public async Task<IActionResult> GetCompetency([FromQuery] DashboardQueryParams query)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) query.SchoolId = userSchoolId;

                var section = await _dashboardService.GetCompetencyAsync(query, userSchoolId, IsSuperAdmin);
                return SuccessResponse(section);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("recent-assessments")]
        [Authorize(Policy = PermissionKeys.DashboardRecentAssessments)]
        public async Task<IActionResult> GetRecentAssessments([FromQuery] DashboardQueryParams query)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) query.SchoolId = userSchoolId;

                var section = await _dashboardService.GetRecentAssessmentsAsync(
                    query, userSchoolId, IsSuperAdmin, IsClassTeacher, GetCurrentUserId());

                return SuccessResponse(section);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("events")]
        [Authorize(Policy = PermissionKeys.DashboardEvents)]
        public async Task<IActionResult> GetEvents([FromQuery] DashboardQueryParams query)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) query.SchoolId = userSchoolId;

                var section = await _dashboardService.GetEventsAsync(query, userSchoolId, IsSuperAdmin);
                return SuccessResponse(section);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("fee-collection")]
        [Authorize(Policy = PermissionKeys.DashboardFeeCollection)]
        public async Task<IActionResult> GetFeeCollection([FromQuery] DashboardQueryParams query)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                if (!IsSuperAdmin) query.SchoolId = userSchoolId;

                var section = await _dashboardService.GetFeeCollectionAsync(query, userSchoolId, IsSuperAdmin);
                return SuccessResponse(section);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("quick-actions")]
        [Authorize(Policy = PermissionKeys.DashboardQuickActions)]
        public async Task<IActionResult> GetQuickActions()
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var section = await _dashboardService.GetQuickActionsAsync(userSchoolId, IsSuperAdmin, User);
                return SuccessResponse(section);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("permissions")]
        [Authorize(Policy = PermissionKeys.DashboardView)]
        public IActionResult GetPermissions()
        {
            try
            {
                return SuccessResponse(BuildPermissions());
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        private DashboardPermissions BuildPermissions() => new()
        {
            CanViewStats = HasAnyPermission(
                PermissionKeys.DashboardStatsEnrollment,
                PermissionKeys.DashboardStatsStaff,
                PermissionKeys.DashboardStatsAssessmentsPending,
                PermissionKeys.DashboardStatsFeeRate),

            CanViewClassPerformance = HasPermission(PermissionKeys.DashboardClassPerformance),
            CanViewCompetency = HasPermission(PermissionKeys.DashboardCompetency),
            CanViewRecentAssessments = HasPermission(PermissionKeys.DashboardRecentAssessments),
            CanViewEvents = HasPermission(PermissionKeys.DashboardEvents),
            CanViewFeeCollection = HasPermission(PermissionKeys.DashboardFeeCollection),
            CanViewQuickActions = HasPermission(PermissionKeys.DashboardQuickActions),
        };

        private bool HasPermission(string policy)
            => User.HasClaim("permission", policy);

        private bool HasAnyPermission(params string[] policies)
        {
            foreach (var p in policies)
                if (HasPermission(p)) return true;
            return false;
        }

        private bool IsClassTeacher
            => User.IsInRole("ClassTeacher");

        private Guid GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += $" | Inner: {inner.Message}";
                inner = inner.InnerException;
            }
            return message;
        }
    }
}