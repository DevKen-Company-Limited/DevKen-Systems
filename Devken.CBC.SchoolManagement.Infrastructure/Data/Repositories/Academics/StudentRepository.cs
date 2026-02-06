using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.Academic
{
    public class StudentRepository : RepositoryBase<Student, Guid>, IStudentRepository
    {
        public StudentRepository(AppDbContext context, TenantContext tenantContext)
            : base(context, tenantContext)
        {
        }

        public async Task<IEnumerable<Student>> GetAllAsync(bool trackChanges = false)
        {
            var query = FindAll(trackChanges)
                .Include(s => s.School)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear);

            return await query.ToListAsync();
        }

        public new async Task<Student?> GetByIdAsync(Guid id, bool trackChanges = false)
        {
            var query = FindByCondition(s => s.Id == id, trackChanges)
                .Include(s => s.School)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Student>> GetBySchoolIdAsync(Guid schoolId, bool trackChanges = false)
        {
            var query = FindByCondition(s => s.TenantId == schoolId, trackChanges)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear);

            return await query.ToListAsync();
        }

        public async Task<Student?> GetByAdmissionNumberAsync(string admissionNumber, Guid tenantId)
        {
            return await FindByCondition(
                s => s.AdmissionNumber == admissionNumber && s.TenantId == tenantId,
                trackChanges: false)
                .FirstOrDefaultAsync();
        }

        public async Task<Student?> GetByNemisNumberAsync(string nemisNumber, Guid tenantId)
        {
            return await FindByCondition(
                s => s.NemisNumber == nemisNumber && s.TenantId == tenantId,
                trackChanges: false)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Student>> GetStudentsByClassAsync(Guid classId, Guid tenantId, bool includeInactive = false)
        {
            var query = FindByCondition(
                s => s.CurrentClassId == classId && s.TenantId == tenantId,
                trackChanges: false);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query
                .Include(s => s.CurrentClass)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsByLevelAsync(CBCLevel level, Guid tenantId, bool includeInactive = false)
        {
            var query = FindByCondition(
                s => s.CurrentLevel == level && s.TenantId == tenantId,
                trackChanges: false);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query
                .Include(s => s.CurrentClass)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsBySchoolAsync(Guid tenantId, bool includeInactive = false)
        {
            var query = FindByCondition(s => s.TenantId == tenantId, trackChanges: false);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .ToListAsync();
        }

        public async Task<(List<Student> Students, int TotalCount)> GetStudentsPagedAsync(
            Guid tenantId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CBCLevel? level = null,
            Guid? classId = null,
            StudentStatus? status = null,
            bool includeInactive = false)
        {
            var query = FindByCondition(s => s.TenantId == tenantId, trackChanges: false);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(term) ||
                    s.LastName.ToLower().Contains(term) ||
                    s.AdmissionNumber.ToLower().Contains(term) ||
                    (s.NemisNumber != null && s.NemisNumber.ToLower().Contains(term)));
            }

            if (level.HasValue)
            {
                query = query.Where(s => s.CurrentLevel == level.Value);
            }

            if (classId.HasValue)
            {
                query = query.Where(s => s.CurrentClassId == classId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.StudentStatus == status.Value);
            }

            var totalCount = await query.CountAsync();

            var students = await query
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (students, totalCount);
        }

        public async Task<bool> AdmissionNumberExistsAsync(string admissionNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            var query = FindByCondition(
                s => s.AdmissionNumber == admissionNumber && s.TenantId == tenantId,
                trackChanges: false);

            if (excludeStudentId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStudentId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> NemisNumberExistsAsync(string nemisNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            var query = FindByCondition(
                s => s.NemisNumber == nemisNumber && s.TenantId == tenantId,
                trackChanges: false);

            if (excludeStudentId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStudentId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Student?> GetStudentWithDetailsAsync(Guid studentId, Guid tenantId)
        {
            return await FindByCondition(
                s => s.Id == studentId && s.TenantId == tenantId,
                trackChanges: false)
                .Include(s => s.School)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .Include(s => s.Parent)
                .Include(s => s.Grades)
                .Include(s => s.ProgressReports)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Student>> GetStudentsByGenderAsync(Gender gender, Guid tenantId)
        {
            return await FindByCondition(
                s => s.Gender == gender && s.TenantId == tenantId && s.IsActive,
                trackChanges: false)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsAdmittedBetweenAsync(DateTime startDate, DateTime endDate, Guid tenantId)
        {
            return await FindByCondition(
                s => s.DateOfAdmission >= startDate &&
                     s.DateOfAdmission <= endDate &&
                     s.TenantId == tenantId,
                trackChanges: false)
                .Include(s => s.CurrentClass)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsWithSpecialNeedsAsync(Guid tenantId)
        {
            return await FindByCondition(
                s => s.RequiresSpecialSupport && s.TenantId == tenantId && s.IsActive,
                trackChanges: false)
                .Include(s => s.CurrentClass)
                .ToListAsync();
        }

        public async Task<Dictionary<CBCLevel, int>> GetStudentCountByLevelAsync(Guid tenantId)
        {
            return await FindByCondition(
                s => s.TenantId == tenantId && s.IsActive,
                trackChanges: false)
                .GroupBy(s => s.CurrentLevel)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Level, x => x.Count);
        }

        public async Task<Dictionary<Guid, int>> GetStudentCountByClassAsync(Guid tenantId)
        {
            return await FindByCondition(
                s => s.TenantId == tenantId && s.IsActive,
                trackChanges: false)
                .GroupBy(s => s.CurrentClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId, x => x.Count);
        }

        public async Task<List<Student>> SearchStudentsAsync(string searchTerm, Guid tenantId, int maxResults = 50)
        {
            var term = searchTerm.ToLower();

            return await FindByCondition(
                s => s.TenantId == tenantId && s.IsActive,
                trackChanges: false)
                .Where(s =>
                    s.FirstName.ToLower().Contains(term) ||
                    s.LastName.ToLower().Contains(term) ||
                    s.AdmissionNumber.ToLower().Contains(term) ||
                    (s.NemisNumber != null && s.NemisNumber.ToLower().Contains(term)))
                .Include(s => s.CurrentClass)
                .Take(maxResults)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsWithPendingFeesAsync(Guid tenantId)
        {
            return await FindByCondition(
                s => s.TenantId == tenantId && s.IsActive,
                trackChanges: false)
                .Include(s => s.Invoices)
                .Include(s => s.Payments)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsByGuardianPhoneAsync(string phoneNumber, Guid tenantId)
        {
            return await FindByCondition(
                s => s.TenantId == tenantId && s.IsActive &&
                    (s.PrimaryGuardianPhone == phoneNumber ||
                     s.SecondaryGuardianPhone == phoneNumber ||
                     s.EmergencyContactPhone == phoneNumber),
                trackChanges: false)
                .Include(s => s.CurrentClass)
                .ToListAsync();
        }

        public async Task<bool> SoftDeleteStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await FindByCondition(
                s => s.Id == studentId && s.TenantId == tenantId,
                trackChanges: true)
                .FirstOrDefaultAsync();

            if (student == null)
                return false;

            student.IsActive = false;
            student.StudentStatus = StudentStatus.Inactive;
            student.DateOfLeaving = DateTime.UtcNow;

            Update(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await FindByCondition(
                s => s.Id == studentId && s.TenantId == tenantId,
                trackChanges: true)
                .FirstOrDefaultAsync();

            if (student == null)
                return false;

            student.IsActive = true;
            student.StudentStatus = StudentStatus.Active;
            student.DateOfLeaving = null;

            Update(student);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}