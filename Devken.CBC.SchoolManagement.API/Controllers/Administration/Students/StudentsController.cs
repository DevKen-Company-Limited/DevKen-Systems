using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Administration.Student;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly IStudentService _studentService;
        private readonly IImageUploadService _imageUpload;

        public StudentsController(
            IStudentService studentService,
            IImageUploadService imageUpload,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _imageUpload = imageUpload ?? throw new ArgumentNullException(nameof(imageUpload));
        }

        #region Helpers

        private string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += $" | Inner: {inner.Message}";
                inner = inner.InnerException;
            }
            return message;
        }

        #endregion

        #region GET

        /// <summary>
        /// Get all students with optional school filter (SuperAdmin)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("Student.Read"))
                return ForbiddenResponse("You do not have permission to view students.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var students = await _studentService.GetAllStudentsAsync(
                    schoolId,
                    userSchoolId,
                    IsSuperAdmin);

                Response.Headers.Add("X-Access-Level", IsSuperAdmin ? "SuperAdmin" : "SchoolAdmin");
                Response.Headers.Add("X-School-Filter", schoolId?.ToString() ?? (IsSuperAdmin ? "All Schools" : userSchoolId.ToString()));

                return SuccessResponse(students);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        /// <summary>
        /// Get student by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("Student.Read"))
                return ForbiddenResponse("You do not have permission to view this student.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var student = await _studentService.GetStudentByIdAsync(
                    id,
                    userSchoolId,
                    IsSuperAdmin);

                return SuccessResponse(student);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region CREATE

        /// <summary>
        /// Create a new student with auto-generated admission number
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to create students.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Enforce schoolId handling based on user role
                if (!IsSuperAdmin)
                {
                    // Non-SuperAdmin: Force their own school
                    request.SchoolId = userSchoolId!.Value;
                }
                else if (request.SchoolId == Guid.Empty)
                {
                    // SuperAdmin: Require schoolId in request
                    return ValidationErrorResponse("SchoolId is required for SuperAdmin.");
                }

                var result = await _studentService.CreateStudentAsync(
                    request,
                    userSchoolId,
                    IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.create",
                    $"Created student {result.AdmissionNumber} - {result.FullName} in school {result.SchoolId}");

                return CreatedResponse(result, "Student created successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region UPDATE

        /// <summary>
        /// Update an existing student
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest request)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to update students.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _studentService.UpdateStudentAsync(
                    id,
                    request,
                    userSchoolId,
                    IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.update",
                    $"Updated student {result.AdmissionNumber} - {result.FullName}");

                return SuccessResponse(result, "Student updated successfully");
            }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region PHOTO MANAGEMENT

        /// <summary>
        /// Upload student photo
        /// </summary>
        [HttpPost("{id:guid}/photo")]
        public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to upload student photos.");

            if (file == null || file.Length == 0)
                return ValidationErrorResponse("No file uploaded.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return ValidationErrorResponse("Only image files (jpg, jpeg, png, gif, webp) are allowed.");

            if (file.Length > 5 * 1024 * 1024)
                return ValidationErrorResponse("File size cannot exceed 5MB.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Get student and validate access (service handles this)
                var student = await _studentService.GetStudentByIdAsync(id, userSchoolId, IsSuperAdmin);

                // Upload new photo
                var photoUrl = await _imageUpload.UploadImageAsync(file, "students");

                // Delete old photo if exists
                if (!string.IsNullOrWhiteSpace(student.PhotoUrl))
                    await _imageUpload.DeleteImageAsync(student.PhotoUrl);

                // Update student with new photo URL
                var updateRequest = new UpdateStudentRequest
                {
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    MiddleName = student.MiddleName,
                    NemisNumber = student.NemisNumber,
                    BirthCertificateNumber = student.BirthCertificateNumber,
                    DateOfBirth = student.DateOfBirth,
                    Gender = Enum.Parse<Domain.Enums.Gender>(student.Gender),
                    PlaceOfBirth = student.PlaceOfBirth,
                    Nationality = student.Nationality,
                    County = student.County,
                    SubCounty = student.SubCounty,
                    HomeAddress = student.HomeAddress,
                    Religion = student.Religion,
                    StudentStatus = Enum.Parse<Domain.Enums.StudentStatus>(student.StudentStatus),
                    CBCLevel = Enum.Parse<Domain.Enums.CBCLevel>(student.CBCLevel),
                    CurrentLevel = Enum.Parse<Domain.Enums.CBCLevel>(student.CurrentLevel),
                    CurrentClassId = student.CurrentClassId,
                    CurrentAcademicYearId = student.CurrentAcademicYearId,
                    PreviousSchool = student.PreviousSchool,
                    BloodGroup = student.BloodGroup,
                    MedicalConditions = student.MedicalConditions,
                    Allergies = student.Allergies,
                    SpecialNeeds = student.SpecialNeeds,
                    RequiresSpecialSupport = student.RequiresSpecialSupport,
                    PrimaryGuardianName = student.PrimaryGuardianName,
                    PrimaryGuardianRelationship = student.PrimaryGuardianRelationship,
                    PrimaryGuardianPhone = student.PrimaryGuardianPhone,
                    PrimaryGuardianEmail = student.PrimaryGuardianEmail,
                    PrimaryGuardianOccupation = student.PrimaryGuardianOccupation,
                    PrimaryGuardianAddress = student.PrimaryGuardianAddress,
                    SecondaryGuardianName = student.SecondaryGuardianName,
                    SecondaryGuardianRelationship = student.SecondaryGuardianRelationship,
                    SecondaryGuardianPhone = student.SecondaryGuardianPhone,
                    SecondaryGuardianEmail = student.SecondaryGuardianEmail,
                    SecondaryGuardianOccupation = student.SecondaryGuardianOccupation,
                    EmergencyContactName = student.EmergencyContactName,
                    EmergencyContactPhone = student.EmergencyContactPhone,
                    EmergencyContactRelationship = student.EmergencyContactRelationship,
                    PhotoUrl = photoUrl,  // New photo URL
                    Notes = student.Notes,
                    IsActive = student.IsActive
                };

                var result = await _studentService.UpdateStudentAsync(id, updateRequest, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.photo.upload",
                    $"Uploaded photo for student {result.AdmissionNumber} - {result.FullName}");

                return SuccessResponse(new { photoUrl }, "Photo uploaded successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        /// <summary>
        /// Delete student photo
        /// </summary>
        [HttpDelete("{id:guid}/photo")]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to remove student photos.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _studentService.DeleteStudentPhotoAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.photo.delete",
                    $"Deleted photo for student ID: {id}");

                return SuccessResponse<object?>(null, "Photo deleted successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion

        #region DELETE & TOGGLE STATUS

        /// <summary>
        /// Delete a student
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("Student.Delete"))
                return ForbiddenResponse("You do not have permission to delete students.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                await _studentService.DeleteStudentAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "student.delete",
                    $"Deleted student with ID: {id}");

                return SuccessResponse("Student deleted successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        /// <summary>
        /// Toggle student active status
        /// </summary>
        [HttpPatch("{id:guid}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] bool isActive)
        {
            if (!HasPermission("Student.Write"))
                return ForbiddenResponse("You do not have permission to update student status.");

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var result = await _studentService.ToggleStudentStatusAsync(
                    id,
                    isActive,
                    userSchoolId,
                    IsSuperAdmin);

                var action = isActive ? "activated" : "deactivated";
                await LogUserActivityAsync(
                    "student.toggle-status",
                    $"{action} student {result.AdmissionNumber} - {result.FullName}");

                return SuccessResponse(result, $"Student {action} successfully");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        #endregion
    }
}