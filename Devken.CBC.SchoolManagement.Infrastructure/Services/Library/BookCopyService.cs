using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class BookCopyService : IBookCopyService
    {
        private readonly IRepositoryManager _repositories;

        public BookCopyService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ALL ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<BookCopyDto>> GetAllCopiesAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            IEnumerable<BookCopy> copies;

            if (isSuperAdmin)
            {
                copies = schoolId.HasValue
                    ? await _repositories.BookCopy.GetBySchoolIdAsync(schoolId.Value, false)
                    : await _repositories.BookCopy.GetAllAsync(false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view book copies.");

                copies = await _repositories.BookCopy.GetBySchoolIdAsync(userSchoolId.Value, false);
            }

            return copies.Select(MapToDto);
        }

        // ── GET BY BOOK ───────────────────────────────────────────────────────

        public async Task<IEnumerable<BookCopyDto>> GetCopiesByBookAsync(
            Guid bookId, Guid? userSchoolId, bool isSuperAdmin)
        {
            var book = await _repositories.Book.GetByIdAsync(bookId, false)
                ?? throw new NotFoundException($"Book with ID '{bookId}' not found.");

            ValidateSchoolAccess(book.TenantId, userSchoolId, isSuperAdmin);

            var copies = await _repositories.BookCopy.GetByBookIdAsync(bookId, false);
            return copies.Select(MapToDto);
        }

        // ── GET BY BRANCH ─────────────────────────────────────────────────────

        public async Task<IEnumerable<BookCopyDto>> GetCopiesByBranchAsync(
            Guid branchId, Guid? userSchoolId, bool isSuperAdmin)
        {
            var branch = await _repositories.LibraryBranch.GetByIdAsync(branchId, false)
                ?? throw new NotFoundException($"Library branch with ID '{branchId}' not found.");

            ValidateSchoolAccess(branch.TenantId, userSchoolId, isSuperAdmin);

            var copies = await _repositories.BookCopy.GetByBranchIdAsync(branchId, false);
            return copies.Select(MapToDto);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        public async Task<BookCopyDto> GetCopyByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var copy = await _repositories.BookCopy.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Book copy with ID '{id}' not found.");

            ValidateSchoolAccess(copy.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(copy);
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<BookCopyDto> CreateCopyAsync(
     CreateBookCopyRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);
            await ValidateBookBelongsToSchoolAsync(request.BookId, targetSchoolId.Value);
            await ValidateBranchBelongsToSchoolAsync(request.LibraryBranchId, targetSchoolId.Value);

            // ── AccessionNumber ───────────────────────────────────────────────────────
            string accessionNumber;
            if (string.IsNullOrWhiteSpace(request.AccessionNumber))
            {
                await EnsureSeriesExistsAsync("BookAccessionNumber", "ACC", targetSchoolId.Value);
                accessionNumber = await _repositories.DocumentNumberSeries
                    .GenerateAsync("BookAccessionNumber", targetSchoolId.Value);
            }
            else
            {
                accessionNumber = request.AccessionNumber.Trim();
                await ValidateAccessionNumberUniqueAsync(accessionNumber, targetSchoolId.Value);
            }

            // ── Barcode ───────────────────────────────────────────────────────────────
            string barcode;
            if (string.IsNullOrWhiteSpace(request.Barcode))
            {
                await EnsureSeriesExistsAsync("BookBarcode", "BAR", targetSchoolId.Value);
                barcode = await _repositories.DocumentNumberSeries
                    .GenerateAsync("BookBarcode", targetSchoolId.Value);
            }
            else
            {
                barcode = request.Barcode.Trim();
                await ValidateBarcodeUniqueAsync(barcode, targetSchoolId.Value);
            }

            // ── QR Code — derive from accession number if not provided ────────────────
            var qrCode = string.IsNullOrWhiteSpace(request.QRCode)
                ? accessionNumber
                : request.QRCode.Trim();

            if (!Enum.TryParse<BookCondition>(request.Condition, true, out var condition))
                throw new ValidationException($"Invalid book condition: '{request.Condition}'.");

            var copy = new BookCopy
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId.Value,
                BookId = request.BookId,
                LibraryBranchId = request.LibraryBranchId,
                AccessionNumber = accessionNumber,
                Barcode = barcode,
                QRCode = qrCode,
                Condition = condition,
                IsAvailable = request.IsAvailable,
                IsLost = request.IsLost,
                IsDamaged = request.IsDamaged,
                AcquiredOn = request.AcquiredOn,
            };

            _repositories.BookCopy.Create(copy);
            await _repositories.SaveAsync();

            await SyncInventoryAsync(request.BookId, targetSchoolId.Value);

            var created = await _repositories.BookCopy.GetByIdWithDetailsAsync(copy.Id, false);
            return MapToDto(created ?? copy);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<BookCopyDto> UpdateCopyAsync(
            Guid id, UpdateBookCopyRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            var copy = await _repositories.BookCopy.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Book copy with ID '{id}' not found.");

            ValidateSchoolAccess(copy.TenantId, userSchoolId, isSuperAdmin);

            await ValidateBranchBelongsToSchoolAsync(request.LibraryBranchId, copy.TenantId);

            // Accession uniqueness excluding self
            var existingAccession = await _repositories.BookCopy
                .GetByAccessionNumberAsync(request.AccessionNumber.Trim(), copy.TenantId);
            if (existingAccession != null && existingAccession.Id != id)
                throw new ConflictException($"Accession number '{request.AccessionNumber}' already exists.");

            // Barcode uniqueness excluding self
            var existingBarcode = await _repositories.BookCopy
                .GetByBarcodeAsync(request.Barcode.Trim(), copy.TenantId);
            if (existingBarcode != null && existingBarcode.Id != id)
                throw new ConflictException($"Barcode '{request.Barcode}' already exists.");

            if (!Enum.TryParse<BookCondition>(request.Condition, true, out var condition))
                throw new ValidationException($"Invalid book condition: '{request.Condition}'.");

            copy.LibraryBranchId = request.LibraryBranchId;
            copy.AccessionNumber = request.AccessionNumber.Trim();
            copy.Barcode = request.Barcode.Trim();
            copy.QRCode = request.QRCode?.Trim();
            copy.Condition = condition;
            copy.IsAvailable = request.IsAvailable;
            copy.IsLost = request.IsLost;
            copy.IsDamaged = request.IsDamaged;
            copy.AcquiredOn = request.AcquiredOn;

            _repositories.BookCopy.Update(copy);
            await _repositories.SaveAsync();

            await SyncInventoryAsync(copy.BookId, copy.TenantId);

            var updated = await _repositories.BookCopy.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? copy);
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task DeleteCopyAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var copy = await _repositories.BookCopy.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Book copy with ID '{id}' not found.");

            ValidateSchoolAccess(copy.TenantId, userSchoolId, isSuperAdmin);

            if (!copy.IsAvailable)
                throw new ValidationException("Cannot delete a copy that is currently borrowed.");

            var bookId = copy.BookId;
            var schoolId = copy.TenantId;

            _repositories.BookCopy.Delete(copy);
            await _repositories.SaveAsync();

            await SyncInventoryAsync(bookId, schoolId);
        }

        // ── MARK AS LOST ──────────────────────────────────────────────────────

        public async Task<BookCopyDto> MarkAsLostAsync(
            Guid id, string? remarks, Guid? userSchoolId, bool isSuperAdmin)
        {
            var copy = await _repositories.BookCopy.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Book copy with ID '{id}' not found.");

            ValidateSchoolAccess(copy.TenantId, userSchoolId, isSuperAdmin);

            if (copy.IsLost)
                throw new ValidationException("This copy is already marked as lost.");

            copy.IsLost = true;
            copy.IsAvailable = false;

            _repositories.BookCopy.Update(copy);
            await _repositories.SaveAsync();

            await SyncInventoryAsync(copy.BookId, copy.TenantId);

            var updated = await _repositories.BookCopy.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? copy);
        }

        // ── MARK AS DAMAGED ───────────────────────────────────────────────────

        public async Task<BookCopyDto> MarkAsDamagedAsync(
            Guid id, string? remarks, Guid? userSchoolId, bool isSuperAdmin)
        {
            var copy = await _repositories.BookCopy.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Book copy with ID '{id}' not found.");

            ValidateSchoolAccess(copy.TenantId, userSchoolId, isSuperAdmin);

            if (copy.IsDamaged)
                throw new ValidationException("This copy is already marked as damaged.");

            copy.IsDamaged = true;
            copy.IsAvailable = false;

            _repositories.BookCopy.Update(copy);
            await _repositories.SaveAsync();

            await SyncInventoryAsync(copy.BookId, copy.TenantId);

            var updated = await _repositories.BookCopy.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? copy);
        }

        // ── MARK AS AVAILABLE ─────────────────────────────────────────────────

        public async Task<BookCopyDto> MarkAsAvailableAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var copy = await _repositories.BookCopy.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Book copy with ID '{id}' not found.");

            ValidateSchoolAccess(copy.TenantId, userSchoolId, isSuperAdmin);

            if (copy.IsAvailable && !copy.IsLost && !copy.IsDamaged)
                throw new ValidationException("This copy is already available.");

            copy.IsAvailable = true;
            copy.IsLost = false;
            copy.IsDamaged = false;

            _repositories.BookCopy.Update(copy);
            await _repositories.SaveAsync();

            await SyncInventoryAsync(copy.BookId, copy.TenantId);

            var updated = await _repositories.BookCopy.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? copy);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────
        /// <summary>
        /// Ensures a DocumentNumberSeries exists for the given entity + tenant.
        /// If missing, auto-creates it with sensible library defaults so the
        /// admin does not need to configure it manually before adding the first copy.
        /// </summary>
        private async Task EnsureSeriesExistsAsync(
            string entityName,
            string defaultPrefix,
            Guid schoolId)
        {
            var exists = await _repositories.DocumentNumberSeries
                .SeriesExistsAsync(entityName, schoolId);

            if (exists) return;

            // Auto-seed with sensible defaults
            await _repositories.DocumentNumberSeries.CreateSeriesAsync(
                entityName: entityName,
                tenantId: schoolId,
                prefix: defaultPrefix,
                padding: 5,
                resetEveryYear: false,
                description: $"Auto-created default series for {entityName}");
        }

        /// <summary>
        /// Keeps BookInventory in sync after any BookCopy mutation.
        /// Creates the inventory record if it does not yet exist.
        /// </summary>
        private async Task SyncInventoryAsync(Guid bookId, Guid schoolId)
        {
            var total = await _repositories.BookCopy.CountByBookIdAsync(bookId);
            var available = await _repositories.BookCopy.CountAvailableByBookIdAsync(bookId);
            var lost = await _repositories.BookCopy.CountLostByBookIdAsync(bookId);
            var damaged = await _repositories.BookCopy.CountDamagedByBookIdAsync(bookId);
            var borrowed = total - available - lost - damaged;

            var inventory = await _repositories.BookInventory.GetByBookIdAsync(bookId, true);

            if (inventory == null)
            {
                inventory = new BookInventory
                {
                    Id = Guid.NewGuid(),
                    TenantId = schoolId,
                    BookId = bookId,
                    TotalCopies = total,
                    AvailableCopies = available,
                    BorrowedCopies = borrowed < 0 ? 0 : borrowed,
                    LostCopies = lost,
                    DamagedCopies = damaged,
                };
                _repositories.BookInventory.Create(inventory);
            }
            else
            {
                inventory.TotalCopies = total;
                inventory.AvailableCopies = available;
                inventory.BorrowedCopies = borrowed < 0 ? 0 : borrowed;
                inventory.LostCopies = lost;
                inventory.DamagedCopies = damaged;
                _repositories.BookInventory.Update(inventory);
            }

            await _repositories.SaveAsync();
        }

        private Guid? ResolveSchoolId(Guid? requestSchoolId, Guid? userSchoolId, bool isSuperAdmin, bool isRequired)
        {
            if (isSuperAdmin)
            {
                if (isRequired && (!requestSchoolId.HasValue || requestSchoolId.Value == Guid.Empty))
                    throw new ValidationException("SchoolId is required for SuperAdmin.");
                return requestSchoolId;
            }

            if (!userSchoolId.HasValue || userSchoolId.Value == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to manage book copies.");

            return userSchoolId;
        }

        private void ValidateSchoolAccess(Guid entitySchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || entitySchoolId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this book copy.");
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

        private async Task ValidateBranchBelongsToSchoolAsync(Guid branchId, Guid schoolId)
        {
            var branch = await _repositories.LibraryBranch.GetByIdAsync(branchId, false)
                ?? throw new NotFoundException($"Library branch with ID '{branchId}' not found.");
            if (branch.TenantId != schoolId)
                throw new ValidationException("The library branch does not belong to the specified school.");
        }

        private async Task ValidateAccessionNumberUniqueAsync(string accessionNumber, Guid schoolId)
        {
            var existing = await _repositories.BookCopy.GetByAccessionNumberAsync(accessionNumber, schoolId);
            if (existing != null)
                throw new ConflictException($"Accession number '{accessionNumber}' already exists in this school.");
        }

        private async Task ValidateBarcodeUniqueAsync(string barcode, Guid schoolId)
        {
            var existing = await _repositories.BookCopy.GetByBarcodeAsync(barcode, schoolId);
            if (existing != null)
                throw new ConflictException($"Barcode '{barcode}' already exists in this school.");
        }

        private static BookCopyDto MapToDto(BookCopy copy)
        {
            string status;
            if (copy.IsLost) status = "Lost";
            else if (copy.IsDamaged) status = "Damaged";
            else if (copy.IsAvailable) status = "Available";
            else status = "Borrowed";

            return new BookCopyDto
            {
                Id = copy.Id,
                SchoolId = copy.TenantId,
                BookId = copy.BookId,
                BookTitle = copy.Book?.Title ?? string.Empty,
                BookISBN = copy.Book?.ISBN ?? string.Empty,
                LibraryBranchId = copy.LibraryBranchId,
                LibraryBranchName = copy.LibraryBranch?.Name ?? string.Empty,
                AccessionNumber = copy.AccessionNumber,
                Barcode = copy.Barcode,
                QRCode = copy.QRCode,
                Condition = copy.Condition.ToString(),
                IsAvailable = copy.IsAvailable,
                IsLost = copy.IsLost,
                IsDamaged = copy.IsDamaged,
                AcquiredOn = copy.AcquiredOn,   // DateTime? → DateTime?, direct assignment
                Status = status,
            };
        }
    }
}