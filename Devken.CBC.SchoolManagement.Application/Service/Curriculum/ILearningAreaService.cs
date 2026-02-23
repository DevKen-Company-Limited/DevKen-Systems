using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.Service.Curriculum
{
    public interface ILearningAreaService
    {
        Task<IEnumerable<LearningAreaResponseDto>> GetAllLearningAreasAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin,
            CBCLevel? level = null, bool? isActive = null);

        Task<LearningAreaResponseDto> GetLearningAreaByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<LearningAreaResponseDto> GetLearningAreaByCodeAsync(string code, Guid? userSchoolId, bool isSuperAdmin);

        Task<LearningAreaResponseDto> CreateLearningAreaAsync(CreateLearningAreaDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task<LearningAreaResponseDto> UpdateLearningAreaAsync(Guid id, UpdateLearningAreaDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteLearningAreaAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}
