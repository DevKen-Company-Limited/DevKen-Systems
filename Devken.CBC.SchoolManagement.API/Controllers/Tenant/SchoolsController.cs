using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Tenant;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Microsoft.AspNetCore.Authorization;
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

        public SchoolsController(
            IRepositoryManager repositories,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        /// <summary>
        /// Get all schools – SuperAdmin only
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
                return ErrorResponse($"Failed to retrieve schools: {ex.Message}");
            }
        }

        /// <summary>
        /// Get school by ID – SuperAdmin or SchoolAdmin (own school)
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
                    return NotFoundResponse("School not found");

                return SuccessResponse(ToDto(school));
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve school: {ex.Message}");
            }
        }

        /// <summary>
        /// Create school – SuperAdmin only
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSchoolRequest request)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to create schools.");

            if (!IsSuperAdmin)
                return ForbiddenResponse("Only Super Administrators can create schools.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            try
            {
                var existing = await _repositories.School.GetBySlugAsync(request.SlugName ?? string.Empty);
                if (existing != null)
                    return ErrorResponse($"School with slug '{request.SlugName}' already exists.", 409);

                var school = new School
                {
                    Id = Guid.NewGuid(),
                    SlugName = (request.SlugName ?? string.Empty).Trim(),
                    Name = (request.Name ?? string.Empty).Trim(),
                    Address = request.Address?.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Email = request.Email?.Trim(),
                    LogoUrl = request.LogoUrl?.Trim(),
                    IsActive = request.IsActive,
                    CreatedOn = DateTime.UtcNow
                };

                _repositories.School.Create(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync("school.create", $"Created school {school.Name} ({school.Id})");

                return SuccessResponse(ToDto(school), "School created successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to create school: {ex.Message}");
            }
        }

        /// <summary>
        /// Update school – SuperAdmin or SchoolAdmin (own school)
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
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            try
            {
                var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
                if (school == null)
                    return NotFoundResponse("School not found");

                // Slug handling
                if (IsSuperAdmin)
                {
                    if (!string.Equals(school.SlugName, request.SlugName ?? string.Empty, StringComparison.Ordinal))
                    {
                        var other = await _repositories.School.GetBySlugAsync(request.SlugName ?? string.Empty);
                        if (other != null && other.Id != id)
                            return ErrorResponse($"Another school with slug '{request.SlugName}' already exists.", 409);

                        school.SlugName = (request.SlugName ?? string.Empty).Trim();
                    }
                }
                else if (!string.Equals(school.SlugName, request.SlugName ?? string.Empty, StringComparison.Ordinal))
                {
                    return ForbiddenResponse("School administrators cannot change the school slug.");
                }

                school.Name = (request.Name ?? string.Empty).Trim();
                school.Address = request.Address?.Trim();
                school.PhoneNumber = request.PhoneNumber?.Trim();
                school.Email = request.Email?.Trim();
                school.LogoUrl = request.LogoUrl?.Trim();
                school.IsActive = request.IsActive;

                _repositories.School.Update(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync("school.update", $"Updated school {school.Name} ({school.Id})");

                return SuccessResponse(ToDto(school), "School updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to update school: {ex.Message}");
            }
        }

        /// <summary>
        /// Update school status (activate/deactivate) – SuperAdmin only
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
                    return NotFoundResponse("School not found");

                school.IsActive = request.IsActive;

                _repositories.School.Update(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync("school.status_update",
                    $"{(request.IsActive ? "Activated" : "Deactivated")} school {school.Name} ({school.Id})");

                return SuccessResponse(ToDto(school),
                    $"School {(request.IsActive ? "activated" : "deactivated")} successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to update school status: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete school – SuperAdmin only
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
                    return NotFoundResponse("School not found");

                var schoolName = school.Name;

                _repositories.School.Delete(school);
                await _repositories.SaveAsync();

                await LogUserActivityAsync("school.delete", $"Deleted school {schoolName} ({id})");

                return SuccessResponse<object?>(null, "School deleted successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to delete school: {ex.Message}");
            }
        }

        /// <summary>
        /// Search schools – SuperAdmin only
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
                    var term = searchTerm.ToLower();
                    schools = schools.Where(s =>
                        (s.Name != null && s.Name.ToLower().Contains(term)) ||
                        (s.Email != null && s.Email.ToLower().Contains(term)) ||
                        (s.SlugName != null && s.SlugName.ToLower().Contains(term)) ||
                        (s.PhoneNumber != null && s.PhoneNumber.Contains(term))
                    ).ToList();
                }

                return SuccessResponse(schools.Select(ToDto), $"Found {schools.Count()} schools");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to search schools: {ex.Message}");
            }
        }

        /// <summary>
        /// Filter schools by status – SuperAdmin only
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

                return SuccessResponse(filtered.Select(ToDto),
                    $"Found {filtered.Count} {(isActive ? "active" : "inactive")} schools");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to filter schools: {ex.Message}");
            }
        }

        private static SchoolDto ToDto(School s) => new()
        {
            Id = s.Id,
            SlugName = s.SlugName ?? string.Empty,
            Name = s.Name ?? string.Empty,
            Address = s.Address ?? string.Empty,
            PhoneNumber = s.PhoneNumber ?? string.Empty,
            Email = s.Email ?? string.Empty,
            LogoUrl = s.LogoUrl ?? string.Empty,
            IsActive = s.IsActive,
            CreatedOn = s.CreatedOn
        };
    }

    /// <summary>
    /// Request model for updating school status
    /// </summary>
    public class UpdateSchoolStatusRequest
    {
        public bool IsActive { get; set; }
    }
}