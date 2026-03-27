// Application/RepositoryManagers/Interfaces/Library/ILibrarySettingsRepository.cs
using System;
using System.Threading.Tasks;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Library;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Library
{
    public interface ILibrarySettingsRepository : IRepositoryBase<LibrarySettings, Guid>
    {
        /// <summary>Gets the library settings for a specific school. Returns null if not configured.</summary>
        Task<LibrarySettings?> GetBySchoolIdAsync(Guid schoolId, bool trackChanges);
    }
}