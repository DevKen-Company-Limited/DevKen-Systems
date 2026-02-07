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
        /// Get students – SuperAdmin sees all, others only their school
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Student.Read"))
                return ForbiddenResponse("You do not have permission to view students.");

            // SuperAdmin: all students or filtered by school
            if (IsSuperAdmin)
            {
                var students = schoolId.HasValue
                    ? await _repositories.Student.GetBySchoolIdAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.Student.GetAllAsync(trackChanges: false);

                return SuccessResponse(students.Select(ToDto));
            }

            // Non-SuperAdmin: only own school
            var userSchoolId = GetCurrentUserSchoolId();
            if (userSchoolId == null)
                return ForbiddenResponse("You must be assigned to a school to view students.");

            var schoolStudents = await _repositories.Student.GetBySchoolIdAsync(userSchoolId, trackChanges: false);
            return SuccessResponse(schoolStudents.Select(ToDto));
        }

        /// <summary>
        /// Get student by ID – SuperAdmin or same school
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("Student.Read"))
                return ForbiddenResponse("You do not have permission to view this student.");

            var student = await _repositories.Student.GetByIdAsync(id, trackChanges: false);
            if (student == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(student.TenantId);
            if (accessError != null)
                return accessError;

            return SuccessResponse(ToDto(student));
        }

        /// <summary>
        /// Create student – SuperAdmin or SchoolAdmin (own school)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to create students.");

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
                    return ForbiddenResponse("You must be assigned to a school to create students.");

                targetSchoolId = userSchoolId;
            }

            var accessError = ValidateSchoolAccess(targetSchoolId);
            if (accessError != null)
                return accessError;

            var school = await _repositories.School.GetByIdAsync(targetSchoolId, false);
            if (school == null)
                return ErrorResponse("School not found.", 404);

            var existing = await _repositories.Student.GetByAdmissionNumberAsync(
                request.AdmissionNumber ?? string.Empty,
                targetSchoolId);

            if (existing != null)
                return ErrorResponse($"Student with admission number '{request.AdmissionNumber}' already exists.", 409);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId,

                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                MiddleName = request.MiddleName?.Trim(),
                AdmissionNumber = request.AdmissionNumber?.Trim(),
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

                DateOfAdmission = request.DateOfAdmission ?? DateTime.UtcNow,
                CurrentLevel = request.CurrentLevel,
                CurrentClassId = request.CurrentClassId,
                CurrentAcademicYearId = request.CurrentAcademicYearId,
                Status = StudentStatus.Active,
                PreviousSchool = request.PreviousSchool?.Trim(),

                BloodGroup = request.BloodGroup?.Trim(),
                MedicalConditions = request.MedicalConditions?.Trim(),
                Allergies = request.Allergies?.Trim(),
                SpecialNeeds = request.SpecialNeeds?.Trim(),
                RequiresSpecialSupport = request.RequiresSpecialSupport,

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

                PhotoUrl = request.PhotoUrl?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = request.IsActive
            };

            _repositories.Student.Create(student);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "student.create",
                $"Created student {student.AdmissionNumber} - {student.FullName}");

            return SuccessResponse(ToDto(student), "Student created successfully");
        }

        /// <summary>
        /// Update student – SuperAdmin or same school
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest request)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to update students.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var student = await _repositories.Student.GetByIdAsync(id, true);
            if (student == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(student.TenantId);
            if (accessError != null)
                return accessError;

            student.FirstName = request.FirstName?.Trim();
            student.LastName = request.LastName?.Trim();
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

            student.CurrentLevel = request.CurrentLevel;
            student.CurrentClassId = request.CurrentClassId;
            student.CurrentAcademicYearId = request.CurrentAcademicYearId;
            student.PreviousSchool = request.PreviousSchool?.Trim();

            student.BloodGroup = request.BloodGroup?.Trim();
            student.MedicalConditions = request.MedicalConditions?.Trim();
            student.Allergies = request.Allergies?.Trim();
            student.SpecialNeeds = request.SpecialNeeds?.Trim();
            student.RequiresSpecialSupport = request.RequiresSpecialSupport;

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

            student.PhotoUrl = request.PhotoUrl?.Trim();
            student.Notes = request.Notes?.Trim();
            student.IsActive = request.IsActive;

            _repositories.Student.Update(student);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "student.update",
                $"Updated student {student.AdmissionNumber} - {student.FullName}");

            return SuccessResponse(ToDto(student), "Student updated successfully");
        }

        /// <summary>
        /// Delete student – SuperAdmin or same school
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("Student.Delete"))
                return ForbiddenResponse("You do not have permission to delete students.");

            var student = await _repositories.Student.GetByIdAsync(id, true);
            if (student == null)
                return NotFoundResponse();

            var accessError = ValidateSchoolAccess(student.TenantId);
            if (accessError != null)
                return accessError;

            _repositories.Student.Delete(student);
            await _repositories.SaveAsync();

            await LogUserActivityAsync(
                "student.delete",
                $"Deleted student {student.AdmissionNumber} - {student.FullName}");

            return SuccessResponse<object?>(null, "Student deleted successfully");
        }

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
    }
}
