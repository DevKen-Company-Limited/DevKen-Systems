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
    public class BookAuthorService : IBookAuthorService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IBookAuthorRepository _authorRepository;

        public BookAuthorService(
            IRepositoryManager repositories,
            IBookAuthorRepository authorRepository)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _authorRepository = authorRepository ?? throw new ArgumentNullException(nameof(authorRepository));
        }

        public async Task<IEnumerable<BookAuthorResponseDto>> GetAllAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            IEnumerable<BookAuthor> authors;

            if (isSuperAdmin)
            {
                authors = schoolId.HasValue
                    ? await _authorRepository.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                    : await _authorRepository.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view book authors.");
                authors = await _authorRepository.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            var tenantIds = authors.Select(a => a.TenantId).Distinct();
            var schoolNameMap = await BuildSchoolNameMapAsync(tenantIds);

            return authors.Select(a => MapToDto(a, schoolNameMap.GetValueOrDefault(a.TenantId)));
        }

        public async Task<BookAuthorResponseDto> GetByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var author = await _authorRepository.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Book author with ID '{id}' not found.");

            ValidateAccess(author.TenantId, userSchoolId, isSuperAdmin);

            var schoolName = await ResolveSchoolNameAsync(author.TenantId);
            return MapToDto(author, schoolName);
        }

        public async Task<BookAuthorResponseDto> CreateAsync(
            CreateBookAuthorDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

            if (await _authorRepository.ExistsByNameAsync(dto.Name, tenantId))
                throw new ConflictException($"A book author named '{dto.Name}' already exists for this school.");

            var author = new BookAuthor
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.Name,
                Biography = dto.Biography,
            };

            _authorRepository.Create(author);
            await _repositories.SaveAsync();

            return MapToDto(author, school.Name);
        }

        public async Task<BookAuthorResponseDto> UpdateAsync(
            Guid id, UpdateBookAuthorDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var existing = await _authorRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Book author with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            if (await _authorRepository.ExistsByNameAsync(dto.Name, existing.TenantId, excludeId: id))
                throw new ConflictException($"A book author named '{dto.Name}' already exists for this school.");

            existing.Name = dto.Name;
            existing.Biography = dto.Biography;

            _authorRepository.Update(existing);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(existing.TenantId);
            return MapToDto(existing, schoolName);
        }

        public async Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var author = await _authorRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Book author with ID '{id}' not found.");

            ValidateAccess(author.TenantId, userSchoolId, isSuperAdmin);

            _authorRepository.Delete(author);
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

        private static BookAuthorResponseDto MapToDto(BookAuthor a, string? schoolName = null) => new()
        {
            Id = (Guid)a.Id!,
            Name = a.Name,
            Biography = a.Biography,
            TenantId = a.TenantId,
            SchoolName = schoolName,
            Status = a.Status.ToString(),
            CreatedOn = a.CreatedOn,
            UpdatedOn = a.UpdatedOn,
        };
    }
}
