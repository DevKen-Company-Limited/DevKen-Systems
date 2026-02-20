using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

                // Apply optional filters
                if (level.HasValue)
                    subjects = subjects.Where(s => s.Level == level.Value);
                if (subjectType.HasValue)
                    subjects = subjects.Where(s => s.SubjectType == subjectType.Value);
                if (isActive.HasValue)
                    subjects = subjects.Where(s => s.IsActive == isActive.Value);

                return subjects.OrderBy(s => s.Name).Select(MapToDto);
            }

            // ─────────────────────────────────────────────────────────────────────
            // GET BY ID
            // ─────────────────────────────────────────────────────────────────────
            public async Task<SubjectResponseDto> GetSubjectByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
            {
                var subject = await _repositories.Subject.GetByIdWithDetailsAsync(id, trackChanges: false)
                    ?? throw new NotFoundException($"Subject with ID '{id}' not found.");

                ValidateAccess(subject.TenantId, userSchoolId, isSuperAdmin);
                return MapToDto(subject);
            }

            // ─────────────────────────────────────────────────────────────────────
            // GET BY CODE
            // ─────────────────────────────────────────────────────────────────────
            public async Task<SubjectResponseDto> GetSubjectByCodeAsync(string code, Guid? userSchoolId, bool isSuperAdmin)
            {
                if (!userSchoolId.HasValue && !isSuperAdmin)
                    throw new UnauthorizedException("School context is required.");

                // For non-SuperAdmin, scope to their school
                var tenantId = userSchoolId
                    ?? throw new ValidationException("SuperAdmin must use GetAll with schoolId filter for code lookup.");

                var subject = await _repositories.Subject.GetByCodeAsync(code, tenantId)
                    ?? throw new NotFoundException($"Subject with code '{code}' not found.");

                return MapToDto(subject);
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
                    // 1. Resolve tenant
                    var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

                    // 2. Validate school exists
                    var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                        ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

                    // 3. Duplicate name check
                    if (await _repositories.Subject.ExistsByNameAsync(dto.Name, tenantId))
                        throw new ConflictException($"A subject named '{dto.Name}' already exists for this school.");

                    // 4. Generate Subject Code via NumberSeries
                    var subjectCode = await ResolveSubjectCodeAsync(tenantId);

                    // 5. Create via primary constructor (Name, Code, Level, SubjectType are private-set)
                    var subject = new Subject(
                        name: dto.Name,
                        code: subjectCode,
                        level: dto.Level,
                        subjectType: dto.SubjectType)
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Description = dto.Description,
                        IsActive = dto.IsActive
                    };

                    _repositories.Subject.Create(subject);
                    await _repositories.SaveAsync();

                    return MapToDto(subject);
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

                // Duplicate name check (exclude self)
                if (await _repositories.Subject.ExistsByNameAsync(dto.Name, existing.TenantId, excludeId: id))
                    throw new ConflictException($"A subject named '{dto.Name}' already exists for this school.");

                // Reconstruct via primary constructor — preserves immutable Code
                var updated = new Subject(
                    name: dto.Name,
                    code: existing.Code,        // Code is immutable
                    level: dto.Level,
                    subjectType: dto.SubjectType)
                {
                    Id = existing.Id,
                    TenantId = existing.TenantId,
                    Description = dto.Description,
                    IsActive = dto.IsActive,
                    CreatedOn = existing.CreatedOn,
                    CreatedBy = existing.CreatedBy,
                    Status = existing.Status
                };

                _repositories.Subject.Update(updated);
                await _repositories.SaveAsync();

                return MapToDto(updated);
            }

            // ─────────────────────────────────────────────────────────────────────
            // DELETE (soft)
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

                return MapToDto(subject);
            }

            // ─────────────────────────────────────────────────────────────────────
            // PRIVATE HELPERS
            // ─────────────────────────────────────────────────────────────────────
            private Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
            {
                if (isSuperAdmin)
                {
                    if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                        throw new ValidationException("TenantId is required for SuperAdmin when creating a subject.");
                    return requestTenantId.Value;
                }

                if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                    throw new UnauthorizedException("You must be assigned to a school to create subjects.");

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
                var seriesExists = await _documentNumberService.SeriesExistsAsync(SUBJECT_NUMBER_SERIES, tenantId);

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

            private static SubjectResponseDto MapToDto(Subject s) => new()
            {
                Id = (Guid)s.Id!,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Level = s.Level.ToString(),
                SubjectType = s.SubjectType.ToString(),
                IsActive = s.IsActive,
                TenantId = s.TenantId,
                Status = s.Status.ToString(),
                CreatedOn = s.CreatedOn,
                UpdatedOn = s.UpdatedOn
            };
        }
}
