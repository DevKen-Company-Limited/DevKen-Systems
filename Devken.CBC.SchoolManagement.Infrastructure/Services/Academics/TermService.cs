using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Academics
{
    public class TermService : ITermService
    {
        private readonly IRepositoryManager _repositories;

        public TermService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL TERMS
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TermDto>> GetAllTermsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var terms = await FetchTermsByAccessLevel(schoolId, userSchoolId, isSuperAdmin);
            return terms.Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET TERMS BY ACADEMIC YEAR
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TermDto>> GetTermsByAcademicYearAsync(
            Guid academicYearId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // First, get the academic year to check school ownership
            var academicYear = await _repositories.AcademicYear.GetByIdAsync(academicYearId, false)
                ?? throw new NotFoundException($"Academic year with ID '{academicYearId}' not found.");

            ValidateSchoolAccess(academicYear.TenantId, userSchoolId, isSuperAdmin);

            var terms = await _repositories.Term.GetByAcademicYearIdAsync(academicYearId, false);
            return terms.Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET TERM BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TermDto> GetTermByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var term = await _repositories.Term.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Term with ID '{id}' not found.");

            ValidateSchoolAccess(term.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(term);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET CURRENT TERM
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TermDto?> GetCurrentTermAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(schoolId, userSchoolId, isSuperAdmin, isRequired: false);

            if (!targetSchoolId.HasValue)
            {
                throw new ValidationException("School ID is required to get the current term.");
            }

            var term = await _repositories.Term.GetCurrentTermAsync(targetSchoolId.Value);
            return term != null ? MapToDto(term) : null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ACTIVE TERMS
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TermDto>> GetActiveTermsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(schoolId, userSchoolId, isSuperAdmin, isRequired: false);

            if (!targetSchoolId.HasValue)
            {
                throw new ValidationException("School ID is required to get active terms.");
            }

            var terms = await _repositories.Term.GetActiveTermsAsync(targetSchoolId.Value, false);
            return terms.Select(MapToDto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE TERM - WITH EXECUTION STRATEGY FOR RETRY COMPATIBILITY
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TermDto> CreateTermAsync(
            CreateTermRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // FIXED: Wrap entire operation in execution strategy to handle retry conflicts
            var strategy = _repositories.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // 1. Resolve and validate school
                var targetSchoolId = ResolveSchoolId(request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);
                await ValidateSchoolExistsAsync(targetSchoolId.Value);

                // 2. Validate academic year exists and belongs to the school
                await ValidateAcademicYearAsync(request.AcademicYearId, targetSchoolId.Value);

                // 3. Validate term number is unique within academic year
                await ValidateTermNumberUniqueAsync(request.TermNumber, request.AcademicYearId);

                // 4. Validate date range
                ValidateDateRange(request.StartDate, request.EndDate);

                // 5. Check for date overlaps
                await ValidateNoDateOverlapAsync(request.AcademicYearId, request.StartDate, request.EndDate);

                // 6. If setting as current, unset other current terms
                if (request.IsCurrent)
                {
                    await UnsetCurrentTermsAsync(targetSchoolId.Value);
                }

                // 7. Create term entity
                var term = CreateTermEntity(request, targetSchoolId.Value);

                // 8. Save to database
                _repositories.Term.Create(term);
                await _repositories.SaveAsync();

                // 9. Reload with navigation properties
                var createdTerm = await _repositories.Term.GetByIdWithDetailsAsync(term.Id, false);

                return MapToDto(createdTerm ?? term);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE TERM
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TermDto> UpdateTermAsync(
            Guid id,
            UpdateTermRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var term = await _repositories.Term.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Term with ID '{id}' not found.");

            ValidateSchoolAccess(term.TenantId, userSchoolId, isSuperAdmin);

            // Validate academic year exists and belongs to the school
            await ValidateAcademicYearAsync(request.AcademicYearId, term.TenantId);

            // Validate term number is unique (excluding current term)
            var existingWithNumber = await _repositories.Term
                .GetByTermNumberAsync(request.TermNumber, request.AcademicYearId);

            if (existingWithNumber != null && existingWithNumber.Id != id)
            {
                throw new ConflictException(
                    $"Term number {request.TermNumber} already exists for this academic year.");
            }

            // Validate date range
            ValidateDateRange(request.StartDate, request.EndDate);

            // Check for date overlaps (excluding current term)
            var hasOverlap = await _repositories.Term
                .HasDateOverlapAsync(request.AcademicYearId, request.StartDate, request.EndDate, id);

            if (hasOverlap)
            {
                throw new ConflictException(
                    "The term dates overlap with another term in the same academic year.");
            }

            // If setting as current, unset other current terms
            if (request.IsCurrent && !term.IsCurrent)
            {
                await UnsetCurrentTermsAsync(term.TenantId);
            }

            UpdateTermEntity(term, request);

            _repositories.Term.Update(term);
            await _repositories.SaveAsync();

            // Reload with navigation properties
            var updatedTerm = await _repositories.Term.GetByIdWithDetailsAsync(id, false);

            return MapToDto(updatedTerm ?? term);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE TERM
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteTermAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var term = await _repositories.Term.GetByIdWithDetailsAsync(id, true)
                ?? throw new NotFoundException($"Term with ID '{id}' not found.");

            ValidateSchoolAccess(term.TenantId, userSchoolId, isSuperAdmin);

            // Prevent deletion if term has related data
            if (term.Assessments.Any())
            {
                throw new ValidationException(
                    "Cannot delete term with existing assessments. Please delete or reassign assessments first.");
            }

            if (term.ProgressReports.Any())
            {
                throw new ValidationException(
                    "Cannot delete term with existing progress reports. Please delete or reassign reports first.");
            }

            if (term.Grades.Any())
            {
                throw new ValidationException(
                    "Cannot delete term with existing grades. Please delete or reassign grades first.");
            }

            _repositories.Term.Delete(term);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // SET CURRENT TERM
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TermDto> SetCurrentTermAsync(
            Guid termId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var term = await _repositories.Term.GetByIdAsync(termId, true)
                ?? throw new NotFoundException($"Term with ID '{termId}' not found.");

            ValidateSchoolAccess(term.TenantId, userSchoolId, isSuperAdmin);

            if (term.IsClosed)
            {
                throw new ValidationException("Cannot set a closed term as current.");
            }

            // Unset all other current terms in the school
            await UnsetCurrentTermsAsync(term.TenantId);

            term.IsCurrent = true;
            _repositories.Term.Update(term);
            await _repositories.SaveAsync();

            var updatedTerm = await _repositories.Term.GetByIdWithDetailsAsync(termId, false);
            return MapToDto(updatedTerm ?? term);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CLOSE TERM
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TermDto> CloseTermAsync(
            Guid termId,
            string? remarks,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var term = await _repositories.Term.GetByIdAsync(termId, true)
                ?? throw new NotFoundException($"Term with ID '{termId}' not found.");

            ValidateSchoolAccess(term.TenantId, userSchoolId, isSuperAdmin);

            if (term.IsClosed)
            {
                throw new ValidationException("This term is already closed.");
            }

            term.IsClosed = true;
            term.IsCurrent = false; // Cannot be current if closed

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                term.Notes = string.IsNullOrWhiteSpace(term.Notes)
                    ? $"Closed: {remarks}"
                    : $"{term.Notes}\nClosed: {remarks}";
            }

            _repositories.Term.Update(term);
            await _repositories.SaveAsync();

            var updatedTerm = await _repositories.Term.GetByIdWithDetailsAsync(termId, false);
            return MapToDto(updatedTerm ?? term);
        }

        // ─────────────────────────────────────────────────────────────────────
        // REOPEN TERM
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TermDto> ReopenTermAsync(
            Guid termId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var term = await _repositories.Term.GetByIdAsync(termId, true)
                ?? throw new NotFoundException($"Term with ID '{termId}' not found.");

            ValidateSchoolAccess(term.TenantId, userSchoolId, isSuperAdmin);

            if (!term.IsClosed)
            {
                throw new ValidationException("This term is not closed.");
            }

            term.IsClosed = false;

            _repositories.Term.Update(term);
            await _repositories.SaveAsync();

            var updatedTerm = await _repositories.Term.GetByIdWithDetailsAsync(termId, false);
            return MapToDto(updatedTerm ?? term);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS - Business Logic
        // ─────────────────────────────────────────────────────────────────────
        private async Task<IEnumerable<Term>> FetchTermsByAccessLevel(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // ✅ SuperAdmin: Full access
            if (isSuperAdmin)
            {
                if (schoolId.HasValue)
                    return await _repositories.Term
                        .GetBySchoolIdAsync(schoolId.Value, trackChanges: false);

                return await _repositories.Term
                    .GetAllAsync(trackChanges: false);
            }

            // ❌ Non-super admin must belong to a school
            if (!userSchoolId.HasValue)
                throw new UnauthorizedException(
                    "You must be assigned to a school to view terms.");

            return await _repositories.Term
                .GetBySchoolIdAsync(userSchoolId.Value, trackChanges: false);
        }

        /// <summary>
        /// Resolves which school ID to use based on user type and request data.
        /// </summary>
        private Guid? ResolveSchoolId(
            Guid? requestSchoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            bool isRequired)
        {
            // ✅ SuperAdmin: Can provide school ID or work across all schools
            if (isSuperAdmin)
            {
                if (isRequired && (!requestSchoolId.HasValue || requestSchoolId.Value == Guid.Empty))
                {
                    throw new ValidationException(
                        "SchoolId is required for SuperAdmin when creating/managing terms. " +
                        "Please specify which school this term should be assigned to.");
                }
                return requestSchoolId;
            }

            // ✅ Regular users: Use their assigned school ID
            if (!userSchoolId.HasValue || userSchoolId.Value == Guid.Empty)
            {
                throw new UnauthorizedException(
                    "You must be assigned to a school to manage terms.");
            }

            return userSchoolId;
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school == null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private void ValidateSchoolAccess(Guid termSchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            // SuperAdmin can access any term
            if (isSuperAdmin)
                return;

            // Regular users can only access terms from their school
            if (!userSchoolId.HasValue || termSchoolId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this term.");
        }

        private async Task ValidateAcademicYearAsync(Guid academicYearId, Guid schoolId)
        {
            var academicYear = await _repositories.AcademicYear.GetByIdAsync(academicYearId, false);

            if (academicYear == null)
                throw new NotFoundException($"Academic year with ID '{academicYearId}' not found.");

            if (academicYear.TenantId != schoolId)
                throw new ValidationException(
                    "The academic year does not belong to the specified school.");
        }

        private async Task ValidateTermNumberUniqueAsync(int termNumber, Guid academicYearId)
        {
            var existing = await _repositories.Term
                .GetByTermNumberAsync(termNumber, academicYearId);

            if (existing != null)
                throw new ConflictException(
                    $"Term number {termNumber} already exists for this academic year.");
        }

        private void ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new ValidationException("Start date must be before end date.");
        }

        private async Task ValidateNoDateOverlapAsync(
            Guid academicYearId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeTermId = null)
        {
            var hasOverlap = await _repositories.Term
                .HasDateOverlapAsync(academicYearId, startDate, endDate, excludeTermId);

            if (hasOverlap)
                throw new ConflictException(
                    "The term dates overlap with another term in the same academic year.");
        }

        private async Task UnsetCurrentTermsAsync(Guid schoolId)
        {
            var currentTerms = await _repositories.Term
                .FindByCondition(t => t.TenantId == schoolId && t.IsCurrent, trackChanges: true)
                .ToListAsync();

            foreach (var term in currentTerms)
            {
                term.IsCurrent = false;
                _repositories.Term.Update(term);
            }

            if (currentTerms.Any())
            {
                await _repositories.SaveAsync();
            }
        }

        private Term CreateTermEntity(CreateTermRequest request, Guid schoolId)
        {
            return new Term
            {
                Id = Guid.NewGuid(),
                TenantId = schoolId,
                Name = request.Name.Trim(),
                TermNumber = request.TermNumber,
                AcademicYearId = request.AcademicYearId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsCurrent = request.IsCurrent,
                IsClosed = request.IsClosed,
                Notes = request.Notes?.Trim()
            };
        }

        private void UpdateTermEntity(Term term, UpdateTermRequest request)
        {
            term.Name = request.Name.Trim();
            term.TermNumber = request.TermNumber;
            term.AcademicYearId = request.AcademicYearId;
            term.StartDate = request.StartDate;
            term.EndDate = request.EndDate;
            term.IsCurrent = request.IsCurrent;
            term.IsClosed = request.IsClosed;
            term.Notes = request.Notes?.Trim();
        }

        private TermDto MapToDto(Term term)
        {
            var today = DateTime.Today;
            var isActive = !term.IsClosed && today >= term.StartDate && today <= term.EndDate;

            string status;
            if (term.IsClosed)
                status = "Closed";
            else if (term.IsCurrent)
                status = "Current";
            else if (today < term.StartDate)
                status = "Upcoming";
            else if (today > term.EndDate)
                status = "Past";
            else
                status = "Active";

            return new TermDto
            {
                Id = term.Id,
                SchoolId = term.TenantId,
                SchoolName = term.AcademicYear?.School?.Name ?? string.Empty,
                Name = term.Name ?? string.Empty,
                TermNumber = term.TermNumber,
                AcademicYearId = term.AcademicYearId,
                AcademicYearName = term.AcademicYear?.Name ?? string.Empty,
                StartDate = term.StartDate,
                EndDate = term.EndDate,
                IsCurrent = term.IsCurrent,
                IsClosed = term.IsClosed,
                IsActive = isActive,
                Notes = term.Notes ?? string.Empty,
                DurationDays = (term.EndDate - term.StartDate).Days + 1,
                Status = status
            };
        }
    }
}
