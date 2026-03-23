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
                var permissions = BuildPermissions();

                // Non-SuperAdmin users are always scoped to their own school.
                // SuperAdmin may optionally supply a schoolId to scope to one school,
                // or omit it to get a system-wide aggregate view.
                if (!IsSuperAdmin)
                    query.SchoolId = userSchoolId;

                // FIX: use CurrentUserId from BaseApiController instead of a duplicate private method
                var dashboard = await _dashboardService.GetDashboardAsync(
                    query, permissions, CurrentUserId, userSchoolId, IsSuperAdmin);

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
                    query, userSchoolId, IsSuperAdmin, IsClassTeacher, CurrentUserId);

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
                    query, userSchoolId, IsSuperAdmin, IsClassTeacher, CurrentUserId);

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

        // ── Private helpers ──────────────────────────────────────────────────────

        private DashboardPermissions BuildPermissions() => new()
        {
            // FIX: delegate to base class HasAnyPermission / HasPermission so SuperAdmin
            //      is handled correctly (the original private overloads did NOT check IsSuperAdmin)
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

        // FIX: use base class HasRole so the check is case-insensitive and consistent
        private bool IsClassTeacher => HasRole("ClassTeacher");

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