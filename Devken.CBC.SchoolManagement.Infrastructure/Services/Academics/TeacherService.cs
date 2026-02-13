using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValidationException = Devken.CBC.SchoolManagement.Application.Exceptions.ValidationException;

namespace Devken.CBC.SchoolManagement.Application.Services.Implementations.Academic
{
    public class TeacherService : ITeacherService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IImageUploadService _imageUpload;
        private readonly IDocumentNumberSeriesRepository _documentNumberService;

        // Constants
        private const string TEACHER_NUMBER_SERIES = "Teacher";
        private const string PHOTO_SUBFOLDER = "teachers";
        private const string DEFAULT_NATIONALITY = "Kenyan";

        public TeacherService(
            IRepositoryManager repositories,
            IImageUploadService imageUpload,
            IDocumentNumberSeriesRepository documentNumberService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _imageUpload = imageUpload ?? throw new ArgumentNullException(nameof(imageUpload));
            _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL TEACHERS
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var teachers = await FetchTeachersByAccessLevel(schoolId, userSchoolId, isSuperAdmin);
            return teachers.Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET TEACHER BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TeacherDto> GetTeacherByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var teacher = await _repositories.Teacher.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Teacher with ID '{id}' not found.");

            ValidateSchoolAccess(teacher.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(teacher);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE TEACHER - WITH AUTO-GENERATED TEACHER NUMBER!
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TeacherDto> CreateTeacherAsync(
            CreateTeacherRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // 1. Resolve and validate school
            var targetSchoolId = ResolveSchoolId(request.SchoolId, userSchoolId, isSuperAdmin);
            await ValidateSchoolExistsAsync(targetSchoolId);

            // 2. Generate or validate teacher number
            var teacherNumber = await ResolveTeacherNumberAsync(request.TeacherNumber, targetSchoolId);

            // 3. Create teacher entity
            var teacher = CreateTeacherEntity(request, targetSchoolId, teacherNumber);

            // 4. Save to database
            _repositories.Teacher.Create(teacher);
            await _repositories.SaveAsync();

            return MapToDto(teacher);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE TEACHER
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TeacherDto> UpdateTeacherAsync(
            Guid id,
            UpdateTeacherRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var teacher = await _repositories.Teacher.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Teacher with ID '{id}' not found.");

            ValidateSchoolAccess(teacher.TenantId, userSchoolId, isSuperAdmin);

            UpdateTeacherEntity(teacher, request);

            _repositories.Teacher.Update(teacher);
            await _repositories.SaveAsync();

            return MapToDto(teacher);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE TEACHER
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteTeacherAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var teacher = await _repositories.Teacher.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Teacher with ID '{id}' not found.");

            ValidateSchoolAccess(teacher.TenantId, userSchoolId, isSuperAdmin);

            await DeleteTeacherPhotoIfExistsAsync(teacher.PhotoUrl);

            _repositories.Teacher.Delete(teacher);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPLOAD TEACHER PHOTO
        // ─────────────────────────────────────────────────────────────────────
        public async Task<string> UploadTeacherPhotoAsync(
            Guid teacherId,
            IFormFile file,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var teacher = await _repositories.Teacher.GetByIdAsync(teacherId, true)
                ?? throw new NotFoundException($"Teacher with ID '{teacherId}' not found.");

            ValidateSchoolAccess(teacher.TenantId, userSchoolId, isSuperAdmin);

            // Delete old photo if exists
            await DeleteTeacherPhotoIfExistsAsync(teacher.PhotoUrl);

            // Upload new photo
            var photoUrl = await UploadPhotoWithValidationAsync(file);

            teacher.PhotoUrl = photoUrl;
            _repositories.Teacher.Update(teacher);
            await _repositories.SaveAsync();

            return photoUrl;
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE TEACHER PHOTO
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteTeacherPhotoAsync(
            Guid teacherId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var teacher = await _repositories.Teacher.GetByIdAsync(teacherId, true)
                ?? throw new NotFoundException($"Teacher with ID '{teacherId}' not found.");

            ValidateSchoolAccess(teacher.TenantId, userSchoolId, isSuperAdmin);

            if (string.IsNullOrWhiteSpace(teacher.PhotoUrl))
                throw new ValidationException("This teacher has no photo to delete.");

            await _imageUpload.DeleteImageAsync(teacher.PhotoUrl);

            teacher.PhotoUrl = null;
            _repositories.Teacher.Update(teacher);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // TOGGLE TEACHER STATUS
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TeacherDto> ToggleTeacherStatusAsync(
            Guid id,
            bool isActive,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var teacher = await _repositories.Teacher.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Teacher with ID '{id}' not found.");

            ValidateSchoolAccess(teacher.TenantId, userSchoolId, isSuperAdmin);

            teacher.IsActive = isActive;
            _repositories.Teacher.Update(teacher);
            await _repositories.SaveAsync();

            return MapToDto(teacher);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS - Business Logic
        // ─────────────────────────────────────────────────────────────────────
        private async Task<IEnumerable<Teacher>> FetchTeachersByAccessLevel(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // ✅ SuperAdmin: Full access
            if (isSuperAdmin)
            {
                if (schoolId.HasValue)
                    return await _repositories.Teacher
                        .GetBySchoolIdAsync(schoolId.Value, trackChanges: false);

                return await _repositories.Teacher
                    .GetAllAsync(trackChanges: false);
            }

            // ❌ Non-super admin must belong to a school
            if (!userSchoolId.HasValue)
                throw new UnauthorizedException(
                    "You must be assigned to a school to view teachers.");

            return await _repositories.Teacher
                .GetBySchoolIdAsync(userSchoolId.Value, trackChanges: false);
        }

        /// <summary>
        /// Resolves which school ID to use based on user type and request data.
        /// SuperAdmin must provide a valid SchoolId in the request.
        /// Regular users use their assigned school ID.
        /// </summary>
        private Guid ResolveSchoolId(Guid? requestSchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            // ✅ SuperAdmin: Must provide a valid school ID in the request
            if (isSuperAdmin)
            {
                if (!requestSchoolId.HasValue || requestSchoolId.Value == Guid.Empty)
                {
                    throw new ValidationException(
                        "SchoolId is required for SuperAdmin when creating a teacher. " +
                        "Please specify which school this teacher should be assigned to.");
                }
                return requestSchoolId.Value;
            }

            // ✅ Regular users: Use their assigned school ID
            if (!userSchoolId.HasValue || userSchoolId.Value == Guid.Empty)
            {
                throw new UnauthorizedException(
                    "You must be assigned to a school to create teachers.");
            }

            return userSchoolId.Value;
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school == null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private void ValidateSchoolAccess(Guid teacherSchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            // SuperAdmin can access any teacher
            if (isSuperAdmin)
                return;

            // Regular users can only access teachers from their school
            if (!userSchoolId.HasValue || teacherSchoolId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this teacher.");
        }

        private async Task<string> ResolveTeacherNumberAsync(string? requestTeacherNumber, Guid schoolId)
        {
            if (string.IsNullOrWhiteSpace(requestTeacherNumber))
            {
                // Generate teacher number - ensure series exists first
                return await GenerateTeacherNumberAsync(schoolId);
            }

            var teacherNumber = requestTeacherNumber.Trim();

            // Check if teacher number already exists in this school
            var existing = await _repositories.Teacher.GetByTeacherNumberAsync(teacherNumber, schoolId);
            if (existing != null)
                throw new ConflictException($"Teacher with number '{teacherNumber}' already exists in this school.");

            return teacherNumber;
        }

        private async Task<string> GenerateTeacherNumberAsync(Guid schoolId)
        {
            // Ensure number series exists for this school
            var seriesExists = await _documentNumberService.SeriesExistsAsync(TEACHER_NUMBER_SERIES, schoolId);
            if (!seriesExists)
            {
                await _documentNumberService.CreateSeriesAsync(
                    entityName: TEACHER_NUMBER_SERIES,
                    prefix: "TCH",
                    padding: 5,
                    resetEveryYear: true,
                    description: "Teacher employment numbers");
            }

            return await _documentNumberService.GenerateAsync(TEACHER_NUMBER_SERIES, schoolId);
        }

        private Teacher CreateTeacherEntity(CreateTeacherRequest request, Guid schoolId, string teacherNumber)
        {
            return new Teacher
            {
                Id = Guid.NewGuid(),
                TenantId = schoolId,
                FirstName = request.FirstName.Trim(),
                MiddleName = request.MiddleName?.Trim(),
                LastName = request.LastName.Trim(),
                TeacherNumber = teacherNumber,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                TscNumber = request.TscNumber?.Trim(),
                Nationality = request.Nationality?.Trim() ?? DEFAULT_NATIONALITY,
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
        }

        private void UpdateTeacherEntity(Teacher teacher, UpdateTeacherRequest request)
        {
            teacher.FirstName = request.FirstName.Trim();
            teacher.MiddleName = request.MiddleName?.Trim();
            teacher.LastName = request.LastName.Trim();
            teacher.DateOfBirth = request.DateOfBirth;
            teacher.Gender = request.Gender;
            teacher.TscNumber = request.TscNumber?.Trim();
            teacher.Nationality = request.Nationality?.Trim() ?? DEFAULT_NATIONALITY;
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
        }

        private async Task DeleteTeacherPhotoIfExistsAsync(string? photoUrl)
        {
            if (!string.IsNullOrWhiteSpace(photoUrl))
                await _imageUpload.DeleteImageAsync(photoUrl);
        }

        private async Task<string> UploadPhotoWithValidationAsync(IFormFile file)
        {
            try
            {
                return await _imageUpload.UploadImageAsync(file, subFolder: PHOTO_SUBFOLDER);
            }
            catch (Exception ex)
            {
                throw new ValidationException($"Failed to upload photo: {ex.Message}");
            }
        }

        private TeacherDto MapToDto(Teacher teacher)
        {
            return new TeacherDto
            {
                Id = teacher.Id,
                SchoolId = teacher.TenantId,
                SchoolName = teacher.School?.Name ?? string.Empty,
                FirstName = teacher.FirstName ?? string.Empty,
                MiddleName = teacher.MiddleName ?? string.Empty,
                LastName = teacher.LastName ?? string.Empty,
                FullName = teacher.FullName ?? string.Empty,
                DisplayName = teacher.DisplayName ?? string.Empty,
                TeacherNumber = teacher.TeacherNumber ?? string.Empty,
                DateOfBirth = teacher.DateOfBirth,
                Age = teacher.Age ?? 0,
                Gender = teacher.Gender.ToString(),
                TscNumber = teacher.TscNumber ?? string.Empty,
                Nationality = teacher.Nationality ?? DEFAULT_NATIONALITY,
                IdNumber = teacher.IdNumber ?? string.Empty,
                PhoneNumber = teacher.PhoneNumber ?? string.Empty,
                Email = teacher.Email ?? string.Empty,
                Address = teacher.Address ?? string.Empty,
                EmploymentType = teacher.EmploymentType.ToString(),
                Designation = teacher.Designation.ToString(),
                Qualification = teacher.Qualification ?? string.Empty,
                Specialization = teacher.Specialization ?? string.Empty,
                DateOfEmployment = teacher.DateOfEmployment,
                IsClassTeacher = teacher.IsClassTeacher,
                CurrentClassId = teacher.CurrentClassId,
                CurrentClassName = teacher.CurrentClass?.Name ?? string.Empty,
                PhotoUrl = teacher.PhotoUrl ?? string.Empty,
                IsActive = teacher.IsActive,
                Notes = teacher.Notes ?? string.Empty
            };
        }
    }
}