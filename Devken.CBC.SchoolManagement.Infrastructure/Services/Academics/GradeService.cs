using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Academics;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments.Academic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Academics
{
    public class GradeService : IGradeService
    {
        private readonly IRepositoryManager _repositories;

        public GradeService(IRepositoryManager repositories)
        {
            _repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<GradeResponseDto>> GetAllGradesAsync(
            Guid? schoolId,
            Guid? userSchoolId,
            bool isSuperAdmin,
            Guid? studentId = null,
            Guid? subjectId = null,
            Guid? termId = null)
        {
            IEnumerable<Grade> grades;

            if (isSuperAdmin)
            {
                grades = schoolId.HasValue
                    ? await _repositories.Grade.GetByTenantIdAsync(schoolId.Value, trackChanges: false)
                    : await _repositories.Grade.GetAllAsync(trackChanges: false);
            }
            else
            {
                if (!userSchoolId.HasValue)
                    throw new UnauthorizedException("You must be assigned to a school to view grades.");

                grades = await _repositories.Grade.GetByTenantIdAsync(userSchoolId.Value, trackChanges: false);
            }

            // Optional filters
            if (studentId.HasValue)
                grades = grades.Where(g => g.StudentId == studentId.Value);
            if (subjectId.HasValue)
                grades = grades.Where(g => g.SubjectId == subjectId.Value);
            if (termId.HasValue)
                grades = grades.Where(g => g.TermId == termId.Value);

            var gradeList = grades.ToList();

            var schoolNameMap = await BuildSchoolNameMapAsync(
                gradeList.Select(g => g.TenantId).Distinct());

            return gradeList.Select(g => MapToDto(g, schoolNameMap.GetValueOrDefault(g.TenantId)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<GradeResponseDto> GetGradeByIdAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var grade = await _repositories.Grade.GetByIdWithDetailsAsync(id, trackChanges: false)
                ?? throw new NotFoundException($"Grade with ID '{id}' not found.");

            ValidateAccess(grade.TenantId, userSchoolId, isSuperAdmin);

            var schoolName = await ResolveSchoolNameAsync(grade.TenantId);
            return MapToDto(grade, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<GradeResponseDto> CreateGradeAsync(
            CreateGradeDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var strategy = _repositories.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var tenantId = ResolveTenantId(dto.TenantId, userSchoolId, isSuperAdmin);

                var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false)
                    ?? throw new NotFoundException($"School with ID '{tenantId}' not found.");

                // Validate student belongs to the tenant
                var student = await _repositories.Student.GetByIdAsync(dto.StudentId, trackChanges: false)
                    ?? throw new NotFoundException($"Student with ID '{dto.StudentId}' not found.");

                if (student.TenantId != tenantId)
                    throw new UnauthorizedException("Student does not belong to this school.");

                // Validate subject belongs to the tenant
                var subject = await _repositories.Subject.GetByIdAsync(dto.SubjectId, trackChanges: false)
                    ?? throw new NotFoundException($"Subject with ID '{dto.SubjectId}' not found.");

                if (subject.TenantId != tenantId)
                    throw new UnauthorizedException("Subject does not belong to this school.");

                // Check for duplicate grade (same student + subject + term)
                if (await _repositories.Grade.ExistsByStudentSubjectTermAsync(
                        dto.StudentId, dto.SubjectId, dto.TermId))
                    throw new ConflictException(
                        "A grade already exists for this student, subject, and term combination.");

                var grade = new Grade
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = dto.StudentId,
                    SubjectId = dto.SubjectId,
                    TermId = dto.TermId,
                    AssessmentId = dto.AssessmentId,
                    Score = dto.Score,
                    MaximumScore = dto.MaximumScore,
                    GradeLetter = dto.GradeLetter,
                    GradeType = dto.GradeType,
                    AssessmentDate = dto.AssessmentDate,
                    Remarks = dto.Remarks,
                    IsFinalized = dto.IsFinalized,
                };

                _repositories.Grade.Create(grade);
                await _repositories.SaveAsync();

                // Reload with navigation properties for response
                var created = await _repositories.Grade.GetByIdWithDetailsAsync(grade.Id, trackChanges: false)
                    ?? grade;

                return MapToDto(created, school.Name);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<GradeResponseDto> UpdateGradeAsync(
            Guid id, UpdateGradeDto dto, Guid? userSchoolId, bool isSuperAdmin)
        {
            var existing = await _repositories.Grade.GetByIdWithDetailsAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Grade with ID '{id}' not found.");

            ValidateAccess(existing.TenantId, userSchoolId, isSuperAdmin);

            if (existing.IsFinalized)
                throw new ValidationException("Cannot update a finalized grade.");

            existing.Score = dto.Score;
            existing.MaximumScore = dto.MaximumScore;
            existing.GradeLetter = dto.GradeLetter;
            existing.GradeType = dto.GradeType;
            existing.AssessmentDate = dto.AssessmentDate;
            existing.Remarks = dto.Remarks;
            existing.IsFinalized = dto.IsFinalized;

            _repositories.Grade.Update(existing);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(existing.TenantId);
            return MapToDto(existing, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteGradeAsync(Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var grade = await _repositories.Grade.GetByIdAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Grade with ID '{id}' not found.");

            ValidateAccess(grade.TenantId, userSchoolId, isSuperAdmin);

            if (grade.IsFinalized)
                throw new ValidationException("Cannot delete a finalized grade.");

            _repositories.Grade.Delete(grade);
            await _repositories.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // FINALIZE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<GradeResponseDto> FinalizeGradeAsync(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var grade = await _repositories.Grade.GetByIdWithDetailsAsync(id, trackChanges: true)
                ?? throw new NotFoundException($"Grade with ID '{id}' not found.");

            ValidateAccess(grade.TenantId, userSchoolId, isSuperAdmin);

            if (grade.IsFinalized)
                throw new ConflictException("Grade is already finalized.");

            grade.IsFinalized = true;
            _repositories.Grade.Update(grade);
            await _repositories.SaveAsync();

            var schoolName = await ResolveSchoolNameAsync(grade.TenantId);
            return MapToDto(grade, schoolName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private async Task<string?> ResolveSchoolNameAsync(Guid tenantId)
        {
            var school = await _repositories.School.GetByIdAsync(tenantId, trackChanges: false);
            return school?.Name;
        }

        private async Task<Dictionary<Guid, string>> BuildSchoolNameMapAsync(
            IEnumerable<Guid> tenantIds)
        {
            var map = new Dictionary<Guid, string>();
            foreach (var tid in tenantIds)
            {
                var school = await _repositories.School.GetByIdAsync(tid, trackChanges: false);
                if (school != null)
                    map[tid] = school.Name;
            }
            return map;
        }

        private Guid ResolveTenantId(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin)
            {
                if (!requestTenantId.HasValue || requestTenantId == Guid.Empty)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "TenantId is required for SuperAdmin when creating a grade.");
                return requestTenantId.Value;
            }

            if (!userSchoolId.HasValue || userSchoolId == Guid.Empty)
                throw new UnauthorizedException("You must be assigned to a school to create grades.");

            return userSchoolId.Value;
        }

        private void ValidateAccess(Guid gradeTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;
            if (!userSchoolId.HasValue || gradeTenantId != userSchoolId.Value)
                throw new UnauthorizedException("You do not have access to this grade.");
        }

        private static GradeResponseDto MapToDto(Grade g, string? schoolName = null)
        {
            decimal? percentage = g.MaximumScore > 0
                ? Math.Round((g.Score ?? 0) / g.MaximumScore!.Value * 100, 2)
                : null;

            return new GradeResponseDto
            {
                Id = (Guid)g.Id!,
                StudentId = g.StudentId,
                StudentName = g.Student != null
                    ? $"{g.Student.FirstName} {g.Student.LastName}".Trim()
                    : null,
                SubjectId = g.SubjectId,
                SubjectName = g.Subject?.Name,
                TermId = g.TermId,
                TermName = g.Term?.Name,
                AssessmentId = g.AssessmentId,
                Score = g.Score,
                MaximumScore = g.MaximumScore,
                Percentage = percentage,
                GradeLetter = g.GradeLetter?.ToString(),
                GradeType = g.GradeType?.ToString(),
                AssessmentDate = g.AssessmentDate,
                Remarks = g.Remarks,
                IsFinalized = g.IsFinalized,
                TenantId = g.TenantId,
                SchoolName = schoolName,
                Status = g.Status.ToString(),
                CreatedOn = g.CreatedOn,
                UpdatedOn = g.UpdatedOn,
            };
        }
    }
}