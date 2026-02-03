using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Domain.Enums.Students;
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

        public async Task<Student?> GetByAdmissionNumberAsync(string admissionNumber, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.AdmissionNumber == admissionNumber &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted)
                .FirstOrDefaultAsync();
        }

        public async Task<Student?> GetByNemisNumberAsync(string nemisNumber, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(nemisNumber))
                return null;

            return await _context.Students
                .Where(s => s.NemisNumber == nemisNumber &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Student>> GetStudentsByClassAsync(Guid classId, Guid tenantId, bool includeInactive = false)
        {
            var query = _context.Students
                .Where(s => s.CurrentClassId == classId &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive && s.StudentStatus == StudentStatus.Active);
            }

            return await query
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsByLevelAsync(CBCLevel level, Guid tenantId, bool includeInactive = false)
        {
            var query = _context.Students
                .Where(s => s.CurrentLevel == level &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive && s.StudentStatus == StudentStatus.Active);
            }

            return await query
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsBySchoolAsync(Guid tenantId, bool includeInactive = false)
        {
            var query = _context.Students
                .Where(s => s.TenantId == tenantId && s.Status != EntityStatus.Deleted);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive && s.StudentStatus == StudentStatus.Active);
            }

            return await query
                .OrderBy(s => s.AdmissionNumber)
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
            var query = _context.Students
                .Where(s => s.TenantId == tenantId && s.Status != EntityStatus.Deleted);

            // Apply filters
            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
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

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower().Trim();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(search) ||
                    s.LastName.ToLower().Contains(search) ||
                    s.AdmissionNumber.ToLower().Contains(search) ||
                    (s.NemisNumber != null && s.NemisNumber.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var students = await query
                .OrderBy(s => s.AdmissionNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (students, totalCount);
        }

        public async Task<bool> AdmissionNumberExistsAsync(string admissionNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            var query = _context.Students
                .Where(s => s.AdmissionNumber == admissionNumber &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted);

            if (excludeStudentId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStudentId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> NemisNumberExistsAsync(string nemisNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            if (string.IsNullOrWhiteSpace(nemisNumber))
                return false;

            var query = _context.Students
                .Where(s => s.NemisNumber == nemisNumber &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted);

            if (excludeStudentId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStudentId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Student?> GetStudentWithDetailsAsync(Guid studentId, Guid tenantId)
        {
            return await _context.Students
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .Include(s => s.School)
                .Where(s => s.Id == studentId &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Student>> GetStudentsByGenderAsync(Gender gender, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.Gender == gender &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted &&
                       s.IsActive)
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsAdmittedBetweenAsync(DateTime startDate, DateTime endDate, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.DateOfAdmission >= startDate &&
                       s.DateOfAdmission <= endDate &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted)
                .OrderBy(s => s.DateOfAdmission)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsWithSpecialNeedsAsync(Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.RequiresSpecialSupport &&
                       s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted &&
                       s.IsActive)
                .OrderBy(s => s.CurrentLevel)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<Dictionary<CBCLevel, int>> GetStudentCountByLevelAsync(Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted &&
                       s.IsActive &&
                       s.StudentStatus == StudentStatus.Active)
                .GroupBy(s => s.CurrentLevel)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Level, x => x.Count);
        }

        public async Task<Dictionary<Guid, int>> GetStudentCountByClassAsync(Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted &&
                       s.IsActive &&
                       s.StudentStatus == StudentStatus.Active)
                .GroupBy(s => s.CurrentClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId, x => x.Count);
        }

        public async Task<List<Student>> SearchStudentsAsync(string searchTerm, Guid tenantId, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Student>();

            var search = searchTerm.ToLower().Trim();

            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted &&
                       (s.FirstName.ToLower().Contains(search) ||
                        s.LastName.ToLower().Contains(search) ||
                        s.AdmissionNumber.ToLower().Contains(search) ||
                        (s.NemisNumber != null && s.NemisNumber.ToLower().Contains(search))))
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .Take(maxResults)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsWithPendingFeesAsync(Guid tenantId)
        {
            return await _context.Students
                .Include(s => s.Invoices)
                .Where(s => s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted &&
                       s.IsActive &&
                       s.Invoices.Any(i => i.PaymentStatus == PaymentStatus.Pending ||
                                          i.PaymentStatus == PaymentStatus.Partial ||
                                          i.PaymentStatus == PaymentStatus.Overdue))
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        public async Task<List<Student>> GetStudentsByGuardianPhoneAsync(string phoneNumber, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new List<Student>();

            var cleanPhone = phoneNumber.Trim();

            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                       s.Status != EntityStatus.Deleted &&
                       (s.PrimaryGuardianPhone == cleanPhone ||
                        s.SecondaryGuardianPhone == cleanPhone ||
                        s.EmergencyContactPhone == cleanPhone))
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();
        }

        public async Task<bool> SoftDeleteStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && s.TenantId == tenantId);

            if (student == null)
                return false;

            student.Status = EntityStatus.Deleted;
            student.IsActive = false;
            student.UpdatedOn = DateTime.UtcNow;
            student.UpdatedBy = _tenantContext.ActingUserId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await _context.Students
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == studentId && s.TenantId == tenantId);

            if (student == null)
                return false;

            student.Status = EntityStatus.Active;
            student.IsActive = true;
            student.UpdatedOn = DateTime.UtcNow;
            student.UpdatedBy = _tenantContext.ActingUserId;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}