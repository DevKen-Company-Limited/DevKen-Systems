using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // MAPPERS — file-scoped static helpers
    // ═══════════════════════════════════════════════════════════════════

    internal static class CompetencyAssessmentMapper
    {
        internal static CompetencyAssessmentDto ToDto(
            CompetencyAssessment a,
            IEnumerable<CompetencyAssessmentScore>? scores = null) => new()
            {
                // ── Base ────────────────────────────────────────────────────
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                AssessmentType = a.AssessmentType,
                MaximumScore = a.MaximumScore,
                AssessmentDate = a.AssessmentDate,
                IsPublished = a.IsPublished,
                PublishedDate = a.PublishedDate,
                CreatedOn = a.CreatedOn,
                SchoolId = a.TenantId,

                // ── Navigations ─────────────────────────────────────────────
                TeacherId = a.TeacherId,
                TeacherName = a.Teacher != null
                                   ? $"{a.Teacher.FirstName} {a.Teacher.LastName}".Trim()
                                   : null,
                SubjectId = a.SubjectId,
                SubjectName = a.Subject?.Name,
                ClassId = a.ClassId,
                ClassName = a.Class?.Name,
                TermId = a.TermId,
                TermName = a.Term?.Name,
                AcademicYearId = a.AcademicYearId,
                AcademicYearName = a.AcademicYear?.Name,

                // ── Competency-specific ─────────────────────────────────────
                CompetencyName = a.CompetencyName,
                Strand = a.Strand,
                SubStrand = a.SubStrand,
                TargetLevel = a.TargetLevel,
                PerformanceIndicators = a.PerformanceIndicators,
                AssessmentMethod = a.AssessmentMethod,
                RatingScale = a.RatingScale,
                IsObservationBased = a.IsObservationBased,
                ToolsRequired = a.ToolsRequired,
                Instructions = a.Instructions,
                SpecificLearningOutcome = a.SpecificLearningOutcome,

                // ── Scores (optional) ───────────────────────────────────────
                Scores = scores?.Select(CompetencyScoreMapper.ToDto)
                  ?? a.Scores?.Select(CompetencyScoreMapper.ToDto),
            };
    }

    internal static class CompetencyScoreMapper
    {
        internal static CompetencyAssessmentScoreDto ToDto(CompetencyAssessmentScore s) => new()
        {
            Id = s.Id,
            CompetencyAssessmentId = s.CompetencyAssessmentId,
            AssessmentTitle = s.CompetencyAssessment?.Title,
            CompetencyName = s.CompetencyAssessment?.CompetencyName,
            StudentId = s.StudentId,
            StudentName = s.Student != null
                                          ? $"{s.Student.FirstName} {s.Student.LastName}".Trim()
                                          : null,
            AssessorId = s.AssessorId,
            AssessorName = s.Assessor != null
                                          ? $"{s.Assessor.FirstName} {s.Assessor.LastName}".Trim()
                                          : null,
            Rating = s.Rating,
            CompetencyLevel = s.CompetencyLevel,   // computed property on entity
            ScoreValue = s.ScoreValue,
            Evidence = s.Evidence,
            AssessmentDate = s.AssessmentDate,
            AssessmentMethod = s.AssessmentMethod,
            ToolsUsed = s.ToolsUsed,
            Feedback = s.Feedback,
            AreasForImprovement = s.AreasForImprovement,
            IsFinalized = s.IsFinalized,
            Strand = s.Strand,
            SubStrand = s.SubStrand,
            SpecificLearningOutcome = s.SpecificLearningOutcome,
            CreatedOn = s.CreatedOn,
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT — Service Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class CompetencyAssessmentService : ICompetencyAssessmentService
    {
        private readonly ICompetencyAssessmentRepository _repo;
        private readonly IRepositoryManager _unitOfWork;

        public CompetencyAssessmentService(
            ICompetencyAssessmentRepository repo,
            IRepositoryManager unitOfWork)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetAllAsync(Guid? schoolId)
        {
            var list = schoolId.HasValue
                ? await _repo.GetBySchoolAsync(schoolId.Value, trackChanges: false)
                : await _repo.GetAllAsync(trackChanges: false);

            return list.Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        public async Task<CompetencyAssessmentDto?> GetByIdAsync(Guid id, Guid? schoolId)
        {
            var a = await _repo.GetByIdAsync(id, trackChanges: false);
            if (a == null) return null;
            if (schoolId.HasValue && a.TenantId != schoolId.Value) return null;
            return CompetencyAssessmentMapper.ToDto(a);
        }

        public async Task<CompetencyAssessmentDto?> GetWithScoresAsync(Guid id, Guid? schoolId)
        {
            var a = await _repo.GetWithScoresAsync(id, trackChanges: false);
            if (a == null) return null;
            if (schoolId.HasValue && a.TenantId != schoolId.Value) return null;
            return CompetencyAssessmentMapper.ToDto(a);
        }

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetByClassAsync(
            Guid classId, Guid? schoolId)
        {
            var list = await _repo.GetByClassAsync(classId, trackChanges: false);
            return Filter(list, schoolId).Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetByTeacherAsync(
            Guid teacherId, Guid? schoolId)
        {
            var list = await _repo.GetByTeacherAsync(teacherId, trackChanges: false);
            return Filter(list, schoolId).Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetByTermAsync(
            Guid termId, Guid academicYearId, Guid? schoolId)
        {
            var list = await _repo.GetByTermAsync(termId, academicYearId, trackChanges: false);
            return Filter(list, schoolId).Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetByCompetencyNameAsync(
            string competencyName, Guid schoolId)
        {
            var list = await _repo.GetByCompetencyNameAsync(competencyName, schoolId, trackChanges: false);
            return list.Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetByTargetLevelAsync(
            CBCLevel level, Guid schoolId)
        {
            var list = await _repo.GetByTargetLevelAsync(level, schoolId, trackChanges: false);
            return list.Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetByStrandAsync(
            string strand, Guid schoolId)
        {
            var list = await _repo.GetByStrandAsync(strand, schoolId, trackChanges: false);
            return list.Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<CompetencyAssessmentDto>> GetPublishedAsync(
            Guid classId, Guid termId, Guid? schoolId)
        {
            var list = await _repo.GetPublishedAsync(classId, termId, trackChanges: false);
            return Filter(list, schoolId).Select(a => CompetencyAssessmentMapper.ToDto(a));
        }

        // ── Write ───────────────────────────────────────────────────────

        public async Task<CompetencyAssessmentDto> CreateAsync(
            CreateCompetencyAssessmentRequest req, Guid schoolId)
        {
            var entity = new CompetencyAssessment
            {
                Id = Guid.NewGuid(),
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                AssessmentType = "Competency",
                MaximumScore = req.MaximumScore,
                AssessmentDate = req.AssessmentDate,
                IsPublished = false,
                TenantId = schoolId,
                CreatedOn = DateTime.UtcNow,

                TeacherId = req.TeacherId ?? Guid.Empty,
                SubjectId = req.SubjectId ?? Guid.Empty,
                ClassId = req.ClassId ?? Guid.Empty,
                TermId = req.TermId ?? Guid.Empty,
                AcademicYearId = req.AcademicYearId ?? Guid.Empty,

                CompetencyName = req.CompetencyName.Trim(),
                Strand = req.Strand?.Trim(),
                SubStrand = req.SubStrand?.Trim(),
                TargetLevel = req.TargetLevel,
                PerformanceIndicators = req.PerformanceIndicators?.Trim(),
                AssessmentMethod = req.AssessmentMethod,
                RatingScale = req.RatingScale?.Trim(),
                IsObservationBased = req.IsObservationBased,
                ToolsRequired = req.ToolsRequired?.Trim(),
                Instructions = req.Instructions?.Trim(),
                SpecificLearningOutcome = req.SpecificLearningOutcome?.Trim(),
            };

            _repo.Create(entity);
            await _unitOfWork.SaveAsync();

            // Re-fetch with navigations for a complete DTO
            var created = await _repo.GetByIdAsync(entity.Id, trackChanges: false);
            return CompetencyAssessmentMapper.ToDto(created!);
        }

        public async Task<CompetencyAssessmentDto> UpdateAsync(
            Guid id, UpdateCompetencyAssessmentRequest req, Guid? schoolId)
        {
            var entity = await GetEntityOrThrowAsync(id, schoolId, trackChanges: true);

            entity.Title = req.Title.Trim();
            entity.Description = req.Description?.Trim();
            entity.MaximumScore = req.MaximumScore;
            entity.AssessmentDate = req.AssessmentDate;
            entity.TeacherId = req.TeacherId ?? Guid.Empty;
            entity.SubjectId = req.SubjectId ?? Guid.Empty;
            entity.ClassId = req.ClassId ?? Guid.Empty;
            entity.TermId = req.TermId ?? Guid.Empty;
            entity.AcademicYearId = req.AcademicYearId ?? Guid.Empty;

            entity.CompetencyName = req.CompetencyName.Trim();
            entity.Strand = req.Strand?.Trim();
            entity.SubStrand = req.SubStrand?.Trim();
            entity.TargetLevel = req.TargetLevel;
            entity.PerformanceIndicators = req.PerformanceIndicators?.Trim();
            entity.AssessmentMethod = req.AssessmentMethod;
            entity.RatingScale = req.RatingScale?.Trim();
            entity.IsObservationBased = req.IsObservationBased;
            entity.ToolsRequired = req.ToolsRequired?.Trim();
            entity.Instructions = req.Instructions?.Trim();
            entity.SpecificLearningOutcome = req.SpecificLearningOutcome?.Trim();

            _repo.Update(entity);
            await _unitOfWork.SaveAsync();

            var updated = await _repo.GetByIdAsync(entity.Id, trackChanges: false);
            return CompetencyAssessmentMapper.ToDto(updated!);
        }

        public async Task PublishAsync(Guid id, bool publish, Guid? schoolId)
        {
            var entity = await GetEntityOrThrowAsync(id, schoolId, trackChanges: true);
            entity.IsPublished = publish;
            entity.PublishedDate = publish ? DateTime.UtcNow : null;
            _repo.Update(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? schoolId)
        {
            var entity = await GetEntityOrThrowAsync(id, schoolId, trackChanges: true);
            _repo.Delete(entity);
            await _unitOfWork.SaveAsync();
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static IEnumerable<CompetencyAssessment> Filter(
            IEnumerable<CompetencyAssessment> list, Guid? schoolId)
            => schoolId.HasValue ? list.Where(a => a.TenantId == schoolId.Value) : list;

        private async Task<CompetencyAssessment> GetEntityOrThrowAsync(
            Guid id, Guid? schoolId, bool trackChanges)
        {
            var entity = await _repo.GetByIdAsync(id, trackChanges);
            if (entity == null)
                throw new KeyNotFoundException($"CompetencyAssessment {id} not found.");
            if (schoolId.HasValue && entity.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this assessment.");
            return entity;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // COMPETENCY ASSESSMENT SCORE — Service Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class CompetencyAssessmentScoreService : ICompetencyAssessmentScoreService
    {
        private readonly ICompetencyAssessmentScoreRepository _scoreRepo;
        private readonly ICompetencyAssessmentRepository _assessmentRepo;
        private readonly IRepositoryManager _unitOfWork;

        public CompetencyAssessmentScoreService(
            ICompetencyAssessmentScoreRepository scoreRepo,
            ICompetencyAssessmentRepository assessmentRepo,
            IRepositoryManager unitOfWork)
        {
            _scoreRepo = scoreRepo ?? throw new ArgumentNullException(nameof(scoreRepo));
            _assessmentRepo = assessmentRepo ?? throw new ArgumentNullException(nameof(assessmentRepo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByAssessmentAsync(
            Guid assessmentId, Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(assessmentId, schoolId);
            var scores = await _scoreRepo.GetAllByAssessmentAsync(assessmentId, trackChanges: false);
            return scores.Select(CompetencyScoreMapper.ToDto);
        }

        public async Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByStudentAsync(
            Guid studentId, Guid? schoolId)
        {
            var scores = await _scoreRepo.GetByStudentAsync(studentId, trackChanges: false);
            return FilterScores(scores, schoolId).Select(CompetencyScoreMapper.ToDto);
        }

        public async Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, Guid? schoolId)
        {
            var scores = await _scoreRepo.GetByStudentAndTermAsync(studentId, termId, trackChanges: false);
            return FilterScores(scores, schoolId).Select(CompetencyScoreMapper.ToDto);
        }

        public async Task<CompetencyAssessmentScoreDto?> GetByIdAsync(Guid id, Guid? schoolId)
        {
            var score = await _scoreRepo.GetByIdAsync(id, trackChanges: false);
            if (score == null) return null;
            if (schoolId.HasValue
                && score.CompetencyAssessment != null
                && score.CompetencyAssessment.TenantId != schoolId.Value) return null;
            return CompetencyScoreMapper.ToDto(score);
        }

        public async Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByRatingAsync(
            Guid assessmentId, string rating, Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(assessmentId, schoolId);
            var scores = await _scoreRepo.GetByRatingAsync(assessmentId, rating, trackChanges: false);
            return scores.Select(CompetencyScoreMapper.ToDto);
        }

        public async Task<IEnumerable<CompetencyAssessmentScoreDto>> GetByAssessorAsync(
            Guid assessorId, Guid? schoolId)
        {
            var scores = await _scoreRepo.GetByAssessorAsync(assessorId, trackChanges: false);
            return FilterScores(scores, schoolId).Select(CompetencyScoreMapper.ToDto);
        }

        // ── Write ───────────────────────────────────────────────────────

        public async Task<CompetencyAssessmentScoreDto> CreateAsync(
            CreateCompetencyAssessmentScoreRequest req,
            Guid? assessorId,
            Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(req.CompetencyAssessmentId, schoolId);

            // Prevent duplicate score for same student + assessment
            var existing = await _scoreRepo.GetByStudentAndAssessmentAsync(
                req.StudentId, req.CompetencyAssessmentId, trackChanges: false);

            if (existing != null)
                throw new InvalidOperationException(
                    $"A score already exists for student {req.StudentId} " +
                    $"on assessment {req.CompetencyAssessmentId}. Use Update instead.");

            var entity = new CompetencyAssessmentScore
            {
                Id = Guid.NewGuid(),
                CompetencyAssessmentId = req.CompetencyAssessmentId,
                StudentId = req.StudentId,
                AssessorId = assessorId,
                Rating = req.Rating.Trim(),
                ScoreValue = req.ScoreValue,
                Evidence = req.Evidence?.Trim(),
                AssessmentDate = req.AssessmentDate,
                AssessmentMethod = req.AssessmentMethod?.Trim(),
                ToolsUsed = req.ToolsUsed?.Trim(),
                Feedback = req.Feedback?.Trim(),
                AreasForImprovement = req.AreasForImprovement?.Trim(),
                IsFinalized = false,
                Strand = req.Strand?.Trim(),
                SubStrand = req.SubStrand?.Trim(),
                SpecificLearningOutcome = req.SpecificLearningOutcome?.Trim(),
                CreatedOn = DateTime.UtcNow,
            };

            _scoreRepo.Create(entity);
            await _unitOfWork.SaveAsync();

            var created = await _scoreRepo.GetByIdAsync(entity.Id, trackChanges: false);
            return CompetencyScoreMapper.ToDto(created!);
        }

        public async Task<CompetencyAssessmentScoreDto> UpdateAsync(
            Guid id,
            UpdateCompetencyAssessmentScoreRequest req,
            Guid? assessorId,
            Guid? schoolId)
        {
            var entity = await GetScoreOrThrowAsync(id, schoolId, trackChanges: true);

            entity.Rating = req.Rating.Trim();
            entity.ScoreValue = req.ScoreValue;
            entity.Evidence = req.Evidence?.Trim();
            entity.AssessmentDate = req.AssessmentDate;
            entity.AssessmentMethod = req.AssessmentMethod?.Trim();
            entity.ToolsUsed = req.ToolsUsed?.Trim();
            entity.Feedback = req.Feedback?.Trim();
            entity.AreasForImprovement = req.AreasForImprovement?.Trim();
            entity.AssessorId = assessorId ?? entity.AssessorId;
            entity.Strand = req.Strand?.Trim();
            entity.SubStrand = req.SubStrand?.Trim();
            entity.SpecificLearningOutcome = req.SpecificLearningOutcome?.Trim();

            _scoreRepo.Update(entity);
            await _unitOfWork.SaveAsync();

            var updated = await _scoreRepo.GetByIdAsync(entity.Id, trackChanges: false);
            return CompetencyScoreMapper.ToDto(updated!);
        }

        public async Task BulkFinalizeAsync(
            BulkFinalizeCompetencyScoresRequest request, Guid? schoolId)
        {
            var now = DateTime.UtcNow;

            foreach (var item in request.Scores)
            {
                var entity = await GetScoreOrThrowAsync(item.ScoreId, schoolId, trackChanges: true);
                entity.IsFinalized = item.IsFinalized;
                _scoreRepo.Update(entity);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? schoolId)
        {
            var entity = await GetScoreOrThrowAsync(id, schoolId, trackChanges: true);
            _scoreRepo.Delete(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteByAssessmentAsync(Guid assessmentId, Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(assessmentId, schoolId);
            var scores = await _scoreRepo.GetAllByAssessmentAsync(assessmentId, trackChanges: true);
            _scoreRepo.DeleteRange(scores);
            await _unitOfWork.SaveAsync();
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static IEnumerable<CompetencyAssessmentScore> FilterScores(
            IEnumerable<CompetencyAssessmentScore> list, Guid? schoolId)
            => schoolId.HasValue
                ? list.Where(s => s.CompetencyAssessment?.TenantId == schoolId.Value)
                : list;

        private async Task EnsureAssessmentAccessAsync(Guid assessmentId, Guid? schoolId)
        {
            var a = await _assessmentRepo.GetByIdAsync(assessmentId, trackChanges: false);
            if (a == null)
                throw new KeyNotFoundException($"CompetencyAssessment {assessmentId} not found.");
            if (schoolId.HasValue && a.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this assessment.");
        }

        private async Task<CompetencyAssessmentScore> GetScoreOrThrowAsync(
            Guid id, Guid? schoolId, bool trackChanges)
        {
            var entity = await _scoreRepo.GetByIdAsync(id, trackChanges);
            if (entity == null)
                throw new KeyNotFoundException($"CompetencyAssessmentScore {id} not found.");
            if (schoolId.HasValue
                && entity.CompetencyAssessment != null
                && entity.CompetencyAssessment.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this score.");
            return entity;
        }
    }
}