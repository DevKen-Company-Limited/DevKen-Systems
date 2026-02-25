using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Curriculum;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.Service.Curriculum
{
    public interface ILearningOutcomeService
    {
        Task<IEnumerable<LearningOutcomeResponseDto>> GetAllLearningOutcomesAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin,
            Guid? subStrandId = null, Guid? strandId = null,
            Guid? learningAreaId = null, CBCLevel? level = null, bool? isCore = null);

        Task<LearningOutcomeResponseDto> GetLearningOutcomeByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<LearningOutcomeResponseDto> GetLearningOutcomeByCodeAsync(string code, Guid? userSchoolId, bool isSuperAdmin);

        Task<LearningOutcomeResponseDto> CreateLearningOutcomeAsync(CreateLearningOutcomeDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task<LearningOutcomeResponseDto> UpdateLearningOutcomeAsync(Guid id, UpdateLearningOutcomeDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteLearningOutcomeAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}
