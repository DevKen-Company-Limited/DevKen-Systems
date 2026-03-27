// Application/Service/Library/ILibrarySettingsService.cs
using System;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.DTOs.Library;

namespace Devken.CBC.SchoolManagement.Application.Service.Library
{
    public interface ILibrarySettingsService
    {
        /// <summary>Gets settings for the given school. Returns defaults if not yet saved.</summary>
        Task<LibrarySettingsDto> GetSettingsAsync(Guid? schoolId, Guid? userSchoolId, bool isSuperAdmin);

        /// <summary>Creates or updates settings for a school (upsert).</summary>
        Task<LibrarySettingsDto> UpsertSettingsAsync(
            UpsertLibrarySettingsRequest request, Guid? userSchoolId, bool isSuperAdmin);
    }
}