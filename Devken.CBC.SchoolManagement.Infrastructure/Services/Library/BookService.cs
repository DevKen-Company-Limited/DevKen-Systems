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
    public class BookService : IBookService
    {
        private readonly IRepositoryManager _repositories;

        public BookService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ── GET ALL ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<BookDto>> GetAllBooksAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var books = await FetchBooksByAccessLevel(schoolId, userSchoolId, isSuperAdmin);
            return books.Select(MapToDto);
        }

        // ── GET BY CATEGORY ───────────────────────────────────────────────────

        public async Task<IEnumerable<BookDto>> GetBooksByCategoryAsync(
            Guid categoryId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            if (!isSuperAdmin && !userSchoolId.HasValue)
                throw new UnauthorizedException("You must be assigned to a school to view books.");

            var schoolId = isSuperAdmin ? (Guid?)null : userSchoolId!.Value;

            IEnumerable<Book> books;
            if (isSuperAdmin)
            {
                // SuperAdmin: all books in this category across all schools
                books = (await _repositories.Book.GetAllAsync(false))
                    .Where(b => b.CategoryId == categoryId);
            }
            else
            {
                books = await _repositories.Book
                    .GetByCategoryAsync(categoryId, userSchoolId!.Value, false);
            }

            return books.Select(MapToDto);
        }

        // ── GET BY AUTHOR ─────────────────────────────────────────────────────

        public async Task<IEnumerable<BookDto>> GetBooksByAuthorAsync(
            Guid authorId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            if (!isSuperAdmin && !userSchoolId.HasValue)
                throw new UnauthorizedException("You must be assigned to a school to view books.");

            IEnumerable<Book> books;
            if (isSuperAdmin)
            {
                books = (await _repositories.Book.GetAllAsync(false))
                    .Where(b => b.AuthorId == authorId);
            }
            else
            {
                books = await _repositories.Book
                    .GetByAuthorAsync(authorId, userSchoolId!.Value, false);
            }

            return books.Select(MapToDto);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────

        public async Task<BookDto> GetBookByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var book = await _repositories.Book.GetByIdWithDetailsAsync(id, false)
                ?? throw new NotFoundException($"Book with ID '{id}' not found.");

            ValidateSchoolAccess(book.TenantId, userSchoolId, isSuperAdmin);

            return MapToDto(book);
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<BookDto> CreateBookAsync(
            CreateBookRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var targetSchoolId = ResolveSchoolId(request.SchoolId, userSchoolId, isSuperAdmin, isRequired: true);

            await ValidateSchoolExistsAsync(targetSchoolId!.Value);
            await ValidateISBNUniqueAsync(request.ISBN, targetSchoolId.Value);
            await ValidateCategoryExistsAsync(request.CategoryId);
            await ValidateAuthorExistsAsync(request.AuthorId);
            await ValidatePublisherExistsAsync(request.PublisherId);

            var book = new Book
            {
                Id = Guid.NewGuid(),
                TenantId = targetSchoolId.Value,
                Title = request.Title.Trim(),
                ISBN = request.ISBN.Trim(),
                CategoryId = request.CategoryId,
                AuthorId = request.AuthorId,
                PublisherId = request.PublisherId,
                PublicationYear = request.PublicationYear,
                Language = request.Language?.Trim(),
                Description = request.Description?.Trim()
            };

            _repositories.Book.Create(book);
            await _repositories.SaveAsync();

            var created = await _repositories.Book.GetByIdWithDetailsAsync(book.Id, false);
            return MapToDto(created ?? book);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<BookDto> UpdateBookAsync(
            Guid id,
            UpdateBookRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var book = await _repositories.Book.GetByIdAsync(id, true)
                ?? throw new NotFoundException($"Book with ID '{id}' not found.");

            ValidateSchoolAccess(book.TenantId, userSchoolId, isSuperAdmin);

            // ISBN uniqueness check (exclude current book)
            var existingWithISBN = await _repositories.Book.GetByISBNAsync(request.ISBN.Trim(), book.TenantId);
            if (existingWithISBN != null && existingWithISBN.Id != id)
                throw new ConflictException($"A book with ISBN '{request.ISBN}' already exists in this school.");

            await ValidateCategoryExistsAsync(request.CategoryId);
            await ValidateAuthorExistsAsync(request.AuthorId);
            await ValidatePublisherExistsAsync(request.PublisherId);

            book.Title = request.Title.Trim();
            book.ISBN = request.ISBN.Trim();
            book.CategoryId = request.CategoryId;
            book.AuthorId = request.AuthorId;
            book.PublisherId = request.PublisherId;
            book.PublicationYear = request.PublicationYear;
            book.Language = request.Language?.Trim();
            book.Description = request.Description?.Trim();

            _repositories.Book.Update(book);
            await _repositories.SaveAsync();

            var updated = await _repositories.Book.GetByIdWithDetailsAsync(id, false);
            return MapToDto(updated ?? book);
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task DeleteBookAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var book = await _repositories.Book.GetByIdWithDetailsAsync(id, true)
                ?? throw new NotFoundException($"Book with ID '{id}' not found.");

            ValidateSchoolAccess(book.TenantId, userSchoolId, isSuperAdmin);

            if (book.Copies.Any())
                throw new ValidationException(
                    "Cannot delete a book that has copies. Please remove all copies first.");

            _repositories.Book.Delete(book);
            await _repositories.SaveAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<IEnumerable<Book>> FetchBooksByAccessLevel(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (schoolId.HasValue)
                    return await _repositories.Book.GetBySchoolIdAsync(schoolId.Value, false);

                return await _repositories.Book.GetAllAsync(false);
            }

            if (!userSchoolId.HasValue)
                throw new UnauthorizedException("You must be assigned to a school to view books.");

            return await _repositories.Book.GetBySchoolIdAsync(userSchoolId.Value, false);
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
                throw new UnauthorizedException("You must be assigned to a school to manage books.");

            return userSchoolId;
        }

        private void ValidateSchoolAccess(Guid entitySchoolId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (!userSchoolId.HasValue || entitySchoolId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this book.");
        }

        private async Task ValidateSchoolExistsAsync(Guid schoolId)
        {
            var school = await _repositories.School.GetByIdAsync(schoolId, false);
            if (school == null)
                throw new NotFoundException($"School with ID '{schoolId}' not found.");
        }

        private async Task ValidateISBNUniqueAsync(string isbn, Guid schoolId)
        {
            var existing = await _repositories.Book.GetByISBNAsync(isbn.Trim(), schoolId);
            if (existing != null)
                throw new ConflictException($"A book with ISBN '{isbn}' already exists in this school.");
        }

        private async Task ValidateCategoryExistsAsync(Guid categoryId)
        {
            var exists = await _repositories.BookCategory.ExistAsync(categoryId);
            if (!exists)
                throw new NotFoundException($"Book category with ID '{categoryId}' not found.");
        }

        private async Task ValidateAuthorExistsAsync(Guid authorId)
        {
            var exists = await _repositories.BookAuthor.ExistAsync(authorId);
            if (!exists)
                throw new NotFoundException($"Book author with ID '{authorId}' not found.");
        }

        private async Task ValidatePublisherExistsAsync(Guid publisherId)
        {
            var exists = await _repositories.BookPublisher.ExistAsync(publisherId);
            if (!exists)
                throw new NotFoundException($"Book publisher with ID '{publisherId}' not found.");
        }

        private static BookDto MapToDto(Book book)
        {
            return new BookDto
            {
                Id = book.Id,
                SchoolId = book.TenantId,
                SchoolName = string.Empty, // populate via School nav if added later
                Title = book.Title,
                ISBN = book.ISBN,
                CategoryId = book.CategoryId,
                CategoryName = book.Category?.Name ?? string.Empty,
                AuthorId = book.AuthorId,
                AuthorName = book.Author?.Name ?? string.Empty,
                PublisherId = book.PublisherId,
                PublisherName = book.Publisher?.Name ?? string.Empty,
                PublicationYear = book.PublicationYear,
                Language = book.Language,
                Description = book.Description,
                TotalCopies = book.Copies?.Count ?? 0,
                AvailableCopies = book.Copies?.Count(c => c.IsAvailable && !c.IsLost && !c.IsDamaged) ?? 0,
                Copies = book.Copies?.Select(c => new BookCopyDto
                {
                    Id = c.Id,
                    BookId = c.BookId,
                    LibraryBranchId = c.LibraryBranchId,
                    LibraryBranchName = c.LibraryBranch?.Name ?? string.Empty,
                    AccessionNumber = c.AccessionNumber,
                    Barcode = c.Barcode,
                    QRCode = c.QRCode,
                    Condition = c.Condition.ToString(),
                    IsAvailable = c.IsAvailable,
                    IsLost = c.IsLost,
                    IsDamaged = c.IsDamaged,
                    AcquiredOn = c.AcquiredOn
                }).ToList() ?? new List<BookCopyDto>()
            };
        }
    }
}