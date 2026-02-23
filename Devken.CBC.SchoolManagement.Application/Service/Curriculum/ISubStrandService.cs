using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Curriculum
{
    public interface ISubStrandService
    {
        Task<IEnumerable<SubStrandResponseDto>> GetAllSubStrandsAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin,
            Guid? strandId = null);

        Task<SubStrandResponseDto> GetSubStrandByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<SubStrandResponseDto> CreateSubStrandAsync(CreateSubStrandDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task<SubStrandResponseDto> UpdateSubStrandAsync(Guid id, UpdateSubStrandDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteSubStrandAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}