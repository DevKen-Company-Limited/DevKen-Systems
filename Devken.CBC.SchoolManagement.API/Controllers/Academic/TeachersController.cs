using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Administration.Teachers
{
    [Route("api/academic/[controller]")]
    [ApiController]
    [Authorize]
    public class TeachersController : BaseApiController
    {
        private readonly ITeacherService _teacherService;

        public TeachersController(
            ITeacherService teacherService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _teacherService = teacherService ?? throw new ArgumentNullException(nameof(teacherService));
        }

        #region Helpers

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

        #endregion

        #region GET

        [HttpGet]
        [Authorize(Policy = PermissionKeys.TeacherRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var teachers = await _teacherService.GetAllTeachersAsync(
                    targetSchoolId,
                    userSchoolId,
                    IsSuperAdmin);

                Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Add("X-School-Filter", targetSchoolId?.ToString() ?? "All Schools");

                return SuccessResponse(teachers);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.TeacherRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var teacher = await _teacherService.GetTeacherByIdAsync(
                    id,
                    userSchoolId,
                    IsSuperAdmin);

                return SuccessResponse(teacher);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region CREATE

        [HttpPost]
        [Authorize(Policy = PermissionKeys.TeacherWrite)]
        public async Task<IActionResult> Create([FromBody] CreateTeacherRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Enforce schoolId handling based on user role
                if (!IsSuperAdmin)
                {
                    // Non-SuperAdmin: Force their own school
                    request.SchoolId = userSchoolId!.Value;
                }
                else if (request.SchoolId == null || request.SchoolId == Guid.Empty)
                {
                    // SuperAdmin: Require schoolId in request
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");
                }

                var result = await _teacherService.CreateTeacherAsync(request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "teacher.create",
                    $"Created teacher {result.TeacherNumber} - {result.FullName} in school {result.SchoolId}");

                return CreatedResponse(result, "Teacher created successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region UPDATE

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.TeacherWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeacherRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _teacherService.UpdateTeacherAsync(id, request, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("teacher.update", $"Updated teacher {result.TeacherNumber} - {result.FullName}");

                return SuccessResponse(result, "Teacher updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region PHOTO MANAGEMENT

        [HttpPost("{id:guid}/photo")]
        [Authorize(Policy = PermissionKeys.TeacherWrite)]
        public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ValidationErrorResponse("No file uploaded.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return ValidationErrorResponse("Only image files (jpg, jpeg, png, gif) are allowed.");

            if (file.Length > 5 * 1024 * 1024)
                return ValidationErrorResponse("File size cannot exceed 5MB.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Use the dedicated service method for photo upload
                var photoUrl = await _teacherService.UploadTeacherPhotoAsync(
                    id,
                    file,
                    userSchoolId,
                    IsSuperAdmin);

                await LogUserActivityAsync(
                    "teacher.photo.upload",
                    $"Uploaded photo for teacher ID: {id}");

                return SuccessResponse(new { photoUrl }, "Photo uploaded successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpDelete("{id:guid}/photo")]
        [Authorize(Policy = PermissionKeys.TeacherWrite)]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Use the dedicated service method for photo deletion
                await _teacherService.DeleteTeacherPhotoAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "teacher.photo.delete",
                    $"Deleted photo for teacher ID: {id}");

                return SuccessResponse<object?>(null, "Photo deleted successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region DELETE & TOGGLE STATUS

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.TeacherDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _teacherService.DeleteTeacherAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("teacher.delete", $"Deleted teacher with ID: {id}");

                return SuccessResponse("Teacher deleted successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        [HttpPatch("{id:guid}/toggle-status")]
        [Authorize(Policy = PermissionKeys.TeacherWrite)]
        public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _teacherService.ToggleTeacherStatusAsync(id, isActive, userSchoolId, IsSuperAdmin);

                var action = isActive ? "activated" : "deactivated";
                await LogUserActivityAsync("teacher.toggle-status", $"{action} teacher {result.TeacherNumber} - {result.FullName}");

                return SuccessResponse(result, $"Teacher {action} successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion
    }
}