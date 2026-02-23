using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Academics
{
    public class StrandService : IStrandService
    {
        private readonly IRepositoryManager _repositories;

        public StrandService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<StrandResponseDto>> GetAllStrandsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            Guid? learningAreaId = null)
        {
            IEnumerable<Strand> strands;

            if (isSuperAdmin)
            {
                strands = learningAreaId.HasValue
                    ? await _repositories.Strand.GetByLearningAreaAsync(learningAreaId.Value, trackChanges: false)
                    : schoolId.HasValue
                        ? await _repositories.Strand.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                        : await _repositories.Strand.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view strands.");

                strands = learningAreaId.HasValue
                    ? (await _repositories.Strand.GetByLearningAreaAsync(learningAreaId.Value, trackChanges: false))
                        .Where(s => s.TenantId == userSchoolId.Value)
                    : await _repositories.Strand.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            return strands.OrderBy(s => s.Name).Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<StrandResponseDto> GetStrandByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var strand = await _repositories.Strand.GetByIdWithDetailsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Strand with ID '{id}' not found.");

            ValidateAccess(strand.TenantId, userSchoolId, isSuperAdmin);
            return MapToDto(strand);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<StrandResponseDto> CreateStrandAsync(
            CreateStrandDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            // Validate LearningArea belongs to same tenant
            var learningArea = await _repositories.LearningArea.GetByIdAsync(dto.LearningAreaId, trackChanges: false)
                ?? throw new NotFoundException($"Learning area with ID '{dto.LearningAreaId}' not found.");

            if (learningArea.TenantId != tenantId)
                throw new ValidationException("The specified learning area does not belong to this school.");

            if (await _repositories.Strand.ExistsByNameAsync(dto.Name, dto.LearningAreaId))
                throw new ConflictException($"A strand named '{dto.Name}' already exists under this learning area.");

            var strand = new Strand
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.Name,
                LearningAreaId = dto.LearningAreaId
            };

            _repositories.Strand.Create(strand);
            await _repositories.SaveAsync();

            strand.LearningArea = learningArea;
            return MapToDto(strand);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<StrandResponseDto> UpdateStrandAsync(
            Guid id,
            UpdateStrandDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var existing = await _repositories.Strand.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Strand with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            // Validate new LearningArea belongs to same tenant
            var learningArea = await _repositories.LearningArea.GetByIdAsync(dto.LearningAreaId, trackChanges: false)
                ?? throw new NotFoundException($"Learning area with ID '{dto.LearningAreaId}' not found.");

            if (learningArea.TenantId != existing.TenantId)
                throw new ValidationException("The specified learning area does not belong to this school.");

            if (await _repositories.Strand.ExistsByNameAsync(dto.Name, dto.LearningAreaId, excludeId: id))
                throw new ConflictException($"A strand named '{dto.Name}' already exists under this learning area.");

            existing.Name = dto.Name;
            existing.LearningAreaId = dto.LearningAreaId;

            _repositories.Strand.Update(existing);
            await _repositories.SaveAsync();

            existing.LearningArea = learningArea;
            return MapToDto(existing);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE (soft)
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteStrandAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var strand = await _repositories.Strand.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Strand with ID '{id}' not found.");

            ValidateAccess(strand.TenantId, userSchoolId, isSuperAdmin);

            _repositories.Strand.Delete(strand);
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
                    throw new ValidationException("TenantId is required for SuperAdmin when creating a strand.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to create strands.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid strandTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || strandTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this strand.");
        }

        private static StrandResponseDto MapToDto(Strand s) => new()
        {
            Id = (Guid)s.Id!,
            Name = s.Name,
            LearningAreaId = s.LearningAreaId,
            LearningAreaName = s.LearningArea?.Name,
            TenantId = s.TenantId,
            Status = s.Status.ToString(),
            CreatedOn = s.CreatedOn,
            UpdatedOn = s.UpdatedOn
        };
    }
}