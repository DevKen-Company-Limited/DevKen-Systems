using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Application.Service.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Finance
{
    [Route("api/finance/[controller]")]
    [ApiController]
    [Authorize]
    public class FeeItemsController : BaseApiController
    {
        private readonly IFeeItemService _feeItemService;

        public FeeItemsController(
            IFeeItemService feeItemService,
            IUserActivityService? activityService = null)
            : base(activityService)
        {
            _feeItemService = feeItemService ?? throw new ArgumentNullException(nameof(feeItemService));
        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null) { message += $" | Inner: {inner.Message}"; inner = inner.InnerException; }
            return message;
        }

        // ───────────────────────────────────────────────────────────────
        // GET ALL  →  GET /api/finance/feeitems
        // ───────────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? schoolId = null,
            [FromQuery] FeeType? feeType = null,
            [FromQuery] CBCLevel? applicableLevel = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var items = await _feeItemService.GetAllFeeItemsAsync(
                    targetSchoolId, userSchoolId, IsSuperAdmin, feeType, applicableLevel, isActive);

                return SuccessResponse(items);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ───────────────────────────────────────────────────────────────
        // GET BY ID  →  GET /api/finance/feeitems/{id}
        // ───────────────────────────────────────────────────────────────
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var item = await _feeItemService.GetFeeItemByIdAsync(id, userSchoolId, IsSuperAdmin);
                return SuccessResponse(item);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ───────────────────────────────────────────────────────────────
        // GET BY CODE  →  GET /api/finance/feeitems/by-code/{code}
        // ───────────────────────────────────────────────────────────────
        [HttpGet("by-code/{code}")]
        [Authorize(Policy = PermissionKeys.FeeRead)]
        public async Task<IActionResult> GetByCode(string code)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var item = await _feeItemService.GetFeeItemByCodeAsync(code, userSchoolId, IsSuperAdmin);
                return SuccessResponse(item);
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ───────────────────────────────────────────────────────────────
        // CREATE  →  POST /api/finance/feeitems
        // ───────────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> Create([FromBody] CreateFeeItemDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                if (!IsSuperAdmin)
                    dto.TenantId = userSchoolId;
                else if (dto.TenantId == null || dto.TenantId == Guid.Empty)
                    return ValidationErrorResponse("TenantId is required for SuperAdmin.");

                var result = await _feeItemService.CreateFeeItemAsync(dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync(
                    "feeitem.create",
                    $"Created fee item '{result.Name}' [{result.Code}] in school {result.TenantId}");

                return CreatedResponse($"api/finance/feeitems/{result.Id}", result, "Fee item created successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ───────────────────────────────────────────────────────────────
        // UPDATE  →  PUT /api/finance/feeitems/{id}
        // ───────────────────────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFeeItemDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _feeItemService.UpdateFeeItemAsync(id, dto, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("feeitem.update", $"Updated fee item '{result.Name}' [{result.Code}]");

                return SuccessResponse(result, "Fee item updated successfully.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return ValidationErrorResponse(ex.Message); }
            catch (ConflictException ex) { return ConflictResponse(ex.Message); }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ───────────────────────────────────────────────────────────────
        // DELETE  →  DELETE /api/finance/feeitems/{id}
        // ───────────────────────────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                await _feeItemService.DeleteFeeItemAsync(id, userSchoolId, IsSuperAdmin);

                await LogUserActivityAsync("feeitem.delete", $"Deleted fee item ID: {id}");

                return SuccessResponse("Fee item deleted successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ───────────────────────────────────────────────────────────────
        // TOGGLE ACTIVE  →  PATCH /api/finance/feeitems/{id}/toggle-active
        // ───────────────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/toggle-active")]
        [Authorize(Policy = PermissionKeys.FeeWrite)]
        public async Task<IActionResult> ToggleActive(Guid id, [FromBody] bool isActive)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var result = await _feeItemService.ToggleFeeItemActiveAsync(id, isActive, userSchoolId, IsSuperAdmin);

                var action = isActive ? "activated" : "deactivated";
                await LogUserActivityAsync("feeitem.toggle-active", $"{action} fee item '{result.Name}' [{result.Code}]");

                return SuccessResponse(result, $"Fee item {action} successfully.");
            }
            catch (NotFoundException ex) { return NotFoundResponse(ex.Message); }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (UnauthorizedException ex) { return ForbiddenResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}