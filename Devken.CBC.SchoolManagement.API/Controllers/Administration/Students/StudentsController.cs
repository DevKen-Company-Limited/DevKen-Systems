using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Administration.Student;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Administration.Students
{
    [Route("api/academic/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsController : BaseApiController
    {
        private readonly IStudentService _studentService;
        private readonly IImageUploadService _imageUpload;

        public StudentsController(
            IStudentService studentService,
            IImageUploadService imageUpload,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _imageUpload = imageUpload ?? throw new ArgumentNullException(nameof(imageUpload));
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

        /// <summary>
        /// Get all students with optional school filter (SuperAdmin)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionKeys.StudentRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var students = await _studentService.GetAllStudentsAsync(
                    schoolId,
                    userSchoolId,
                    IsSuperAdmin);

                Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Add("X-School-Filter", schoolId?.ToString() ?? (IsSuperAdmin ? "All Schools" : userSchoolId.ToString()));

                return SuccessResponse(students);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        /// <summary>
        /// Get student by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.StudentRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var student = await _studentService.GetStudentByIdAsync(
                    id,
                    userSchoolId,
                    IsSuperAdmin);

                return SuccessResponse(student);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region CREATE

        /// <summary>
        /// Create a new student with auto-generated admission number
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionKeys.StudentWrite)]
        public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
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

                var result = await _studentService.CreateStudentAsync(
                    request,
                    userSchoolId,
                    IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.create",
                    $"Created student {result.AdmissionNumber} - {result.FullName} in school {result.SchoolId}");

                return CreatedResponse(result, "Student created successfully");
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

        /// <summary>
        /// Update an existing student
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.StudentWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _studentService.UpdateStudentAsync(
                    id,
                    request,
                    userSchoolId,
                    IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.update",
                    $"Updated student {result.AdmissionNumber} - {result.FullName}");

                return SuccessResponse(result, "Student updated successfully");
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

        /// <summary>
        /// Upload student photo
        /// </summary>
        [HttpPost("{id:guid}/photo")]
        [Authorize(Policy = PermissionKeys.StudentWrite)]
        public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ValidationErrorResponse("No file uploaded.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return ValidationErrorResponse("Only image files (jpg, jpeg, png, gif, webp) are allowed.");

            if (file.Length > 5 * 1024 * 1024)
                return ValidationErrorResponse("File size cannot exceed 5MB.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Use the dedicated service method for photo upload
                var photoUrl = await _studentService.UploadStudentPhotoAsync(
                    id,
                    file,
                    userSchoolId,
                    IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.photo.upload",
                    $"Uploaded photo for student ID: {id}");

                return SuccessResponse(new { photoUrl }, "Photo uploaded successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        /// <summary>
        /// Delete student photo
        /// </summary>
        [HttpDelete("{id:guid}/photo")]
        [Authorize(Policy = PermissionKeys.StudentWrite)]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _studentService.DeleteStudentPhotoAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.photo.delete",
                    $"Deleted photo for student ID: {id}");

                return SuccessResponse<object?>(null, "Photo deleted successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region DELETE & TOGGLE STATUS

        /// <summary>
        /// Delete a student
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.StudentDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _studentService.DeleteStudentAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.delete",
                    $"Deleted student with ID: {id}");

                return SuccessResponse("Student deleted successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        /// <summary>
        /// Toggle student active status
        /// </summary>
        [HttpPatch("{id:guid}/toggle-status")]
        [Authorize(Policy = PermissionKeys.StudentWrite)]
        public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _studentService.ToggleStudentStatusAsync(
                    id,
                    isActive,
                    userSchoolId,
                    IsSuperAdmin);

                var action = isActive ? "activated" : "deactivated";
                await LogUserActivityAsync(
                    "student.toggle-status",
                    $"{action} student {result.AdmissionNumber} - {result.FullName}");

                return SuccessResponse(result, $"Student {action} successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion
    }
}