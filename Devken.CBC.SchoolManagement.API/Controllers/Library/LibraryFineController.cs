using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Library
{
    [ApiController]
    [Route("api/library/fines")]
    [Authorize]
    public class LibraryFineController : BaseApiController
    {
        private readonly ILibraryFineService _fineService;
        private readonly ILogger<LibraryFineController> _logger;

        public LibraryFineController(
            ILibraryFineService fineService,
            IUserActivityService activityService,
            ILogger<LibraryFineController> logger)
            : base(activityService, logger)
        {
            _fineService = fineService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new library fine
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> CreateFine([FromBody] CreateLibraryFineDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var fine = await _fineService.CreateFineAsync(dto, userSchoolId);

                await LogUserActivityAsync("library.fine.create",
                    $"Created fine of {dto.Amount:C} for borrow item {dto.BorrowItemId}");

                return CreatedResponse(fine, "Fine created successfully");
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
                _logger.LogError(ex, "Error creating fine");
                return InternalServerErrorResponse("Failed to create fine");
            }
        }

        /// <summary>
        /// Get fine by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetFineById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var fine = await _fineService.GetFineByIdAsync(id, userSchoolId);
                return SuccessResponse(fine);
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
                _logger.LogError(ex, "Error retrieving fine {FineId}", id);
                return InternalServerErrorResponse("Failed to retrieve fine");
            }
        }

        /// <summary>
        /// Get all fines
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetAllFines()
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var fines = await _fineService.GetAllFinesAsync(userSchoolId);
                return SuccessResponse(fines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all fines");
                return InternalServerErrorResponse("Failed to retrieve fines");
            }
        }

        /// <summary>
        /// Get unpaid fines
        /// </summary>
        [HttpGet("unpaid")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetUnpaidFines()
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var fines = await _fineService.GetUnpaidFinesAsync(userSchoolId);
                return SuccessResponse(fines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unpaid fines");
                return InternalServerErrorResponse("Failed to retrieve unpaid fines");
            }
        }

        /// <summary>
        /// Get fines by member ID
        /// </summary>
        [HttpGet("member/{memberId}")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetFinesByMember(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var fines = await _fineService.GetFinesByMemberIdAsync(memberId, userSchoolId);
                return SuccessResponse(fines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fines for member {MemberId}", memberId);
                return InternalServerErrorResponse("Failed to retrieve member fines");
            }
        }

        /// <summary>
        /// Pay a fine
        /// </summary>
        [HttpPost("pay")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> PayFine([FromBody] PayFineDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var fine = await _fineService.PayFineAsync(dto, userSchoolId);

                await LogUserActivityAsync("library.fine.pay",
                    $"Paid fine {dto.FineId}");

                return SuccessResponse(fine, "Fine paid successfully");
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
                _logger.LogError(ex, "Error paying fine");
                return InternalServerErrorResponse("Failed to pay fine");
            }
        }

        /// <summary>
        /// Pay multiple fines
        /// </summary>
        [HttpPost("pay/multiple")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> PayMultipleFines([FromBody] PayMultipleFinesDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var fines = await _fineService.PayMultipleFinesAsync(dto, userSchoolId);

                await LogUserActivityAsync("library.fines.pay.multiple",
                    $"Paid {dto.FineIds.Count} fines");

                return SuccessResponse(fines, "Fines paid successfully");
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
                _logger.LogError(ex, "Error paying multiple fines");
                return InternalServerErrorResponse("Failed to pay fines");
            }
        }

        /// <summary>
        /// Waive a fine
        /// </summary>
        [HttpPost("waive")]
        [Authorize(Policy = PermissionKeys.LibraryWrite)]
        public async Task<IActionResult> WaiveFine([FromBody] WaiveFineDto dto)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _fineService.WaiveFineAsync(dto, userSchoolId);

                await LogUserActivityAsync("library.fine.waive",
                    $"Waived fine {dto.FineId}: {dto.Reason}");

                return SuccessResponse("Fine waived successfully");
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
                _logger.LogError(ex, "Error waiving fine");
                return InternalServerErrorResponse("Failed to waive fine");
            }
        }

        /// <summary>
        /// Delete a fine
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionKeys.LibraryDelete)]
        public async Task<IActionResult> DeleteFine(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _fineService.DeleteFineAsync(id, userSchoolId);

                await LogUserActivityAsync("library.fine.delete",
                    $"Deleted fine {id}");

                return SuccessResponse("Fine deleted successfully");
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
                _logger.LogError(ex, "Error deleting fine {FineId}", id);
                return InternalServerErrorResponse("Failed to delete fine");
            }
        }

        /// <summary>
        /// Get total unpaid fines for member
        /// </summary>
        [HttpGet("member/{memberId}/unpaid-total")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetTotalUnpaidFines(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var total = await _fineService.GetTotalUnpaidFinesForMemberAsync(memberId, userSchoolId);
                return SuccessResponse(new { TotalUnpaidFines = total });
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
                _logger.LogError(ex, "Error getting total unpaid fines for member {MemberId}", memberId);
                return InternalServerErrorResponse("Failed to get total unpaid fines");
            }
        }

        /// <summary>
        /// Get total paid fines for member
        /// </summary>
        [HttpGet("member/{memberId}/paid-total")]
        [Authorize(Policy = PermissionKeys.LibraryRead)]
        public async Task<IActionResult> GetTotalPaidFines(Guid memberId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNull();
                var total = await _fineService.GetTotalPaidFinesForMemberAsync(memberId, userSchoolId);
                return SuccessResponse(new { TotalPaidFines = total });
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
                _logger.LogError(ex, "Error getting total paid fines for member {MemberId}", memberId);
                return InternalServerErrorResponse("Failed to get total paid fines");
            }
        }
    }
}