// Api/Controllers/Library/LibrarySettingsController.cs
using System;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Library
{
    [Route("api/library/[controller]")]
    [ApiController]
    [Authorize]
    public class LibrarySettingsController : BaseApiController
    {
        private readonly ILibrarySettingsService _settingsService;

        public LibrarySettingsController(
            ILibrarySettingsService settingsService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _settingsService = settingsService
                ?? throw new ArgumentNullException(nameof(settingsService));
        }

        private string GetFullExceptionMessage(Exception ex)
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

        // ── GET ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Gets the library settings for the current school.
        /// SuperAdmin may pass ?schoolId= to query any school.
        /// Returns sensible defaults if settings have not been saved yet.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> Get([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var settings = await _settingsService.GetSettingsAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin);

                return SuccessResponse(settings);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ── UPSERT ────────────────────────────────────────────────────────────
        /// <summary>
        /// Creates or updates the library settings for a school.
        /// Sending the same payload twice is safe (idempotent upsert).
        /// </summary>
        [HttpPut]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> Upsert([FromBody] UpsertLibrarySettingsRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // School-scoped users cannot override their school
                if (!IsSuperAdmin)
                    request.SchoolId = userSchoolId!.Value;
                else if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");

                var result = await _settingsService.UpsertSettingsAsync(
                    request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "library-settings.upsert",
                    $"Updated library settings for school '{result.SchoolId}'");

                return SuccessResponse(result, "Library settings saved successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}