using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Curriculum
{
    public class SubStrandService : ISubStrandService
    {
        private readonly IRepositoryManager _repositories;

        public SubStrandService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<SubStrandResponseDto>> GetAllSubStrandsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            Guid? strandId = null)
        {
            IEnumerable<SubStrand> subStrands;

            if (isSuperAdmin)
            {
                subStrands = strandId.HasValue
                    ? await _repositories.SubStrand.GetByStrandAsync(strandId.Value, trackChanges: false)
                    : schoolId.HasValue
                        ? await _repositories.SubStrand.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                        : await _repositories.SubStrand.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view sub-strands.");

                subStrands = strandId.HasValue
                    ? (await _repositories.SubStrand.GetByStrandAsync(strandId.Value, trackChanges: false))
                        .Where(ss => ss.TenantId == userSchoolId.Value)
                    : await _repositories.SubStrand.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            return subStrands.OrderBy(ss => ss.Name).Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubStrandResponseDto> GetSubStrandByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var subStrand = await _repositories.SubStrand.GetByIdWithDetailsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Sub-strand with ID '{id}' not found.");

            ValidateAccess(subStrand.TenantId, userSchoolId, isSuperAdmin);
            return MapToDto(subStrand);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubStrandResponseDto> CreateSubStrandAsync(
            CreateSubStrandDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            // Validate Strand belongs to same tenant
            var strand = await _repositories.Strand.GetByIdWithDetailsAsync(dto.StrandId, trackChanges: false)
                ?? throw new NotFoundException($"Strand with ID '{dto.StrandId}' not found.");

            if (strand.TenantId != tenantId)
                throw new ValidationException("The specified strand does not belong to this school.");

            if (await _repositories.SubStrand.ExistsByNameAsync(dto.Name, dto.StrandId))
                throw new ConflictException($"A sub-strand named '{dto.Name}' already exists under this strand.");

            var subStrand = new SubStrand
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.Name,
                StrandId = dto.StrandId
            };

            _repositories.SubStrand.Create(subStrand);
            await _repositories.SaveAsync();

            subStrand.Strand = strand;
            return MapToDto(subStrand);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubStrandResponseDto> UpdateSubStrandAsync(
            Guid id,
            UpdateSubStrandDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var existing = await _repositories.SubStrand.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Sub-strand with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            var strand = await _repositories.Strand.GetByIdWithDetailsAsync(dto.StrandId, trackChanges: false)
                ?? throw new NotFoundException($"Strand with ID '{dto.StrandId}' not found.");

            if (strand.TenantId != existing.TenantId)
                throw new ValidationException("The specified strand does not belong to this school.");

            if (await _repositories.SubStrand.ExistsByNameAsync(dto.Name, dto.StrandId, excludeId: id))
                throw new ConflictException($"A sub-strand named '{dto.Name}' already exists under this strand.");

            existing.Name = dto.Name;
            existing.StrandId = dto.StrandId;

            _repositories.SubStrand.Update(existing);
            await _repositories.SaveAsync();

            existing.Strand = strand;
            return MapToDto(existing);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE (soft)
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteSubStrandAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var subStrand = await _repositories.SubStrand.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Sub-strand with ID '{id}' not found.");

            ValidateAccess(subStrand.TenantId, userSchoolId, isSuperAdmin);

            _repositories.SubStrand.Delete(subStrand);
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
                    throw new ValidationException("TenantId is required for SuperAdmin when creating a sub-strand.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to create sub-strands.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid subStrandTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || subStrandTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this sub-strand.");
        }

        private static SubStrandResponseDto MapToDto(SubStrand ss) => new()
        {
            Id = (Guid)ss.Id!,
            Name = ss.Name,
            StrandId = ss.StrandId,
            StrandName = ss.Strand?.Name,
            LearningAreaId = ss.Strand?.LearningAreaId ?? Guid.Empty,
            LearningAreaName = ss.Strand?.LearningArea?.Name,
            TenantId = ss.TenantId,
            Status = ss.Status.ToString(),
            CreatedOn = ss.CreatedOn,
            UpdatedOn = ss.UpdatedOn
        };
    }
}
