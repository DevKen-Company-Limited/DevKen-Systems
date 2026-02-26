using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Application.DTOs.Finance;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Activities;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Api.Controllers.Finance
{
    /// <summary>
    /// CRUD endpoints for FeeStructure.
    /// A FeeStructure defines the actual fee amount for a given FeeItem, academic
    /// year, optional term, CBC level, and student category.
    ///
    /// Base route: /api/finance/feestructures
    /// </summary>
    [Route("api/finance/[controller]")]
    [ApiController]
    [Authorize]
    public class FeeStructuresController : BaseApiController
    {
        private readonly IRepositoryManager _repo;

        public FeeStructuresController(
            IRepositoryManager repo,
            IUserActivityService? activityService = null,
            ILogger<FeeStructuresController>? logger = null)
            : base(activityService, logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string GetFullExceptionMessage(Exception ex)
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

        /// <summary>
        /// Maps a domain entity to the API response DTO.
        /// </summary>
        private static FeeStructureDto ToDto(FeeStructure fs) => new()
        {
            Id = fs.Id,
            TenantId = fs.TenantId,
            FeeItemId = fs.FeeItemId,
            FeeItemName = fs.FeeItem?.Name ?? string.Empty,
            AcademicYearId = fs.AcademicYearId,
            AcademicYearName = fs.AcademicYear?.Name ?? string.Empty,
            TermId = fs.TermId,
            TermName = fs.Term?.Name,
            Level = fs.Level,
            ApplicableTo = fs.ApplicableTo,
            Amount = fs.Amount,
            MaxDiscountPercent = fs.MaxDiscountPercent,
            EffectiveFrom = fs.EffectiveFrom,
            EffectiveTo = fs.EffectiveTo,
            IsActive = fs.IsActive,
            CreatedOn = fs.CreatedOn,
            UpdatedOn = fs.UpdatedOn,
        };

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/finance/feestructures
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all fee structures visible to the calling user.
        /// SuperAdmin may pass an optional schoolId query parameter to filter by tenant.
        /// Non-SuperAdmin users always see only their own school's data.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionKeys.FinanceRead)]
        public async Task<IActionResult> GetAll([FromQuery] Guid? schoolId = null)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();
                var targetSchoolId = IsSuperAdmin ? schoolId : userSchoolId;

                var structures = await _repo.FeeStructure.GetAllAsync(targetSchoolId);
                var dtos = structures.Select(ToDto);

                return SuccessResponse(dtos);
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/finance/feestructures/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns a single fee structure by its primary key.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FinanceRead)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structure = await _repo.FeeStructure.GetByIdWithDetailsAsync(id);
                if (structure == null)
                    return NotFoundResponse($"Fee structure with ID '{id}' was not found.");

                // Tenant isolation: non-SuperAdmin users cannot see other schools' records.
                if (!IsSuperAdmin && structure.TenantId != userSchoolId)
                    return ForbiddenResponse("You do not have access to this fee structure.");

                return SuccessResponse(ToDto(structure));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/finance/feestructures/by-fee-item/{feeItemId}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns all fee structures associated with a specific FeeItem.</summary>
        [HttpGet("by-fee-item/{feeItemId:guid}")]
        [Authorize(Policy = PermissionKeys.FinanceRead)]
        public async Task<IActionResult> GetByFeeItem(Guid feeItemId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structures = await _repo.FeeStructure.GetByFeeItemAsync(feeItemId, userSchoolId);
                return SuccessResponse(structures.Select(ToDto));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/finance/feestructures/by-academic-year/{academicYearId}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns all fee structures for a given academic year.</summary>
        [HttpGet("by-academic-year/{academicYearId:guid}")]
        [Authorize(Policy = PermissionKeys.FinanceRead)]
        public async Task<IActionResult> GetByAcademicYear(Guid academicYearId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structures = await _repo.FeeStructure.GetByAcademicYearAsync(academicYearId, userSchoolId);
                return SuccessResponse(structures.Select(ToDto));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/finance/feestructures/by-term/{termId}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns all fee structures for a given term.</summary>
        [HttpGet("by-term/{termId:guid}")]
        [Authorize(Policy = PermissionKeys.FinanceRead)]
        public async Task<IActionResult> GetByTerm(Guid termId)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structures = await _repo.FeeStructure.GetByTermAsync(termId, userSchoolId);
                return SuccessResponse(structures.Select(ToDto));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/finance/feestructures/by-level/{level}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns fee structures that apply to a specific CBC level
        /// (includes records where Level is null, i.e. applicable to all levels).
        /// </summary>
        [HttpGet("by-level/{level}")]
        [Authorize(Policy = PermissionKeys.FinanceRead)]
        public async Task<IActionResult> GetByLevel(CBCLevel level)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structures = await _repo.FeeStructure.GetByLevelAsync(level, userSchoolId);
                return SuccessResponse(structures.Select(ToDto));
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/finance/feestructures
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new FeeStructure.
        /// For SuperAdmin, TenantId must be supplied in the body.
        /// For non-SuperAdmin users, TenantId is taken from the JWT automatically.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionKeys.FinanceWrite)]
        public async Task<IActionResult> Create([FromBody] CreateFeeStructureDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                // Resolve tenant
                Guid tenantId;
                if (!IsSuperAdmin)
                {
                    tenantId = userSchoolId!.Value;
                    dto.TenantId = tenantId;
                }
                else
                {
                    if (dto.TenantId == null || dto.TenantId == Guid.Empty)
                        return ValidationErrorResponse("TenantId is required for SuperAdmin.");
                    tenantId = dto.TenantId.Value;
                }

                // Validate FK existence
                if (!await _repo.FeeItem.ExistAsync(dto.FeeItemId))
                    return NotFoundResponse($"Fee item with ID '{dto.FeeItemId}' does not exist.");

                if (!await _repo.AcademicYear.ExistAsync(dto.AcademicYearId))
                    return NotFoundResponse($"Academic year with ID '{dto.AcademicYearId}' does not exist.");

                if (dto.TermId.HasValue && !await _repo.Term.ExistAsync(dto.TermId.Value))
                    return NotFoundResponse($"Term with ID '{dto.TermId}' does not exist.");

                // Duplicate check
                var isDuplicate = await _repo.FeeStructure.ExistsDuplicateAsync(
                    tenantId, dto.FeeItemId, dto.AcademicYearId,
                    dto.TermId, dto.Level, dto.ApplicableTo);

                if (isDuplicate)
                    return ConflictResponse(
                        "A fee structure with the same FeeItem, AcademicYear, Term, Level, " +
                        "and ApplicableTo already exists for this school.");

                // Map & persist
                var structure = new FeeStructure
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FeeItemId = dto.FeeItemId,
                    AcademicYearId = dto.AcademicYearId,
                    TermId = dto.TermId,
                    Level = dto.Level,
                    ApplicableTo = dto.ApplicableTo,
                    Amount = dto.Amount,
                    MaxDiscountPercent = dto.MaxDiscountPercent,
                    EffectiveFrom = dto.EffectiveFrom,
                    EffectiveTo = dto.EffectiveTo,
                    IsActive = dto.IsActive,
                };

                _repo.FeeStructure.Create(structure);
                await _repo.SaveAsync();

                // Reload with navigation properties for response
                var created = await _repo.FeeStructure.GetByIdWithDetailsAsync(structure.Id);

                await LogUserActivityAsync(
                    "fee_structure.create",
                    $"Created fee structure ID '{structure.Id}' for school '{tenantId}'");

                return CreatedResponse(
                    $"api/finance/feestructures/{structure.Id}",
                    ToDto(created!),
                    "Fee structure created successfully.");
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/finance/feestructures/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates Amount, MaxDiscountPercent, Level, ApplicableTo, and validity
        /// dates on an existing FeeStructure.
        /// FeeItemId, AcademicYearId, and TermId are intentionally immutable to
        /// preserve financial integrity; delete and recreate instead.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FinanceWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFeeStructureDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelState);

            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structure = await _repo.FeeStructure.GetByIdWithDetailsAsync(id, trackChanges: true);
                if (structure == null)
                    return NotFoundResponse($"Fee structure with ID '{id}' was not found.");

                // Tenant isolation
                if (!IsSuperAdmin && structure.TenantId != userSchoolId)
                    return ForbiddenResponse("You do not have access to this fee structure.");

                // Duplicate check (exclude current record)
                var isDuplicate = await _repo.FeeStructure.ExistsDuplicateAsync(
                    structure.TenantId, structure.FeeItemId, structure.AcademicYearId,
                    structure.TermId, dto.Level, dto.ApplicableTo,
                    excludeId: id);

                if (isDuplicate)
                    return ConflictResponse(
                        "Another fee structure with the same combination already exists for this school.");

                // Apply changes
                structure.Level = dto.Level;
                structure.ApplicableTo = dto.ApplicableTo;
                structure.Amount = dto.Amount;
                structure.MaxDiscountPercent = dto.MaxDiscountPercent;
                structure.EffectiveFrom = dto.EffectiveFrom;
                structure.EffectiveTo = dto.EffectiveTo;
                structure.IsActive = dto.IsActive;

                _repo.FeeStructure.Update(structure);
                await _repo.SaveAsync();

                // Reload to get full navigation properties
                var updated = await _repo.FeeStructure.GetByIdWithDetailsAsync(id);

                await LogUserActivityAsync(
                    "fee_structure.update",
                    $"Updated fee structure ID '{id}'");

                return SuccessResponse(ToDto(updated!), "Fee structure updated successfully.");
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH /api/finance/feestructures/{id}/toggle-active
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Toggles the IsActive flag on a fee structure without a full update payload.
        /// Useful for quickly activating / deactivating a fee structure.
        /// </summary>
        [HttpPatch("{id:guid}/toggle-active")]
        [Authorize(Policy = PermissionKeys.FinanceWrite)]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structure = await _repo.FeeStructure.GetByIdWithDetailsAsync(id, trackChanges: true);
                if (structure == null)
                    return NotFoundResponse($"Fee structure with ID '{id}' was not found.");

                if (!IsSuperAdmin && structure.TenantId != userSchoolId)
                    return ForbiddenResponse("You do not have access to this fee structure.");

                structure.IsActive = !structure.IsActive;
                _repo.FeeStructure.Update(structure);
                await _repo.SaveAsync();

                var updated = await _repo.FeeStructure.GetByIdWithDetailsAsync(id);

                await LogUserActivityAsync(
                    "fee_structure.toggle_active",
                    $"Set fee structure ID '{id}' IsActive={structure.IsActive}");

                return SuccessResponse(
                    ToDto(updated!),
                    $"Fee structure {(structure.IsActive ? "activated" : "deactivated")} successfully.");
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /api/finance/feestructures/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Hard-deletes a fee structure.
        /// Consider whether soft-delete (IsActive = false) is more appropriate
        /// once fee structures are linked to student fee accounts.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionKeys.FinanceWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userSchoolId = GetUserSchoolIdOrNullWithValidation();

                var structure = await _repo.FeeStructure.GetByIdAsync(id, trackChanges: true);
                if (structure == null)
                    return NotFoundResponse($"Fee structure with ID '{id}' was not found.");

                if (!IsSuperAdmin && structure.TenantId != userSchoolId)
                    return ForbiddenResponse("You do not have access to this fee structure.");

                _repo.FeeStructure.Delete(structure);
                await _repo.SaveAsync();

                await LogUserActivityAsync(
                    "fee_structure.delete",
                    $"Deleted fee structure ID '{id}'");

                return SuccessResponse("Fee structure deleted successfully.");
            }
            catch (UnauthorizedAccessException ex) { return UnauthorizedResponse(ex.Message); }
            catch (Exception ex) { return InternalServerErrorResponse(GetFullExceptionMessage(ex)); }
        }
    }
}