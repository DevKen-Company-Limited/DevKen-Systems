using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Domain.Enums.Students;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Academic
{
    /// <summary>
    /// Service interface for student management operations
    /// </summary>
    public interface IStudentService
    {
        /// <summary>
        /// Create a new student
        /// </summary>
        Task<(bool Success, string Message, StudentResponse? Student)> CreateStudentAsync(
            CreateStudentRequest request, Guid tenantId);

        /// <summary>
        /// Update student information
        /// </summary>
        Task<(bool Success, string Message, StudentResponse? Student)> UpdateStudentAsync(
            UpdateStudentRequest request, Guid tenantId);

        /// <summary>
        /// Get student by ID
        /// </summary>
        Task<StudentResponse?> GetStudentByIdAsync(Guid studentId, Guid tenantId);

        /// <summary>
        /// Get student by admission number
        /// </summary>
        Task<StudentResponse?> GetStudentByAdmissionNumberAsync(string admissionNumber, Guid tenantId);

        /// <summary>
        /// Get paginated list of students
        /// </summary>
        Task<StudentPagedResponse> GetStudentsPagedAsync(StudentSearchRequest request, Guid tenantId);

        /// <summary>
        /// Get all students in a class
        /// </summary>
        Task<List<StudentListItemResponse>> GetStudentsByClassAsync(Guid classId, Guid tenantId);

        /// <summary>
        /// Get all students in a CBC level
        /// </summary>
        Task<List<StudentListItemResponse>> GetStudentsByLevelAsync(CBCLevel level, Guid tenantId);

        /// <summary>
        /// Search students
        /// </summary>
        Task<List<StudentListItemResponse>> SearchStudentsAsync(string searchTerm, Guid tenantId);

        /// <summary>
        /// Transfer student to another class
        /// </summary>
        Task<(bool Success, string Message)> TransferStudentAsync(TransferStudentRequest request, Guid tenantId);

        /// <summary>
        /// Withdraw student from school
        /// </summary>
        Task<(bool Success, string Message)> WithdrawStudentAsync(WithdrawStudentRequest request, Guid tenantId);

        /// <summary>
        /// Delete student (soft delete)
        /// </summary>
        Task<(bool Success, string Message)> DeleteStudentAsync(Guid studentId, Guid tenantId);

        /// <summary>
        /// Restore deleted student
        /// </summary>
        Task<(bool Success, string Message)> RestoreStudentAsync(Guid studentId, Guid tenantId);

        /// <summary>
        /// Get student statistics
        /// </summary>
        Task<StudentStatisticsResponse> GetStudentStatisticsAsync(Guid tenantId);

        /// <summary>
        /// Get students with special needs
        /// </summary>
        Task<List<StudentListItemResponse>> GetStudentsWithSpecialNeedsAsync(Guid tenantId);

        /// <summary>
        /// Get students with pending fees
        /// </summary>
        Task<List<StudentListItemResponse>> GetStudentsWithPendingFeesAsync(Guid tenantId);

        /// <summary>
        /// Validate admission number uniqueness
        /// </summary>
        Task<bool> ValidateAdmissionNumberAsync(string admissionNumber, Guid tenantId, Guid? excludeStudentId = null);

        /// <summary>
        /// Validate NEMIS number uniqueness
        /// </summary>
        Task<bool> ValidateNemisNumberAsync(string nemisNumber, Guid tenantId, Guid? excludeStudentId = null);
    }
}