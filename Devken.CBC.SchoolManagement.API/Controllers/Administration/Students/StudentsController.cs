using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IRepositoryManager _repositories;

        public StudentsController(
            IRepositoryManager repositories,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        /// <summary>
        /// Get all students - SuperAdmin can see all, others see only their school's students
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Student.Read"))
                return ForbiddenResponse("You do not have permission to view students.");

            // SuperAdmin can view all students or filter by school
            if (HasRole("SuperAdmin"))
            {
                var students = schoolId.HasValue
                    ? await _repositories.Student.GetBySchoolIdAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.Student.GetAllAsync(trackChanges: false);

                var dtos = students.Select(ToDto);
                return SuccessResponse(dtos);
            }

            // Non-SuperAdmin users can only view students from their school
            var userSchoolId = GetCurrentUserSchoolId();
            if (userSchoolId == null)
                return ForbiddenResponse("You must be assigned to a school to view students.");

            var schoolStudents = await _repositories.Student.GetBySchoolIdAsync(userSchoolId, trackChanges: false);
            var schoolDtos = schoolStudents.Select(ToDto);
            return SuccessResponse(schoolDtos);
        }

        /// <summary>
        /// Get student by ID - SuperAdmin or users from the same school
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("Student.Read"))
                return ForbiddenResponse("You do not have permission to view this student.");

            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: false);
            if (student == null)
                return NotFoundResponse();

            // Non-SuperAdmin users can only view students from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (userSchoolId == null || student.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only view students from your school.");
            }

            return SuccessResponse(ToDto(student));
        }

        /// <summary>
        /// Create student - SuperAdmin or SchoolAdmin
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to create students.");

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
                var userSchoolId = GetCurrentUserSchoolId();
                if (userSchoolId == null)
                    return ForbiddenResponse("You must be assigned to a school to create students.");

                targetSchoolId = userSchoolId;
            }

            // Verify school exists
            var school = await _repositories.School.GetByIdAsync(targetSchoolId, trackChanges: false);
            if (school == null)
                return ErrorResponse("School not found.", 404);

            // Check if admission number already exists
            var existingStudent = await _repositories.Student.GetByAdmissionNumberAsync(
                request.AdmissionNumber ?? string.Empty,
                targetSchoolId);

            if (existingStudent != null)
                return ErrorResponse($"Student with admission number '{request.AdmissionNumber}' already exists.", 409);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId,

                // Personal Information
                FirstName = (request.FirstName ?? string.Empty).Trim(),
                LastName = (request.LastName ?? string.Empty).Trim(),
                MiddleName = request.MiddleName?.Trim(),
                AdmissionNumber = (request.AdmissionNumber ?? string.Empty).Trim(),
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

                // Academic Information
                DateOfAdmission = request.DateOfAdmission ?? DateTime.UtcNow,
                CurrentLevel = request.CurrentLevel,
                CurrentClassId = request.CurrentClassId,
                CurrentAcademicYearId = request.CurrentAcademicYearId,
                Status = StudentStatus.Active,
                PreviousSchool = request.PreviousSchool?.Trim(),

                // Health Information
                BloodGroup = request.BloodGroup?.Trim(),
                MedicalConditions = request.MedicalConditions?.Trim(),
                Allergies = request.Allergies?.Trim(),
                SpecialNeeds = request.SpecialNeeds?.Trim(),
                RequiresSpecialSupport = request.RequiresSpecialSupport,

                // Guardian Information
                PrimaryGuardianName = request.PrimaryGuardianName?.Trim(),
                PrimaryGuardianRelationship = request.PrimaryGuardianRelationship?.Trim(),
                PrimaryGuardianPhone = request.PrimaryGuardianPhone?.Trim(),
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

                // Additional Information
                PhotoUrl = request.PhotoUrl?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = request.IsActive
            };

            _repositories.Student.Create(student);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("student.create", $"Created student {student.AdmissionNumber} - {student.FullName}");

            return SuccessResponse(ToDto(student), "Student created successfully");
        }

        /// <summary>
        /// Update student - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest request)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to update students.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true);
            if (student == null)
                return NotFoundResponse();

            // Non-SuperAdmin users can only update students from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (userSchoolId == null || student.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only update students from your school.");
            }

            // Update Personal Information
            student.FirstName = (request.FirstName ?? string.Empty).Trim();
            student.LastName = (request.LastName ?? string.Empty).Trim();
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

            // Update Academic Information
            student.CurrentLevel = request.CurrentLevel;
            student.CurrentClassId = request.CurrentClassId;
            student.CurrentAcademicYearId = request.CurrentAcademicYearId;
            student.PreviousSchool = request.PreviousSchool?.Trim();

            // Update Health Information
            student.BloodGroup = request.BloodGroup?.Trim();
            student.MedicalConditions = request.MedicalConditions?.Trim();
            student.Allergies = request.Allergies?.Trim();
            student.SpecialNeeds = request.SpecialNeeds?.Trim();
            student.RequiresSpecialSupport = request.RequiresSpecialSupport;

            // Update Guardian Information
            student.PrimaryGuardianName = request.PrimaryGuardianName?.Trim();
            student.PrimaryGuardianRelationship = request.PrimaryGuardianRelationship?.Trim();
            student.PrimaryGuardianPhone = request.PrimaryGuardianPhone?.Trim();
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

            // Update Additional Information
            student.PhotoUrl = request.PhotoUrl?.Trim();
            student.Notes = request.Notes?.Trim();
            student.IsActive = request.IsActive;

            _repositories.Student.Update(student);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("student.update", $"Updated student {student.AdmissionNumber} - {student.FullName}");

            return SuccessResponse(ToDto(student), "Student updated successfully");
        }

        /// <summary>
        /// Delete student - SuperAdmin or SchoolAdmin (own school only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("Student.Delete"))
                return ForbiddenResponse("You do not have permission to delete students.");

            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: true);
            if (student == null)
                return NotFoundResponse();

            // Non-SuperAdmin users can only delete students from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (userSchoolId == null || student.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only delete students from your school.");
            }

            var admissionNumber = student.AdmissionNumber;
            var fullName = student.FullName;

            _repositories.Student.Delete(student);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("student.delete", $"Deleted student {admissionNumber} - {fullName}");

            return SuccessResponse<object?>(null, "Student deleted successfully");
        }

        private static StudentDto ToDto(Student s) => new()
        {
            Id = s.Id,
            SchoolId = s.TenantId,

            // Personal Information
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

            // Academic Information
            DateOfAdmission = s.DateOfAdmission,
            CurrentLevel = s.CurrentLevel.ToString(),
            CurrentClassId = s.CurrentClassId,
            CurrentClassName = s.CurrentClass?.Name ?? string.Empty,
            CurrentAcademicYearId = s.CurrentAcademicYearId,
            Status = s.Status.ToString(),
            PreviousSchool = s.PreviousSchool ?? string.Empty,

            // Health Information
            BloodGroup = s.BloodGroup ?? string.Empty,
            MedicalConditions = s.MedicalConditions ?? string.Empty,
            Allergies = s.Allergies ?? string.Empty,
            SpecialNeeds = s.SpecialNeeds ?? string.Empty,
            RequiresSpecialSupport = s.RequiresSpecialSupport,

            // Guardian Information
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

            // Additional Information
            PhotoUrl = s.PhotoUrl ?? string.Empty,
            Notes = s.Notes ?? string.Empty,
            IsActive = s.IsActive
        };
    }
}