using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.API.Controllers.Academic
{
    [Route("api/academic/[controller]")]
    [ApiController]
    [Authorize]
    public class AcademicYearController : BaseApiController
    {
        private readonly IRepositoryManager _repositories;

        public AcademicYearController(
            IRepositoryManager repositories,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        /// <summary>
        /// Get all academic years - SuperAdmin can see all, others see only their school's academic years
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionKeys.AcademicYearRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            // SuperAdmin can view all academic years or filter by school
            if (HasRole("SuperAdmin"))
            {
                var academicYears = schoolId.HasValue
                    ? await _repositories.AcademicYear.GetAllByTenantAsync(schoolId.Value, trackChanges: false)
                    : _repositories.AcademicYear.FindAll(trackChanges: false).ToList();

                var dtos = academicYears.Select(ToDto);
                return SuccessResponse(dtos);
            }

            // Non-SuperAdmin users can only view academic years from their school
            var userSchoolId = GetCurrentUserSchoolId();

            var schoolAcademicYears = await _repositories.AcademicYear.GetAllByTenantAsync(userSchoolId, trackChanges: false);
            var schoolDtos = schoolAcademicYears.Select(ToDto);
            return SuccessResponse(schoolDtos);
        }

        /// <summary>
        /// Get academic year by ID - SuperAdmin or users from the same school
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.AcademicYearRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var academicYear = await _repositories.AcademicYear.GetByIdAsync(id, trackChanges: false);
            if (academicYear == null)
                return NotFoundResponse("Academic year not found");

            // Non-SuperAdmin users can only view academic years from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (academicYear.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only view academic years from your school.");
            }

            return SuccessResponse(ToDto(academicYear));
        }

        /// <summary>
        /// Get current academic year for the school
        /// </summary>
        [HttpGet("current")]
        [Authorize(Policy = PermissionKeys.AcademicYearRead)]
        public async Task<IActionResult> GetCurrent([FromQuery] Guid? schoolId = null)
        {
            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var academicYear = await _repositories.AcademicYear.GetCurrentAcademicYearAsync(targetSchoolId);
            if (academicYear == null)
                return NotFoundResponse("No current academic year found");

            return SuccessResponse(ToDto(academicYear));
        }

        /// <summary>
        /// Get all open academic years for the school
        /// </summary>
        [HttpGet("open")]
        [Authorize(Policy = PermissionKeys.AcademicYearRead)]
        public async Task<IActionResult> GetOpen([FromQuery] Guid? schoolId = null)
        {
            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var academicYears = await _repositories.AcademicYear.GetOpenAcademicYearsAsync(targetSchoolId, trackChanges: false);
            var dtos = academicYears.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Create academic year - SuperAdmin or SchoolAdmin
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionKeys.AcademicYearWrite)]
        public async Task<IActionResult> Create([FromBody] CreateAcademicYearRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            // Determine school/tenant ID
            Guid targetSchoolId;
            if (HasRole("SuperAdmin"))
            {
                targetSchoolId = request.SchoolId;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            // Verify school exists
            var school = await _repositories.School.GetByIdAsync(targetSchoolId, trackChanges: false);
            if (school == null)
                return ErrorResponse("School not found.", 404);

            // Validate dates
            if (request.StartDate >= request.EndDate)
                return ErrorResponse("End date must be after start date.", 400);

            // Check if code already exists
            if (await _repositories.AcademicYear.CodeExistsAsync(targetSchoolId, request.Code ?? string.Empty))
                return ErrorResponse($"Academic year with code '{request.Code}' already exists.", 409);

            // Check for overlapping years (optional - log warning instead of blocking)
            if (await _repositories.AcademicYear.HasOverlappingYearsAsync(targetSchoolId, request.StartDate, request.EndDate))
            {
                // You can choose to return an error or just log a warning
                // return ErrorResponse("This academic year overlaps with an existing year.", 400);
            }

            var academicYear = new AcademicYear
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId,
                Name = (request.Name ?? string.Empty).Trim(),
                Code = (request.Code ?? string.Empty).Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsCurrent = request.IsCurrent,
                IsClosed = false,
                Notes = request.Notes?.Trim()
            };

            _repositories.AcademicYear.Create(academicYear);

            // If this is set as current, unset all others
            if (request.IsCurrent)
            {
                await _repositories.AcademicYear.SetAsCurrentAsync(targetSchoolId, academicYear.Id);
            }

            await _repositories.SaveAsync();

            await LogUserActivityAsync("academic_year.create", $"Created academic year {academicYear.Code} - {academicYear.Name}");

            return SuccessResponse(ToDto(academicYear), "Academic year created successfully");
        }

        /// <summary>
        /// Update academic year - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.AcademicYearWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAcademicYearRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var academicYear = await _repositories.AcademicYear.GetByIdAsync(id, trackChanges: true);
            if (academicYear == null)
                return NotFoundResponse("Academic year not found");

            // Non-SuperAdmin users can only update academic years from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (academicYear.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only update academic years from your school.");
            }

            // Check if academic year is closed
            if (academicYear.IsClosed)
                return ErrorResponse("Cannot update a closed academic year.", 400);

            // Validate dates if both are provided
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                if (request.StartDate.Value >= request.EndDate.Value)
                    return ErrorResponse("End date must be after start date.", 400);
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                academicYear.Name = request.Name.Trim();

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                if (await _repositories.AcademicYear.CodeExistsAsync(academicYear.TenantId, request.Code, id))
                    return ErrorResponse($"Academic year with code '{request.Code}' already exists.", 409);

                academicYear.Code = request.Code.Trim();
            }

            if (request.StartDate.HasValue)
                academicYear.StartDate = request.StartDate.Value;

            if (request.EndDate.HasValue)
                academicYear.EndDate = request.EndDate.Value;

            if (request.Notes != null)
                academicYear.Notes = request.Notes.Trim();

            _repositories.AcademicYear.Update(academicYear);

            // Handle IsCurrent flag
            if (request.IsCurrent.HasValue && request.IsCurrent.Value && !academicYear.IsCurrent)
            {
                await _repositories.AcademicYear.SetAsCurrentAsync(academicYear.TenantId, id);
            }

            await _repositories.SaveAsync();

            await LogUserActivityAsync("academic_year.update", $"Updated academic year {academicYear.Code} - {academicYear.Name}");

            return SuccessResponse(ToDto(academicYear), "Academic year updated successfully");
        }

        /// <summary>
        /// Set academic year as current - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpPut("{id:guid}/set-current")]
        [Authorize(Policy = PermissionKeys.AcademicYearWrite)]
        public async Task<IActionResult> SetAsCurrent(Guid id)
        {
            var academicYear = await _repositories.AcademicYear.GetByIdAsync(id, trackChanges: false);
            if (academicYear == null)
                return NotFoundResponse("Academic year not found");

            // Non-SuperAdmin users can only update academic years from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (academicYear.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only modify academic years from your school.");
            }

            if (academicYear.IsClosed)
                return ErrorResponse("Cannot set a closed academic year as current.", 400);

            await _repositories.AcademicYear.SetAsCurrentAsync(academicYear.TenantId, id);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("academic_year.set_current", $"Set academic year {academicYear.Code} as current");

            return SuccessResponse("Academic year set as current successfully");
        }

        /// <summary>
        /// Close academic year - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpPut("{id:guid}/close")]
        [Authorize(Policy = PermissionKeys.AcademicYearClose)]
        public async Task<IActionResult> Close(Guid id)
        {
            var academicYear = await _repositories.AcademicYear.GetByIdAsync(id, trackChanges: true);
            if (academicYear == null)
                return NotFoundResponse("Academic year not found");

            // Non-SuperAdmin users can only update academic years from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (academicYear.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only close academic years from your school.");
            }

            academicYear.IsClosed = true;
            academicYear.IsCurrent = false; // A closed year cannot be current

            _repositories.AcademicYear.Update(academicYear);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("academic_year.close", $"Closed academic year {academicYear.Code} - {academicYear.Name}");

            return SuccessResponse("Academic year closed successfully");
        }

        /// <summary>
        /// Delete academic year - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.AcademicYearDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var academicYear = await _repositories.AcademicYear.GetByIdAsync(id, trackChanges: true);
            if (academicYear == null)
                return NotFoundResponse("Academic year not found");

            // Non-SuperAdmin users can only delete academic years from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (academicYear.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only delete academic years from your school.");
            }

            var code = academicYear.Code;
            var name = academicYear.Name;

            _repositories.AcademicYear.Delete(academicYear);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("academic_year.delete", $"Deleted academic year {code} - {name}");

            return SuccessResponse<object?>(null, "Academic year deleted successfully");
        }

        private static AcademicYearDto ToDto(AcademicYear ay) => new()
        {
            Id = ay.Id,
            SchoolId = ay.TenantId,
            Name = ay.Name ?? string.Empty,
            Code = ay.Code ?? string.Empty,
            StartDate = ay.StartDate,
            EndDate = ay.EndDate,
            IsCurrent = ay.IsCurrent,
            IsClosed = ay.IsClosed,
            IsActive = ay.IsActive,
            Notes = ay.Notes ?? string.Empty,
            CreatedOn = ay.CreatedOn,
            CreatedBy = ay.CreatedBy,
            UpdatedOn = ay.UpdatedOn,
            UpdatedBy = ay.UpdatedBy
        };
    }
}