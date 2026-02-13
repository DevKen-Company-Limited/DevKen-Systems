using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Service.Administration.Student;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValidationException = Devken.CBC.SchoolManagement.Application.Exceptions.ValidationException;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Administration.Students
{
    /// <summary>
    /// Service for student-related business logic
    /// Handles validation, tenant isolation, and business rules
    /// Uses execution strategy for create operations to handle retry conflicts
    /// </summary>
    public class StudentService : IStudentService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IDocumentNumberSeriesRepository _numberSeries;
        private readonly IImageUploadService _imageUpload;

        // Constants
        private const string STUDENT_NUMBER_SERIES = "Student";
        private const string PHOTO_SUBFOLDER = "students";
        private const string DEFAULT_NATIONALITY = "Kenyan";

        public StudentService(
            IRepositoryManager repositories,
            IDocumentNumberSeriesRepository numberSeries,
            IImageUploadService imageUpload)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _numberSeries = numberSeries ?? throw new ArgumentNullException(nameof(numberSeries));
            _imageUpload = imageUpload ?? throw new ArgumentNullException(nameof(imageUpload));
        }

        #region Get Operations

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            IEnumerable<Student> students;

            if (isSuperAdmin)
            {
                // SuperAdmin: return all or filter by school
                students = schoolId.HasValue
                    ? await _repositories.Student.GetBySchoolIdAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.Student.GetAllAsync(trackChanges: false);
            }
            else
            {
                // Non-SuperAdmin: only their school
                if (userSchoolId == null)
                    throw new UnauthorizedException("You must be assigned to a school to view students.");

                students = await _repositories.Student.GetBySchoolIdAsync(userSchoolId.Value, trackChanges: false);
            }

            return students.Select(ToDto);
        }

        public async Task<StudentDto> GetStudentByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            // Validate access
            if (!isSuperAdmin && student.TenantId != userSchoolId)
                throw new UnauthorizedException("You do not have access to this student.");

            return ToDto(student);
        }

        #endregion

        #region Create Operation - WITH EXECUTION STRATEGY

        public async Task<StudentDto> CreateStudentAsync(
            CreateStudentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // FIXED: Wrap entire operation in execution strategy to handle retry conflicts
            var strategy = _repositories.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // 1. Determine target school
                var targetSchoolId = ResolveSchoolId(request.SchoolId, userSchoolId, isSuperAdmin);

                // 2. Validate school exists
                await ValidateSchoolExistsAsync(targetSchoolId);

                // 3. Generate or validate admission number
                var admissionNumber = await ResolveAdmissionNumberAsync(request.AdmissionNumber, targetSchoolId);

                // 4. Validate class if provided
                if (request.CurrentClassId.HasValue)
                {
                    await ValidateClassExistsAsync(request.CurrentClassId.Value);
                }

                // 5. Create student entity
                var student = CreateStudentEntity(request, targetSchoolId, admissionNumber);

                // 6. Save to database (EF Core handles the transaction)
                _repositories.Student.Create(student);
                await _repositories.SaveAsync();

                // 7. Return DTO
                return ToDto(student);
            });
        }

        #endregion

        #region Update Operation

        public async Task<StudentDto> UpdateStudentAsync(
            Guid id,
            UpdateStudentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            // Validate access
            if (!isSuperAdmin && student.TenantId != userSchoolId)
                throw new UnauthorizedException("You do not have access to update this student.");

            // Validate class if changed
            if (request.CurrentClassId.HasValue && request.CurrentClassId != student.CurrentClassId)
            {
                await ValidateClassExistsAsync(request.CurrentClassId.Value);
            }

            // Update student entity
            UpdateStudentEntity(student, request);

            _repositories.Student.Update(student);
            await _repositories.SaveAsync();

            return ToDto(student);
        }

        #endregion

        #region Delete Operation

        public async Task DeleteStudentAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            // Validate access
            if (!isSuperAdmin && student.TenantId != userSchoolId)
                throw new UnauthorizedException("You do not have access to delete this student.");

            // Store photo URL before deletion
            var photoUrl = student.PhotoUrl;

            // Delete student from database FIRST
            _repositories.Student.Delete(student);
            await _repositories.SaveAsync();

            // Delete photo AFTER successful database deletion
            await DeletePhotoIfExistsAsync(photoUrl);
        }

        #endregion

        #region Status Toggle

        public async Task<StudentDto> ToggleStudentStatusAsync(
            Guid id,
            bool isActive,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            // Validate access
            if (!isSuperAdmin && student.TenantId != userSchoolId)
                throw new UnauthorizedException("You do not have access to update this student.");

            student.IsActive = isActive;

            _repositories.Student.Update(student);
            await _repositories.SaveAsync();

            return ToDto(student);
        }

        #endregion

        #region Photo Management

        public async Task<string> UploadStudentPhotoAsync(
            Guid id,
            IFormFile file,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            // Validate access
            if (!isSuperAdmin && student.TenantId != userSchoolId)
                throw new UnauthorizedException("You do not have access to update this student.");

            // Store old photo URL for cleanup
            var oldPhotoUrl = student.PhotoUrl;

            // Upload new photo first (if this fails, nothing is changed)
            var newPhotoUrl = await UploadPhotoWithValidationAsync(file);

            // Update database with new photo URL
            student.PhotoUrl = newPhotoUrl;
            _repositories.Student.Update(student);
            await _repositories.SaveAsync();

            // Delete old photo after successful database update
            await DeletePhotoIfExistsAsync(oldPhotoUrl);

            return newPhotoUrl;
        }

        public async Task DeleteStudentPhotoAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            // Validate access
            if (!isSuperAdmin && student.TenantId != userSchoolId)
                throw new UnauthorizedException("You do not have access to update this student.");

            if (string.IsNullOrWhiteSpace(student.PhotoUrl))
                throw new ValidationException("This student has no photo to delete.");

            // Store photo URL for cleanup
            var photoUrl = student.PhotoUrl;

            // Update database FIRST
            student.PhotoUrl = null;
            _repositories.Student.Update(student);
            await _repositories.SaveAsync();

            // Delete physical file AFTER successful database update
            try
            {
                await _imageUpload.DeleteImageAsync(photoUrl);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the operation
                System.Diagnostics.Debug.WriteLine($"Failed to delete photo file: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods - Business Logic

        private Guid ResolveSchoolId(Guid? requestSchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (requestSchoolId == null || requestSchoolId == Guid.Empty)
                {
                    throw new ValidationException(
                        "SchoolId is required for SuperAdmin when creating a student. " +
                        "Please specify which school this student should be assigned to.");
                }
                return requestSchoolId.Value;
            }

            if (userSchoolId == null || userSchoolId == Guid.Empty)
            {
                throw new UnauthorizedException(
                    "You must be assigned to a school to create students.");
            }

            return userSchoolId.Value;
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school == null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private async Task ValidateClassExistsAsync(Guid classId)
        {
            var classEntity = await _repositories.Class.GetByIdAsync(classId, false);
            if (classEntity == null)
                throw new NotFoundException($"Class with ID '{classId}' not found.");
        }

        private async Task<string> ResolveAdmissionNumberAsync(string? requestAdmissionNumber, Guid schoolId)
        {
            if (string.IsNullOrWhiteSpace(requestAdmissionNumber))
            {
                // Generate admission number
                return await GenerateAdmissionNumberAsync(schoolId);
            }

            var admissionNumber = requestAdmissionNumber.Trim();

            // Check for duplicates
            var exists = await AdmissionNumberExistsAsync(admissionNumber, schoolId);
            if (exists)
                throw new ConflictException($"Admission number '{admissionNumber}' already exists in this school.");

            return admissionNumber;
        }

        public async Task<string> GenerateAdmissionNumberAsync(Guid schoolId)
        {
            // Ensure number series exists for this school
            var seriesExists = await _numberSeries.SeriesExistsAsync(STUDENT_NUMBER_SERIES, schoolId);
            if (!seriesExists)
            {
                // Use the overloaded method that accepts tenantId explicitly
                await _numberSeries.CreateSeriesAsync(
                    entityName: STUDENT_NUMBER_SERIES,
                    tenantId: schoolId,
                    prefix: "ADM",
                    padding: 5,
                    resetEveryYear: true,
                    description: "Student admission numbers");
            }

            return await _numberSeries.GenerateAsync(STUDENT_NUMBER_SERIES, schoolId);
        }

        public async Task<bool> AdmissionNumberExistsAsync(
            string admissionNumber,
            Guid schoolId,
            Guid? excludeStudentId = null)
        {
            var existing = await _repositories.Student.GetByAdmissionNumberAsync(
                admissionNumber,
                schoolId);

            if (existing == null)
                return false;

            // If excluding a student (for updates), check if it's the same student
            if (excludeStudentId.HasValue && existing.Id == excludeStudentId.Value)
                return false;

            return true;
        }

        public async Task<bool> CanPromoteStudentAsync(Guid id)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            return student.IsEligibleForPromotion();
        }

        public async Task<int> GetStudentCountBySchoolAsync(Guid schoolId)
        {
            var students = await _repositories.Student.GetBySchoolIdAsync(schoolId, trackChanges: false);
            return students.Count();
        }

        private async Task DeletePhotoIfExistsAsync(string? photoUrl)
        {
            if (string.IsNullOrWhiteSpace(photoUrl))
                return;

            try
            {
                await _imageUpload.DeleteImageAsync(photoUrl);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw
                System.Diagnostics.Debug.WriteLine($"Failed to delete photo file: {ex.Message}");
            }
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

        #endregion

        #region Entity Creation/Update

        private Student CreateStudentEntity(CreateStudentRequest request, Guid schoolId, string admissionNumber)
        {
            return new Student
            {
                Id = Guid.NewGuid(),
                TenantId = schoolId,

                // Personal Information
                FirstName = request.FirstName?.Trim() ?? throw new ValidationException("FirstName is required."),
                LastName = request.LastName?.Trim() ?? throw new ValidationException("LastName is required."),
                MiddleName = request.MiddleName?.Trim(),
                AdmissionNumber = admissionNumber,
                NemisNumber = request.NemisNumber?.Trim(),
                BirthCertificateNumber = request.BirthCertificateNumber?.Trim(),
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                PlaceOfBirth = request.PlaceOfBirth?.Trim(),
                Nationality = request.Nationality?.Trim() ?? DEFAULT_NATIONALITY,
                County = request.County?.Trim(),
                SubCounty = request.SubCounty?.Trim(),
                HomeAddress = request.HomeAddress?.Trim(),
                Religion = request.Religion?.Trim(),

                // Academic Details
                DateOfAdmission = request.DateOfAdmission ?? DateTime.UtcNow,
                StudentStatus = request.StudentStatus,
                CBCLevel = request.CBCLevel,
                CurrentLevel = request.CurrentLevel,
                CurrentClassId = request.CurrentClassId,
                CurrentAcademicYearId = request.CurrentAcademicYearId,
                Status = StudentStatus.Active,
                PreviousSchool = request.PreviousSchool?.Trim(),

                // Medical Information
                BloodGroup = request.BloodGroup?.Trim(),
                MedicalConditions = request.MedicalConditions?.Trim(),
                Allergies = request.Allergies?.Trim(),
                SpecialNeeds = request.SpecialNeeds?.Trim(),
                RequiresSpecialSupport = request.RequiresSpecialSupport,

                // Guardian Information
                PrimaryGuardianName = request.PrimaryGuardianName?.Trim()
                    ?? throw new ValidationException("Primary guardian name is required."),
                PrimaryGuardianRelationship = request.PrimaryGuardianRelationship?.Trim()
                    ?? throw new ValidationException("Primary guardian relationship is required."),
                PrimaryGuardianPhone = request.PrimaryGuardianPhone?.Trim()
                    ?? throw new ValidationException("Primary guardian phone is required."),
                PrimaryGuardianEmail = request.PrimaryGuardianEmail?.Trim(),
                PrimaryGuardianOccupation = request.PrimaryGuardianOccupation?.Trim(),
                PrimaryGuardianAddress = request.PrimaryGuardianAddress?.Trim(),

                SecondaryGuardianName = request.SecondaryGuardianName?.Trim(),
                SecondaryGuardianRelationship = request.SecondaryGuardianRelationship?.Trim(),
                SecondaryGuardianPhone = request.SecondaryGuardianPhone?.Trim(),
                SecondaryGuardianEmail = request.SecondaryGuardianEmail?.Trim(),
                SecondaryGuardianOccupation = request.SecondaryGuardianOccupation?.Trim(),

                EmergencyContactName = request.EmergencyContactName?.Trim(),
                EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
                EmergencyContactRelationship = request.EmergencyContactRelationship?.Trim(),

                // Additional
                PhotoUrl = request.PhotoUrl?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = request.IsActive
            };
        }

        private void UpdateStudentEntity(Student student, UpdateStudentRequest request)
        {
            // Personal Information
            student.FirstName = request.FirstName?.Trim() ?? throw new ValidationException("FirstName is required.");
            student.LastName = request.LastName?.Trim() ?? throw new ValidationException("LastName is required.");
            student.MiddleName = request.MiddleName?.Trim();
            student.NemisNumber = request.NemisNumber?.Trim();
            student.BirthCertificateNumber = request.BirthCertificateNumber?.Trim();
            student.DateOfBirth = request.DateOfBirth;
            student.Gender = request.Gender;
            student.PlaceOfBirth = request.PlaceOfBirth?.Trim();
            student.Nationality = request.Nationality?.Trim() ?? DEFAULT_NATIONALITY;
            student.County = request.County?.Trim();
            student.SubCounty = request.SubCounty?.Trim();
            student.HomeAddress = request.HomeAddress?.Trim();
            student.Religion = request.Religion?.Trim();

            // Academic Details
            student.StudentStatus = request.StudentStatus;
            student.CBCLevel = request.CBCLevel;
            student.CurrentLevel = request.CurrentLevel;
            student.CurrentClassId = request.CurrentClassId;
            student.CurrentAcademicYearId = request.CurrentAcademicYearId;
            student.PreviousSchool = request.PreviousSchool?.Trim();

            // Medical Information
            student.BloodGroup = request.BloodGroup?.Trim();
            student.MedicalConditions = request.MedicalConditions?.Trim();
            student.Allergies = request.Allergies?.Trim();
            student.SpecialNeeds = request.SpecialNeeds?.Trim();
            student.RequiresSpecialSupport = request.RequiresSpecialSupport;

            // Guardian Information
            student.PrimaryGuardianName = request.PrimaryGuardianName?.Trim()
                ?? throw new ValidationException("Primary guardian name is required.");
            student.PrimaryGuardianRelationship = request.PrimaryGuardianRelationship?.Trim()
                ?? throw new ValidationException("Primary guardian relationship is required.");
            student.PrimaryGuardianPhone = request.PrimaryGuardianPhone?.Trim()
                ?? throw new ValidationException("Primary guardian phone is required.");
            student.PrimaryGuardianEmail = request.PrimaryGuardianEmail?.Trim();
            student.PrimaryGuardianOccupation = request.PrimaryGuardianOccupation?.Trim();
            student.PrimaryGuardianAddress = request.PrimaryGuardianAddress?.Trim();

            student.SecondaryGuardianName = request.SecondaryGuardianName?.Trim();
            student.SecondaryGuardianRelationship = request.SecondaryGuardianRelationship?.Trim();
            student.SecondaryGuardianPhone = request.SecondaryGuardianPhone?.Trim();
            student.SecondaryGuardianEmail = request.SecondaryGuardianEmail?.Trim();
            student.SecondaryGuardianOccupation = request.SecondaryGuardianOccupation?.Trim();

            student.EmergencyContactName = request.EmergencyContactName?.Trim();
            student.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
            student.EmergencyContactRelationship = request.EmergencyContactRelationship?.Trim();

            // Additional
            student.PhotoUrl = request.PhotoUrl?.Trim();
            student.Notes = request.Notes?.Trim();
            student.IsActive = request.IsActive;
        }

        #endregion

        #region DTO Mapping

        // ─── DTO Mapping Section for StudentService.cs ───────────────────────────────
        // REPLACE the ToDto method with this version

        private static StudentDto ToDto(Student s) => new()
        {
            Id = s.Id,
            SchoolId = s.TenantId,
            SchoolName = s.School?.Name ?? string.Empty,
            FirstName = s.FirstName ?? string.Empty,
            LastName = s.LastName ?? string.Empty,
            MiddleName = s.MiddleName ?? string.Empty,
            FullName = s.FullName,
            AdmissionNumber = s.AdmissionNumber ?? string.Empty,
            NemisNumber = s.NemisNumber ?? string.Empty,
            BirthCertificateNumber = s.BirthCertificateNumber ?? string.Empty,
            DateOfBirth = s.DateOfBirth,
            Age = s.Age,

            // CRITICAL: Return enum values as INTEGERS, not strings
            Gender = ((int)s.Gender).ToString(),  // Convert enum to int, then to string for JSON

            PlaceOfBirth = s.PlaceOfBirth ?? string.Empty,
            Nationality = s.Nationality ?? DEFAULT_NATIONALITY,
            County = s.County ?? string.Empty,
            SubCounty = s.SubCounty ?? string.Empty,
            HomeAddress = s.HomeAddress ?? string.Empty,
            Religion = s.Religion ?? string.Empty,
            DateOfAdmission = s.DateOfAdmission,

            // CRITICAL: Return enum values as INTEGERS, not strings
            StudentStatus = ((int)s.StudentStatus).ToString(),
            CBCLevel = ((int)s.CBCLevel).ToString(),
            CurrentLevel = ((int)s.CurrentLevel).ToString(),

            CurrentClassId = s.CurrentClassId,
            CurrentClassName = s.CurrentClass?.Name ?? string.Empty,
            CurrentAcademicYearId = s.CurrentAcademicYearId,
            Status = s.Status.ToString(),  // This can stay as string since it's not used in forms
            PreviousSchool = s.PreviousSchool ?? string.Empty,
            BloodGroup = s.BloodGroup ?? string.Empty,
            MedicalConditions = s.MedicalConditions ?? string.Empty,
            Allergies = s.Allergies ?? string.Empty,
            SpecialNeeds = s.SpecialNeeds ?? string.Empty,
            RequiresSpecialSupport = s.RequiresSpecialSupport,
            PrimaryGuardianName = s.PrimaryGuardianName ?? string.Empty,
            PrimaryGuardianRelationship = s.PrimaryGuardianRelationship ?? string.Empty,
            PrimaryGuardianPhone = s.PrimaryGuardianPhone ?? string.Empty,
            PrimaryGuardianEmail = s.PrimaryGuardianEmail ?? string.Empty,
            PrimaryGuardianOccupation = s.PrimaryGuardianOccupation ?? string.Empty,
            PrimaryGuardianAddress = s.PrimaryGuardianAddress ?? string.Empty,
            SecondaryGuardianName = s.SecondaryGuardianName ?? string.Empty,
            SecondaryGuardianRelationship = s.SecondaryGuardianRelationship ?? string.Empty,
            SecondaryGuardianPhone = s.SecondaryGuardianPhone ?? string.Empty,
            SecondaryGuardianEmail = s.SecondaryGuardianEmail ?? string.Empty,
            SecondaryGuardianOccupation = s.SecondaryGuardianOccupation ?? string.Empty,
            EmergencyContactName = s.EmergencyContactName ?? string.Empty,
            EmergencyContactPhone = s.EmergencyContactPhone ?? string.Empty,
            EmergencyContactRelationship = s.EmergencyContactRelationship ?? string.Empty,
            PhotoUrl = s.PhotoUrl ?? string.Empty,
            Notes = s.Notes ?? string.Empty,
            IsActive = s.IsActive
        };

        #endregion
    }
}