using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface ILibraryMemberService
    {
        Task<IEnumerable<LibraryMemberDto>> GetAllMembersAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryMemberDto> GetMemberByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryMemberDto> CreateMemberAsync(
            CreateLibraryMemberRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task<LibraryMemberDto> UpdateMemberAsync(
            Guid id, UpdateLibraryMemberRequest request, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteMemberAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}