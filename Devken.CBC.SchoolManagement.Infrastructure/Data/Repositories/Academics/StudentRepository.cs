using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Application.Service;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using Devken.CBC.SchoolManagement.Domain.Entities.Reports;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Data.EF;
using Devken.CBC.SchoolManagement.Infrastructure.Data.Repositories.common;
using Devken.CBC.SchoolManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Academic
{
    /// <summary>
    /// Repository implementation for Student entity
    /// NOTE: This version uses StudentStatus exclusively (no EntityStatus soft-delete pattern)
    /// </summary>
    public class StudentRepository : RepositoryBase<Student, Guid>, IStudentRepository
    {
        private new readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StudentRepository(AppDbContext context, ICurrentUserService currentUserService)
            : base(context, new TenantContext { ActingUserId = currentUserService.UserId })
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Get student by admission number
        /// </summary>
        public async Task<Student?> GetByAdmissionNumberAsync(string admissionNumber, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.AdmissionNumber == admissionNumber &&
                           s.StudentStatus != StudentStatus.Withdrawn)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get student by NEMIS number
        /// </summary>
        public async Task<Student?> GetByNemisNumberAsync(string nemisNumber, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.NemisNumber == nemisNumber &&
                           s.StudentStatus != StudentStatus.Withdrawn)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get all students in a specific class
        /// </summary>
        public async Task<List<Student>> GetStudentsByClassAsync(Guid classId, Guid tenantId, bool includeInactive = false)
        {
            var query = _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.CurrentClassId == classId &&
                           s.StudentStatus != StudentStatus.Withdrawn);

            if (!includeInactive)
            {
                query = query.Where(s => s.StudentStatus == StudentStatus.Active);
            }

            return await query
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get all students in a specific CBC level
        /// </summary>
        public async Task<List<Student>> GetStudentsByLevelAsync(CBCLevel level, Guid tenantId, bool includeInactive = false)
        {
            var query = _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.CurrentLevel == level &&
                           s.StudentStatus != StudentStatus.Withdrawn);

            if (!includeInactive)
            {
                query = query.Where(s => s.StudentStatus == StudentStatus.Active);
            }

            return await query
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get all students in a school
        /// </summary>
        public async Task<List<Student>> GetStudentsBySchoolAsync(Guid tenantId, bool includeInactive = false)
        {
            var query = _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.StudentStatus != StudentStatus.Withdrawn);

            if (!includeInactive)
            {
                query = query.Where(s => s.StudentStatus == StudentStatus.Active);
            }

            return await query
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get students with pagination
        /// </summary>
        public async Task<(List<Student> Students, int TotalCount)> GetStudentsPagedAsync(
            Guid tenantId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CBCLevel? level = null,
            Guid? classId = null,
            StudentStatus? studentStatus = null,
            bool includeInactive = false)
        {
            var query = _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.StudentStatus != StudentStatus.Withdrawn);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(searchTerm) ||
                    s.LastName.ToLower().Contains(searchTerm) ||
                    s.AdmissionNumber.ToLower().Contains(searchTerm) ||
                    (s.MiddleName != null && s.MiddleName.ToLower().Contains(searchTerm)) ||
                    (s.NemisNumber != null && s.NemisNumber.ToLower().Contains(searchTerm)));
            }

            if (level.HasValue)
            {
                query = query.Where(s => s.CurrentLevel == level.Value);
            }

            if (classId.HasValue)
            {
                query = query.Where(s => s.CurrentClassId == classId.Value);
            }

            if (studentStatus.HasValue)
            {
                query = query.Where(s => s.StudentStatus == studentStatus.Value);
            }

            if (!includeInactive)
            {
                query = query.Where(s => s.StudentStatus == StudentStatus.Active);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var students = await query
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (students, totalCount);
        }

        /// <summary>
        /// Check if admission number exists
        /// </summary>
        public async Task<bool> AdmissionNumberExistsAsync(string admissionNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            var query = _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.AdmissionNumber == admissionNumber &&
                           s.StudentStatus != StudentStatus.Withdrawn);

            if (excludeStudentId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStudentId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Check if NEMIS number exists
        /// </summary>
        public async Task<bool> NemisNumberExistsAsync(string nemisNumber, Guid tenantId, Guid? excludeStudentId = null)
        {
            if (string.IsNullOrWhiteSpace(nemisNumber))
                return false;

            var query = _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.NemisNumber == nemisNumber &&
                           s.StudentStatus != StudentStatus.Withdrawn);

            if (excludeStudentId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStudentId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get student with complete details (including all navigation properties)
        /// </summary>
        public async Task<Student?> GetStudentWithDetailsAsync(Guid studentId, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.Id == studentId &&
                           s.TenantId == tenantId &&
                           s.StudentStatus != StudentStatus.Withdrawn)
                .Include(s => s.CurrentClass)
                    .ThenInclude(c => c!.ClassTeacher)
                .Include(s => s.CurrentAcademicYear)
                .Include(s => s.Grades.Where(g => g.Status != EntityStatus.Deleted))
                    .ThenInclude(g => g.Subject)
                .Include(s => s.FormativeAssessmentScores.Where(fas => fas.Status != EntityStatus.Deleted))
                    .ThenInclude(fas => fas.FormativeAssessment)
                .Include(s => s.SummativeAssessmentScores.Where(sas => sas.Status != EntityStatus.Deleted))
                    .ThenInclude(sas => sas.SummativeAssessment)
                .Include(s => s.CompetencyAssessmentScores.Where(cas => cas.Status != EntityStatus.Deleted))
                    .ThenInclude(cas => cas.CompetencyAssessment)
                .Include(s => s.ProgressReports.Where(pr => pr.Status != EntityStatus.Deleted))
                    .ThenInclude(pr => pr.SubjectReports.Where(sr => sr.Status != EntityStatus.Deleted))
                .Include(s => s.Invoices.Where(i => i.Status != EntityStatus.Deleted))
                    .ThenInclude(i => i.Items.Where(ii => ii.Status != EntityStatus.Deleted))
                .Include(s => s.Payments.Where(p => p.Status != EntityStatus.Deleted))
                .Include(s => s.Parent)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get students by gender
        /// </summary>
        public async Task<List<Student>> GetStudentsByGenderAsync(Gender gender, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.Gender == gender &&
                           s.StudentStatus == StudentStatus.Active)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get students admitted in a date range
        /// </summary>
        public async Task<List<Student>> GetStudentsAdmittedBetweenAsync(DateTime startDate, DateTime endDate, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.DateOfAdmission >= startDate &&
                           s.DateOfAdmission <= endDate &&
                           s.StudentStatus != StudentStatus.Withdrawn)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.DateOfAdmission)
                .ToListAsync();
        }

        /// <summary>
        /// Get students with special needs
        /// </summary>
        public async Task<List<Student>> GetStudentsWithSpecialNeedsAsync(Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.RequiresSpecialSupport &&
                           s.StudentStatus == StudentStatus.Active)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get student count by level
        /// </summary>
        public async Task<Dictionary<CBCLevel, int>> GetStudentCountByLevelAsync(Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.StudentStatus == StudentStatus.Active)
                .GroupBy(s => s.CurrentLevel)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Level, x => x.Count);
        }

        /// <summary>
        /// Get student count by class
        /// </summary>
        public async Task<Dictionary<Guid, int>> GetStudentCountByClassAsync(Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.StudentStatus == StudentStatus.Active)
                .GroupBy(s => s.CurrentClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId, x => x.Count);
        }

        /// <summary>
        /// Search students by name, admission number, or NEMIS number
        /// </summary>
        public async Task<List<Student>> SearchStudentsAsync(string searchTerm, Guid tenantId, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Student>();

            searchTerm = searchTerm.ToLower();

            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.StudentStatus == StudentStatus.Active &&
                           (s.FirstName.ToLower().Contains(searchTerm) ||
                            s.LastName.ToLower().Contains(searchTerm) ||
                            s.AdmissionNumber.ToLower().Contains(searchTerm) ||
                            (s.MiddleName != null && s.MiddleName.ToLower().Contains(searchTerm)) ||
                            (s.NemisNumber != null && s.NemisNumber.ToLower().Contains(searchTerm)) ||
                            (s.PrimaryGuardianName != null && s.PrimaryGuardianName.ToLower().Contains(searchTerm)) ||
                            (s.PrimaryGuardianPhone != null && s.PrimaryGuardianPhone.Contains(searchTerm))))
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .Take(maxResults)
                .ToListAsync();
        }

        /// <summary>
        /// Get students with pending fees
        /// </summary>
        public async Task<List<Student>> GetStudentsWithPendingFeesAsync(Guid tenantId)
        {
            // Get students with invoices that have balance > 0
            var studentIds = await _context.Invoices
                .Where(i => i.TenantId == tenantId &&
                           i.Balance > 0 &&
                           i.Status != EntityStatus.Deleted)
                .Select(i => i.StudentId)
                .Distinct()
                .ToListAsync();

            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           studentIds.Contains(s.Id) &&
                           s.StudentStatus == StudentStatus.Active)
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .Include(s => s.Invoices.Where(i => i.Balance > 0 && i.Status != EntityStatus.Deleted))
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get students by guardian phone number
        /// </summary>
        public async Task<List<Student>> GetStudentsByGuardianPhoneAsync(string phoneNumber, Guid tenantId)
        {
            return await _context.Students
                .Where(s => s.TenantId == tenantId &&
                           s.StudentStatus == StudentStatus.Active &&
                           (s.PrimaryGuardianPhone == phoneNumber ||
                            s.SecondaryGuardianPhone == phoneNumber ||
                            s.EmergencyContactPhone == phoneNumber))
                .Include(s => s.CurrentClass)
                .Include(s => s.CurrentAcademicYear)
                .OrderBy(s => s.AdmissionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Soft delete student (using StudentStatus.Withdrawn instead of EntityStatus.Deleted)
        /// </summary>
        public async Task<bool> SoftDeleteStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId &&
                                        s.TenantId == tenantId &&
                                        s.StudentStatus != StudentStatus.Withdrawn);

            if (student == null)
                return false;

            student.StudentStatus = StudentStatus.Withdrawn;
            student.UpdatedOn = DateTime.UtcNow;
            student.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Restore soft deleted student
        /// </summary>
        public async Task<bool> RestoreStudentAsync(Guid studentId, Guid tenantId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId &&
                                        s.TenantId == tenantId &&
                                        s.StudentStatus == StudentStatus.Withdrawn);

            if (student == null)
                return false;

            student.StudentStatus = StudentStatus.Active;
            student.UpdatedOn = DateTime.UtcNow;
            student.UpdatedBy = _currentUserService.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Get student grades for a specific term
        /// </summary>
        public async Task<List<Grade>> GetStudentGradesByTermAsync(Guid studentId, Guid termId, Guid tenantId)
        {
            return await _context.Grades
                .Where(g => g.StudentId == studentId &&
                           g.TermId == termId &&
                           g.TenantId == tenantId &&
                           g.Status != EntityStatus.Deleted)
                .Include(g => g.Subject)
                .Include(g => g.Assessment)
                .OrderBy(g => g.Subject.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get student assessments for a specific academic year
        /// </summary>
        public async Task<List<Assessment1>> GetStudentAssessmentsByAcademicYearAsync(Guid studentId, Guid academicYearId, Guid tenantId)
        {
            // Get assessments where student has grades or scores
            var gradeAssessmentIds = await _context.Grades
                .Where(g => g.StudentId == studentId &&
                           g.TenantId == tenantId &&
                           g.Status != EntityStatus.Deleted &&
                           g.AssessmentId.HasValue)
                .Select(g => g.AssessmentId!.Value)
                .Distinct()
                .ToListAsync();

            var formativeAssessmentIds = await _context.FormativeAssessmentScores
                .Where(fas => fas.StudentId == studentId &&
                             fas.TenantId == tenantId &&
                             fas.Status != EntityStatus.Deleted)
                .Select(fas => fas.FormativeAssessmentId)
                .Distinct()
                .ToListAsync();

            var summativeAssessmentIds = await _context.SummativeAssessmentScores
                .Where(sas => sas.StudentId == studentId &&
                             sas.TenantId == tenantId &&
                             sas.Status != EntityStatus.Deleted)
                .Select(sas => sas.SummativeAssessmentId)
                .Distinct()
                .ToListAsync();

            var competencyAssessmentIds = await _context.CompetencyAssessmentScores
                .Where(cas => cas.StudentId == studentId &&
                             cas.TenantId == tenantId &&
                             cas.Status != EntityStatus.Deleted)
                .Select(cas => cas.CompetencyAssessmentId)
                .Distinct()
                .ToListAsync();

            // Combine all assessment IDs
            var allAssessmentIds = new List<Guid>();
            allAssessmentIds.AddRange(gradeAssessmentIds);
            allAssessmentIds.AddRange((IEnumerable<Guid>)formativeAssessmentIds);
            allAssessmentIds.AddRange((IEnumerable<Guid>)summativeAssessmentIds);
            allAssessmentIds.AddRange((IEnumerable<Guid>)competencyAssessmentIds);
            allAssessmentIds = allAssessmentIds.Distinct().ToList();

            return await _context.Assessments
                .Where(a => allAssessmentIds.Contains(a.Id) &&
                           a.AcademicYearId == academicYearId &&
                           a.TenantId == tenantId &&
                           a.Status != EntityStatus.Deleted)
                .Include(a => a.Subject)
                .Include(a => a.Class)
                .Include(a => a.Term)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get student progress reports
        /// </summary>
        public async Task<List<ProgressReport>> GetStudentProgressReportsAsync(Guid studentId, Guid tenantId)
        {
            return await _context.ProgressReports
                .Where(pr => pr.StudentId == studentId &&
                            pr.TenantId == tenantId &&
                            pr.Status != EntityStatus.Deleted)
                .Include(pr => pr.Term)
                .Include(pr => pr.AcademicYear)
                .Include(pr => pr.SubjectReports.Where(sr => sr.Status != EntityStatus.Deleted))
                    .ThenInclude(sr => sr.Subject)
                .OrderByDescending(pr => pr.ReportDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get student invoices with outstanding balance
        /// </summary>
        public async Task<List<Invoice>> GetStudentOutstandingInvoicesAsync(Guid studentId, Guid tenantId)
        {
            return await _context.Invoices
                .Where(i => i.StudentId == studentId &&
                           i.TenantId == tenantId &&
                           i.Balance > 0 &&
                           i.Status != EntityStatus.Deleted)
                .Include(i => i.Items.Where(ii => ii.Status != EntityStatus.Deleted))
                .Include(i => i.Term)
                .Include(i => i.AcademicYear)
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get student payment history
        /// </summary>
        public async Task<List<Payment>> GetStudentPaymentHistoryAsync(Guid studentId, Guid tenantId)
        {
            return await _context.Payments
                .Where(p => p.StudentId == studentId &&
                           p.TenantId == tenantId &&
                           p.StatusPayment != PaymentStatus.Failed)
                .Include(p => p.Invoice)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get student competency assessment scores
        /// </summary>
        public async Task<List<CompetencyAssessmentScore>> GetStudentCompetencyScoresAsync(Guid studentId, Guid tenantId)
        {
            return await _context.CompetencyAssessmentScores
                .Where(cas => cas.StudentId == studentId &&
                             cas.TenantId == tenantId &&
                             cas.Status != EntityStatus.Deleted)
                .Include(cas => cas.CompetencyAssessment)
                    .ThenInclude(ca => ca!.Subject)
                .Include(cas => cas.Assessor)
                .OrderByDescending(cas => cas.AssessmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get student formative assessment scores
        /// </summary>
        public async Task<List<FormativeAssessmentScore>> GetStudentFormativeScoresAsync(Guid studentId, Guid tenantId)
        {
            return await _context.FormativeAssessmentScores
                .Where(fas => fas.StudentId == studentId &&
                             fas.TenantId == tenantId &&
                             fas.Status != EntityStatus.Deleted)
                .Include(fas => fas.FormativeAssessment)
                    .ThenInclude(fa => fa!.Subject)
                .Include(fas => fas.GradedBy)
                .OrderByDescending(fas => fas.SubmissionDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get student summative assessment scores
        /// </summary>
        public async Task<List<SummativeAssessmentScore>> GetStudentSummativeScoresAsync(Guid studentId, Guid tenantId)
        {
            return await _context.SummativeAssessmentScores
                .Where(sas => sas.StudentId == studentId &&
                             sas.TenantId == tenantId &&
                             sas.Status != EntityStatus.Deleted)
                .Include(sas => sas.SummativeAssessment)
                    .ThenInclude(sa => sa!.Subject)
                .Include(sas => sas.GradedBy)
                .OrderByDescending(sas => sas.GradedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Update student class and level
        /// </summary>
        public async Task<bool> UpdateStudentClassAsync(Guid studentId, Guid newClassId, CBCLevel newLevel, Guid tenantId, string reason)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId &&
                                        s.TenantId == tenantId &&
                                        s.StudentStatus != StudentStatus.Withdrawn);

            if (student == null)
                return false;

            // Store previous values for history
            var previousClassId = student.CurrentClassId;
            var previousLevel = student.CurrentLevel;

            // Update student
            student.CurrentClassId = newClassId;
            student.CurrentLevel = newLevel;
            student.UpdatedOn = DateTime.UtcNow;
            student.UpdatedBy = _currentUserService.UserId;

            // TODO: Create transfer history record
            // await CreateStudentTransferHistory(studentId, previousClassId, newClassId, previousLevel, newLevel, reason);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Get student statistics
        /// </summary>
        public async Task<StudentStatistics> GetStudentStatisticsAsync(Guid tenantId)
        {
            var statistics = new StudentStatistics
            {
                StudentsByLevel = new Dictionary<string, int>(),
                StudentsByStatus = new Dictionary<string, int>()
            };

            var students = _context.Students
                .Where(s => s.TenantId == tenantId && s.StudentStatus != StudentStatus.Withdrawn);

            statistics.TotalStudents = await students.CountAsync();
            statistics.ActiveStudents = await students.CountAsync(s => s.StudentStatus == StudentStatus.Active);
            statistics.InactiveStudents = await students.CountAsync(s => s.StudentStatus != StudentStatus.Active);
            statistics.MaleStudents = await students.CountAsync(s => s.Gender == Gender.Male);
            statistics.FemaleStudents = await students.CountAsync(s => s.Gender == Gender.Female);
            statistics.StudentsWithSpecialNeeds = await students.CountAsync(s => s.RequiresSpecialSupport);

            // Count by level
            var levelCounts = await students
                .Where(s => s.StudentStatus == StudentStatus.Active)
                .GroupBy(s => s.CurrentLevel)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in levelCounts)
            {
                statistics.StudentsByLevel[item.Level.ToString()] = item.Count;
            }

            // Count by status
            var statusCounts = await students
                .Where(s => s.StudentStatus == StudentStatus.Active)
                .GroupBy(s => s.StudentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in statusCounts)
            {
                statistics.StudentsByStatus[item.Status.ToString()] = item.Count;
            }

            return statistics;
        }
    }

    /// <summary>
    /// Student statistics DTO
    /// </summary>
    public class StudentStatistics
    {
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int InactiveStudents { get; set; }
        public int MaleStudents { get; set; }
        public int FemaleStudents { get; set; }
        public int StudentsWithSpecialNeeds { get; set; }
        public Dictionary<string, int> StudentsByLevel { get; set; } = new();
        public Dictionary<string, int> StudentsByStatus { get; set; } = new();
    }
}