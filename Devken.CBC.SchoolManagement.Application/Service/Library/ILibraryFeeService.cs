using Devken.CBC.SchoolManagement.Application.DTOs.Library;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface ILibraryFeeService
    {
        Task<IEnumerable<LibraryFeeDto>> GetAllFeesAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<LibraryFeeDto>> GetFilteredFeesAsync(
            LibraryFeeFilterRequest filter, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<LibraryFeeDto>> GetFeesByMemberAsync(
            Guid memberId, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryFeeDto> GetFeeByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<decimal> GetOutstandingBalanceAsync(
            Guid memberId, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryFeeDto> CreateFeeAsync(
            CreateLibraryFeeRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryFeeDto> UpdateFeeAsync(
            Guid id, UpdateLibraryFeeRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryFeeDto> RecordPaymentAsync(
            Guid id, RecordLibraryFeePaymentRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryFeeDto> WaiveFeeAsync(
            Guid id, WaiveLibraryFeeRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteFeeAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}