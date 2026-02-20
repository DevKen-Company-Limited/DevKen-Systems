using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports;
using Devken.CBC.SchoolManagement.Application.Service.Administration.Student;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Reports.Assessment;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Reports.Student;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Reports.Subject;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly IRepositoryManager _repositories;
        private readonly IStudentService _studentService;
        private readonly IWebHostEnvironment _env;

        public ReportService(
            IRepositoryManager repositories,
            IStudentService studentService,
            IWebHostEnvironment env)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        // ── Single-school assessment report ───────────────────────────────
        public async Task<byte[]> GenerateAssessmentsListReportAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            AssessmentTypeDto? type = null)
        {
            var finalSchoolId = isSuperAdmin ? schoolId : userSchoolId;
            if (finalSchoolId == null)
                throw new InvalidOperationException("School context not found.");

            var school = await _repositories.School
                .GetByIdAsync(finalSchoolId.Value, trackChanges: false)
                ?? throw new KeyNotFoundException($"School {finalSchoolId.Value} not found.");

            byte[]? logoBytes = await ResolveLogoAsync(school.LogoUrl);

            // Fetch all assessment types, tenant-scoped
            var formative = await _repositories.FormativeAssessment
                .GetAllAsync(null, null, null, null, null, trackChanges: false);
            var summative = await _repositories.SummativeAssessment
                .GetAllAsync(null, null, null, null, null, trackChanges: false);
            var competency = await _repositories.CompetencyAssessment
                .GetAllAsync(null, null, null, null, null, trackChanges: false);

            // Map all to AssessmentBase before concatenation
            var allAssessments = formative.Select(MapToAssessmentBase)
                .Concat(summative.Select(MapToAssessmentBase))
                .Concat(competency.Select(MapToAssessmentBase))
              .Where(a => !type.HasValue ||
            a.AssessmentType == (AssessmentType)type.Value)
                .Select(a => new AssessmentReportDto
                {
                    Title = a.Title,
                    AssessmentType = (AssessmentTypeDto)a.AssessmentType,
                    TeacherName = a.TeacherName ?? string.Empty,
                    SubjectName = a.SubjectName ?? string.Empty,
                    ClassName = a.ClassName ?? string.Empty,
                    TermName = a.TermName ?? string.Empty,
                    AssessmentDate = a.AssessmentDate,
                    MaximumScore = a.MaximumScore,
                    IsPublished = a.IsPublished,
                    ScoreCount = a.ScoreCount
                })
                .OrderByDescending(a => a.AssessmentDate)
                .ToList();

            var document = new AssessmentsListReportDocument(
                school: school,
                assessments: allAssessments,
                logoBytes: logoBytes,
                isSuperAdmin: isSuperAdmin);

            return document.ExportToPdfBytes();
        }

        // ── All-schools assessment report ───────────────────────────────
        public async Task<byte[]> GenerateAllSchoolsAssessmentsListReportAsync(
            AssessmentTypeDto? type = null)
        {
            var allSchools = await _repositories.School.GetAllAsync(trackChanges: false);
            var schoolMap = allSchools.ToDictionary(s => s.Id, s => s.Name);

            // Fetch all assessments across all tenants
            var formative = await _repositories.FormativeAssessment
                .GetAllAsync(null, null, null, null, null, trackChanges: false);
            var summative = await _repositories.SummativeAssessment
                .GetAllAsync(null, null, null, null, null, trackChanges: false);
            var competency = await _repositories.CompetencyAssessment
                .GetAllAsync(null, null, null, null, null, trackChanges: false);

            var allAssessments = formative.Select(MapToAssessmentBase)
                .Concat(summative.Select(MapToAssessmentBase))
                .Concat(competency.Select(MapToAssessmentBase))
            .Where(a => !type.HasValue ||
            a.AssessmentType == (AssessmentType)type.Value)
                .Select(a => new AssessmentReportDto
                {
                    Title = a.Title,
                    AssessmentType = (AssessmentTypeDto)a.AssessmentType,
                    TeacherName = a.TeacherName ?? string.Empty,
                    SubjectName = a.SubjectName ?? string.Empty,
                    ClassName = a.ClassName ?? string.Empty,
                    TermName = a.TermName ?? string.Empty,
                    AssessmentDate = a.AssessmentDate,
                    MaximumScore = a.MaximumScore,
                    IsPublished = a.IsPublished,
                    ScoreCount = a.ScoreCount,
                    SchoolId = a.TenantId,
                    SchoolName = schoolMap.TryGetValue(a.TenantId, out var name) ? name : string.Empty
                })
                .OrderByDescending(a => a.AssessmentDate)
                .ToList();

            var document = new AssessmentsListReportDocument(
                school: null,
                assessments: allAssessments,
                logoBytes: null,
                isSuperAdmin: true);

            return document.ExportToPdfBytes();
        }

        // ── Students reports ─────────────────────────────────────────────
        public async Task<byte[]> GenerateStudentsListReportAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var finalSchoolId = isSuperAdmin ? schoolId : userSchoolId;
            if (finalSchoolId == null)
                throw new InvalidOperationException("School context not found.");

            var school = await _repositories.School
                .GetByIdAsync(finalSchoolId.Value, trackChanges: false)
                ?? throw new KeyNotFoundException($"School {finalSchoolId.Value} not found.");

            byte[]? logoBytes = await ResolveLogoAsync(school.LogoUrl);

            var studentsData = await _studentService.GetAllStudentsAsync(
                finalSchoolId, userSchoolId, isSuperAdmin);

            var students = studentsData.Select(s => new StudentDto
            {
                AdmissionNumber = s.AdmissionNumber,
                FullName = s.FullName,
                CurrentClassName = s.CurrentClassName,
                IsActive = s.IsActive
            }).ToList();

            var document = new StudentsListReportDocument(
                school: school,
                students: students,
                logoBytes: logoBytes,
                isSuperAdmin: isSuperAdmin);

            return document.ExportToPdfBytes();
        }

        public async Task<byte[]> GenerateAllSchoolsStudentsListReportAsync()
        {
            var allSchools = await _repositories.School.GetAllAsync(trackChanges: false);
            var schoolMap = allSchools.ToDictionary(s => s.Id, s => s.Name);

            var studentsData = await _studentService.GetAllStudentsAsync(
                schoolId: null,
                userSchoolId: null,
                isSuperAdmin: true);

            var students = studentsData.Select(s => new StudentDto
            {
                AdmissionNumber = s.AdmissionNumber,
                FullName = s.FullName,
                CurrentClassName = s.CurrentClassName,
                IsActive = s.IsActive,
                SchoolName = s.SchoolId != Guid.Empty && schoolMap.TryGetValue(s.SchoolId, out var name)
                                ? name
                                : s.SchoolName ?? string.Empty
            }).ToList();

            var document = new StudentsListReportDocument(
                school: null,
                students: students,
                logoBytes: null,
                isSuperAdmin: true);

            return document.ExportToPdfBytes();
        }

        // ── Subjects reports ─────────────────────────────────────────────
        public async Task<byte[]> GenerateSubjectsListReportAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var finalSchoolId = isSuperAdmin ? schoolId : userSchoolId;
            if (finalSchoolId == null)
                throw new InvalidOperationException("School context not found.");

            var school = await _repositories.School
                .GetByIdAsync(finalSchoolId.Value, trackChanges: false)
                ?? throw new KeyNotFoundException($"School {finalSchoolId.Value} not found.");

            byte[]? logoBytes = await ResolveLogoAsync(school.LogoUrl);

            var subjectsData = await _repositories.Subject.GetAllAsync(trackChanges: false);

            var subjects = subjectsData.Select(s => new SubjectReportDto
            {
                Code = s.Code,
                Name = s.Name,
                Level = s.Level.ToString(),
                SubjectType = s.SubjectType.ToString(),
                IsActive = s.IsActive
            }).ToList();

            var document = new SubjectsListReportDocument(
                school: school,
                subjects: subjects,
                logoBytes: logoBytes,
                isSuperAdmin: isSuperAdmin);

            return document.ExportToPdfBytes();
        }

        public async Task<byte[]> GenerateAllSchoolsSubjectsListReportAsync()
        {
            var allSchools = await _repositories.School.GetAllAsync(trackChanges: false);
            var schoolMap = allSchools.ToDictionary(s => s.Id, s => s.Name);

            var allSubjects = await _repositories.Subject.GetAllAsync(trackChanges: false);

            var subjects = allSubjects.Select(s => new SubjectReportDto
            {
                Code = s.Code,
                Name = s.Name,
                Level = s.Level.ToString(),
                SubjectType = s.SubjectType.ToString(),
                IsActive = s.IsActive,
                SchoolId = s.TenantId,
                SchoolName = schoolMap.TryGetValue(s.TenantId, out var name) ? name : string.Empty
            }).ToList();

            var document = new SubjectsListReportDocument(
                school: null,
                subjects: subjects,
                logoBytes: null,
                isSuperAdmin: true);

            return document.ExportToPdfBytes();
        }

        // ── Private helpers ────────────────────────────────────────────────
        private async Task<byte[]?> ResolveLogoAsync(string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
                return null;

            var logoPath = Path.Combine(_env.WebRootPath, logoUrl.TrimStart('/'));
            if (!File.Exists(logoPath))
                return null;

            return await File.ReadAllBytesAsync(logoPath);
        }

        // ── Map assessment entities to common base ─────────────────────────
        private AssessmentBase MapToAssessmentBase(FormativeAssessment a) => new AssessmentBase
        {
            TenantId = a.TenantId,
            Title = a.Title,
            AssessmentType = (AssessmentType)AssessmentTypeDto.Competency,
            TeacherName = a.Teacher?.FullName,
            SubjectName = a.Subject?.Name,
            ClassName = a.Class?.Name,
            TermName = a.Term?.Name,
            AssessmentDate = a.AssessmentDate,
            MaximumScore = (int)a.MaximumScore,
            IsPublished = a.IsPublished,
            ScoreCount = a.Scores?.Count ?? 0
        };

        private AssessmentBase MapToAssessmentBase(SummativeAssessment a) => new AssessmentBase
        {
            TenantId = a.TenantId,
            Title = a.Title,
            AssessmentType = (AssessmentType)AssessmentTypeDto.Summative,
            TeacherName = a.Teacher?.FullName,
            SubjectName = a.Subject?.Name,
            ClassName = a.Class?.Name,
            TermName = a.Term?.Name,
            AssessmentDate = a.AssessmentDate,
            MaximumScore = (int)a.MaximumScore,
            IsPublished = a.IsPublished,
            ScoreCount = a.Scores?.Count ?? 0
        };

        private AssessmentBase MapToAssessmentBase(CompetencyAssessment a) => new AssessmentBase
        {
            TenantId = a.TenantId,
            Title = a.Title,
            AssessmentType = (AssessmentType)AssessmentTypeDto.Competency,
            TeacherName = a.Teacher?.FullName,
            SubjectName = a.Subject?.Name,
            ClassName = a.Class?.Name,
            TermName = a.Term?.Name,
            AssessmentDate = a.AssessmentDate,
            MaximumScore = (int)a.MaximumScore,
            IsPublished = a.IsPublished,
            ScoreCount = a.Scores?.Count ?? 0
        };

        internal  class AssessmentBase
        {
            public Guid TenantId { get; set; }
            public string Title { get; set; } = string.Empty;
            public AssessmentType AssessmentType { get; set; }
            public string? TeacherName { get; set; }
            public string? SubjectName { get; set; }
            public string? ClassName { get; set; }
            public string? TermName { get; set; }
            public DateTime AssessmentDate { get; set; }
            public int MaximumScore { get; set; }
            public bool IsPublished { get; set; }
            public int ScoreCount { get; set; }
        }
    }
}