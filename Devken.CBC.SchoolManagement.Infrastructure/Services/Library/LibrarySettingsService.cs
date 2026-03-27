// Infrastructure/Services/Library/LibrarySettingsService.cs
using System;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class LibrarySettingsService : ILibrarySettingsService
    {
        private readonly IRepositoryManager _repositories;

        public LibrarySettingsService(IRepositoryManager repositories)
        {
            _repositories = repositories
                ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ───────────────────────────────────────────────────────────────

        public async Task<LibrarySettingsDto> GetSettingsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(schoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);

            var settings = await _repositories.LibrarySettings
                .GetBySchoolIdAsync(targetSchoolId.Value, trackChanges: false);

            // Return default DTO if no settings row exists yet
            return settings is null
                ? BuildDefaultDto(targetSchoolId.Value)
                : MapToDto(settings);
        }

        // ── UPSERT ────────────────────────────────────────────────────────────

        public async Task<LibrarySettingsDto> UpsertSettingsAsync(
            UpsertLibrarySettingsRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(
                request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);

            var existing = await _repositories.LibrarySettings
                .GetBySchoolIdAsync(targetSchoolId.Value, trackChanges: true);

            if (existing is null)
            {
                // ── CREATE ────────────────────────────────────────────────────
                var newSettings = new LibrarySettings
                {
                    Id = Guid.NewGuid(),
                    TenantId = targetSchoolId.Value,
                    MaxBooksPerStudent = request.MaxBooksPerStudent,
                    MaxBooksPerTeacher = request.MaxBooksPerTeacher,
                    BorrowDaysStudent = request.BorrowDaysStudent,
                    BorrowDaysTeacher = request.BorrowDaysTeacher,
                    FinePerDay = request.FinePerDay,
                    AllowBookReservation = request.AllowBookReservation
                };

                _repositories.LibrarySettings.Create(newSettings);
                await _repositories.SaveAsync();

                return MapToDto(newSettings);
            }

            // ── UPDATE ────────────────────────────────────────────────────────
            existing.MaxBooksPerStudent = request.MaxBooksPerStudent;
            existing.MaxBooksPerTeacher = request.MaxBooksPerTeacher;
            existing.BorrowDaysStudent = request.BorrowDaysStudent;
            existing.BorrowDaysTeacher = request.BorrowDaysTeacher;
            existing.FinePerDay = request.FinePerDay;
            existing.AllowBookReservation = request.AllowBookReservation;

            _repositories.LibrarySettings.Update(existing);
            await _repositories.SaveAsync();

            return MapToDto(existing);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

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
                    "You must be assigned to a school to manage library settings.");

            return userSchoolId;
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school is null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private static LibrarySettingsDto BuildDefaultDto(Guid schoolId) => new()
        {
            Id = Guid.Empty,
            SchoolId = schoolId,
            MaxBooksPerStudent = 2,
            MaxBooksPerTeacher = 5,
            BorrowDaysStudent = 7,
            BorrowDaysTeacher = 14,
            FinePerDay = 10,
            AllowBookReservation = true
        };

        private static LibrarySettingsDto MapToDto(LibrarySettings s) => new()
        {
            Id = s.Id,
            SchoolId = s.TenantId,
            MaxBooksPerStudent = s.MaxBooksPerStudent,
            MaxBooksPerTeacher = s.MaxBooksPerTeacher,
            BorrowDaysStudent = s.BorrowDaysStudent,
            BorrowDaysTeacher = s.BorrowDaysTeacher,
            FinePerDay = s.FinePerDay,
            AllowBookReservation = s.AllowBookReservation
        };
    }
}