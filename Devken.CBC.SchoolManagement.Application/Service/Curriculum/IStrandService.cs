using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Curriculum
{
    public interface IStrandService
    {
        Task<IEnumerable<StrandResponseDto>> GetAllStrandsAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin,
            Guid? learningAreaId = null);

        Task<StrandResponseDto> GetStrandByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<StrandResponseDto> CreateStrandAsync(CreateStrandDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task<StrandResponseDto> UpdateStrandAsync(Guid id, UpdateStrandDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteStrandAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}