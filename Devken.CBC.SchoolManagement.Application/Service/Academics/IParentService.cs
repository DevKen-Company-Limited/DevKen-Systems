using Devken.CBC.SchoolManagement.Application.DTOs.Parents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IParentService
    {
        Task<IEnumerable<ParentSummaryDto>> GetAllAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin, ParentQueryDto query);

        Task<ParentDto> GetByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<IEnumerable<ParentSummaryDto>> GetByStudentIdAsync(
            Guid studentId, Guid? userSchoolId, bool isSuperAdmin);

        Task<ParentDto> CreateAsync(
            CreateParentDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task<ParentDto> UpdateAsync(
            Guid id, UpdateParentDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<ParentDto> ActivateAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<ParentDto> DeactivateAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}