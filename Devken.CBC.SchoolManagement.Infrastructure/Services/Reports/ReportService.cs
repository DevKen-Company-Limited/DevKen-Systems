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
            _repositories = repositories;
            _studentService = studentService;
            _env = env;
        }

        public async Task<byte[]> GenerateStudentsListReportAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin)
        {
            var finalSchoolId = isSuperAdmin ? schoolId : userSchoolId;

            if (finalSchoolId == null)
                throw new Exception("School context not found.");

            var school = await _repositories.School
                .GetByIdAsync(finalSchoolId.Value, trackChanges: false);

            if (school == null)
                throw new Exception("School not found.");

            // Get students
            var studentsData = await _studentService.GetAllStudentsAsync(
                finalSchoolId, userSchoolId, isSuperAdmin);

            var students = studentsData.Select(s => new StudentDto
            {
                AdmissionNumber = s.AdmissionNumber,
                FullName = s.FullName,
                CurrentClassName = s.CurrentClassName,
                IsActive = s.IsActive
            }).ToList();

            // Resolve logo
            byte[]? logoBytes = null;
            if (!string.IsNullOrWhiteSpace(school.LogoUrl))
            {
                var logoPath = Path.Combine(_env.WebRootPath, school.LogoUrl.TrimStart('/'));
                if (File.Exists(logoPath))
                    logoBytes = await File.ReadAllBytesAsync(logoPath);
            }

            // ✅ DevExpress XtraReport — no QuestPDF, no Element() calls
            var document = new StudentsListReportDocument(school, students, logoBytes);
            return document.ExportToPdfBytes();
        }
    }
}