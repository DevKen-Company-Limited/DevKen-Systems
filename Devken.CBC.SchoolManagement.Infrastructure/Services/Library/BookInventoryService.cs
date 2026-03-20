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
    public class BookInventoryService : IBookInventoryService
    {
        private readonly IRepositoryManager _repositories;

        public BookInventoryService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ALL ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<BookInventoryDto>> GetAllInventoryAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            IEnumerable<BookInventory> records;

            if (isSuperAdmin)
            {
                records = schoolId.HasValue
                    ? await _repositories.BookInventory.GetBySchoolIdAsync(schoolId.Value, false)
                    : await _repositories.BookInventory.GetAllAsync(false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view inventory.");

                records = await _repositories.BookInventory.GetBySchoolIdAsync(userSchoolId.Value, false);
            }

            return records.Select(MapToDto);
        }

        // ── GET BY BOOK ───────────────────────────────────────────────────────

        public async Task<BookInventoryDto> GetInventoryByBookAsync(
            Guid bookId, Guid? userSchoolId, bool isSuperAdmin)
        {
            var inventory = await _repositories.BookInventory.GetByBookIdAsync(bookId, false)
                ?? throw new NotFoundException($"Inventory record for book ID '{bookId}' not found.");

            ValidateSchoolAccess(inventory.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(inventory);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        public async Task<BookInventoryDto> GetInventoryByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var inventory = await _repositories.BookInventory.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Inventory record with ID '{id}' not found.");

            ValidateSchoolAccess(inventory.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(inventory);
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<BookInventoryDto> CreateInventoryAsync(
            CreateBookInventoryRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);
            await ValidateBookBelongsToSchoolAsync(request.BookId, targetSchoolId.Value);

            if (await _repositories.BookInventory.ExistsByBookIdAsync(request.BookId))
                throw new ConflictException(
                    "An inventory record already exists for this book. Use update or recalculate instead.");

            ValidateInventoryCounts(request.TotalCopies, request.AvailableCopies,
                request.BorrowedCopies, request.LostCopies, request.DamagedCopies);

            var inventory = new BookInventory
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId.Value,
                BookId = request.BookId,
                TotalCopies = request.TotalCopies,
                AvailableCopies = request.AvailableCopies,
                BorrowedCopies = request.BorrowedCopies,
                LostCopies = request.LostCopies,
                DamagedCopies = request.DamagedCopies,
            };

            _repositories.BookInventory.Create(inventory);
            await _repositories.SaveAsync();

            var created = await _repositories.BookInventory.GetByIdWithDetailsAsync(inventory.Id, false);
            return MapToDto(created ?? inventory);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<BookInventoryDto> UpdateInventoryAsync(
            Guid id, UpdateBookInventoryRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            var inventory = await _repositories.BookInventory.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Inventory record with ID '{id}' not found.");

            ValidateSchoolAccess(inventory.TenantId, userSchoolId, isSuperAdmin);

            ValidateInventoryCounts(request.TotalCopies, request.AvailableCopies,
                request.BorrowedCopies, request.LostCopies, request.DamagedCopies);

            inventory.TotalCopies = request.TotalCopies;
            inventory.AvailableCopies = request.AvailableCopies;
            inventory.BorrowedCopies = request.BorrowedCopies;
            inventory.LostCopies = request.LostCopies;
            inventory.DamagedCopies = request.DamagedCopies;

            _repositories.BookInventory.Update(inventory);
            await _repositories.SaveAsync();

            var updated = await _repositories.BookInventory.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? inventory);
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task DeleteInventoryAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var inventory = await _repositories.BookInventory.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Inventory record with ID '{id}' not found.");

            ValidateSchoolAccess(inventory.TenantId, userSchoolId, isSuperAdmin);

            _repositories.BookInventory.Delete(inventory);
            await _repositories.SaveAsync();
        }

        // ── RECALCULATE ───────────────────────────────────────────────────────

        public async Task<BookInventoryDto> RecalculateAsync(
            Guid bookId, Guid? userSchoolId, bool isSuperAdmin)
        {
            var book = await _repositories.Book.GetByIdAsync(bookId, false)
                ?? throw new NotFoundException($"Book with ID '{bookId}' not found.");

            ValidateSchoolAccess(book.TenantId, userSchoolId, isSuperAdmin);

            var total = await _repositories.BookCopy.CountByBookIdAsync(bookId);
            var available = await _repositories.BookCopy.CountAvailableByBookIdAsync(bookId);
            var lost = await _repositories.BookCopy.CountLostByBookIdAsync(bookId);
            var damaged = await _repositories.BookCopy.CountDamagedByBookIdAsync(bookId);
            var borrowed = Math.Max(0, total - available - lost - damaged);

            var inventory = await _repositories.BookInventory.GetByBookIdAsync(bookId, true);

            if (inventory == null)
            {
                inventory = new BookInventory
                {
                    Id = Guid.NewGuid(),
                    TenantId = book.TenantId,
                    BookId = bookId,
                    TotalCopies = total,
                    AvailableCopies = available,
                    BorrowedCopies = borrowed,
                    LostCopies = lost,
                    DamagedCopies = damaged,
                };
                _repositories.BookInventory.Create(inventory);
            }
            else
            {
                inventory.TotalCopies = total;
                inventory.AvailableCopies = available;
                inventory.BorrowedCopies = borrowed;
                inventory.LostCopies = lost;
                inventory.DamagedCopies = damaged;
                _repositories.BookInventory.Update(inventory);
            }

            await _repositories.SaveAsync();

            var result = await _repositories.BookInventory.GetByIdWithDetailsAsync(inventory.Id, false);
            return MapToDto(result ?? inventory);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private Guid? ResolveSchoolId(Guid? requestSchoolId, Guid? userSchoolId, bool isSuperAdmin, bool isRequired)
        {
            if (isSuperAdmin)
            {
                if (isRequired && (!requestSchoolId.HasValue || requestSchoolId.Value == Guid.Empty))
                    throw new ValidationException("SchoolId is required for SuperAdmin.");
                return requestSchoolId;
            }

            if (!userSchoolId.HasValue || userSchoolId.Value == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to manage inventory.");

            return userSchoolId;
        }

        private void ValidateSchoolAccess(Guid entitySchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || entitySchoolId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this inventory record.");
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            if (!await _repositories.School.ExistAsync(schoolId))
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private async Task ValidateBookBelongsToSchoolAsync(Guid bookId, Guid schoolId)
        {
            var book = await _repositories.Book.GetByIdAsync(bookId, false)
                ?? throw new NotFoundException($"Book with ID '{bookId}' not found.");
            if (book.TenantId != schoolId)
                throw new ValidationException("The book does not belong to the specified school.");
        }

        private static void ValidateInventoryCounts(
            int total, int available, int borrowed, int lost, int damaged)
        {
            if (available + borrowed + lost + damaged > total)
                throw new ValidationException(
                    "The sum of available, borrowed, lost and damaged copies cannot exceed total copies.");
        }

        private static BookInventoryDto MapToDto(BookInventory i)
        {
            var availability = i.TotalCopies > 0
                ? Math.Round((double)i.AvailableCopies / i.TotalCopies * 100, 1)
                : 0.0;

            return new BookInventoryDto
            {
                Id = i.Id,
                SchoolId = i.TenantId,
                BookId = i.BookId,
                BookTitle = i.Book?.Title ?? string.Empty,
                BookISBN = i.Book?.ISBN ?? string.Empty,
                AuthorName = i.Book?.Author?.Name ?? string.Empty,
                CategoryName = i.Book?.Category?.Name ?? string.Empty,
                TotalCopies = i.TotalCopies,
                AvailableCopies = i.AvailableCopies,
                BorrowedCopies = i.BorrowedCopies,
                LostCopies = i.LostCopies,
                DamagedCopies = i.DamagedCopies,
                AvailabilityPercentage = availability,
            };
        }
    }
}