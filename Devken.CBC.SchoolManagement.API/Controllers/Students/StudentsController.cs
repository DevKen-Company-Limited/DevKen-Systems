using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Academic;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Domain.Enums.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Academic
{
    [ApiController]
    [Route("api/students")]
    [Authorize]
    public class StudentController : BaseApiController
    {
        private readonly IStudentService _studentService;

        public StudentController(
            IStudentService studentService,
            IUserActivityService activityService)
            : base(activityService)
        {
            _studentService = studentService;
        }

        /// <summary>
        /// Create a new student
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
        {
            if (!HasPermission(PermissionKeys.StudentWrite))
                return ForbiddenResponse("You do not have permission to create students");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var (success, message, student) = await _studentService.CreateStudentAsync(request, CurrentTenantId.Value);

            if (!success)
                return ErrorResponse(message, StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("CreateStudent", $"Student: {request.AdmissionNumber}");

            return SuccessResponse(student, message);
        }

        /// <summary>
        /// Update student information
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentRequest request)
        {
            if (!HasPermission(PermissionKeys.StudentWrite))
                return ForbiddenResponse("You do not have permission to update students");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (id != request.Id)
                return ErrorResponse("ID mismatch", StatusCodes.Status400BadRequest);

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var (success, message, student) = await _studentService.UpdateStudentAsync(request, CurrentTenantId.Value);

            if (!success)
                return ErrorResponse(message, StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("UpdateStudent", $"StudentId: {id}");

            return SuccessResponse(student, message);
        }

        /// <summary>
        /// Get student by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var student = await _studentService.GetStudentByIdAsync(id, CurrentTenantId.Value);

            if (student == null)
                return NotFoundResponse("Student not found");

            return SuccessResponse(student, "Student retrieved successfully");
        }

        /// <summary>
        /// Get student by admission number
        /// </summary>
        [HttpGet("admission/{admissionNumber}")]
        public async Task<IActionResult> GetStudentByAdmissionNumber(string admissionNumber)
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var student = await _studentService.GetStudentByAdmissionNumberAsync(admissionNumber, CurrentTenantId.Value);

            if (student == null)
                return NotFoundResponse("Student not found");

            return SuccessResponse(student, "Student retrieved successfully");
        }

        /// <summary>
        /// Get paginated list of students with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStudents([FromQuery] StudentSearchRequest request)
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 20;

            var result = await _studentService.GetStudentsPagedAsync(request, CurrentTenantId.Value);

            return SuccessResponse(result, "Students retrieved successfully");
        }

        /// <summary>
        /// Get students by class
        /// </summary>
        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetStudentsByClass(Guid classId)
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var students = await _studentService.GetStudentsByClassAsync(classId, CurrentTenantId.Value);

            return SuccessResponse(students, $"{students.Count} student(s) found in class");
        }

        /// <summary>
        /// Get students by CBC level
        /// </summary>
        [HttpGet("level/{level}")]
        public async Task<IActionResult> GetStudentsByLevel(CBCLevel level)
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var students = await _studentService.GetStudentsByLevelAsync(level, CurrentTenantId.Value);

            return SuccessResponse(students, $"{students.Count} student(s) found in {level}");
        }

        /// <summary>
        /// Search students by name, admission number, or NEMIS number
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchStudents([FromQuery] string searchTerm)
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (string.IsNullOrWhiteSpace(searchTerm))
                return ErrorResponse("Search term is required", StatusCodes.Status400BadRequest);

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var students = await _studentService.SearchStudentsAsync(searchTerm, CurrentTenantId.Value);

            return SuccessResponse(students, $"{students.Count} student(s) found");
        }

        /// <summary>
        /// Transfer student to another class
        /// </summary>
        [HttpPost("transfer")]
        public async Task<IActionResult> TransferStudent([FromBody] TransferStudentRequest request)
        {
            if (!HasPermission(PermissionKeys.StudentWrite))
                return ForbiddenResponse("You do not have permission to transfer students");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var (success, message) = await _studentService.TransferStudentAsync(request, CurrentTenantId.Value);

            if (!success)
                return ErrorResponse(message, StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("TransferStudent", $"StudentId: {request.StudentId}, NewClassId: {request.NewClassId}");

            return SuccessResponse(new { }, message);
        }

        /// <summary>
        /// Withdraw student from school
        /// </summary>
        [HttpPost("withdraw")]
        public async Task<IActionResult> WithdrawStudent([FromBody] WithdrawStudentRequest request)
        {
            if (!HasPermission(PermissionKeys.StudentWrite))
                return ForbiddenResponse("You do not have permission to withdraw students");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ToErrorDictionary(ModelState));

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var (success, message) = await _studentService.WithdrawStudentAsync(request, CurrentTenantId.Value);

            if (!success)
                return ErrorResponse(message, StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("WithdrawStudent", $"StudentId: {request.StudentId}, Reason: {request.Reason}");

            return SuccessResponse(new { }, message);
        }

        /// <summary>
        /// Delete student (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            if (!HasPermission(PermissionKeys.StudentDelete))
                return ForbiddenResponse("You do not have permission to delete students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var (success, message) = await _studentService.DeleteStudentAsync(id, CurrentTenantId.Value);

            if (!success)
                return ErrorResponse(message, StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("DeleteStudent", $"StudentId: {id}");

            return SuccessResponse(new { }, message);
        }

        /// <summary>
        /// Restore deleted student
        /// </summary>
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreStudent(Guid id)
        {
            if (!HasPermission(PermissionKeys.StudentWrite))
                return ForbiddenResponse("You do not have permission to restore students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var (success, message) = await _studentService.RestoreStudentAsync(id, CurrentTenantId.Value);

            if (!success)
                return ErrorResponse(message, StatusCodes.Status400BadRequest);

            await LogUserActivityAsync("RestoreStudent", $"StudentId: {id}");

            return SuccessResponse(new { }, message);
        }

        /// <summary>
        /// Get student statistics for the school
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStudentStatistics()
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view student statistics");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var statistics = await _studentService.GetStudentStatisticsAsync(CurrentTenantId.Value);

            return SuccessResponse(statistics, "Statistics retrieved successfully");
        }

        /// <summary>
        /// Get students with special needs
        /// </summary>
        [HttpGet("special-needs")]
        public async Task<IActionResult> GetStudentsWithSpecialNeeds()
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var students = await _studentService.GetStudentsWithSpecialNeedsAsync(CurrentTenantId.Value);

            return SuccessResponse(students, $"{students.Count} student(s) with special needs found");
        }

        /// <summary>
        /// Get students with pending fees
        /// </summary>
        [HttpGet("pending-fees")]
        public async Task<IActionResult> GetStudentsWithPendingFees()
        {
            if (!HasPermission(PermissionKeys.StudentRead))
                return ForbiddenResponse("You do not have permission to view students");

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var students = await _studentService.GetStudentsWithPendingFeesAsync(CurrentTenantId.Value);

            return SuccessResponse(students, $"{students.Count} student(s) with pending fees found");
        }

        /// <summary>
        /// Validate admission number uniqueness
        /// </summary>
        [HttpGet("validate-admission-number")]
        public async Task<IActionResult> ValidateAdmissionNumber(
            [FromQuery] string admissionNumber,
            [FromQuery] Guid? excludeStudentId = null)
        {
            if (string.IsNullOrWhiteSpace(admissionNumber))
                return ErrorResponse("Admission number is required", StatusCodes.Status400BadRequest);

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var isValid = await _studentService.ValidateAdmissionNumberAsync(
                admissionNumber,
                CurrentTenantId.Value,
                excludeStudentId);

            return SuccessResponse(new { IsValid = isValid, AdmissionNumber = admissionNumber },
                isValid ? "Admission number is available" : "Admission number already exists");
        }

        /// <summary>
        /// Validate NEMIS number uniqueness
        /// </summary>
        [HttpGet("validate-nemis-number")]
        public async Task<IActionResult> ValidateNemisNumber(
            [FromQuery] string nemisNumber,
            [FromQuery] Guid? excludeStudentId = null)
        {
            if (string.IsNullOrWhiteSpace(nemisNumber))
                return ErrorResponse("NEMIS number is required", StatusCodes.Status400BadRequest);

            if (CurrentTenantId == null)
                return ErrorResponse("Tenant context is required", StatusCodes.Status400BadRequest);

            var isValid = await _studentService.ValidateNemisNumberAsync(
                nemisNumber,
                CurrentTenantId.Value,
                excludeStudentId);

            return SuccessResponse(new { IsValid = isValid, NemisNumber = nemisNumber },
                isValid ? "NEMIS number is available" : "NEMIS number already exists");
        }

        #region Helpers

        private static IDictionary<string, string[]> ToErrorDictionary(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) =>
            modelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        #endregion
    }
}