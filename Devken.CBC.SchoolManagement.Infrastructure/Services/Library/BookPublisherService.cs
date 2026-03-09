using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class BookPublisherService : IBookPublisherService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IBookPublisherRepository _publisherRepository;

        public BookPublisherService(
            IRepositoryManager repositories,
            IBookPublisherRepository publisherRepository)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _publisherRepository = publisherRepository ?? throw new ArgumentNullException(nameof(publisherRepository));
        }

        public async Task<IEnumerable<BookPublisherResponseDto>> GetAllAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            IEnumerable<BookPublisher> publishers;

            if (isSuperAdmin)
            {
                publishers = schoolId.HasValue
                    ? await _publisherRepository.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                    : await _publisherRepository.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view book publishers.");
                publishers = await _publisherRepository.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            var schoolNameMap = await BuildSchoolNameMapAsync(publishers.Select(p => p.TenantId).Distinct());
            return publishers.Select(p => MapToDto(p, schoolNameMap.GetValueOrDefault(p.TenantId)));
        }

        public async Task<BookPublisherResponseDto> GetByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var publisher = await _publisherRepository.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Book publisher with ID '{id}' not found.");

            ValidateAccess(publisher.TenantId, userSchoolId, isSuperAdmin);

            var schoolName = await ResolveSchoolNameAsync(publisher.TenantId);
            return MapToDto(publisher, schoolName);
        }

        public async Task<BookPublisherResponseDto> CreateAsync(
            CreateBookPublisherDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

            if (await _publisherRepository.ExistsByNameAsync(dto.Name, tenantId))
                throw new ConflictException($"A book publisher named '{dto.Name}' already exists for this school.");

            var publisher = new BookPublisher
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.Name,
                Address = dto.Address,
            };

            _publisherRepository.Create(publisher);
            await _repositories.SaveAsync();

            return MapToDto(publisher, school.Name);
        }

        public async Task<BookPublisherResponseDto> UpdateAsync(
            Guid id, UpdateBookPublisherDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var existing = await _publisherRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Book publisher with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            if (await _publisherRepository.ExistsByNameAsync(dto.Name, existing.TenantId, excludeId: id))
                throw new ConflictException($"A book publisher named '{dto.Name}' already exists for this school.");

            existing.Name = dto.Name;
            existing.Address = dto.Address;

            _publisherRepository.Update(existing);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(existing.TenantId);
            return MapToDto(existing, schoolName);
        }

        public async Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var publisher = await _publisherRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Book publisher with ID '{id}' not found.");

            ValidateAccess(publisher.TenantId, userSchoolId, isSuperAdmin);

            _publisherRepository.Delete(publisher);
            await _repositories.SaveAsync();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task<string?> ResolveSchoolNameAsync(Guid tenantId)
        {
            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false);
            return school?.Name;
        }

        private async Task<Dictionary<Guid, string>> BuildSchoolNameMapAsync(IEnumerable<Guid> tenantIds)
        {
            var map = new Dictionary<Guid, string>();
            foreach (var tid in tenantIds)
            {
                var school = await _repositories.School.GetByIdAsync(tid, trackChanges: false);
                if (school != null) map[tid] = school.Name;
            }
            return map;
        }

        private static Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "TenantId is required for SuperAdmin.");
                return requestTenantId.Value;
            }
            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to perform this action.");
            return userSchoolId.Value;
        }

        private static void ValidateAccess(Guid entityTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || entityTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this resource.");
        }

        private static BookPublisherResponseDto MapToDto(BookPublisher p, string? schoolName = null) => new()
        {
            Id = (Guid)p.Id!,
            Name = p.Name,
            Address = p.Address,
            TenantId = p.TenantId,
            SchoolName = schoolName,
            Status = p.Status.ToString(),
            CreatedOn = p.CreatedOn,
            UpdatedOn = p.UpdatedOn,
        };
    }
}
