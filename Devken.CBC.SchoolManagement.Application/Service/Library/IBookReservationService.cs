using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface IBookReservationService
    {
        Task<IEnumerable<BookReservationDto>> GetAllReservationsAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookReservationDto> GetReservationByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<BookReservationDto>> GetReservationsByBookAsync(
            Guid bookId, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<BookReservationDto>> GetReservationsByMemberAsync(
            Guid memberId, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookReservationDto> CreateReservationAsync(
            CreateBookReservationRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookReservationDto> UpdateReservationAsync(
            Guid id, UpdateBookReservationRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<BookReservationDto> FulfillReservationAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteReservationAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}