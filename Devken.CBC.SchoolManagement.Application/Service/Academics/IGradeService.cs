using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Academics
{
    public interface IGradeService
    {
        Task<IEnumerable<GradeResponseDto>> GetAllGradesAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin,
            Guid? studentId = null, Guid? subjectId = null, Guid? termId = null);

        Task<GradeResponseDto> GetGradeByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<GradeResponseDto> CreateGradeAsync(CreateGradeDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task<GradeResponseDto> UpdateGradeAsync(Guid id, UpdateGradeDto dto, Guid? userSchoolId, bool isSuperAdmin);

        Task DeleteGradeAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        Task<GradeResponseDto> FinalizeGradeAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
    }
}