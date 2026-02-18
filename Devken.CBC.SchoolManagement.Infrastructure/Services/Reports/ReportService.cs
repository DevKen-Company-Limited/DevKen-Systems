using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Reports;
using Devken.CBC.SchoolManagement.Application.Service.Administration.Student;
using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Academic;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Reports.Student;
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

        // ── Single-school report ───────────────────────────────────────────
        public async Task<byte[]> GenerateStudentsListReportAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            // SuperAdmin uses whichever schoolId they supplied;
            // regular users are always locked to their own school.
            var finalSchoolId = isSuperAdmin ? schoolId : userSchoolId;
            if (finalSchoolId == null)
                throw new InvalidOperationException("School context not found.");

            var school = await _repositories.School
                .GetByIdAsync(finalSchoolId.Value, trackChanges: false)
                ?? throw new KeyNotFoundException($"School {finalSchoolId.Value} not found.");

            // ── Logo: resolve URL stored in DB to a physical wwwroot path ─
            byte[]? logoBytes = await ResolveLogoAsync(school.LogoUrl);

            // ── Students ──────────────────────────────────────────────────
            var studentsData = await _studentService.GetAllStudentsAsync(
                finalSchoolId, userSchoolId, isSuperAdmin);

            var students = studentsData.Select(s => new StudentDto
            {
                AdmissionNumber = s.AdmissionNumber,
                FullName = s.FullName,
                CurrentClassName = s.CurrentClassName,
                IsActive = s.IsActive
                // SchoolName intentionally left empty for single-school reports
            }).ToList();

            var document = new StudentsListReportDocument(
                school: school,
                students: students,
                logoBytes: logoBytes,
                isSuperAdmin: isSuperAdmin);

            return document.ExportToPdfBytes();
        }

        // ── All-schools report (SuperAdmin only) ───────────────────────────
        public async Task<byte[]> GenerateAllSchoolsStudentsListReportAsync()
        {
            // Fetch all schools so we can enrich each student row with its school name.
            var allSchools = await _repositories.School
                .GetAllAsync(trackChanges: false);

            var schoolMap = allSchools.ToDictionary(s => s.Id, s => s.Name);

            // Fetch every student across all schools via the existing service method.
            // Passing null schoolId + isSuperAdmin=true should return all students —
            // adjust if your IStudentService has a more specific "get all" overload.
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
                SchoolName = s.SchoolId != Guid.Empty &&
                             schoolMap.TryGetValue(s.SchoolId, out var name)
                                ? name
                                : s.SchoolName ?? string.Empty
            }).ToList();


            // school = null → triggers "All Schools" header + watermark in the document
            var document = new StudentsListReportDocument(
                school: null,
                students: students,
                logoBytes: null,   // no single logo for cross-school reports
                isSuperAdmin: true);

            return document.ExportToPdfBytes();
        }

        // ── Private helpers ────────────────────────────────────────────────

        /// <summary>
        /// Resolves a logo URL saved in the database (e.g. "/uploads/logos/school.png")
        /// to an absolute file path under wwwroot and reads the bytes.
        /// Returns <c>null</c> if the URL is empty or the file does not exist.
        /// </summary>
        private async Task<byte[]?> ResolveLogoAsync(string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
                return null;

            var logoPath = Path.Combine(_env.WebRootPath, logoUrl.TrimStart('/'));

            if (!File.Exists(logoPath))
                return null;

            return await File.ReadAllBytesAsync(logoPath);
        }
    }
}