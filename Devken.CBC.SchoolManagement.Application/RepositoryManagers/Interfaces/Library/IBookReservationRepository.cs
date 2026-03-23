using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface IBookReservationRepository : IRepositoryBase<BookReservation, Guid>
    {
        /// <summary>Get all reservations with related Book and Member data.</summary>
        Task<IEnumerable<BookReservation>> GetAllAsync(bool trackChanges);

        /// <summary>Get all reservations for a specific school/tenant.</summary>
        Task<IEnumerable<BookReservation>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);

        /// <summary>Get reservations for a specific book.</summary>
        Task<IEnumerable<BookReservation>> GetByBookIdAsync(Guid bookId, bool trackChanges);

        /// <summary>Get reservations for a specific member.</summary>
        Task<IEnumerable<BookReservation>> GetByMemberIdAsync(Guid memberId, bool trackChanges);

        /// <summary>Get a reservation by ID with full Book and Member details included.</summary>
        Task<BookReservation?> GetByIdWithDetailsAsync(Guid id, bool trackChanges);

        /// <summary>
        /// Check whether a member already has an unfulfilled reservation for the same book.
        /// Used for duplicate-reservation guard.
        /// </summary>
        Task<BookReservation?> GetPendingReservationAsync(Guid bookId, Guid memberId);
    }
}