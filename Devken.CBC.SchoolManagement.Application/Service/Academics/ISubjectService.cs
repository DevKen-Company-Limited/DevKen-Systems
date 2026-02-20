using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Domain.Enums;

namespace Devken.CBC.SchoolManagement.Application.Service.Academics
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectResponseDto>> GetAllSubjectsAsync(
            Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin,
            CBCLevel? level = null, SubjectType? subjectType = null, bool? isActive = null);

        Task<SubjectResponseDto> GetSubjectByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
        Task<SubjectResponseDto> GetSubjectByCodeAsync(string code, Guid? userSchoolId, bool isSuperAdmin);
        Task<SubjectResponseDto> CreateSubjectAsync(CreateSubjectDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task<SubjectResponseDto> UpdateSubjectAsync(Guid id, UpdateSubjectDto dto, Guid? userSchoolId, bool isSuperAdmin);
        Task DeleteSubjectAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);
        Task<SubjectResponseDto> ToggleSubjectActiveAsync(Guid id, bool isActive, Guid? userSchoolId, bool isSuperAdmin);
    }
}
