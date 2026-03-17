using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class LibraryBranchService : ILibraryBranchService
    {
        private readonly IRepositoryManager _repositories;

        public LibraryBranchService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ALL ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<LibraryBranchDto>> GetAllBranchesAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var branches = await FetchBranchesByAccessLevel(schoolId, userSchoolId, isSuperAdmin);
            return branches.Select(MapToDto);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        public async Task<LibraryBranchDto> GetBranchByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var branch = await _repositories.LibraryBranch.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Library branch with ID '{id}' not found.");

            ValidateSchoolAccess(branch.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(branch);
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<LibraryBranchDto> CreateBranchAsync(
            CreateLibraryBranchRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);
            await ValidateNameUniqueAsync(request.Name.Trim(), targetSchoolId.Value);

            var branch = new LibraryBranch
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId.Value,
                Name = request.Name.Trim(),
                Location = request.Location?.Trim()
            };

            _repositories.LibraryBranch.Create(branch);
            await _repositories.SaveAsync();

            var created = await _repositories.LibraryBranch.GetByIdWithDetailsAsync(branch.Id, false);
            return MapToDto(created ?? branch);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<LibraryBranchDto> UpdateBranchAsync(
            Guid id,
            UpdateLibraryBranchRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var branch = await _repositories.LibraryBranch.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Library branch with ID '{id}' not found.");

            ValidateSchoolAccess(branch.TenantId, userSchoolId, isSuperAdmin);

            // Check name uniqueness (excluding self)
            var existingWithName = await _repositories.LibraryBranch
                .GetByNameAsync(request.Name.Trim(), branch.TenantId);

            if (existingWithName != null && existingWithName.Id != id)
                throw new ConflictException(
                    $"A library branch named '{request.Name}' already exists in this school.");

            branch.Name = request.Name.Trim();
            branch.Location = request.Location?.Trim();

            _repositories.LibraryBranch.Update(branch);
            await _repositories.SaveAsync();

            var updated = await _repositories.LibraryBranch.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? branch);
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task DeleteBranchAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var branch = await _repositories.LibraryBranch.GetByIdWithDetailsAsync(id, true)
                ?? throw new NotFoundException($"Library branch with ID '{id}' not found.");

            ValidateSchoolAccess(branch.TenantId, userSchoolId, isSuperAdmin);

            if (branch.BookCopies.Any())
                throw new ValidationException(
                    "Cannot delete a library branch that holds book copies. " +
                    "Please move or remove all copies first.");

            _repositories.LibraryBranch.Delete(branch);
            await _repositories.SaveAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<IEnumerable<LibraryBranch>> FetchBranchesByAccessLevel(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (schoolId.HasValue)
                    return await _repositories.LibraryBranch
                        .GetBySchoolIdAsync(schoolId.Value, false);

                return await _repositories.LibraryBranch.GetAllAsync(false);
            }

            if (!userSchoolId.HasValue)
                throw new UnauthorizedException(
                    "You must be assigned to a school to view library branches.");

            return await _repositories.LibraryBranch
                .GetBySchoolIdAsync(userSchoolId.Value, false);
        }

        private Guid? ResolveSchoolId(
            Guid? requestSchoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isRequired)
        {
            if (isSuperAdmin)
            {
                if (isRequired && (!requestSchoolId.HasValue || requestSchoolId.Value == Guid.Empty))
                    throw new ValidationException("SchoolId is required for SuperAdmin.");
                return requestSchoolId;
            }

            if (!userSchoolId.HasValue || userSchoolId.Value == Guid.Empty)
                throw new UnauthorizedException(
                    "You must be assigned to a school to manage library branches.");

            return userSchoolId;
        }

        private void ValidateSchoolAccess(Guid entitySchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || entitySchoolId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this library branch.");
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school == null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private async Task ValidateNameUniqueAsync(string name, Guid schoolId)
        {
            var existing = await _repositories.LibraryBranch.GetByNameAsync(name, schoolId);
            if (existing != null)
                throw new ConflictException(
                    $"A library branch named '{name}' already exists in this school.");
        }

        private static LibraryBranchDto MapToDto(LibraryBranch branch)
        {
            return new LibraryBranchDto
            {
                Id = branch.Id,
                SchoolId = branch.TenantId,
                SchoolName = string.Empty,
                Name = branch.Name,
                Location = branch.Location,
                TotalCopies = branch.BookCopies?.Count ?? 0,
                AvailableCopies = branch.BookCopies?.Count(c => c.IsAvailable && !c.IsLost && !c.IsDamaged) ?? 0
            };
        }
    }
}