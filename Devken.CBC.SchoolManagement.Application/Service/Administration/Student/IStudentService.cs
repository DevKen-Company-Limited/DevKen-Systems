using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Administration.Student
{
    /// <summary>
    /// Interface for student service operations
    /// </summary>
    public interface IStudentService
    {
        // ─────────────────────────────────────────────────────────────────────
        // GET OPERATIONS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Get all students with optional school filter (SuperAdmin)
        /// </summary>
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Get student by ID with access validation
        /// </summary>
        Task<StudentDto> GetStudentByIdAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ─────────────────────────────────────────────────────────────────────
        // CREATE OPERATION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a new student with auto-generated admission number
        /// Uses execution strategy for retry compatibility
        /// </summary>
        Task<StudentDto> CreateStudentAsync(
            CreateStudentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE OPERATION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Update an existing student
        /// </summary>
        Task<StudentDto> UpdateStudentAsync(
            Guid id,
            UpdateStudentRequest request,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ─────────────────────────────────────────────────────────────────────
        // DELETE OPERATION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Delete a student
        /// </summary>
        Task DeleteStudentAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ─────────────────────────────────────────────────────────────────────
        // STATUS TOGGLE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Toggle student active status
        /// </summary>
        Task<StudentDto> ToggleStudentStatusAsync(
            Guid id,
            bool isActive,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ─────────────────────────────────────────────────────────────────────
        // PHOTO MANAGEMENT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Upload student photo
        /// UPDATED: Now accepts IFormFile instead of byte array
        /// </summary>
        Task<string> UploadStudentPhotoAsync(
            Guid id,
            IFormFile file,
            Guid? userSchoolId,
            bool isSuperAdmin);

        /// <summary>
        /// Delete student photo
        /// </summary>
        Task DeleteStudentPhotoAsync(
            Guid id,
            Guid? userSchoolId,
            bool isSuperAdmin);

        // ─────────────────────────────────────────────────────────────────────
        // HELPER METHODS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Check if admission number already exists in a school
        /// </summary>
        Task<bool> AdmissionNumberExistsAsync(
            string admissionNumber,
            Guid schoolId,
            Guid? excludeStudentId = null);

        /// <summary>
        /// Generate a new admission number for a school
        /// </summary>
        Task<string> GenerateAdmissionNumberAsync(Guid schoolId);

        /// <summary>
        /// Check if student can be promoted to next level
        /// </summary>
        Task<bool> CanPromoteStudentAsync(Guid id);

        /// <summary>
        /// Get total student count for a school
        /// </summary>
        Task<int> GetStudentCountBySchoolAsync(Guid schoolId);
    }
}