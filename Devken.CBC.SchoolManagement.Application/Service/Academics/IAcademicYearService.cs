//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Devken.CBC.SchoolManagement.Application.DTOs.Academics;

//namespace Devken.CBC.SchoolManagement.Application.Service.Academics
//{
//    public interface IAcademicYearService
//    {
//        /// <summary>
//        /// Creates a new academic year
//        /// </summary>
//        Task<AcademicYearResponseDto> CreateAsync(Guid tenantId, CreateAcademicYearDto dto);

//        /// <summary>
//        /// Updates an existing academic year
//        /// </summary>
//        Task<AcademicYearResponseDto> UpdateAsync(Guid tenantId, Guid id, UpdateAcademicYearDto dto);

//        /// <summary>
//        /// Gets an academic year by ID
//        /// </summary>
//        Task<AcademicYearResponseDto?> GetByIdAsync(Guid tenantId, Guid id);

//        /// <summary>
//        /// Gets all academic years for a tenant
//        /// </summary>
//        Task<IEnumerable<AcademicYearResponseDto>> GetAllAsync(Guid tenantId);

//        /// <summary>
//        /// Gets the current academic year
//        /// </summary>
//        Task<AcademicYearResponseDto?> GetCurrentAsync(Guid tenantId);

//        /// <summary>
//        /// Sets a specific academic year as current
//        /// </summary>
//        Task<bool> SetAsCurrentAsync(Guid tenantId, Guid id);

//        /// <summary>
//        /// Closes an academic year
//        /// </summary>
//        Task<bool> CloseAcademicYearAsync(Guid tenantId, Guid id);

//        /// <summary>
//        /// Deletes an academic year
//        /// </summary>
//        Task<bool> DeleteAsync(Guid tenantId, Guid id);

//        /// <summary>
//        /// Gets all open academic years
//        /// </summary>
//        Task<IEnumerable<AcademicYearResponseDto>> GetOpenAcademicYearsAsync(Guid tenantId);
//    }
//}
