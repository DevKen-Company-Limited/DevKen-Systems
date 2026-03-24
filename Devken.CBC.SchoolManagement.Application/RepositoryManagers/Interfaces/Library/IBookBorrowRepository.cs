using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookBorrowRepository : IRepositoryBase<BookBorrow, Guid>
    {
        Task<BookBorrow?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<BookBorrow>> GetByMemberIdAsync(Guid memberId);
        Task<IEnumerable<BookBorrow>> GetOverdueBorrowsAsync();
        Task<IEnumerable<BookBorrow>> GetActiveBorrowsAsync();
        Task<IEnumerable<BookBorrow>> GetBorrowsByStatusAsync(BorrowStatus status);
        Task<bool> HasActiveBorrowsAsync(Guid memberId);
        Task<int> GetActiveBorrowCountAsync(Guid memberId);
    }
}