using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Tenant;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Administration
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchoolsController : BaseApiController
    {
        private readonly IRepositoryManager _repositories;
        private readonly IImageUploadService _imageUpload;

        public SchoolsController(
            IRepositoryManager repositories,
            IImageUploadService imageUpload,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _imageUpload = imageUpload ?? throw new ArgumentNullException(nameof(imageUpload));
        }

        #region Helpers

        /// <summary>
        /// Builds a user-friendly error message by chaining all inner exception messages.
        /// </summary>
        private static string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += $" | Detail: {inner.Message}";
                inner = inner.InnerException;
            }
            return message;
        }

        /// <summary>
        /// Maps a School entity to its DTO.
        /// </summary>
        private static SchoolDto ToDto(School s) => new()
        {
            Id = s.Id,
            SlugName = s.SlugName,
            Name = s.Name,
            RegistrationNumber = s.RegistrationNumber,
            KnecCenterCode = s.KnecCenterCode,
            KraPin = s.KraPin,
            Address = s.Address,
            County = s.County,
            SubCounty = s.SubCounty,
            PhoneNumber = s.PhoneNumber,
            Email = s.Email,
            LogoUrl = s.LogoUrl,
            SchoolType = s.SchoolType,
            Category = s.Category,
            IsActive = s.IsActive,
            CreatedOn = s.CreatedOn
        };

        #endregion

        #region GET

        /// <summary>
        /// Get all schools – SuperAdmin only.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!HasPermission("School.Read"))
                return ForbiddenResponse("You do not have permission to view schools.");

            if (!IsSuperAdmin)
                return ForbiddenResponse("Only Super Administrators can view all schools.");

            try
            {
                var schools = await _repositories.School.GetAllAsync(trackChanges: false);
                return SuccessResponse(schools.Select(ToDto), "Schools retrieved successfully");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        /// <summary>
        /// Get school by ID – SuperAdmin or SchoolAdmin (own school only).
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("School.Read"))
                return ForbiddenResponse("You do not have permission to view this school.");

            var accessError = ValidateSchoolAccess(id);
            if (accessError != null)
                return accessError;

            try
            {
                var school = await _repositories.School.GetByIdAsync(id, trackChanges: false);
                if (school == null)
                    return NotFoundResponse("School not found.");

                return SuccessResponse(ToDto(school));
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        /// <summary>
        /// Search schools by name, email, slug, or phone – SuperAdmin only.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string searchTerm)
        {
            if (!HasPermission("School.Read"))
                return ForbiddenResponse("You do not have permission to view schools.");

            if (!IsSuperAdmin)
                return ForbiddenResponse("Only Super Administrators can search schools.");

            try
            {
                var schools = await _repositories.School.GetAllAsync(trackChanges: false);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLowerInvariant();
                    schools = schools.Where(s =>
                        (s.Name != null && s.Name.ToLower().Contains(term)) ||
                        (s.Email != null && s.Email.ToLower().Contains(term)) ||
                        (s.SlugName != null && s.SlugName.ToLower().Contains(term)) ||
                        (s.PhoneNumber != null && s.PhoneNumber.Contains(term)) ||
                        (s.County != null && s.County.ToLower().Contains(term)) ||
                        (s.RegistrationNumber != null && s.RegistrationNumber.ToLower().Contains(term))
                    ).ToList();
                }

                return SuccessResponse(schools.Select(ToDto), $"Found {schools.Count()} school(s)");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        /// <summary>
        /// Filter schools by active status – SuperAdmin only.
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> Filter([FromQuery] bool isActive)
        {
            if (!HasPermission("School.Read"))
                return ForbiddenResponse("You do not have permission to view schools.");

            if (!IsSuperAdmin)
                return ForbiddenResponse("Only Super Administrators can filter schools.");

            try
            {
                var schools = await _repositories.School.GetAllAsync(trackChanges: false);
                var filtered = schools.Where(s => s.IsActive == isActive).ToList();

                return SuccessResponse(
                    filtered.Select(ToDto),
                    $"Found {filtered.Count} {(isActive ? "active" : "inactive")} school(s)");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        #endregion

        #region CREATE

        /// <summary>
        /// Create a new school – SuperAdmin only.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSchoolRequest request)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to create schools.");

            if (!IsSuperAdmin)
                return ForbiddenResponse("Only Super Administrators can create schools.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var existing = await _repositories.School.GetBySlugAsync(request.SlugName);
                if (existing != null)
                    return ConflictResponse($"A school with slug '{request.SlugName}' already exists.");

                var school = new School
                {
                    Id = Guid.NewGuid(),
                    SlugName = request.SlugName.Trim(),
                    Name = request.Name.Trim(),
                    RegistrationNumber = request.RegistrationNumber?.Trim(),
                    KnecCenterCode = request.KnecCenterCode?.Trim(),
                    KraPin = request.KraPin?.Trim(),
                    Address = request.Address?.Trim(),
                    County = request.County?.Trim(),
                    SubCounty = request.SubCounty?.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Email = request.Email?.Trim(),
                    LogoUrl = request.LogoUrl?.Trim(),
                    SchoolType = request.SchoolType,
                    Category = request.Category,
                    IsActive = request.IsActive,
                    CreatedOn = DateTime.UtcNow
                };

                _repositories.School.Create(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync(
                    "school.create",
                    $"Created school '{school.Name}' ({school.Id})");

                return CreatedResponse(ToDto(school), "School created successfully");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        #endregion

        #region UPDATE

        /// <summary>
        /// Update an existing school – SuperAdmin or SchoolAdmin (own school; cannot change slug).
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSchoolRequest request)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to update schools.");

            var accessError = ValidateSchoolAccess(id);
            if (accessError != null)
                return accessError;

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
                if (school == null)
                    return NotFoundResponse("School not found.");

                // Slug: only SuperAdmin may change it
                if (IsSuperAdmin)
                {
                    var normalizedNewSlug = (request.SlugName ?? string.Empty).Trim();
                    if (!string.Equals(school.SlugName, normalizedNewSlug, StringComparison.Ordinal))
                    {
                        var other = await _repositories.School.GetBySlugAsync(normalizedNewSlug);
                        if (other != null && other.Id != id)
                            return ConflictResponse($"Another school with slug '{normalizedNewSlug}' already exists.");

                        school.SlugName = normalizedNewSlug;
                    }
                }
                else if (!string.Equals(school.SlugName, (request.SlugName ?? string.Empty).Trim(), StringComparison.Ordinal))
                {
                    return ForbiddenResponse("School administrators cannot change the school slug.");
                }

                school.Name = request.Name.Trim();
                school.RegistrationNumber = request.RegistrationNumber?.Trim();
                school.KnecCenterCode = request.KnecCenterCode?.Trim();
                school.KraPin = request.KraPin?.Trim();
                school.Address = request.Address?.Trim();
                school.County = request.County?.Trim();
                school.SubCounty = request.SubCounty?.Trim();
                school.PhoneNumber = request.PhoneNumber?.Trim();
                school.Email = request.Email?.Trim();
                school.SchoolType = request.SchoolType;
                school.Category = request.Category;
                school.IsActive = request.IsActive;

                // Only update LogoUrl from body if no dedicated logo upload has been used
                // (i.e. caller is setting a direct URL). Uploading via /logo endpoint is preferred.
                if (!string.IsNullOrWhiteSpace(request.LogoUrl))
                    school.LogoUrl = request.LogoUrl.Trim();

                _repositories.School.Update(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync(
                    "school.update",
                    $"Updated school '{school.Name}' ({school.Id})");

                return SuccessResponse(ToDto(school), "School updated successfully");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        /// <summary>
        /// Activate or deactivate a school – SuperAdmin only.
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSchoolStatusRequest request)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to update school status.");

            if (!IsSuperAdmin)
                return ForbiddenResponse("Only Super Administrators can update school status.");

            try
            {
                var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
                if (school == null)
                    return NotFoundResponse("School not found.");

                school.IsActive = request.IsActive;

                _repositories.School.Update(school);
                await _repositories.SaveAsync();

                var action = request.IsActive ? "activated" : "deactivated";

                await LogUserActivityAsync(
                    "school.status_update",
                    $"{char.ToUpper(action[0]) + action[1..]} school '{school.Name}' ({school.Id})");

                return SuccessResponse(ToDto(school), $"School {action} successfully");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        #endregion

        #region LOGO MANAGEMENT

        /// <summary>
        /// Upload or replace the school logo – SuperAdmin or SchoolAdmin (own school).
        /// Accepts: jpg, jpeg, png, gif, webp · Max size: 5 MB.
        /// </summary>
        [HttpPost("{id:guid}/logo")]
        public async Task<IActionResult> UploadLogo(Guid id, IFormFile file)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to update this school.");

            var accessError = ValidateSchoolAccess(id);
            if (accessError != null)
                return accessError;

            // Pre-validate before hitting the service
            if (file == null || file.Length == 0)
                return ValidationErrorResponse("No file was uploaded.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return ValidationErrorResponse("Only image files (jpg, jpeg, png, gif, webp) are allowed.");

            if (file.Length > 5 * 1024 * 1024)
                return ValidationErrorResponse("File size cannot exceed 5 MB.");

            try
            {
                var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
                if (school == null)
                    return NotFoundResponse("School not found.");

                // Delete the old logo from disk if one exists
                if (!string.IsNullOrWhiteSpace(school.LogoUrl))
                    await _imageUpload.DeleteImageAsync(school.LogoUrl);

                // Upload new logo → returns root-relative URL, e.g. /uploads/schools/abc.jpg
                var logoUrl = await _imageUpload.UploadImageAsync(file, "schools");

                school.LogoUrl = logoUrl;

                _repositories.School.Update(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync(
                    "school.logo.upload",
                    $"Uploaded logo for school '{school.Name}' ({school.Id})");

                return SuccessResponse(new { logoUrl }, "School logo uploaded successfully");
            }
            catch (ArgumentException ex)
            {
                // Thrown by ImageUploadService for missing/empty file
                return ValidationErrorResponse(GetFullExceptionMessage(ex));
            }
            catch (InvalidOperationException ex)
            {
                // Thrown by ImageUploadService for size/type violations
                return ValidationErrorResponse(GetFullExceptionMessage(ex));
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        /// <summary>
        /// Delete the school logo – SuperAdmin or SchoolAdmin (own school).
        /// </summary>
        [HttpDelete("{id:guid}/logo")]
        public async Task<IActionResult> DeleteLogo(Guid id)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to update this school.");

            var accessError = ValidateSchoolAccess(id);
            if (accessError != null)
                return accessError;

            try
            {
                var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
                if (school == null)
                    return NotFoundResponse("School not found.");

                if (string.IsNullOrWhiteSpace(school.LogoUrl))
                    return ValidationErrorResponse("This school does not have a logo to delete.");

                var deleted = await _imageUpload.DeleteImageAsync(school.LogoUrl);
                if (!deleted)
                {
                    // File may already be missing on disk – still clear the DB reference
                }

                school.LogoUrl = null;

                _repositories.School.Update(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync(
                    "school.logo.delete",
                    $"Deleted logo for school '{school.Name}' ({school.Id})");

                return SuccessResponse<object?>(null, "School logo deleted successfully");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Permanently delete a school – SuperAdmin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("School.Delete"))
                return ForbiddenResponse("You do not have permission to delete schools.");

            if (!IsSuperAdmin)
                return ForbiddenResponse("Only Super Administrators can delete schools.");

            try
            {
                var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
                if (school == null)
                    return NotFoundResponse("School not found.");

                var schoolName = school.Name;

                // Clean up logo from disk before removing the record
                if (!string.IsNullOrWhiteSpace(school.LogoUrl))
                    await _imageUpload.DeleteImageAsync(school.LogoUrl);

                _repositories.School.Delete(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync(
                    "school.delete",
                    $"Deleted school '{schoolName}' ({id})");

                return SuccessResponse<object?>(null, "School deleted successfully");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse(GetFullExceptionMessage(ex));
            }
        }

        #endregion
    }
}