using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Academics
{
    public class SubjectService : ISubjectService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IDocumentNumberSeriesRepository _documentNumberService;

        private const string SUBJECT_NUMBER_SERIES = "Subject";
        private const string SUBJECT_PREFIX = "SUB";

        public SubjectService(
            IRepositoryManager repositories,
            IDocumentNumberSeriesRepository documentNumberService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<SubjectResponseDto>> GetAllSubjectsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            CBCLevel? level = null,
            SubjectType? subjectType = null,
            bool? isActive = null)
        {
            IEnumerable<Subject> subjects;

            if (isSuperAdmin)
            {
                subjects = schoolId.HasValue
                    ? await _repositories.Subject.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.Subject.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view subjects.");

                subjects = await _repositories.Subject.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            if (level.HasValue)
                subjects = subjects.Where(s => s.Level == level.Value);
            if (subjectType.HasValue)
                subjects = subjects.Where(s => s.SubjectType == subjectType.Value);
            if (isActive.HasValue)
                subjects = subjects.Where(s => s.IsActive == isActive.Value);

            var subjectList = subjects.OrderBy(s => s.Name).ToList();

            // Build school name map by fetching each distinct TenantId once.
            // Subject has no School navigation property so we look up via repository.
            var schoolNameMap = await BuildSchoolNameMapAsync(
                subjectList.Select(s => s.TenantId).Distinct());

            return subjectList.Select(s =>
                MapToDto(s, schoolNameMap.GetValueOrDefault(s.TenantId)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubjectResponseDto> GetSubjectByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var subject = await _repositories.Subject.GetByIdWithDetailsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Subject with ID '{id}' not found.");

            ValidateAccess(subject.TenantId, userSchoolId, isSuperAdmin);

            var schoolName = await ResolveSchoolNameAsync(subject.TenantId);

            return MapToDto(subject, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY CODE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubjectResponseDto> GetSubjectByCodeAsync(
            string code, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (!userSchoolId.HasValue && !isSuperAdmin)
                throw new UnauthorizedException("School context is required.");

            var tenantId = userSchoolId
                ?? throw new ValidationException(
                    "SuperAdmin must use GetAll with schoolId filter for code lookup.");

            var subject = await _repositories.Subject.GetByCodeAsync(code, tenantId)
                ?? throw new NotFoundException($"Subject with code '{code}' not found.");

            var schoolName = await ResolveSchoolNameAsync(subject.TenantId);

            return MapToDto(subject, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubjectResponseDto> CreateSubjectAsync(
            CreateSubjectDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var strategy = _repositories.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

                var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                    ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

                if (await _repositories.Subject.ExistsByNameAsync(dto.Name, tenantId))
                    throw new ConflictException(
                        $"A subject named '{dto.Name}' already exists for this school.");

                var subjectCode = await ResolveSubjectCodeAsync(tenantId);

                // dto.Level is the alias for dto.CbcLevel (bound via [JsonPropertyName("cbcLevel")])
                var subject = new Subject(
                    name: dto.Name,
                    code: subjectCode,
                    level: dto.Level,
                    subjectType: dto.SubjectType)
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Description = dto.Description,
                    IsActive = dto.IsActive,
                };

                _repositories.Subject.Create(subject);
                await _repositories.SaveAsync();

                return MapToDto(subject, school.Name);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubjectResponseDto> UpdateSubjectAsync(
            Guid id,
            UpdateSubjectDto dto,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var existing = await _repositories.Subject.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Subject with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            if (await _repositories.Subject.ExistsByNameAsync(
                    dto.Name, existing.TenantId, excludeId: id))
                throw new ConflictException(
                    $"A subject named '{dto.Name}' already exists for this school.");

            // dto.Level is the alias for dto.CbcLevel
            var updated = new Subject(
                name: dto.Name,
                code: existing.Code,
                level: dto.Level,
                subjectType: dto.SubjectType)
            {
                Id = existing.Id,
                TenantId = existing.TenantId,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedOn = existing.CreatedOn,
                CreatedBy = existing.CreatedBy,
                Status = existing.Status,
            };

            _repositories.Subject.Update(updated);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(existing.TenantId);

            return MapToDto(updated, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteSubjectAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var subject = await _repositories.Subject.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Subject with ID '{id}' not found.");

            ValidateAccess(subject.TenantId, userSchoolId, isSuperAdmin);
            _repositories.Subject.Delete(subject);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // TOGGLE ACTIVE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SubjectResponseDto> ToggleSubjectActiveAsync(
            Guid id,
            bool isActive,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var subject = await _repositories.Subject.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Subject with ID '{id}' not found.");

            ValidateAccess(subject.TenantId, userSchoolId, isSuperAdmin);

            subject.IsActive = isActive;
            _repositories.Subject.Update(subject);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(subject.TenantId);

            return MapToDto(subject, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fetches the school name for a single TenantId.
        /// Returns null gracefully if the school record cannot be found.
        /// </summary>
        private async Task<string?> ResolveSchoolNameAsync(Guid tenantId)
        {
            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false);
            return school?.Name;
        }

        /// <summary>
        /// Fetches school names for a set of TenantIds in bulk (one await per distinct id).
        /// Never touches Subject.School — Subject has no such navigation property.
        /// </summary>
        private async Task<Dictionary<Guid, string>> BuildSchoolNameMapAsync(
            IEnumerable<Guid> tenantIds)
        {
            var map = new Dictionary<Guid, string>();

            foreach (var tid in tenantIds)
            {
                var school = await _repositories.School.GetByIdAsync(tid, trackChanges: false);
                if (school != null)
                    map[tid] = school.Name;
            }

            return map;
        }

        private Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new ValidationException(
                        "TenantId is required for SuperAdmin when creating a subject.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException(
                    "You must be assigned to a school to create subjects.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid subjectTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || subjectTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this subject.");
        }

        private async Task<string> ResolveSubjectCodeAsync(Guid tenantId)
        {
            var seriesExists = await _documentNumberService
                .SeriesExistsAsync(SUBJECT_NUMBER_SERIES, tenantId);

            if (!seriesExists)
            {
                await _documentNumberService.CreateSeriesAsync(
                    entityName: SUBJECT_NUMBER_SERIES,
                    tenantId: tenantId,
                    prefix: SUBJECT_PREFIX,
                    padding: 5,
                    resetEveryYear: false,
                    description: "Subject codes");
            }

            return await _documentNumberService.GenerateAsync(SUBJECT_NUMBER_SERIES, tenantId);
        }

        /// <summary>
        /// Maps Subject entity → SubjectResponseDto.
        /// schoolName must be passed in explicitly because Subject has no School
        /// navigation property — it is fetched separately via _repositories.School.
        /// Level and SubjectType are serialised as numeric strings so the frontend
        /// resolveCBCLevel() / resolveSubjectType() helpers work without changes.
        /// </summary>
        private static SubjectResponseDto MapToDto(Subject s, string? schoolName = null) => new()
        {
            Id = (Guid)s.Id!,
            Code = s.Code,
            Name = s.Name,
            Description = s.Description,
            Level = ((int)s.Level).ToString(),        // "3" → frontend: "Grade 1"
            SubjectType = ((int)s.SubjectType).ToString(),  // "1" → frontend: "Core"
            IsActive = s.IsActive,
            TenantId = s.TenantId,
            SchoolName = schoolName,                       // resolved via repository, not nav prop
            Status = s.Status.ToString(),
            CreatedOn = s.CreatedOn,
            UpdatedOn = s.UpdatedOn,
        };
    }
}