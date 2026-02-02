//sample code for a StudentsController in an ASP.NET Core Web API project for school management.

using Devken.CBC.SchoolManagement.Api.Attributes;
using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Academic
{
    [ApiController]
    [Route("api/students")]
    [Authorize]
    [RequirePermission(PermissionKeys.StudentRead)]
    public class StudentsController : BaseApiController
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStudents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var tenantId = CurrentTenantId;
            var userId = CurrentUserId;

            var students = new List<object>();

            return SuccessResponse(new
            {
                Students = students,
                Page = page,
                PageSize = pageSize,
                TotalCount = students.Count
            });
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStudent(Guid id)
        {
            if (!HasPermission(PermissionKeys.StudentRead))
            {
                return ErrorResponse("You do not have permission to view students", 403);
            }

            var student = new { Id = id, Name = "John Doe" };

            if (student == null)
            {
                return ErrorResponse("Student not found", 404);
            }

            return SuccessResponse(student);
        }

        [HttpPost]
        [RequirePermission(PermissionKeys.StudentWrite)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationErrorResponse(
                    ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    )
                );
            }

            var studentId = Guid.NewGuid();

            return CreatedAtAction(
                nameof(GetStudent),
                new { id = studentId },
                new { Id = studentId, Message = "Student created successfully" }
            );
        }

        [HttpPut("{id:guid}")]
        [RequirePermission(PermissionKeys.StudentWrite)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentDto dto)
        {
            var updated = true;

            if (!updated)
            {
                return ErrorResponse("Student not found", 404);
            }

            return SuccessResponse(new { Id = id }, "Student updated successfully");
        }

        [HttpDelete("{id:guid}")]
        [RequirePermission(PermissionKeys.StudentDelete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            var deleted = true;

            if (!deleted)
            {
                return ErrorResponse("Student not found", 404);
            }

            return NoContent();
        }

        [HttpPost("{id:guid}/enroll")]
        [RequirePermission(PermissionKeys.StudentWrite, PermissionKeys.ClassRead)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> EnrollStudent(
            Guid id,
            [FromBody] EnrollStudentDto dto)
        {
            if (!HasAllPermissions(PermissionKeys.StudentWrite, PermissionKeys.ClassRead))
            {
                return ErrorResponse("Insufficient permissions to enroll students", 403);
            }

            return SuccessResponse(
                new { StudentId = id, ClassId = dto.ClassId },
                "Student enrolled successfully"
            );
        }

        [HttpGet("{id:guid}/grades")]
        [RequireAnyPermission(PermissionKeys.StudentRead, PermissionKeys.GradeRead)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStudentGrades(Guid id)
        {
            if (!HasAnyPermission(PermissionKeys.StudentRead, PermissionKeys.GradeRead))
            {
                return ErrorResponse("You need either Student.Read or Grade.Read permission", 403);
            }

            var grades = new List<object>();

            return SuccessResponse(new { StudentId = id, Grades = grades });
        }

        [HttpGet("export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportStudents()
        {
            var csv =
                "Id,Name,Email\n" +
                "1,John Doe,john@example.com\n" +
                "2,Jane Smith,jane@example.com";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"students_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpGet("my-info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetMyInfo()
        {
            return Ok(new
            {
                UserId = CurrentUserId,
                TenantId = CurrentTenantId,
                Email = CurrentUserEmail,
                Name = CurrentUserName,
                Permissions = CurrentUserPermissions.ToList()
            });
        }
    }

    public record CreateStudentDto(
        string FirstName,
        string LastName,
        string Email,
        DateTime DateOfBirth,
        Guid ClassId
    );

    public record UpdateStudentDto(
        string FirstName,
        string LastName,
        string Email,
        DateTime DateOfBirth
    );

    public record EnrollStudentDto(
        Guid ClassId,
        DateTime EnrollmentDate
    );
}
