using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface ILibraryFineService
    {
        Task<LibraryFineDto> CreateFineAsync(CreateLibraryFineDto dto, Guid? userSchoolId);
        Task<LibraryFineDto> GetFineByIdAsync(Guid id, Guid? userSchoolId);
        Task<IEnumerable<LibraryFineDto>> GetAllFinesAsync(Guid? userSchoolId);
        Task<IEnumerable<LibraryFineDto>> GetUnpaidFinesAsync(Guid? userSchoolId);
        Task<IEnumerable<LibraryFineDto>> GetFinesByMemberIdAsync(Guid memberId, Guid? userSchoolId);
        Task<LibraryFineDto> PayFineAsync(PayFineDto dto, Guid? userSchoolId);
        Task<IEnumerable<LibraryFineDto>> PayMultipleFinesAsync(PayMultipleFinesDto dto, Guid? userSchoolId);
        Task WaiveFineAsync(WaiveFineDto dto, Guid? userSchoolId);
        Task DeleteFineAsync(Guid id, Guid? userSchoolId);
        Task<decimal> GetTotalUnpaidFinesForMemberAsync(Guid memberId, Guid? userSchoolId);
        Task<decimal> GetTotalPaidFinesForMemberAsync(Guid memberId, Guid? userSchoolId);
    }
}