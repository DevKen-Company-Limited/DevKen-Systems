using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Curriculum
{
    public class LearningOutcomeService : ILearningOutcomeService
    {
        private readonly IRepositoryManager _repositories;

        public LearningOutcomeService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<LearningOutcomeResponseDto>> GetAllLearningOutcomesAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            Guid? subStrandId = null,
            Guid? strandId = null,
            Guid? learningAreaId = null,
            CBCLevel? level = null,
            bool? isCore = null)
        {
            IEnumerable<LearningOutcome> outcomes;

            if (isSuperAdmin)
            {
                if (subStrandId.HasValue)
                    outcomes = await _repositories.LearningOutcome.GetBySubStrandAsync(subStrandId.Value, trackChanges: false);
                else if (strandId.HasValue)
                    outcomes = await _repositories.LearningOutcome.GetByStrandAsync(strandId.Value, trackChanges: false);
                else if (learningAreaId.HasValue)
                    outcomes = await _repositories.LearningOutcome.GetByLearningAreaAsync(learningAreaId.Value, trackChanges: false);
                else if (schoolId.HasValue)
                    outcomes = await _repositories.LearningOutcome.GetByTenantIdAsync(schoolId.Value, trackChanges: false);
                else
                    outcomes = await _repositories.LearningOutcome.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view learning outcomes.");

                outcomes = subStrandId.HasValue
                    ? (await _repositories.LearningOutcome.GetBySubStrandAsync(subStrandId.Value, trackChanges: false))
                        .Where(lo => lo.TenantId == userSchoolId.Value)
                    : strandId.HasValue
                        ? (await _repositories.LearningOutcome.GetByStrandAsync(strandId.Value, trackChanges: false))
                            .Where(lo => lo.TenantId == userSchoolId.Value)
                        : learningAreaId.HasValue
                            ? (await _repositories.LearningOutcome.GetByLearningAreaAsync(learningAreaId.Value, trackChanges: false))
                                .Where(lo => lo.TenantId == userSchoolId.Value)
                            : await _repositories.LearningOutcome.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            // Apply optional filters
            if (level.HasValue)
                outcomes = outcomes.Where(lo => lo.Level == level.Value);
            if (isCore.HasValue)
                outcomes = outcomes.Where(lo => lo.IsCore == isCore.Value);

            return outcomes.OrderBy(lo => lo.Code).ThenBy(lo => lo.Outcome).Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningOutcomeResponseDto> GetLearningOutcomeByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var outcome = await _repositories.LearningOutcome.GetByIdWithDetailsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Learning outcome with ID '{id}' not found.");

            ValidateAccess(outcome.TenantId, userSchoolId, isSuperAdmin);
            return MapToDto(outcome);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY CODE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningOutcomeResponseDto> GetLearningOutcomeByCodeAsync(string code, Guid? userSchoolId, bool isSuperAdmin)
        {
            var tenantId = userSchoolId
                ?? throw new ValidationException("SuperAdmin must use GetAll with schoolId filter for code lookup.");

            var outcome = await _repositories.LearningOutcome.GetByCodeAsync(code, tenantId)
                ?? throw new NotFoundException($"Learning outcome with code '{code}' not found.");

            return MapToDto(outcome);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningOutcomeResponseDto> CreateLearningOutcomeAsync(
            CreateLearningOutcomeDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            // Validate hierarchy — all must belong to same tenant
            var learningArea = await _repositories.LearningArea.GetByIdAsync(dto.LearningAreaId, trackChanges: false)
                ?? throw new NotFoundException($"Learning area with ID '{dto.LearningAreaId}' not found.");
            if (learningArea.TenantId != tenantId)
                throw new ValidationException("The specified learning area does not belong to this school.");

            var strand = await _repositories.Strand.GetByIdAsync(dto.StrandId, trackChanges: false)
                ?? throw new NotFoundException($"Strand with ID '{dto.StrandId}' not found.");
            if (strand.TenantId != tenantId)
                throw new ValidationException("The specified strand does not belong to this school.");
            if (strand.LearningAreaId != dto.LearningAreaId)
                throw new ValidationException("The specified strand does not belong to the given learning area.");

            var subStrand = await _repositories.SubStrand.GetByIdAsync(dto.SubStrandId, trackChanges: false)
                ?? throw new NotFoundException($"Sub-strand with ID '{dto.SubStrandId}' not found.");
            if (subStrand.TenantId != tenantId)
                throw new ValidationException("The specified sub-strand does not belong to this school.");
            if (subStrand.StrandId != dto.StrandId)
                throw new ValidationException("The specified sub-strand does not belong to the given strand.");

            // Unique code check
            if (!string.IsNullOrWhiteSpace(dto.Code) &&
                await _repositories.LearningOutcome.ExistsByCodeAsync(dto.Code, tenantId))
                throw new ConflictException($"A learning outcome with code '{dto.Code}' already exists for this school.");

            var outcome = new LearningOutcome
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Outcome = dto.Outcome,
                Code = dto.Code,
                Description = dto.Description,
                Level = dto.Level,
                IsCore = dto.IsCore,
                LearningAreaId = dto.LearningAreaId,
                StrandId = dto.StrandId,
                SubStrandId = dto.SubStrandId
            };

            _repositories.LearningOutcome.Create(outcome);
            await _repositories.SaveAsync();

            // Attach nav properties for the response DTO
            outcome.LearningArea = learningArea;
            outcome.Strand = strand;
            outcome.SubStrand = subStrand;

            return MapToDto(outcome);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningOutcomeResponseDto> UpdateLearningOutcomeAsync(
            Guid id,
            UpdateLearningOutcomeDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var existing = await _repositories.LearningOutcome.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Learning outcome with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            // Validate hierarchy
            var learningArea = await _repositories.LearningArea.GetByIdAsync(dto.LearningAreaId, trackChanges: false)
                ?? throw new NotFoundException($"Learning area with ID '{dto.LearningAreaId}' not found.");
            if (learningArea.TenantId != existing.TenantId)
                throw new ValidationException("The specified learning area does not belong to this school.");

            var strand = await _repositories.Strand.GetByIdAsync(dto.StrandId, trackChanges: false)
                ?? throw new NotFoundException($"Strand with ID '{dto.StrandId}' not found.");
            if (strand.LearningAreaId != dto.LearningAreaId)
                throw new ValidationException("The specified strand does not belong to the given learning area.");

            var subStrand = await _repositories.SubStrand.GetByIdAsync(dto.SubStrandId, trackChanges: false)
                ?? throw new NotFoundException($"Sub-strand with ID '{dto.SubStrandId}' not found.");
            if (subStrand.StrandId != dto.StrandId)
                throw new ValidationException("The specified sub-strand does not belong to the given strand.");

            // Unique code check (exclude self)
            if (!string.IsNullOrWhiteSpace(dto.Code) &&
                await _repositories.LearningOutcome.ExistsByCodeAsync(dto.Code, existing.TenantId, excludeId: id))
                throw new ConflictException($"A learning outcome with code '{dto.Code}' already exists for this school.");

            existing.Outcome = dto.Outcome;
            existing.Code = dto.Code;
            existing.Description = dto.Description;
            existing.Level = dto.Level;
            existing.IsCore = dto.IsCore;
            existing.LearningAreaId = dto.LearningAreaId;
            existing.StrandId = dto.StrandId;
            existing.SubStrandId = dto.SubStrandId;

            _repositories.LearningOutcome.Update(existing);
            await _repositories.SaveAsync();

            existing.LearningArea = learningArea;
            existing.Strand = strand;
            existing.SubStrand = subStrand;

            return MapToDto(existing);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE (soft)
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteLearningOutcomeAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var outcome = await _repositories.LearningOutcome.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Learning outcome with ID '{id}' not found.");

            ValidateAccess(outcome.TenantId, userSchoolId, isSuperAdmin);

            _repositories.LearningOutcome.Delete(outcome);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────
        private Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new ValidationException("TenantId is required for SuperAdmin when creating a learning outcome.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to create learning outcomes.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid outcomeTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || outcomeTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this learning outcome.");
        }

        private static LearningOutcomeResponseDto MapToDto(LearningOutcome lo) => new()
        {
            Id = (Guid)lo.Id!,
            Outcome = lo.Outcome,
            Code = lo.Code,
            Description = lo.Description,
            Level = lo.Level.ToString(),
            IsCore = lo.IsCore,
            LearningAreaId = lo.LearningAreaId,
            LearningAreaName = lo.LearningArea?.Name,
            StrandId = lo.StrandId,
            StrandName = lo.Strand?.Name,
            SubStrandId = lo.SubStrandId,
            SubStrandName = lo.SubStrand?.Name,
            TenantId = lo.TenantId,
            Status = lo.Status.ToString(),
            CreatedOn = lo.CreatedOn,
            UpdatedOn = lo.UpdatedOn
        };
    }
}
