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
        /// Get all schools - SuperAdmin only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAll()
        {
            if (!HasPermission("School.Read"))
                return ForbiddenResponse("You do not have permission to view schools.");

            var schools = await _repositories.School.GetAllAsync(trackChanges: false);
            var dtos = schools.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Get school by ID - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("School.Read"))
                return ForbiddenResponse("You do not have permission to view this school.");

            // If SchoolAdmin, verify they're accessing their own school
            if (HasRole("SchoolAdmin") && !HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (userSchoolId == null || userSchoolId != id)
                    return ForbiddenResponse("You can only view your own school.");
            }

            var school = await _repositories.School.GetByIdAsync(id, trackChanges: false);
            if (school == null) return NotFoundResponse();

            return SuccessResponse(ToDto(school));
        }

        /// <summary>
        /// Create school - SuperAdmin only
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateSchoolRequest request)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to create schools.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

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
                IsActive = request.IsActive
            };

            _repositories.School.Create(school);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("school.create", $"Created school {school.Id}");

            return SuccessResponse(ToDto(school), "School created");
        }

        /// <summary>
        /// Update school - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSchoolRequest request)
        {
            if (!HasPermission("School.Write"))
                return ForbiddenResponse("You do not have permission to update schools.");

            // If SchoolAdmin, verify they're updating their own school
            if (HasRole("SchoolAdmin") && !HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (userSchoolId == null || userSchoolId != id)
                    return ForbiddenResponse("You can only update your own school.");
            }

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
            if (school == null) return NotFoundResponse();

            // SuperAdmin can change slug, SchoolAdmin cannot
            if (!HasRole("SuperAdmin"))
            {
                if (!string.Equals(school.SlugName, request.SlugName ?? string.Empty, StringComparison.Ordinal))
                    return ForbiddenResponse("School administrators cannot change the school slug.");
            }
            else
            {
                // Only SuperAdmin needs to check for slug conflicts
                if (!string.Equals(school.SlugName, request.SlugName ?? string.Empty, StringComparison.Ordinal))
                {
                    var other = await _repositories.School.GetBySlugAsync(request.SlugName ?? string.Empty);
                    if (other != null && other.Id != id)
                        return ErrorResponse($"Another school with slug '{request.SlugName}' already exists.", 409);
                }
                school.SlugName = (request.SlugName ?? string.Empty).Trim();
            }

            school.Name = (request.Name ?? string.Empty).Trim();
            school.Address = request.Address?.Trim();
            school.PhoneNumber = request.PhoneNumber?.Trim();
            school.Email = request.Email?.Trim();
            school.LogoUrl = request.LogoUrl?.Trim();
            school.IsActive = request.IsActive;

            _repositories.School.Update(school);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("school.update", $"Updated school {school.Id}");

            return SuccessResponse(ToDto(school), "School updated");
        }

        /// <summary>
        /// Delete school - SuperAdmin only
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("School.Delete"))
                return ForbiddenResponse("You do not have permission to delete schools.");

            var school = await _repositories.School.GetByIdAsync(id, trackChanges: true);
            if (school == null) return NotFoundResponse();

            _repositories.School.Delete(school);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("school.delete", $"Deleted school {school.Id}");

            return SuccessResponse<object?>(null, "School deleted");
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
            IsActive = s.IsActive
        };
    }
}