using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Library
{
    [Route("api/library/[controller]")]
    [ApiController]
    [Authorize]
    public class BookRecommendationsController : BaseApiController
    {
        private readonly IRepositoryManager _repositories;

        public BookRecommendationsController(
            IRepositoryManager repositories,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        /// <summary>
        /// Get all book recommendations - SuperAdmin can see all, others see only their school's recommendations
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian")]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("BookRecommendation.Read"))
                return ForbiddenResponse("You do not have permission to view book recommendations.");

            IEnumerable<BookRecommendation> recommendations;
            if (HasRole("SuperAdmin"))
            {
                recommendations = schoolId.HasValue
                    ? await _repositories.BookRecommendation.GetAllByTenantAsync(schoolId.Value, false)
                    : await _repositories.BookRecommendation.GetAllByTenantAsync(Guid.Empty, false);
            }
            else
            {
                recommendations = await _repositories.BookRecommendation.GetAllByTenantAsync(GetCurrentUserSchoolId(), false);
            }

            var allSchools = await _repositories.School.FindAll(false).ToListAsync();

            var dtos = recommendations.Select(br =>
            {
                var dto = ToDto(br);
                var schoolMatch = allSchools.FirstOrDefault(s => s.Id == br.TenantId);
                dto.SchoolName = schoolMatch?.Name ?? "Unknown School";
                return dto;
            });

            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Get book recommendation by ID - SuperAdmin or users from the same school
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!HasPermission("BookRecommendation.Read"))
                return ForbiddenResponse("You do not have permission to view this book recommendation.");

            var recommendation = await _repositories.BookRecommendation.GetByIdAsync(id, trackChanges: false);
            if (recommendation == null)
                return NotFoundResponse("Book recommendation not found");
            // MANUAL LOOKUP: Fetch the school name separately
            var school = await _repositories.School.GetByIdAsync(recommendation.TenantId, false);
            // Non-SuperAdmin users can only view recommendations from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (recommendation.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only view book recommendations from your school.");
            }

            return SuccessResponse(ToDto(recommendation));
        }

        /// <summary>
        /// Get recommendations for a specific student
        /// </summary>
        [HttpGet("student/{studentId:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian,Student")]
        public async Task<IActionResult> GetByStudentId(Guid studentId, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("BookRecommendation.Read"))
                return ForbiddenResponse("You do not have permission to view book recommendations.");

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var recommendations = await _repositories.BookRecommendation.GetByStudentIdAsync(targetSchoolId, studentId, trackChanges: false);
            var dtos = recommendations.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Get recommendations for a specific book
        /// </summary>
        [HttpGet("book/{bookId:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian")]
        public async Task<IActionResult> GetByBookId(Guid bookId, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("BookRecommendation.Read"))
                return ForbiddenResponse("You do not have permission to view book recommendations.");

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var recommendations = await _repositories.BookRecommendation.GetByBookIdAsync(targetSchoolId, bookId, trackChanges: false);
            var dtos = recommendations.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Get top N recommendations for a student
        /// </summary>
        [HttpGet("student/{studentId:guid}/top")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian,Student")]
        public async Task<IActionResult> GetTopRecommendations(Guid studentId, [FromQuery] int topN = 10, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("BookRecommendation.Read"))
                return ForbiddenResponse("You do not have permission to view book recommendations.");

            if (topN < 1 || topN > 50)
                return ErrorResponse("TopN must be between 1 and 50.", 400);

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            var recommendations = await _repositories.BookRecommendation.GetTopRecommendationsAsync(targetSchoolId, studentId, topN, trackChanges: false);
            var dtos = recommendations.Select(ToDto);
            return SuccessResponse(dtos);
        }

        /// <summary>
        /// Create book recommendation - SuperAdmin, SchoolAdmin, Teacher, or Librarian
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian")]
        public async Task<IActionResult> Create([FromBody] CreateBookRecommendationRequest request)
        {
            if (!HasPermission("BookRecommendation.Write"))
                return ForbiddenResponse("You do not have permission to create book recommendations.");

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
                return ErrorResponse("School not found.", 404);

            // Verify book exists and belongs to the school
            var book = await _repositories.Book.GetByIdAsync(request.BookId, false);
            if (book == null || book.TenantId != targetSchoolId)
                return ErrorResponse("Book not found", 404);

            // Verify student exists and belongs to the school
            var student = await _repositories.Student.GetByIdAsync(request.StudentId, false);
            if (student == null || student.TenantId != targetSchoolId)
                return ErrorResponse("Student not found", 404);

            // Check if recommendation already exists
            if (await _repositories.BookRecommendation.ExistsAsync(targetSchoolId, request.BookId, request.StudentId))
                return ErrorResponse("A recommendation for this book and student already exists.", 409);

            var recommendation = new BookRecommendation
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId,
                BookId = request.BookId,
                StudentId = request.StudentId,
                Score = request.Score,
                Reason = request.Reason.Trim()
            };

            // Inside your Create method...
            _repositories.BookRecommendation.Create(recommendation);
            await _repositories.SaveAsync();

            var responseDto = ToDto(recommendation);
            responseDto.BookTitle = book.Title;
            responseDto.StudentName = $"{student.FirstName} {student.LastName}";
            // Use the 'school' variable you already fetched at the top of the method
            responseDto.SchoolName = school.Name;

            return SuccessResponse(responseDto, "Book recommendation created successfully");

        }

        /// <summary>
        /// Update book recommendation - SuperAdmin, SchoolAdmin, Teacher, or Librarian (own school only)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookRecommendationRequest request)
        {
            if (!HasPermission("BookRecommendation.Write"))
                return ForbiddenResponse("You do not have permission to update book recommendations.");

            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()));

            var recommendation = await _repositories.BookRecommendation.GetByIdAsync(id, trackChanges: true);
            if (recommendation == null)
                return NotFoundResponse("Book recommendation not found");

            // Non-SuperAdmin users can only update recommendations from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (recommendation.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only update book recommendations from your school.");
            }

            // Update fields
            if (request.Score.HasValue)
                recommendation.Score = request.Score.Value;

            if (request.Reason != null)
                recommendation.Reason = request.Reason.Trim();

            _repositories.BookRecommendation.Update(recommendation);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("book_recommendation.update", $"Updated recommendation {id}");

            return SuccessResponse(ToDto(recommendation), "Book recommendation updated successfully");
        }

        /// <summary>
        /// Delete book recommendation - SuperAdmin, SchoolAdmin, Teacher, or Librarian (own school only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Teacher,Librarian")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!HasPermission("BookRecommendation.Delete"))
                return ForbiddenResponse("You do not have permission to delete book recommendations.");

            var recommendation = await _repositories.BookRecommendation.GetByIdAsync(id, trackChanges: true);
            if (recommendation == null)
                return NotFoundResponse("Book recommendation not found");

            // Non-SuperAdmin users can only delete recommendations from their school
            if (!HasRole("SuperAdmin"))
            {
                var userSchoolId = GetCurrentUserSchoolId();
                if (recommendation.TenantId != userSchoolId)
                    return ForbiddenResponse("You can only delete book recommendations from your school.");
            }

            _repositories.BookRecommendation.Delete(recommendation);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("book_recommendation.delete", $"Deleted recommendation {id}");

            return SuccessResponse<object?>(null, "Book recommendation deleted successfully");
        }

        /// <summary>
        /// Delete all recommendations for a specific student
        /// </summary>
        [HttpDelete("student/{studentId:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Librarian")]
        public async Task<IActionResult> DeleteByStudentId(Guid studentId, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("BookRecommendation.Delete"))
                return ForbiddenResponse("You do not have permission to delete book recommendations.");

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            await _repositories.BookRecommendation.DeleteByStudentIdAsync(targetSchoolId, studentId);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("book_recommendation.delete_by_student", $"Deleted all recommendations for student {studentId}");

            return SuccessResponse<object?>(null, "All recommendations for the student deleted successfully");
        }

        /// <summary>
        /// Delete all recommendations for a specific book
        /// </summary>
        [HttpDelete("book/{bookId:guid}")]
        [Authorize(Roles = "SuperAdmin,SchoolAdmin,Librarian")]
        public async Task<IActionResult> DeleteByBookId(Guid bookId, [FromQuery] Guid? schoolId = null)
        {
            if (!HasPermission("BookRecommendation.Delete"))
                return ForbiddenResponse("You do not have permission to delete book recommendations.");

            Guid targetSchoolId;
            if (HasRole("SuperAdmin") && schoolId.HasValue)
            {
                targetSchoolId = schoolId.Value;
            }
            else
            {
                targetSchoolId = GetCurrentUserSchoolId();
            }

            await _repositories.BookRecommendation.DeleteByBookIdAsync(targetSchoolId, bookId);
            await _repositories.SaveAsync();

            await LogUserActivityAsync("book_recommendation.delete_by_book", $"Deleted all recommendations for book {bookId}");

            return SuccessResponse<object?>(null, "All recommendations for the book deleted successfully");
        }

        private static BookRecommendationDto ToDto(BookRecommendation br) => new()
        {
            Id = br.Id,
            SchoolId = br.TenantId,
            StudentName = br.Student != null ? $"{br.Student.FirstName} {br.Student.LastName}" : "Unknown Student",
            BookId = br.BookId,
            BookTitle = br.Book?.Title ?? "Unknown Book",
            StudentId = br.StudentId,
            Score = br.Score,
            Reason = br.Reason ?? string.Empty,
            CreatedOn = br.CreatedOn,
            CreatedBy = br.CreatedBy,
            UpdatedOn = br.UpdatedOn,
            UpdatedBy = br.UpdatedBy
        };
    }
}