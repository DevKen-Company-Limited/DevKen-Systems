using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
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
        private readonly IRepositoryManager _repositories;
        private readonly IImageUploadService _imageUpload;

        public TeachersController(
            IRepositoryManager repositories,
            IImageUploadService imageUpload,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _imageUpload = imageUpload ?? throw new ArgumentNullException(nameof(imageUpload));
        }

        // ── GET api/academic/teachers ─────────────────────────────────────────
        /// <summary>
        /// Get teachers – SuperAdmin sees all, others only their school.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Teacher.Read"))
                return ForbiddenResponse("You do not have permission to view teachers.");

            if (IsSuperAdmin)
            {
                var teachers = schoolId.HasValue
                    ? await _repositories.Teacher.GetBySchoolIdAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.Teacher.GetAllAsync(trackChanges: false);

                return SuccessResponse(teachers.Select(ToDto));
            }

            var userSchoolId = GetCurrentUserSchoolId();
            if (userSchoolId == null)
                return ForbiddenResponse("You must be assigned to a school to view teachers.");

            var schoolTeachers = await _repositories.Teacher.GetBySchoolIdAsync(userSchoolId, trackChanges: false);
            return SuccessResponse(schoolTeachers.Select(ToDto));
        }

        // ── GET api/academic/teachers/{id} ────────────────────────────────────
        /// <summary>
        /// Get teacher by ID – SuperAdmin or same school.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("Teacher.Read"))
                return ForbiddenResponse("You do not have permission to view this teacher.");

            var teacher = await _repositories.Teacher.GetByIdWithDetailsAsync(id, trackChanges: false);
            if (teacher == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(teacher.TenantId);
            if (accessError != null)
                return accessError;

            return SuccessResponse(ToDto(teacher));
        }

        // ── POST api/academic/teachers ────────────────────────────────────────
        /// <summary>
        /// Create a teacher – SuperAdmin or SchoolAdmin (own school).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTeacherRequest request)
        {
            if (!HasPermission("Teacher.Write"))
                return ForbiddenResponse("You do not have permission to create teachers.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            // Resolve target school
            Guid targetSchoolId;
            if (IsSuperAdmin)
            {
                targetSchoolId = request.SchoolId;
            }
            else
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (userSchoolId == null)
                    return ForbiddenResponse("You must be assigned to a school to create teachers.");
                targetSchoolId = userSchoolId;
            }

            var accessError = ValidateSchoolAccess(targetSchoolId);
            if (accessError != null)
                return accessError;

            var school = await _repositories.School.GetByIdAsync(targetSchoolId, trackChanges: false);
            if (school == null)
                return ErrorResponse("School not found.", 404);

            var existing = await _repositories.Teacher.GetByTeacherNumberAsync(
                request.TeacherNumber ?? string.Empty, targetSchoolId);

            if (existing != null)
                return ErrorResponse(
                    $"Teacher with number '{request.TeacherNumber}' already exists in this school.", 409);

            var teacher = new Teacher
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId,
                FirstName = request.FirstName.Trim(),
                MiddleName = request.MiddleName?.Trim(),
                LastName = request.LastName.Trim(),
                TeacherNumber = request.TeacherNumber.Trim(),
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                TscNumber = request.TscNumber?.Trim(),
                Nationality = request.Nationality?.Trim() ?? "Kenyan",
                IdNumber = request.IdNumber?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Email = request.Email?.Trim(),
                Address = request.Address?.Trim(),
                EmploymentType = request.EmploymentType,
                Designation = request.Designation,
                Qualification = request.Qualification?.Trim(),
                Specialization = request.Specialization?.Trim(),
                DateOfEmployment = request.DateOfEmployment,
                IsClassTeacher = request.IsClassTeacher,
                CurrentClassId = request.CurrentClassId,
                PhotoUrl = request.PhotoUrl?.Trim(),
                IsActive = request.IsActive,
                Notes = request.Notes?.Trim()
            };

            _repositories.Teacher.Create(teacher);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "teacher.create",
                $"Created teacher {teacher.TeacherNumber} - {teacher.FullName}");

            return SuccessResponse(ToDto(teacher), "Teacher created successfully");
        }

        // ── PUT api/academic/teachers/{id} ────────────────────────────────────
        /// <summary>
        /// Update a teacher – SuperAdmin or same school.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeacherRequest request)
        {
            if (!HasPermission("Teacher.Write"))
                return ForbiddenResponse("You do not have permission to update teachers.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var teacher = await _repositories.Teacher.GetByIdAsync(id, trackChanges: true);
            if (teacher == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(teacher.TenantId);
            if (accessError != null)
                return accessError;

            teacher.FirstName = request.FirstName.Trim();
            teacher.MiddleName = request.MiddleName?.Trim();
            teacher.LastName = request.LastName.Trim();
            teacher.DateOfBirth = request.DateOfBirth;
            teacher.Gender = request.Gender;
            teacher.TscNumber = request.TscNumber?.Trim();
            teacher.Nationality = request.Nationality?.Trim() ?? "Kenyan";
            teacher.IdNumber = request.IdNumber?.Trim();
            teacher.PhoneNumber = request.PhoneNumber?.Trim();
            teacher.Email = request.Email?.Trim();
            teacher.Address = request.Address?.Trim();
            teacher.EmploymentType = request.EmploymentType;
            teacher.Designation = request.Designation;
            teacher.Qualification = request.Qualification?.Trim();
            teacher.Specialization = request.Specialization?.Trim();
            teacher.DateOfEmployment = request.DateOfEmployment;
            teacher.IsClassTeacher = request.IsClassTeacher;
            teacher.CurrentClassId = request.CurrentClassId;
            teacher.PhotoUrl = request.PhotoUrl?.Trim();
            teacher.IsActive = request.IsActive;
            teacher.Notes = request.Notes?.Trim();

            _repositories.Teacher.Update(teacher);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "teacher.update",
                $"Updated teacher {teacher.TeacherNumber} - {teacher.FullName}");

            return SuccessResponse(ToDto(teacher), "Teacher updated successfully");
        }

        // ── DELETE api/academic/teachers/{id} ────────────────────────────────
        /// <summary>
        /// Delete a teacher – SuperAdmin or same school.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("Teacher.Delete"))
                return ForbiddenResponse("You do not have permission to delete teachers.");

            var teacher = await _repositories.Teacher.GetByIdAsync(id, trackChanges: true);
            if (teacher == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(teacher.TenantId);
            if (accessError != null)
                return accessError;

            // If there is a stored photo, remove it from disk too
            if (!string.IsNullOrWhiteSpace(teacher.PhotoUrl))
                await _imageUpload.DeleteImageAsync(teacher.PhotoUrl);

            _repositories.Teacher.Delete(teacher);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "teacher.delete",
                $"Deleted teacher {teacher.TeacherNumber} - {teacher.FullName}");

            return SuccessResponse<object?>(null, "Teacher deleted successfully");
        }

        // ── POST api/academic/teachers/{id}/photo ────────────────────────────
        /// <summary>
        /// Upload or replace a teacher's profile photo.
        /// Accepts multipart/form-data with field name "file".
        /// </summary>
        [HttpPost("{id:guid}/photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
        {
            if (!HasPermission("Teacher.Write"))
                return ForbiddenResponse("You do not have permission to update teacher photos.");

            var teacher = await _repositories.Teacher.GetByIdAsync(id, trackChanges: true);
            if (teacher == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(teacher.TenantId);
            if (accessError != null)
                return accessError;

            // Delete old photo if one already exists
            if (!string.IsNullOrWhiteSpace(teacher.PhotoUrl))
                await _imageUpload.DeleteImageAsync(teacher.PhotoUrl);

            string photoUrl;
            try
            {
                photoUrl = await _imageUpload.UploadImageAsync(file, subFolder: "teachers");
            }
            catch (Exception ex)
            {
                return ErrorResponse(ex.Message, 400);
            }

            teacher.PhotoUrl = photoUrl;

            _repositories.Teacher.Update(teacher);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "teacher.photo.upload",
                $"Uploaded photo for teacher {teacher.TeacherNumber} - {teacher.FullName}");

            return SuccessResponse(new { photoUrl }, "Photo uploaded successfully");
        }

        // ── DELETE api/academic/teachers/{id}/photo ──────────────────────────
        /// <summary>
        /// Remove a teacher's profile photo.
        /// </summary>
        [HttpDelete("{id:guid}/photo")]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            if (!HasPermission("Teacher.Write"))
                return ForbiddenResponse("You do not have permission to remove teacher photos.");

            var teacher = await _repositories.Teacher.GetByIdAsync(id, trackChanges: true);
            if (teacher == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(teacher.TenantId);
            if (accessError != null)
                return accessError;

            if (string.IsNullOrWhiteSpace(teacher.PhotoUrl))
                return ErrorResponse("This teacher has no photo to delete.", 400);

            await _imageUpload.DeleteImageAsync(teacher.PhotoUrl);

            teacher.PhotoUrl = null;
            _repositories.Teacher.Update(teacher);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "teacher.photo.delete",
                $"Deleted photo for teacher {teacher.TeacherNumber} - {teacher.FullName}");

            return SuccessResponse<object?>(null, "Photo deleted successfully");
        }

        // ── Mapper ────────────────────────────────────────────────────────────
        private static TeacherDto ToDto(Teacher t) => new()
        {
            Id = t.Id,
            SchoolId = t.TenantId,
            FirstName = t.FirstName,
            MiddleName = t.MiddleName ?? string.Empty,
            LastName = t.LastName,
            FullName = t.FullName,
            DisplayName = t.DisplayName,
            TeacherNumber = t.TeacherNumber,
            DateOfBirth = t.DateOfBirth,
            Age = t.Age,
            Gender = t.Gender.ToString(),
            TscNumber = t.TscNumber ?? string.Empty,
            Nationality = t.Nationality ?? "Kenyan",
            IdNumber = t.IdNumber ?? string.Empty,
            PhoneNumber = t.PhoneNumber ?? string.Empty,
            Email = t.Email ?? string.Empty,
            Address = t.Address ?? string.Empty,
            EmploymentType = t.EmploymentType.ToString(),
            Designation = t.Designation.ToString(),
            Qualification = t.Qualification ?? string.Empty,
            Specialization = t.Specialization ?? string.Empty,
            DateOfEmployment = t.DateOfEmployment,
            IsClassTeacher = t.IsClassTeacher,
            CurrentClassId = t.CurrentClassId,
            CurrentClassName = t.CurrentClass?.Name ?? string.Empty,
            PhotoUrl = t.PhotoUrl ?? string.Empty,
            IsActive = t.IsActive,
            Notes = t.Notes ?? string.Empty
        };
    }
}