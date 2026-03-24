using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookBorrowItemRepository : IRepositoryBase<BookBorrowItem, Guid>
    {
        Task<BookBorrowItem?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<BookBorrowItem>> GetByBorrowIdAsync(Guid borrowId);
        Task<IEnumerable<BookBorrowItem>> GetUnreturnedItemsAsync();
        Task<IEnumerable<BookBorrowItem>> GetOverdueItemsAsync();
        Task<BookBorrowItem?> GetByBookCopyIdAsync(Guid bookCopyId);
        Task<bool> IsBookCopyBorrowedAsync(Guid bookCopyId);
    }
}