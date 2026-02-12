// Application/Services/Interfaces/ITeacherService.cs
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic
{
    public interface ITeacherService
    {
        // Queries
        Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);
        Task<TeacherDto> GetTeacherByIdAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        // Commands
        Task<TeacherDto> CreateTeacherAsync(CreateTeacherRequest request, Guid? userSchoolId, bool isSuperAdmin);
        Task<TeacherDto> UpdateTeacherAsync(Guid id, UpdateTeacherRequest request, Guid? userSchoolId, bool isSuperAdmin);
        Task DeleteTeacherAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin);

        // Photo Management
        Task<string> UploadTeacherPhotoAsync(Guid teacherId, IFormFile file, Guid? userSchoolId, bool isSuperAdmin);
        Task DeleteTeacherPhotoAsync(Guid teacherId, Guid? userSchoolId, bool isSuperAdmin);

        // Status Management
        Task<TeacherDto> ToggleTeacherStatusAsync(Guid id, bool isActive, Guid? userSchoolId, bool isSuperAdmin);
    }
}