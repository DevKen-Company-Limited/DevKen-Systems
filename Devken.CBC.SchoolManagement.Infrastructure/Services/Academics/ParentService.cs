using Devken.CBC.SchoolManagement.Application.DTOs.Parents;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.NumberSeries;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Resolves ambiguity between Application.Exceptions.ValidationException
// and System.ComponentModel.DataAnnotations.ValidationException
using DataValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services
{
    public class ParentService : IParentService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IDocumentNumberSeriesRepository _documentNumberService;

        public ParentService(
            IRepositoryManager repositories,
            IDocumentNumberSeriesRepository documentNumberService)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<ParentSummaryDto>> GetAllAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            ParentQueryDto query)
        {
            IEnumerable<Parent> parents;

            if (isSuperAdmin)
            {
                if (!schoolId.HasValue)
                    throw new DataValidationException(
                        "schoolId is required for SuperAdmin when listing parents.");

                parents = await _repositories.Parent.GetByTenantIdAsync(
                    schoolId.Value, trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view parents.");

                parents = await _repositories.Parent.GetByTenantIdAsync(
                    userSchoolId.Value, trackChanges: false);
            }

            // ── In-memory filters ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim().ToLower();
                parents = parents.Where(p =>
                    p.FirstName.ToLower().Contains(term) ||
                    p.LastName.ToLower().Contains(term) ||
                    (p.Email != null && p.Email.ToLower().Contains(term)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(term)));
            }

            if (query.Relationship.HasValue)
                parents = parents.Where(p => p.Relationship == query.Relationship.Value);

            if (query.IsPrimaryContact.HasValue)
                parents = parents.Where(p => p.IsPrimaryContact == query.IsPrimaryContact.Value);

            if (query.IsEmergencyContact.HasValue)
                parents = parents.Where(p => p.IsEmergencyContact == query.IsEmergencyContact.Value);

            if (query.HasPortalAccess.HasValue)
                parents = parents.Where(p => p.HasPortalAccess == query.HasPortalAccess.Value);

            // true → Active only | false → Inactive only | null → exclude Deleted
            if (query.IsActive.HasValue)
                parents = query.IsActive.Value
                    ? parents.Where(p => p.Status == EntityStatus.Active)
                    : parents.Where(p => p.Status == EntityStatus.Inactive);
            else
                parents = parents.Where(p => p.Status != EntityStatus.Deleted);

            return parents.Select(MapToSummary).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ParentDto> GetByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var parent = await _repositories.Parent.GetWithStudentsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            ValidateAccess(parent.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(parent);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY STUDENT
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<ParentSummaryDto>> GetByStudentIdAsync(
            Guid studentId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (!isSuperAdmin && !userSchoolId.HasValue)
                throw new UnauthorizedException("You must be assigned to a school to view parents.");

            var student = await _repositories.Student.GetByIdAsync(studentId, trackChanges: false)
                ?? throw new NotFoundException($"Student with ID '{studentId}' not found.");

            ValidateAccess(student.TenantId, userSchoolId, isSuperAdmin);

            var parents = await _repositories.Parent.GetByStudentIdAsync(
                studentId, student.TenantId, trackChanges: false);

            return parents.Select(MapToSummary).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ParentDto> CreateAsync(
            CreateParentDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var strategy = _repositories.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

                if (!string.IsNullOrWhiteSpace(dto.NationalIdNumber))
                {
                    if (await _repositories.Parent.NationalIdExistsAsync(dto.NationalIdNumber, tenantId))
                        throw new ConflictException(
                            $"A parent with National ID '{dto.NationalIdNumber}' already exists.");
                }

                var parent = new Parent
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FirstName = dto.FirstName.Trim(),
                    MiddleName = dto.MiddleName?.Trim(),
                    LastName = dto.LastName.Trim(),
                    PhoneNumber = dto.PhoneNumber?.Trim(),
                    AlternativePhoneNumber = dto.AlternativePhoneNumber?.Trim(),
                    Email = dto.Email?.Trim().ToLower(),
                    Address = dto.Address?.Trim(),
                    NationalIdNumber = dto.NationalIdNumber?.Trim(),
                    PassportNumber = dto.PassportNumber?.Trim(),
                    Occupation = dto.Occupation?.Trim(),
                    Employer = dto.Employer?.Trim(),
                    EmployerContact = dto.EmployerContact?.Trim(),
                    Relationship = dto.Relationship,
                    IsPrimaryContact = dto.IsPrimaryContact,
                    IsEmergencyContact = dto.IsEmergencyContact,
                    HasPortalAccess = dto.HasPortalAccess,
                    PortalUserId = dto.PortalUserId?.Trim(),
                    Status = EntityStatus.Active
                };

                _repositories.Parent.Create(parent);
                await _repositories.SaveAsync();

                var created = await _repositories.Parent.GetWithStudentsAsync(
                    (Guid)parent.Id!, trackChanges: false)
                    ?? throw new NotFoundException("Failed to reload created parent.");

                return MapToDto(created);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ParentDto> UpdateAsync(
            Guid id, UpdateParentDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var parent = await _repositories.Parent.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            ValidateAccess(parent.TenantId, userSchoolId, isSuperAdmin);

            if (!string.IsNullOrWhiteSpace(dto.NationalIdNumber))
            {
                if (await _repositories.Parent.NationalIdExistsAsync(
                        dto.NationalIdNumber, parent.TenantId, excludeId: id))
                    throw new ConflictException(
                        $"A parent with National ID '{dto.NationalIdNumber}' already exists.");
            }

            parent.FirstName = dto.FirstName.Trim();
            parent.MiddleName = dto.MiddleName?.Trim();
            parent.LastName = dto.LastName.Trim();
            parent.PhoneNumber = dto.PhoneNumber?.Trim();
            parent.AlternativePhoneNumber = dto.AlternativePhoneNumber?.Trim();
            parent.Email = dto.Email?.Trim().ToLower();
            parent.Address = dto.Address?.Trim();
            parent.NationalIdNumber = dto.NationalIdNumber?.Trim();
            parent.PassportNumber = dto.PassportNumber?.Trim();
            parent.Occupation = dto.Occupation?.Trim();
            parent.Employer = dto.Employer?.Trim();
            parent.EmployerContact = dto.EmployerContact?.Trim();
            parent.Relationship = dto.Relationship;
            parent.IsPrimaryContact = dto.IsPrimaryContact;
            parent.IsEmergencyContact = dto.IsEmergencyContact;
            parent.HasPortalAccess = dto.HasPortalAccess;
            parent.PortalUserId = dto.PortalUserId?.Trim();

            _repositories.Parent.Update(parent);
            await _repositories.SaveAsync();

            var updated = await _repositories.Parent.GetWithStudentsAsync(
                id, trackChanges: false)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            return MapToDto(updated);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE (soft)
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var parent = await _repositories.Parent.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            ValidateAccess(parent.TenantId, userSchoolId, isSuperAdmin);

            parent.Status = EntityStatus.Deleted;
            _repositories.Parent.Update(parent);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // ACTIVATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ParentDto> ActivateAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var parent = await _repositories.Parent.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            ValidateAccess(parent.TenantId, userSchoolId, isSuperAdmin);

            parent.Status = EntityStatus.Active;
            _repositories.Parent.Update(parent);
            await _repositories.SaveAsync();

            var updated = await _repositories.Parent.GetWithStudentsAsync(
                id, trackChanges: false)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            return MapToDto(updated);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DEACTIVATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ParentDto> DeactivateAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var parent = await _repositories.Parent.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            ValidateAccess(parent.TenantId, userSchoolId, isSuperAdmin);

            parent.Status = EntityStatus.Inactive;
            _repositories.Parent.Update(parent);
            await _repositories.SaveAsync();

            var updated = await _repositories.Parent.GetWithStudentsAsync(
                id, trackChanges: false)
                ?? throw new NotFoundException($"Parent with ID '{id}' not found.");

            return MapToDto(updated);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new DataValidationException(
                        "TenantId is required for SuperAdmin when creating a parent.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException(
                    "You must be assigned to a school to create parents.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid parentTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || parentTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this parent record.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // MAPPERS
        // ─────────────────────────────────────────────────────────────────────

        private static ParentDto MapToDto(Parent p) => new()
        {
            Id = (Guid)p.Id!,
            TenantId = p.TenantId,
            FirstName = p.FirstName,
            MiddleName = p.MiddleName,
            LastName = p.LastName,
            // FullName and RelationshipDisplay are expression-bodied on ParentDto — not set here
            PhoneNumber = p.PhoneNumber,
            AlternativePhoneNumber = p.AlternativePhoneNumber,
            Email = p.Email,
            Address = p.Address,
            NationalIdNumber = p.NationalIdNumber,
            PassportNumber = p.PassportNumber,
            Occupation = p.Occupation,
            Employer = p.Employer,
            EmployerContact = p.EmployerContact,
            Relationship = p.Relationship,
            IsPrimaryContact = p.IsPrimaryContact,
            IsEmergencyContact = p.IsEmergencyContact,
            HasPortalAccess = p.HasPortalAccess,
            PortalUserId = p.PortalUserId,
            Status = p.Status.ToString(),
            CreatedOn = p.CreatedOn,
            UpdatedOn = p.UpdatedOn,
            StudentCount = p.Students?.Count ?? 0
        };

        private static ParentSummaryDto MapToSummary(Parent p) => new()
        {
            Id = (Guid)p.Id!,
            // FullName is a plain property on ParentSummaryDto — must be built here
            FullName = string.Join(" ",
                                    new[] { p.FirstName, p.MiddleName, p.LastName }
                                    .Where(s => !string.IsNullOrWhiteSpace(s))),
            PhoneNumber = p.PhoneNumber,
            Email = p.Email,
            Relationship = p.Relationship,
            // RelationshipDisplay is expression-bodied on ParentSummaryDto — not set here
            IsPrimaryContact = p.IsPrimaryContact,
            IsEmergencyContact = p.IsEmergencyContact,
            HasPortalAccess = p.HasPortalAccess,
            StudentCount = p.Students?.Count ?? 0,
            Status = p.Status.ToString()
        };
    }
}