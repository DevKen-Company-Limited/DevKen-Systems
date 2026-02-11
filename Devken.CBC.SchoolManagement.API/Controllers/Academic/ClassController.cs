using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.API.Controllers.Academic
{
    [Route("api/academic/[controller]")]
    [ApiController]
    [Authorize]
    public class ClassController : BaseApiController
    {
        private readonly IRepositoryManager _repositories;

        public ClassController(
            IRepositoryManager repositories,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        /// <summary>
        /// Get all classes - SuperAdmin can see all, others see only their school's classes
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] Guid? academicYearId = null,
            [FromQuery] CBCLevel? level = null,
            [FromQuery] bool? activeOnly = null)
        {
            if (!HasPermission("Class.Read"))
                return ForbiddenResponse("You do not have permission to view classes.");

            Guid targetSchoolId;

            // SuperAdmin can view all classes or filter by school
            if (HasRole("SuperAdmin"))
            {
                if (!schoolId.HasValue)
                {
                    var allClasses = _repositories.Class.FindAll(trackChanges: false).ToList();
                    var allDtos = allClasses.Select(ToDto);
                    return SuccessResponse(allDtos);
                }
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            // Get classes with filters
            var classes = await _repositories.Class.GetAllByTenantAsync(targetSchoolId, trackChanges: false);

            // Apply filters
            if (academicYearId.HasValue)
            {
                classes = classes.Where(c => c.AcademicYearId == academicYearId.Value);
            }

            if (level.HasValue)
            {
                classes = classes.Where(c => c.Level == level.Value);
            }

            if (activeOnly.HasValue && activeOnly.Value)
            {
                classes = classes.Where(c => c.IsActive);
            }

            var dtos = classes.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Get class by ID - SuperAdmin or users from the same school
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includeDetails = false)
        {
            if (!HasPermission("Class.Read"))
                return ForbiddenResponse("You do not have permission to view this class.");

            var classEntity = includeDetails
                ? await _repositories.Class.GetWithDetailsAsync(id, trackChanges: false)
                : await _repositories.Class.GetByIdAsync(id, trackChanges: false);

            if (classEntity == null)
                return NotFoundResponse("Class not found");

            // Non-SuperAdmin users can only view classes from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (classEntity.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only view classes from your school.");
            }

            if (includeDetails)
            {
                return SuccessResponse(ToDetailDto(classEntity));
            }

            return SuccessResponse(ToDto(classEntity));
        }

        /// <summary>
        /// Get classes by academic year
        /// </summary>
        [HttpGet("by-academic-year/{academicYearId:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetByAcademicYear(Guid academicYearId, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Class.Read"))
                return ForbiddenResponse("You do not have permission to view classes.");

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var classes = await _repositories.Class.GetByAcademicYearAsync(targetSchoolId, academicYearId, trackChanges: false);
            var dtos = classes.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Get classes by CBC level
        /// </summary>
        [HttpGet("by-level/{level}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetByLevel(CBCLevel level, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Class.Read"))
                return ForbiddenResponse("You do not have permission to view classes.");

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var classes = await _repositories.Class.GetByLevelAsync(targetSchoolId, level, trackChanges: false);
            var dtos = classes.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Get classes by teacher
        /// </summary>
        [HttpGet("by-teacher/{teacherId:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetByTeacher(Guid teacherId, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Class.Read"))
                return ForbiddenResponse("You do not have permission to view classes.");

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var classes = await _repositories.Class.GetByTeacherAsync(targetSchoolId, teacherId, trackChanges: false);
            var dtos = classes.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Create class - SuperAdmin or SchoolAdmin
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
        {
            if (!HasPermission("Class.Write"))
                return ForbiddenResponse("You do not have permission to create classes.");

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
                return NotFoundResponse("School not found.");

            // Verify academic year exists and belongs to school
            var academicYear = await _repositories.AcademicYear.GetByIdAsync(request.AcademicYearId, trackChanges: false);
            if (academicYear == null)
                return NotFoundResponse("Academic year not found.");

            if (academicYear.TenantId != targetSchoolId)
                return ErrorResponse("Academic year does not belong to this school.", 400);

            // Check if code already exists
            if (await _repositories.Class.CodeExistsAsync(targetSchoolId, request.Code ?? string.Empty))
                return ConflictResponse($"Class with code '{request.Code}' already exists.");

            // Verify teacher if provided
            if (request.TeacherId.HasValue)
            {
                var teacher = await _repositories.Teacher.GetByIdAsync(request.TeacherId.Value, trackChanges: false);
                if (teacher == null || teacher.TenantId != targetSchoolId)
                    return NotFoundResponse("Teacher not found or does not belong to this school.");
            }

            var classEntity = new Class
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId,
                Name = (request.Name ?? string.Empty).Trim(),
                Code = (request.Code ?? string.Empty).Trim(),
                Level = request.Level,
                Description = request.Description?.Trim(),
                Capacity = request.Capacity,
                CurrentEnrollment = 0,
                AcademicYearId = request.AcademicYearId,
                TeacherId = request.TeacherId,
                IsActive = request.IsActive
            };

            _repositories.Class.Create(classEntity);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("class.create", $"Created class {classEntity.Code} - {classEntity.Name}");

            return CreatedResponse($"/api/academic/class/{classEntity.Id}", ToDto(classEntity), "Class created successfully");
        }

        /// <summary>
        /// Update class - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassRequest request)
        {
            if (!HasPermission("Class.Write"))
                return ForbiddenResponse("You do not have permission to update classes.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var classEntity = await _repositories.Class.GetByIdAsync(id, trackChanges: true);
            if (classEntity == null)
                return NotFoundResponse("Class not found");

            // Non-SuperAdmin users can only update classes from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (classEntity.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only update classes from your school.");
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                classEntity.Name = request.Name.Trim();

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                if (await _repositories.Class.CodeExistsAsync(classEntity.TenantId, request.Code, id))
                    return ConflictResponse($"Class with code '{request.Code}' already exists.");

                classEntity.Code = request.Code.Trim();
            }

            if (request.Level.HasValue)
                classEntity.Level = request.Level.Value;

            if (request.Description != null)
                classEntity.Description = request.Description.Trim();

            if (request.Capacity.HasValue)
            {
                if (request.Capacity.Value < classEntity.CurrentEnrollment)
                    return ErrorResponse($"Capacity cannot be less than current enrollment ({classEntity.CurrentEnrollment}).", 400);

                classEntity.Capacity = request.Capacity.Value;
            }

            if (request.AcademicYearId.HasValue)
            {
                var academicYear = await _repositories.AcademicYear.GetByIdAsync(request.AcademicYearId.Value, trackChanges: false);
                if (academicYear == null || academicYear.TenantId != classEntity.TenantId)
                    return NotFoundResponse("Academic year not found or does not belong to this school.");

                classEntity.AcademicYearId = request.AcademicYearId.Value;
            }

            if (request.TeacherId.HasValue)
            {
                var teacher = await _repositories.Teacher.GetByIdAsync(request.TeacherId.Value, trackChanges: false);
                if (teacher == null || teacher.TenantId != classEntity.TenantId)
                    return NotFoundResponse("Teacher not found or does not belong to this school.");

                classEntity.TeacherId = request.TeacherId.Value;
            }

            if (request.IsActive.HasValue)
                classEntity.IsActive = request.IsActive.Value;

            _repositories.Class.Update(classEntity);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("class.update", $"Updated class {classEntity.Code} - {classEntity.Name}");

            return SuccessResponse(ToDto(classEntity), "Class updated successfully");
        }

        /// <summary>
        /// Delete class - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("Class.Delete"))
                return ForbiddenResponse("You do not have permission to delete classes.");

            var classEntity = await _repositories.Class.GetWithStudentsAsync(id, trackChanges: true);
            if (classEntity == null)
                return NotFoundResponse("Class not found");

            // Non-SuperAdmin users can only delete classes from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (classEntity.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only delete classes from your school.");
            }

            // Check if class has students
            if (classEntity.Students != null && classEntity.Students.Any())
                return ErrorResponse("Cannot delete class with enrolled students. Please reassign students first.", 400);

            var code = classEntity.Code;
            var name = classEntity.Name;

            _repositories.Class.Delete(classEntity);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("class.delete", $"Deleted class {code} - {name}");

            return SuccessResponse<object?>(null, "Class deleted successfully");
        }

        private static ClassDto ToDto(Class c) => new()
        {
            Id = c.Id,
            SchoolId = c.TenantId,
            Name = c.Name ?? string.Empty,
            Code = c.Code ?? string.Empty,
            Level = c.Level,
            LevelName = c.Level.ToString(),
            Description = c.Description,
            Capacity = c.Capacity,
            CurrentEnrollment = c.CurrentEnrollment,
            AvailableSeats = c.AvailableSeats,
            IsFull = c.IsFull,
            IsActive = c.IsActive,
            AcademicYearId = c.AcademicYearId,
            AcademicYearName = c.AcademicYear?.Name,
            AcademicYearCode = c.AcademicYear?.Code,
            TeacherId = c.TeacherId,
            TeacherName = c.ClassTeacher != null ? $"{c.ClassTeacher.FirstName} {c.ClassTeacher.LastName}" : null,
            CreatedOn = c.CreatedOn,
            CreatedBy = c.CreatedBy,
            UpdatedOn = c.UpdatedOn,
            UpdatedBy = c.UpdatedBy
        };

        private static ClassDetailDto ToDetailDto(Class c) => new()
        {
            Id = c.Id,
            SchoolId = c.TenantId,
            Name = c.Name ?? string.Empty,
            Code = c.Code ?? string.Empty,
            Level = c.Level,
            LevelName = c.Level.ToString(),
            Description = c.Description,
            Capacity = c.Capacity,
            CurrentEnrollment = c.CurrentEnrollment,
            AvailableSeats = c.AvailableSeats,
            IsFull = c.IsFull,
            IsActive = c.IsActive,
            AcademicYearId = c.AcademicYearId,
            AcademicYearName = c.AcademicYear?.Name,
            AcademicYearCode = c.AcademicYear?.Code,
            TeacherId = c.TeacherId,
            TeacherName = c.ClassTeacher != null ? $"{c.ClassTeacher.FirstName} {c.ClassTeacher.LastName}" : null,
            StudentCount = c.Students?.Count ?? 0,
            SubjectCount = c.Subjects?.Count ?? 0,
            CreatedOn = c.CreatedOn,
            CreatedBy = c.CreatedBy,
            UpdatedOn = c.UpdatedOn,
            UpdatedBy = c.UpdatedBy
        };
    }
}
