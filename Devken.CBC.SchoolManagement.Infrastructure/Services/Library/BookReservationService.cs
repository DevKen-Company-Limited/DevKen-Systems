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
    public class BookReservationService : IBookReservationService
    {
        private readonly IRepositoryManager _repositories;

        public BookReservationService(IRepositoryManager repositories)
        {
            _repositories = repositories
                ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ALL ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<BookReservationDto>> GetAllReservationsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var reservations = await FetchByAccessLevel(schoolId, userSchoolId, isSuperAdmin);
            return reservations.Select(MapToDto);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        public async Task<BookReservationDto> GetReservationByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var reservation = await _repositories.BookReservation
                .GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException(
                    $"Book reservation with ID '{id}' not found.");

            ValidateSchoolAccess(reservation.TenantId, userSchoolId, isSuperAdmin);
            return MapToDto(reservation);
        }

        // ── GET BY BOOK ───────────────────────────────────────────────────────

        public async Task<IEnumerable<BookReservationDto>> GetReservationsByBookAsync(
            Guid bookId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // Verify the book exists and the caller can see it
            var book = await _repositories.Book.GetByIdAsync(bookId, false)
                ?? throw new NotFoundException($"Book with ID '{bookId}' not found.");

            ValidateSchoolAccess(book.TenantId, userSchoolId, isSuperAdmin);

            var reservations = await _repositories.BookReservation
                .GetByBookIdAsync(bookId, false);

            return reservations.Select(MapToDto);
        }

        // ── GET BY MEMBER ─────────────────────────────────────────────────────

        public async Task<IEnumerable<BookReservationDto>> GetReservationsByMemberAsync(
            Guid memberId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var reservations = await _repositories.BookReservation
                .GetByMemberIdAsync(memberId, false);

            // Filter by school for non-SuperAdmin callers
            if (!isSuperAdmin && userSchoolId.HasValue)
                reservations = reservations
                    .Where(r => r.TenantId == userSchoolId.Value)
                    .ToList();

            return reservations.Select(MapToDto);
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<BookReservationDto> CreateReservationAsync(
            CreateBookReservationRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(
                request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            // Validate school exists
            await ValidateSchoolExistsAsync(targetSchoolId!.Value);

            // Validate book belongs to the school
            var book = await _repositories.Book.GetByIdAsync(request.BookId, false)
                ?? throw new NotFoundException(
                    $"Book with ID '{request.BookId}' not found.");

            if (book.TenantId != targetSchoolId.Value)
                throw new ValidationException(
                    "The specified book does not belong to the target school.");

            // Validate member belongs to the school
            var member = await _repositories.LibraryMember.GetByIdAsync(request.MemberId, false)
                ?? throw new NotFoundException(
                    $"Library member with ID '{request.MemberId}' not found.");

            if (member.TenantId != targetSchoolId.Value)
                throw new ValidationException(
                    "The specified member does not belong to the target school.");

            // Guard: no duplicate pending reservation
            var existing = await _repositories.BookReservation
                .GetPendingReservationAsync(request.BookId, request.MemberId);

            if (existing != null)
                throw new ConflictException(
                    "This member already has an unfulfilled reservation for this book.");

            var reservation = new BookReservation
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId.Value,
                BookId = request.BookId,
                MemberId = request.MemberId,
                ReservedOn = DateTime.UtcNow,
                IsFulfilled = false
            };

            _repositories.BookReservation.Create(reservation);
            await _repositories.SaveAsync();

            var created = await _repositories.BookReservation
                .GetByIdWithDetailsAsync(reservation.Id, false);

            return MapToDto(created ?? reservation);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<BookReservationDto> UpdateReservationAsync(
            Guid id,
            UpdateBookReservationRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var reservation = await _repositories.BookReservation
                .GetByIdAsync(id, true)
                ?? throw new NotFoundException(
                    $"Book reservation with ID '{id}' not found.");

            ValidateSchoolAccess(reservation.TenantId, userSchoolId, isSuperAdmin);

            // Validate book belongs to the same school
            var book = await _repositories.Book.GetByIdAsync(request.BookId, false)
                ?? throw new NotFoundException(
                    $"Book with ID '{request.BookId}' not found.");

            if (book.TenantId != reservation.TenantId)
                throw new ValidationException(
                    "The specified book does not belong to the same school as this reservation.");

            // Validate member belongs to the same school
            var member = await _repositories.LibraryMember.GetByIdAsync(request.MemberId, false)
                ?? throw new NotFoundException(
                    $"Library member with ID '{request.MemberId}' not found.");

            if (member.TenantId != reservation.TenantId)
                throw new ValidationException(
                    "The specified member does not belong to the same school as this reservation.");

            // Guard: avoid creating a duplicate reservation for the new book/member combo
            if (reservation.BookId != request.BookId || reservation.MemberId != request.MemberId)
            {
                var duplicate = await _repositories.BookReservation
                    .GetPendingReservationAsync(request.BookId, request.MemberId);

                if (duplicate != null && duplicate.Id != id)
                    throw new ConflictException(
                        "This member already has an unfulfilled reservation for this book.");
            }

            reservation.BookId = request.BookId;
            reservation.MemberId = request.MemberId;
            reservation.IsFulfilled = request.IsFulfilled;

            _repositories.BookReservation.Update(reservation);
            await _repositories.SaveAsync();

            var updated = await _repositories.BookReservation
                .GetByIdWithDetailsAsync(id, false);

            return MapToDto(updated ?? reservation);
        }

        // ── FULFILL ───────────────────────────────────────────────────────────

        public async Task<BookReservationDto> FulfillReservationAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var reservation = await _repositories.BookReservation
                .GetByIdAsync(id, true)
                ?? throw new NotFoundException(
                    $"Book reservation with ID '{id}' not found.");

            ValidateSchoolAccess(reservation.TenantId, userSchoolId, isSuperAdmin);

            if (reservation.IsFulfilled)
                throw new ConflictException(
                    "This reservation has already been fulfilled.");

            reservation.IsFulfilled = true;

            _repositories.BookReservation.Update(reservation);
            await _repositories.SaveAsync();

            var fulfilled = await _repositories.BookReservation
                .GetByIdWithDetailsAsync(id, false);

            return MapToDto(fulfilled ?? reservation);
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task DeleteReservationAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var reservation = await _repositories.BookReservation
                .GetByIdWithDetailsAsync(id, true)
                ?? throw new NotFoundException(
                    $"Book reservation with ID '{id}' not found.");

            ValidateSchoolAccess(reservation.TenantId, userSchoolId, isSuperAdmin);

            if (reservation.IsFulfilled)
                throw new ValidationException(
                    "Cannot delete a fulfilled reservation. " +
                    "Archive it or contact an administrator.");

            _repositories.BookReservation.Delete(reservation);
            await _repositories.SaveAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<IEnumerable<BookReservation>> FetchByAccessLevel(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                return schoolId.HasValue
                    ? await _repositories.BookReservation
                        .GetBySchoolIdAsync(schoolId.Value, false)
                    : await _repositories.BookReservation.GetAllAsync(false);
            }

            if (!userSchoolId.HasValue)
                throw new UnauthorizedException(
                    "You must be assigned to a school to view reservations.");

            return await _repositories.BookReservation
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
                    "You must be assigned to a school to manage reservations.");

            return userSchoolId;
        }

        private void ValidateSchoolAccess(Guid entitySchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || entitySchoolId != userSchoolId.Value)
                throw new UnauthorizedException(
                    "You do not have access to this reservation.");
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school == null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private static BookReservationDto MapToDto(BookReservation r) => new()
        {
            Id = r.Id,
            SchoolId = r.TenantId,
            BookId = r.BookId,
            BookTitle = r.Book?.Title ?? string.Empty,
            MemberId = r.MemberId,
            MemberName = r.Member?.MemberNumber ?? r.MemberId.ToString(),
            ReservedOn = r.ReservedOn,
            IsFulfilled = r.IsFulfilled
        };
    }
}