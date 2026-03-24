using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Library;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Library
{
    public class BookBorrowService : IBookBorrowService
    {
        private readonly IRepositoryManager _repository;

        public BookBorrowService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task<BookBorrowDto> CreateBorrowAsync(CreateBookBorrowDto dto, Guid? userSchoolId)
        {
            // Validate member exists and is active
            var member = await _repository.LibraryMember.GetByIdAsync(dto.MemberId);
            if (member == null)
                throw new KeyNotFoundException("Library member not found");

            if (userSchoolId.HasValue && member.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this library member");

            if (!member.IsActive)
                throw new InvalidOperationException("Library member is not active");

            // Validate all book copies exist and are available
            var bookCopies = new List<BookCopy>();
            foreach (var copyId in dto.BookCopyIds)
            {
                var copy = await _repository.BookCopy.GetByIdAsync(copyId);
                if (copy == null)
                    throw new KeyNotFoundException($"Book copy {copyId} not found");

                if (userSchoolId.HasValue && copy.TenantId != userSchoolId.Value)
                    throw new UnauthorizedAccessException($"Access denied to book copy {copyId}");

                if (!copy.IsAvailable)
                    throw new InvalidOperationException($"Book copy {copy.AccessionNumber} is not available");

                if (copy.IsLost || copy.IsDamaged)
                    throw new InvalidOperationException($"Book copy {copy.AccessionNumber} is marked as lost or damaged");

                bookCopies.Add(copy);
            }

            // Create borrow transaction
            var borrow = new BookBorrow
            {
                Id = Guid.NewGuid(),
                MemberId = dto.MemberId,
                BorrowDate = dto.BorrowDate,
                DueDate = dto.DueDate,
                BStatus = BorrowStatus.Borrowed,
                TenantId = member.TenantId
            };

            _repository.BookBorrow.Create(borrow);

            // Create borrow items and mark copies as unavailable
            foreach (var copy in bookCopies)
            {
                var borrowItem = new BookBorrowItem
                {
                    Id = Guid.NewGuid(),
                    BorrowId = borrow.Id,
                    BookCopyId = copy.Id,
                    IsOverdue = false,
                    TenantId = member.TenantId
                };

                _repository.BookBorrowItem.Create(borrowItem);

                // Mark book copy as unavailable
                copy.IsAvailable = false;
                _repository.BookCopy.Update(copy);
            }

            await _repository.SaveAsync();

            return await GetBorrowByIdAsync(borrow.Id, userSchoolId);
        }

        public async Task<BookBorrowDto> GetBorrowByIdAsync(Guid id, Guid? userSchoolId)
        {
            var borrow = await _repository.BookBorrow.GetByIdWithDetailsAsync(id);
            if (borrow == null)
                throw new KeyNotFoundException("Borrow transaction not found");

            if (userSchoolId.HasValue && borrow.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this borrow transaction");

            return MapToDto(borrow);
        }

        public async Task<IEnumerable<BookBorrowDto>> GetAllBorrowsAsync(Guid? userSchoolId)
        {
            var query = _repository.BookBorrow.FindAll(trackChanges: false);

            if (userSchoolId.HasValue)
                query = query.Where(b => b.TenantId == userSchoolId.Value);

            var borrows = await query
                .Include(b => b.Member)
                .Include(b => b.Items)
                    .ThenInclude(i => i.BookCopy)
                        .ThenInclude(bc => bc.Book)
                .Include(b => b.Items)
                    .ThenInclude(i => i.Fines)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return borrows.Select(MapToDto);
        }

        public async Task<IEnumerable<BookBorrowDto>> GetBorrowsByMemberIdAsync(Guid memberId, Guid? userSchoolId)
        {
            var borrows = await _repository.BookBorrow.GetByMemberIdAsync(memberId);

            if (userSchoolId.HasValue)
                borrows = borrows.Where(b => b.TenantId == userSchoolId.Value);

            return borrows.Select(MapToDto);
        }

        public async Task<IEnumerable<BookBorrowDto>> GetActiveBorrowsAsync(Guid? userSchoolId)
        {
            var borrows = await _repository.BookBorrow.GetActiveBorrowsAsync();

            if (userSchoolId.HasValue)
                borrows = borrows.Where(b => b.TenantId == userSchoolId.Value);

            return borrows.Select(MapToDto);
        }

        public async Task<IEnumerable<BookBorrowDto>> GetOverdueBorrowsAsync(Guid? userSchoolId)
        {
            var borrows = await _repository.BookBorrow.GetOverdueBorrowsAsync();

            if (userSchoolId.HasValue)
                borrows = borrows.Where(b => b.TenantId == userSchoolId.Value);

            return borrows.Select(MapToDto);
        }

        public async Task<BookBorrowDto> UpdateBorrowAsync(Guid id, UpdateBookBorrowDto dto, Guid? userSchoolId)
        {
            var borrow = await _repository.BookBorrow.GetByIdAsync(id);
            if (borrow == null)
                throw new KeyNotFoundException("Borrow transaction not found");

            if (userSchoolId.HasValue && borrow.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this borrow transaction");

            if (dto.DueDate.HasValue)
            {
                if (dto.DueDate.Value < borrow.BorrowDate)
                    throw new InvalidOperationException("Due date cannot be before borrow date");

                borrow.DueDate = dto.DueDate.Value;
            }

            _repository.BookBorrow.Update(borrow);
            await _repository.SaveAsync();

            return await GetBorrowByIdAsync(id, userSchoolId);
        }

        public async Task<BookBorrowItemDto> ReturnBookAsync(ReturnBookDto dto, Guid? userSchoolId)
        {
            var borrowItem = await _repository.BookBorrowItem.GetByIdWithDetailsAsync(dto.BorrowItemId);
            if (borrowItem == null)
                throw new KeyNotFoundException("Borrow item not found");

            if (userSchoolId.HasValue && borrowItem.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this borrow item");

            if (borrowItem.IsReturned)
                throw new InvalidOperationException("Book has already been returned");

            // Set return date
            borrowItem.ReturnedOn = dto.ReturnDate ?? DateTime.UtcNow;

            // Mark book copy as available
            var bookCopy = await _repository.BookCopy.GetByIdAsync(borrowItem.BookCopyId);
            if (bookCopy != null)
            {
                bookCopy.IsAvailable = true;
                _repository.BookCopy.Update(bookCopy);
            }

            _repository.BookBorrowItem.Update(borrowItem);

            // Update borrow status if all items are returned
            var allItems = await _repository.BookBorrowItem.GetByBorrowIdAsync(borrowItem.BorrowId);
            if (allItems.All(i => i.Id == borrowItem.Id || i.IsReturned))
            {
                var borrow = await _repository.BookBorrow.GetByIdAsync(borrowItem.BorrowId);
                if (borrow != null)
                {
                    borrow.BStatus = BorrowStatus.Returned;
                    _repository.BookBorrow.Update(borrow);
                }
            }

            await _repository.SaveAsync();

            borrowItem = await _repository.BookBorrowItem.GetByIdWithDetailsAsync(dto.BorrowItemId);
            return MapItemToDto(borrowItem!);
        }

        public async Task<IEnumerable<BookBorrowItemDto>> ReturnMultipleBooksAsync(ReturnMultipleBooksDto dto, Guid? userSchoolId)
        {
            var returnedItems = new List<BookBorrowItemDto>();

            foreach (var itemId in dto.BorrowItemIds)
            {
                var returnDto = new ReturnBookDto
                {
                    BorrowItemId = itemId,
                    ReturnDate = dto.ReturnDate
                };

                var returnedItem = await ReturnBookAsync(returnDto, userSchoolId);
                returnedItems.Add(returnedItem);
            }

            return returnedItems;
        }

        public async Task DeleteBorrowAsync(Guid id, Guid? userSchoolId)
        {
            var borrow = await _repository.BookBorrow.GetByIdWithDetailsAsync(id);
            if (borrow == null)
                throw new KeyNotFoundException("Borrow transaction not found");

            if (userSchoolId.HasValue && borrow.TenantId != userSchoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this borrow transaction");

            // Can only delete if all items are returned
            if (borrow.Items.Any(i => !i.IsReturned))
                throw new InvalidOperationException("Cannot delete borrow with unreturned items");

            // Delete all borrow items and their fines
            foreach (var item in borrow.Items)
            {
                foreach (var fine in item.Fines)
                {
                    _repository.LibraryFine.Delete(fine);
                }
                _repository.BookBorrowItem.Delete(item);
            }

            _repository.BookBorrow.Delete(borrow);
            await _repository.SaveAsync();
        }

        public async Task<bool> CanMemberBorrowAsync(Guid memberId, Guid? userSchoolId)
        {
            var member = await _repository.LibraryMember.GetByIdAsync(memberId);
            if (member == null)
                return false;

            if (userSchoolId.HasValue && member.TenantId != userSchoolId.Value)
                return false;

            if (!member.IsActive)
                return false;

            // Check for unpaid fines
            var unpaidFines = await _repository.LibraryFine.GetTotalUnpaidFinesForMemberAsync(memberId);
            if (unpaidFines > 0)
                return false;

            // Check for overdue books
            var hasOverdue = await _repository.BookBorrow
                .FindByCondition(
                    b => b.MemberId == memberId && b.BStatus == BorrowStatus.Overdue,
                    trackChanges: false)
                .AnyAsync();

            return !hasOverdue;
        }

        public async Task<int> GetActiveBorrowCountAsync(Guid memberId, Guid? userSchoolId)
        {
            return await _repository.BookBorrow.GetActiveBorrowCountAsync(memberId);
        }

        public async Task ProcessOverdueItemsAsync(Guid? userSchoolId)
        {
            var today = DateTime.UtcNow.Date;

            var query = _repository.BookBorrowItem.FindByCondition(
                bi => bi.ReturnedOn == null && bi.Borrow.DueDate < today,
                trackChanges: true);

            if (userSchoolId.HasValue)
                query = query.Where(bi => bi.TenantId == userSchoolId.Value);

            var overdueItems = await query.ToListAsync();

            foreach (var item in overdueItems)
            {
                if (!item.IsOverdue)
                {
                    item.IsOverdue = true;
                    _repository.BookBorrowItem.Update(item);
                }

                // Update borrow status
                var borrow = await _repository.BookBorrow.GetByIdAsync(item.BorrowId);
                if (borrow != null && borrow.BStatus == BorrowStatus.Borrowed)
                {
                    borrow.BStatus = BorrowStatus.Overdue;
                    _repository.BookBorrow.Update(borrow);
                }
            }

            await _repository.SaveAsync();
        }

        private BookBorrowDto MapToDto(BookBorrow borrow)
        {
            var totalFines = borrow.Items.SelectMany(i => i.Fines).Sum(f => f.Amount);
            var today = DateTime.UtcNow.Date;

            return new BookBorrowDto
            {
                Id = borrow.Id,
                MemberId = borrow.MemberId,
                MemberName = borrow.Member?.MemberNumber ?? "Unknown",
                MemberNumber = borrow.Member?.MemberNumber ?? "Unknown",
                BorrowDate = borrow.BorrowDate,
                DueDate = borrow.DueDate,
                BorrowStatus = borrow.BStatus.ToString(),
                IsOverdue = borrow.DueDate < today && borrow.BStatus == BorrowStatus.Borrowed,
                TotalItems = borrow.Items.Count,
                ReturnedItems = borrow.Items.Count(i => i.IsReturned),
                UnreturnedItems = borrow.Items.Count(i => !i.IsReturned),
                TotalFines = totalFines,
                Items = borrow.Items.Select(MapItemToDto).ToList()
            };
        }

        private BookBorrowItemDto MapItemToDto(BookBorrowItem item)
        {
            var today = DateTime.UtcNow.Date;
            var daysOverdue = item.Borrow.DueDate < today && !item.IsReturned
                ? (today - item.Borrow.DueDate).Days
                : 0;

            return new BookBorrowItemDto
            {
                Id = item.Id,
                BorrowId = item.BorrowId,
                BookCopyId = item.BookCopyId,
                BookTitle = item.BookCopy?.Book?.Title ?? "Unknown",
                ISBN = item.BookCopy?.Book?.ISBN ?? "Unknown",
                AccessionNumber = item.BookCopy?.AccessionNumber ?? "Unknown",
                Barcode = item.BookCopy?.Barcode ?? "Unknown",
                ReturnedOn = item.ReturnedOn,
                IsReturned = item.IsReturned,
                IsOverdue = item.IsOverdue,
                DaysOverdue = daysOverdue,
                Fines = item.Fines.Select(f => new LibraryFineDto
                {
                    Id = f.Id,
                    BorrowItemId = f.BorrowItemId,
                    Amount = f.Amount,
                    IsPaid = f.IsPaid,
                    IssuedOn = f.IssuedOn,
                    PaidOn = f.PaidOn,
                    Reason = f.Reason
                }).ToList()
            };
        }
    }
}