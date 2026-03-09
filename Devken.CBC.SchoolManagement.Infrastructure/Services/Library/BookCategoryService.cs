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
    public class BookCategoryService : IBookCategoryService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IBookCategoryRepository _categoryRepository;

        public BookCategoryService(
            IRepositoryManager repositories,
            IBookCategoryRepository categoryRepository)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        public async Task<IEnumerable<BookCategoryResponseDto>> GetAllAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            IEnumerable<BookCategory> categories;

            if (isSuperAdmin)
            {
                categories = schoolId.HasValue
                    ? await _categoryRepository.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                    : await _categoryRepository.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view book categories.");
                categories = await _categoryRepository.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            var schoolNameMap = await BuildSchoolNameMapAsync(categories.Select(c => c.TenantId).Distinct());
            return categories.Select(c => MapToDto(c, schoolNameMap.GetValueOrDefault(c.TenantId)));
        }

        public async Task<BookCategoryResponseDto> GetByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var category = await _categoryRepository.GetByIdAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Book category with ID '{id}' not found.");

            ValidateAccess(category.TenantId, userSchoolId, isSuperAdmin);

            var schoolName = await ResolveSchoolNameAsync(category.TenantId);
            return MapToDto(category, schoolName);
        }

        public async Task<BookCategoryResponseDto> CreateAsync(
            CreateBookCategoryDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

            if (await _categoryRepository.ExistsByNameAsync(dto.Name, tenantId))
                throw new ConflictException($"A book category named '{dto.Name}' already exists for this school.");

            var category = new BookCategory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.Name,
                Description = dto.Description,
            };

            _categoryRepository.Create(category);
            await _repositories.SaveAsync();

            return MapToDto(category, school.Name);
        }

        public async Task<BookCategoryResponseDto> UpdateAsync(
            Guid id, UpdateBookCategoryDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var existing = await _categoryRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Book category with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            if (await _categoryRepository.ExistsByNameAsync(dto.Name, existing.TenantId, excludeId: id))
                throw new ConflictException($"A book category named '{dto.Name}' already exists for this school.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;

            _categoryRepository.Update(existing);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(existing.TenantId);
            return MapToDto(existing, schoolName);
        }

        public async Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var category = await _categoryRepository.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Book category with ID '{id}' not found.");

            ValidateAccess(category.TenantId, userSchoolId, isSuperAdmin);

            _categoryRepository.Delete(category);
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

        private static BookCategoryResponseDto MapToDto(BookCategory c, string? schoolName = null) => new()
        {
            Id = (Guid)c.Id!,
            Name = c.Name,
            Description = c.Description,
            TenantId = c.TenantId,
            SchoolName = schoolName,
            Status = c.Status.ToString(),
            CreatedOn = c.CreatedOn,
            UpdatedOn = c.UpdatedOn,
        };
    }
}
