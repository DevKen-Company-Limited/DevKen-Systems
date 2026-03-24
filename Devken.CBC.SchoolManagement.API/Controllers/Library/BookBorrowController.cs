using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Library
{
    [ApiController]
    [Route("api/library/borrows")]
    [Authorize]
    public class BookBorrowController : BaseApiController
    {
        private readonly IBookBorrowService _borrowService;
        private readonly ILogger<BookBorrowController> _logger;

        public BookBorrowController(
            IBookBorrowService borrowService,
            IUserActivityService activityService,
            ILogger<BookBorrowController> logger)
            : base(activityService, logger)
        {
            _borrowService = borrowService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new book borrow transaction
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionKeys.BookIssueWrite)]
        public async Task<IActionResult> CreateBorrow([FromBody] CreateBookBorrowDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var borrow = await _borrowService.CreateBorrowAsync(dto, userSchoolId);

                await LogUserActivityAsync("library.borrow.create",
                    $"Created borrow transaction for member {dto.MemberId}");

                return CreatedResponse(borrow, "Borrow transaction created successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message, 400);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating borrow transaction");
                return InternalServerErrorResponse("Failed to create borrow transaction");
            }
        }

        /// <summary>
        /// Get borrow transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetBorrowById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var borrow = await _borrowService.GetBorrowByIdAsync(id, userSchoolId);
                return SuccessResponse(borrow);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving borrow transaction {BorrowId}", id);
                return InternalServerErrorResponse("Failed to retrieve borrow transaction");
            }
        }

        /// <summary>
        /// Get all borrow transactions
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetAllBorrows()
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var borrows = await _borrowService.GetAllBorrowsAsync(userSchoolId);
                return SuccessResponse(borrows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all borrow transactions");
                return InternalServerErrorResponse("Failed to retrieve borrow transactions");
            }
        }

        /// <summary>
        /// Get borrow transactions by member ID
        /// </summary>
        [HttpGet("member/{memberId}")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetBorrowsByMember(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var borrows = await _borrowService.GetBorrowsByMemberIdAsync(memberId, userSchoolId);
                return SuccessResponse(borrows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving borrows for member {MemberId}", memberId);
                return InternalServerErrorResponse("Failed to retrieve member borrows");
            }
        }

        /// <summary>
        /// Get active borrow transactions
        /// </summary>
        [HttpGet("active")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetActiveBorrows()
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var borrows = await _borrowService.GetActiveBorrowsAsync(userSchoolId);
                return SuccessResponse(borrows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active borrows");
                return InternalServerErrorResponse("Failed to retrieve active borrows");
            }
        }

        /// <summary>
        /// Get overdue borrow transactions
        /// </summary>
        [HttpGet("overdue")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetOverdueBorrows()
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var borrows = await _borrowService.GetOverdueBorrowsAsync(userSchoolId);
                return SuccessResponse(borrows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue borrows");
                return InternalServerErrorResponse("Failed to retrieve overdue borrows");
            }
        }

        /// <summary>
        /// Update borrow transaction
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = PermissionKeys.BookIssueWrite)]
        public async Task<IActionResult> UpdateBorrow(Guid id, [FromBody] UpdateBookBorrowDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var borrow = await _borrowService.UpdateBorrowAsync(id, dto, userSchoolId);

                await LogUserActivityAsync("library.borrow.update",
                    $"Updated borrow transaction {id}");

                return SuccessResponse(borrow, "Borrow transaction updated successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message, 400);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating borrow transaction {BorrowId}", id);
                return InternalServerErrorResponse("Failed to update borrow transaction");
            }
        }

        /// <summary>
        /// Return a borrowed book
        /// </summary>
        [HttpPost("return")]
        [Authorize(Policy = PermissionKeys.BookReturnWrite)]
        public async Task<IActionResult> ReturnBook([FromBody] ReturnBookDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var borrowItem = await _borrowService.ReturnBookAsync(dto, userSchoolId);

                await LogUserActivityAsync("library.book.return",
                    $"Returned book {borrowItem.BookTitle}");

                return SuccessResponse(borrowItem, "Book returned successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message, 400);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning book");
                return InternalServerErrorResponse("Failed to return book");
            }
        }

        /// <summary>
        /// Return multiple borrowed books
        /// </summary>
        [HttpPost("return/multiple")]
        [Authorize(Policy = PermissionKeys.BookReturnWrite)]
        public async Task<IActionResult> ReturnMultipleBooks([FromBody] ReturnMultipleBooksDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var borrowItems = await _borrowService.ReturnMultipleBooksAsync(dto, userSchoolId);

                await LogUserActivityAsync("library.books.return.multiple",
                    $"Returned {dto.BorrowItemIds.Count} books");

                return SuccessResponse(borrowItems, "Books returned successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message, 400);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning multiple books");
                return InternalServerErrorResponse("Failed to return books");
            }
        }

        /// <summary>
        /// Delete borrow transaction
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionKeys.BookIssueWrite)]
        public async Task<IActionResult> DeleteBorrow(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _borrowService.DeleteBorrowAsync(id, userSchoolId);

                await LogUserActivityAsync("library.borrow.delete",
                    $"Deleted borrow transaction {id}");

                return SuccessResponse("Borrow transaction deleted successfully");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message, 400);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ForbiddenResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting borrow transaction {BorrowId}", id);
                return InternalServerErrorResponse("Failed to delete borrow transaction");
            }
        }

        /// <summary>
        /// Check if member can borrow books
        /// </summary>
        [HttpGet("member/{memberId}/can-borrow")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> CanMemberBorrow(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var canBorrow = await _borrowService.CanMemberBorrowAsync(memberId, userSchoolId);
                return SuccessResponse(new { CanBorrow = canBorrow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking borrow eligibility for member {MemberId}", memberId);
                return InternalServerErrorResponse("Failed to check borrow eligibility");
            }
        }

        /// <summary>
        /// Get active borrow count for member
        /// </summary>
        [HttpGet("member/{memberId}/active-count")]
        [Authorize(Policy = PermissionKeys.BookIssueRead)]
        public async Task<IActionResult> GetActiveBorrowCount(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var count = await _borrowService.GetActiveBorrowCountAsync(memberId, userSchoolId);
                return SuccessResponse(new { ActiveBorrowCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active borrow count for member {MemberId}", memberId);
                return InternalServerErrorResponse("Failed to get active borrow count");
            }
        }

        /// <summary>
        /// Process overdue items (admin task)
        /// </summary>
        [HttpPost("process-overdue")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> ProcessOverdueItems()
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                await _borrowService.ProcessOverdueItemsAsync(userSchoolId);

                await LogUserActivityAsync("library.overdue.process",
                    "Processed overdue items");

                return SuccessResponse("Overdue items processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing overdue items");
                return InternalServerErrorResponse("Failed to process overdue items");
            }
        }
    }
}