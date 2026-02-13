using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Service.Administration.Student;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Administration.Students
{
    /// <summary>
    /// Service for student-related business logic
    /// Handles validation, tenant isolation, and business rules
    /// </summary>
    public class StudentService : IStudentService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IDocumentNumberSeriesRepository _numberSeries;
        private readonly IImageUploadService _imageUpload;

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

        #region Create Operation

        public async Task<StudentDto> CreateStudentAsync(
            CreateStudentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // Determine target school
            Guid targetSchoolId;
            if (isSuperAdmin)
            {
                if (request.SchoolId == Guid.Empty)
                    throw new ValidationException("SchoolId is required for SuperAdmin.");

                targetSchoolId = request.SchoolId;
            }
            else
            {
                if (userSchoolId == null)
                    throw new UnauthorizedException("You must be assigned to a school to create students.");

                targetSchoolId = userSchoolId.Value;
            }

            // Validate access to target school
            if (!isSuperAdmin && targetSchoolId != userSchoolId)
                throw new UnauthorizedException("You do not have access to create students in this school.");

            // Validate school exists
            var school = await _repositories.School.GetByIdAsync(targetSchoolId, false)
                ?? throw new NotFoundException($"School with ID '{targetSchoolId}' not found.");

            // Generate or validate admission number
            string admissionNumber;
            if (string.IsNullOrWhiteSpace(request.AdmissionNumber))
            {
                admissionNumber = await GenerateAdmissionNumberAsync(targetSchoolId);
            }
            else
            {
                admissionNumber = request.AdmissionNumber.Trim();

                // Check for duplicates
                var exists = await AdmissionNumberExistsAsync(admissionNumber, targetSchoolId);
                if (exists)
                    throw new ConflictException($"Admission number '{admissionNumber}' already exists in this school.");
            }

            // Validate class exists if provided
            if (request.CurrentClassId.HasValue)
            {
                var classExists = await _repositories.Class.GetByIdAsync(request.CurrentClassId.Value, false);
                if (classExists == null)
                    throw new NotFoundException($"Class with ID '{request.CurrentClassId}' not found.");
            }

            // Create student entity
            var student = new Student
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId,

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
                Nationality = request.Nationality?.Trim() ?? "Kenyan",
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
                PrimaryGuardianName = request.PrimaryGuardianName?.Trim() ?? throw new ValidationException("Primary guardian name is required."),
                PrimaryGuardianRelationship = request.PrimaryGuardianRelationship?.Trim() ?? throw new ValidationException("Primary guardian relationship is required."),
                PrimaryGuardianPhone = request.PrimaryGuardianPhone?.Trim() ?? throw new ValidationException("Primary guardian phone is required."),
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

            // Save to database (implicit transaction via SaveAsync)
            _repositories.Student.Create(student);
            await _repositories.SaveAsync();

            return ToDto(student);
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
                var classExists = await _repositories.Class.GetByIdAsync(request.CurrentClassId.Value, false);
                if (classExists == null)
                    throw new NotFoundException($"Class with ID '{request.CurrentClassId}' not found.");
            }

            // Update fields (admission number is immutable)
            student.FirstName = request.FirstName?.Trim() ?? throw new ValidationException("FirstName is required.");
            student.LastName = request.LastName?.Trim() ?? throw new ValidationException("LastName is required.");
            student.MiddleName = request.MiddleName?.Trim();
            student.NemisNumber = request.NemisNumber?.Trim();
            student.BirthCertificateNumber = request.BirthCertificateNumber?.Trim();
            student.DateOfBirth = request.DateOfBirth;
            student.Gender = request.Gender;
            student.PlaceOfBirth = request.PlaceOfBirth?.Trim();
            student.Nationality = request.Nationality?.Trim() ?? "Kenyan";
            student.County = request.County?.Trim();
            student.SubCounty = request.SubCounty?.Trim();
            student.HomeAddress = request.HomeAddress?.Trim();
            student.Religion = request.Religion?.Trim();

            student.StudentStatus = request.StudentStatus;
            student.CBCLevel = request.CBCLevel;
            student.CurrentLevel = request.CurrentLevel;
            student.CurrentClassId = request.CurrentClassId;
            student.CurrentAcademicYearId = request.CurrentAcademicYearId;
            student.PreviousSchool = request.PreviousSchool?.Trim();

            student.BloodGroup = request.BloodGroup?.Trim();
            student.MedicalConditions = request.MedicalConditions?.Trim();
            student.Allergies = request.Allergies?.Trim();
            student.SpecialNeeds = request.SpecialNeeds?.Trim();
            student.RequiresSpecialSupport = request.RequiresSpecialSupport;

            student.PrimaryGuardianName = request.PrimaryGuardianName?.Trim() ?? throw new ValidationException("Primary guardian name is required.");
            student.PrimaryGuardianRelationship = request.PrimaryGuardianRelationship?.Trim() ?? throw new ValidationException("Primary guardian relationship is required.");
            student.PrimaryGuardianPhone = request.PrimaryGuardianPhone?.Trim() ?? throw new ValidationException("Primary guardian phone is required.");
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

            student.PhotoUrl = request.PhotoUrl?.Trim();
            student.Notes = request.Notes?.Trim();
            student.IsActive = request.IsActive;

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
            // If this fails, we have an orphaned file, but the database is consistent
            // Orphaned files can be cleaned up separately
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
            byte[] photoData,
            string fileName,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Student with ID '{id}' not found.");

            // Validate access
            if (!isSuperAdmin && student.TenantId != userSchoolId)
                throw new UnauthorizedException("You do not have access to update this student.");

            // Delete old photo if exists
            if (!string.IsNullOrWhiteSpace(student.PhotoUrl))
            {
                await _imageUpload.DeleteImageAsync(student.PhotoUrl);
            }

            // Upload is handled by controller using IFormFile
            // This method is a placeholder for business logic
            throw new NotImplementedException("Photo upload is handled directly in the controller.");
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
            // If this fails, we have an orphaned file, but the database is consistent
            try
            {
                await _imageUpload.DeleteImageAsync(photoUrl);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the operation
                // The database record is already updated
                // Orphaned files can be cleaned up separately
                System.Diagnostics.Debug.WriteLine($"Failed to delete photo file: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

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

        public async Task<string> GenerateAdmissionNumberAsync(Guid schoolId)
        {
            // Ensure number series exists for this school
            var seriesExists = await _numberSeries.SeriesExistsAsync("Student", schoolId);
            if (!seriesExists)
            {
                await _numberSeries.CreateSeriesAsync(
                    entityName: "Student",
                    prefix: "ADM",
                    padding: 5,
                    resetEveryYear: true,
                    description: "Student admission numbers");
            }

            return await _numberSeries.GenerateAsync("Student", schoolId);
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

        /// <summary>
        /// Helper method to delete photo file with error handling
        /// </summary>
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
                // Orphaned files can be cleaned up separately
                System.Diagnostics.Debug.WriteLine($"Failed to delete photo file: {ex.Message}");
            }
        }

        #endregion

        #region DTO Mapping

        private static StudentDto ToDto(Student s) => new()
        {
            Id = s.Id,
            SchoolId = s.TenantId,
            FirstName = s.FirstName ?? string.Empty,
            LastName = s.LastName ?? string.Empty,
            MiddleName = s.MiddleName ?? string.Empty,
            FullName = s.FullName,
            AdmissionNumber = s.AdmissionNumber ?? string.Empty,
            NemisNumber = s.NemisNumber ?? string.Empty,
            BirthCertificateNumber = s.BirthCertificateNumber ?? string.Empty,
            DateOfBirth = s.DateOfBirth,
            Age = s.Age,
            Gender = s.Gender.ToString(),
            PlaceOfBirth = s.PlaceOfBirth ?? string.Empty,
            Nationality = s.Nationality ?? "Kenyan",
            County = s.County ?? string.Empty,
            SubCounty = s.SubCounty ?? string.Empty,
            HomeAddress = s.HomeAddress ?? string.Empty,
            Religion = s.Religion ?? string.Empty,
            DateOfAdmission = s.DateOfAdmission,
            StudentStatus = s.StudentStatus.ToString(),
            CBCLevel = s.CBCLevel.ToString(),
            CurrentLevel = s.CurrentLevel.ToString(),
            CurrentClassId = s.CurrentClassId,
            CurrentClassName = s.CurrentClass?.Name ?? string.Empty,
            CurrentAcademicYearId = s.CurrentAcademicYearId,
            Status = s.Status.ToString(),
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