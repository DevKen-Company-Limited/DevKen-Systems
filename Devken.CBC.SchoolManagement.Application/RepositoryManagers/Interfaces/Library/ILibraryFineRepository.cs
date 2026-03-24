using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface ILibraryFineRepository : IRepositoryBase<LibraryFine, Guid>
    {
        Task<LibraryFine?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<LibraryFine>> GetByBorrowItemIdAsync(Guid borrowItemId);
        Task<IEnumerable<LibraryFine>> GetUnpaidFinesAsync();
        Task<IEnumerable<LibraryFine>> GetFinesByMemberIdAsync(Guid memberId);
        Task<decimal> GetTotalUnpaidFinesForMemberAsync(Guid memberId);
        Task<decimal> GetTotalPaidFinesForMemberAsync(Guid memberId);
    }
}