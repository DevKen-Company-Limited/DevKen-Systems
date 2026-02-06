using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic
{
    /// <summary>
    /// Repository interface for Student entity
    /// </summary>
    public interface IStudentRepository : IRepositoryBase<Student, Guid>
    {
        /// <summary>
        /// Get all students across all schools (SuperAdmin only)
        /// </summary>
        Task<IEnumerable<Student>> GetAllAsync(bool trackChanges = false);

        /// <summary>
        /// Get student by ID
        /// </summary>
        Task<Student?> GetByIdAsync(Guid id, bool trackChanges = false);

        /// <summary>
        /// Get all students in a specific school/tenant
        /// </summary>
        Task<IEnumerable<Student>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges = false);

        /// <summary>
        /// Get student by admission number within a specific school/tenant
        /// </summary>
        Task<Student?> GetByAdmissionNumberAsync(string admissionNumber, Guid tenantId);

        /// <summary>
        /// Get student by NEMIS number
        /// </summary>
        Task<Student?> GetByNemisNumberAsync(string nemisNumber, Guid tenantId);

        /// <summary>
        /// Get all students in a specific class
        /// </summary>
        Task<List<Student>> GetStudentsByClassAsync(Guid classId, Guid tenantId, bool includeInactive = false);

        /// <summary>
        /// Get all students in a specific CBC level
        /// </summary>
        Task<List<Student>> GetStudentsByLevelAsync(CBCLevel level, Guid tenantId, bool includeInactive = false);

        /// <summary>
        /// Get all students in a school
        /// </summary>
        Task<List<Student>> GetStudentsBySchoolAsync(Guid tenantId, bool includeInactive = false);

        /// <summary>
        /// Get students with pagination
        /// </summary>
        Task<(List<Student> Students, int TotalCount)> GetStudentsPagedAsync(
            Guid tenantId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CBCLevel? level = null,
            Guid? classId = null,
            StudentStatus? status = null,
            bool includeInactive = false);

        /// <summary>
        /// Check if admission number exists
        /// </summary>
        Task<bool> AdmissionNumberExistsAsync(string admissionNumber, Guid tenantId, Guid? excludeStudentId = null);

        /// <summary>
        /// Check if NEMIS number exists
        /// </summary>
        Task<bool> NemisNumberExistsAsync(string nemisNumber, Guid tenantId, Guid? excludeStudentId = null);

        /// <summary>
        /// Get student with complete details (including all navigation properties)
        /// </summary>
        Task<Student?> GetStudentWithDetailsAsync(Guid studentId, Guid tenantId);

        /// <summary>
        /// Get students by gender
        /// </summary>
        Task<List<Student>> GetStudentsByGenderAsync(Gender gender, Guid tenantId);

        /// <summary>
        /// Get students admitted in a date range
        /// </summary>
        Task<List<Student>> GetStudentsAdmittedBetweenAsync(DateTime startDate, DateTime endDate, Guid tenantId);

        /// <summary>
        /// Get students with special needs
        /// </summary>
        Task<List<Student>> GetStudentsWithSpecialNeedsAsync(Guid tenantId);

        /// <summary>
        /// Get student count by level
        /// </summary>
        Task<Dictionary<CBCLevel, int>> GetStudentCountByLevelAsync(Guid tenantId);

        /// <summary>
        /// Get student count by class
        /// </summary>
        Task<Dictionary<Guid, int>> GetStudentCountByClassAsync(Guid tenantId);

        /// <summary>
        /// Search students by name, admission number, or NEMIS number
        /// </summary>
        Task<List<Student>> SearchStudentsAsync(string searchTerm, Guid tenantId, int maxResults = 50);

        /// <summary>
        /// Get students with pending fees
        /// </summary>
        Task<List<Student>> GetStudentsWithPendingFeesAsync(Guid tenantId);

        /// <summary>
        /// Get students by guardian phone number
        /// </summary>
        Task<List<Student>> GetStudentsByGuardianPhoneAsync(string phoneNumber, Guid tenantId);

        /// <summary>
        /// Soft delete student
        /// </summary>
        Task<bool> SoftDeleteStudentAsync(Guid studentId, Guid tenantId);

        /// <summary>
        /// Restore soft deleted student
        /// </summary>
        Task<bool> RestoreStudentAsync(Guid studentId, Guid tenantId);

        /// <summary>
        /// Create a new student
        /// </summary>
        void Create(Student student);

        /// <summary>
        /// Update an existing student
        /// </summary>
        void Update(Student student);

        /// <summary>
        /// Delete a student
        /// </summary>
        void Delete(Student student);
    }
}