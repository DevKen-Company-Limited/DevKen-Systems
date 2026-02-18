using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Administration
{
    /// <summary>
    /// SuperAdmin-only endpoint to trigger permission/role seeding
    /// across all schools in the system.
    /// </summary>
    [ApiController]
    [Route("api/admin/seed")]
    [Authorize]
    public class SeedController : BaseApiController
    {
        private readonly IPermissionSeedService _seedService;

        public SeedController(
            IPermissionSeedService seedService,
            IUserActivityService? activityService = null,
            ILogger<SeedController>? logger = null)
            : base(activityService, logger)
        {
            _seedService = seedService;
        }

        /// <summary>
        /// Seeds default roles and permissions for ALL schools.
        /// Safe to call multiple times — skips schools that are already fully seeded.
        /// SuperAdmin only.
        /// </summary>
        [HttpPost("permissions/all-schools")]
        public async Task<IActionResult> SeedAllSchools()
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse("Only SuperAdmin can run system-wide seeding.");

            try
            {
                await _seedService.SeedAllSchoolsAsync();

                await LogUserActivityAsync(
                    "admin.seed.permissions.all_schools",
                    "Ran permission/role seed across all schools");

                return SuccessResponse(
                    new { },
                    "Permissions and roles seeded successfully for all schools.");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(
                    $"Seeding failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Seeds default roles and permissions for a single school.
        /// SuperAdmin only.
        /// </summary>
        [HttpPost("permissions/school/{schoolId:guid}")]
        public async Task<IActionResult> SeedSingleSchool(Guid schoolId)
        {
            if (!IsSuperAdmin)
                return ForbiddenResponse("Only SuperAdmin can run seeding.");

            try
            {
                await _seedService.SeedPermissionsAndRolesAsync(schoolId);

                await LogUserActivityAsync(
                    "admin.seed.permissions.single_school",
                    $"Ran permission/role seed for school {schoolId}");

                return SuccessResponse(
                    new { },
                    $"Permissions and roles seeded for school {schoolId}.");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse($"Seeding failed: {ex.Message}");
            }
        }
    }
}