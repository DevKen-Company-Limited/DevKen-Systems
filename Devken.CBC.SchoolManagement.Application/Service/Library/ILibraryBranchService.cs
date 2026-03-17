using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface ILibraryBranchService
    {
        Task<IEnumerable<LibraryBranchDto>> GetAllBranchesAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryBranchDto> GetBranchByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryBranchDto> CreateBranchAsync(CreateLibraryBranchRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryBranchDto> UpdateBranchAsync(Guid id, UpdateLibraryBranchRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteBranchAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}