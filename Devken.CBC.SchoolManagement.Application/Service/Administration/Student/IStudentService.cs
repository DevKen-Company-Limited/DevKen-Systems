using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Administration.Student
{
    /// <summary>
    /// Service interface for student-related business logic
    /// </summary>
    public interface IStudentService
    {
        /// <summary>
        /// Gets all students with proper tenant filtering
        /// </summary>
        /// <param name="schoolId">Optional school filter (SuperAdmin only)</param>
        /// <param name="userSchoolId">Current user's school ID</param>
        /// <param name="isSuperAdmin">Is user a SuperAdmin</param>
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Gets a single student by ID with access validation
        /// </summary>
        Task<StudentDto> GetStudentByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Creates a new student with auto-generated admission number
        /// </summary>
        Task<StudentDto> CreateStudentAsync(
            CreateStudentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Updates an existing student
        /// </summary>
        Task<StudentDto> UpdateStudentAsync(
            Guid id,
            UpdateStudentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Deletes a student and associated resources
        /// </summary>
        Task DeleteStudentAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Toggles student active status
        /// </summary>
        Task<StudentDto> ToggleStudentStatusAsync(
            Guid id,
            bool isActive,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Uploads a student photo
        /// </summary>
        Task<string> UploadStudentPhotoAsync(
            Guid id,
            byte[] photoData,
            string fileName,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Deletes a student photo
        /// </summary>
        Task DeleteStudentPhotoAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Checks if admission number already exists in a school
        /// </summary>
        Task<bool> AdmissionNumberExistsAsync(
            string admissionNumber,
            Guid schoolId,
            Guid? excludeStudentId = null);

        /// <summary>
        /// Generates next admission number for a school
        /// </summary>
        Task<string> GenerateAdmissionNumberAsync(Guid schoolId);

        /// <summary>
        /// Validates student can be promoted to next level
        /// </summary>
        Task<bool> CanPromoteStudentAsync(Guid id);

        /// <summary>
        /// Gets student count by school
        /// </summary>
        Task<int> GetStudentCountBySchoolAsync(Guid schoolId);
    }
}
