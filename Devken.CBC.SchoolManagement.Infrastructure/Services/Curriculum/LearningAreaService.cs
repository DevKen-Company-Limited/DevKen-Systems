using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Application.Service.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Academics
{
    public class LearningAreaService : ILearningAreaService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IDocumentNumberSeriesRepository _documentNumberService;

        private const string LA_NUMBER_SERIES = "LearningArea";
        private const string LA_PREFIX = "LA";

        public LearningAreaService(
            IRepositoryManager repositories,
            IDocumentNumberSeriesRepository documentNumberService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<LearningAreaResponseDto>> GetAllLearningAreasAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            CBCLevel? level = null,
            bool? isActive = null)
        {
            IEnumerable<LearningArea> areas;

            if (isSuperAdmin)
            {
                areas = schoolId.HasValue
                    ? await _repositories.LearningArea.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.LearningArea.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view learning areas.");

                areas = await _repositories.LearningArea.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            if (level.HasValue)
                areas = areas.Where(la => la.Level == level.Value);

            return areas.OrderBy(la => la.Name).Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningAreaResponseDto> GetLearningAreaByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var area = await _repositories.LearningArea.GetByIdWithDetailsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Learning area with ID '{id}' not found.");

            ValidateAccess(area.TenantId, userSchoolId, isSuperAdmin);
            return MapToDto(area);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY CODE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningAreaResponseDto> GetLearningAreaByCodeAsync(string code, Guid? userSchoolId, bool isSuperAdmin)
        {
            var tenantId = userSchoolId
                ?? throw new ValidationException("SuperAdmin must use GetAll with schoolId filter for code lookup.");

            var area = await _repositories.LearningArea.GetByCodeAsync(code, tenantId)
                ?? throw new NotFoundException($"Learning area with code '{code}' not found.");

            return MapToDto(area);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningAreaResponseDto> CreateLearningAreaAsync(
            CreateLearningAreaDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

            if (await _repositories.LearningArea.ExistsByNameAsync(dto.Name, tenantId))
                throw new ConflictException($"A learning area named '{dto.Name}' already exists for this school.");

            // Auto-generate code via number series
            var code = await ResolveLearningAreaCodeAsync(tenantId);

            var area = new LearningArea
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.Name,
                Code = code,
                Level = dto.Level
            };

            _repositories.LearningArea.Create(area);
            await _repositories.SaveAsync();

            return MapToDto(area);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<LearningAreaResponseDto> UpdateLearningAreaAsync(
            Guid id,
            UpdateLearningAreaDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var existing = await _repositories.LearningArea.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Learning area with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            if (await _repositories.LearningArea.ExistsByNameAsync(dto.Name, existing.TenantId, excludeId: id))
                throw new ConflictException($"A learning area named '{dto.Name}' already exists for this school.");

            existing.Name = dto.Name;
            // Code is immutable after creation — do not update
            existing.Level = dto.Level;

            _repositories.LearningArea.Update(existing);
            await _repositories.SaveAsync();

            return MapToDto(existing);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE (soft)
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteLearningAreaAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var area = await _repositories.LearningArea.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Learning area with ID '{id}' not found.");

            ValidateAccess(area.TenantId, userSchoolId, isSuperAdmin);

            _repositories.LearningArea.Delete(area);
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
                    throw new ValidationException("TenantId is required for SuperAdmin when creating a learning area.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to create learning areas.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid areaTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || areaTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this learning area.");
        }

        private async Task<string> ResolveLearningAreaCodeAsync(Guid tenantId)
        {
            var seriesExists = await _documentNumberService.SeriesExistsAsync(LA_NUMBER_SERIES, tenantId);

            if (!seriesExists)
            {
                await _documentNumberService.CreateSeriesAsync(
                    entityName: LA_NUMBER_SERIES,
                    tenantId: tenantId,
                    prefix: LA_PREFIX,
                    padding: 4,
                    resetEveryYear: false,
                    description: "Learning area codes");
            }

            return await _documentNumberService.GenerateAsync(LA_NUMBER_SERIES, tenantId);
        }

        // Returns level as its integer value so the Angular mat-select can bind
        // directly by numeric value (avoids C# enum name vs label mismatch).
        private static LearningAreaResponseDto MapToDto(LearningArea la) => new()
        {
            Id = (Guid)la.Id!,
            Name = la.Name,
            Code = la.Code,
            Level = ((int)la.Level).ToString(),   // ← integer, not enum name
            TenantId = la.TenantId,
            Status = la.Status.ToString(),
            CreatedOn = la.CreatedOn,
            UpdatedOn = la.UpdatedOn
        };
    }
}