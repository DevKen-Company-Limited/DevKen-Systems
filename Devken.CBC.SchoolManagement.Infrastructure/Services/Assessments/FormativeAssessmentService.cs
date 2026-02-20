using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Assessments
{
    // ═══════════════════════════════════════════════════════════════════
    // HELPERS — shared mapping methods (file-scoped)
    // ═══════════════════════════════════════════════════════════════════

    internal static class FormativeAssessmentMapper
    {
        internal static FormativeAssessmentDto ToDto(
            FormativeAssessment a,
            IEnumerable<FormativeAssessmentScore>? scores = null) => new()
            {
                // Base
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

                // Navigations
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

                // Formative-specific
                FormativeType = a.FormativeType,
                CompetencyArea = a.CompetencyArea,
                LearningOutcomeId = a.LearningOutcomeId,
                LearningOutcomeName = a.LearningOutcome?.Outcome,
                FeedbackTemplate = a.FeedbackTemplate,
                RequiresRubric = a.RequiresRubric,
                Strand = a.Strand,
                SubStrand = a.SubStrand,
                Criteria = a.Criteria,
                Instructions = a.Instructions,
                AssessmentWeight = a.AssessmentWeight,

                // Scores (optional)
                Scores = scores?.Select(ScoreMapper.ToDto)
                  ?? a.Scores?.Select(ScoreMapper.ToDto),
            };
    }

    internal static class ScoreMapper
    {
        internal static FormativeAssessmentScoreDto ToDto(FormativeAssessmentScore s) => new()
        {
            Id = s.Id,
            FormativeAssessmentId = s.FormativeAssessmentId,
            AssessmentTitle = s.FormativeAssessment?.Title,
            StudentId = s.StudentId,
            StudentName = s.Student != null
                                       ? $"{s.Student.FirstName} {s.Student.LastName}".Trim()
                                       : null,
            Score = s.Score,
            MaximumScore = s.MaximumScore,
            Percentage = s.Percentage,
            Grade = s.Grade,
            PerformanceLevel = s.PerformanceLevel,
            Feedback = s.Feedback,
            Strengths = s.Strengths,
            AreasForImprovement = s.AreasForImprovement,
            IsSubmitted = s.IsSubmitted,
            SubmissionDate = s.SubmissionDate,
            GradedDate = s.GradedDate,
            GradedById = s.GradedById,
            GradedByName = s.GradedBy != null
                                       ? $"{s.GradedBy.FirstName} {s.GradedBy.LastName}".Trim()
                                       : null,
            CompetencyArea = s.CompetencyArea,
            CompetencyAchieved = s.CompetencyAchieved,
            CreatedOn = s.CreatedOn,
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT — Service Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class FormativeAssessmentService : IFormativeAssessmentService
    {
        private readonly IFormativeAssessmentRepository _repo;
        private readonly IRepositoryManager _unitOfWork;

        public FormativeAssessmentService(
            IFormativeAssessmentRepository repo,
            IRepositoryManager unitOfWork)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<FormativeAssessmentDto>> GetAllAsync(Guid? schoolId)
        {
            var list = schoolId.HasValue
                ? await _repo.GetBySchoolAsync(schoolId.Value, trackChanges: false)
                : await _repo.GetAllAsync(trackChanges: false);

            return list.Select(a => FormativeAssessmentMapper.ToDto(a));
        }

        public async Task<FormativeAssessmentDto?> GetByIdAsync(Guid id, Guid? schoolId)
        {
            var a = await _repo.GetByIdAsync(id, trackChanges: false);
            if (a == null) return null;
            if (schoolId.HasValue && a.TenantId != schoolId.Value) return null;
            return FormativeAssessmentMapper.ToDto(a);
        }

        public async Task<FormativeAssessmentDto?> GetWithScoresAsync(Guid id, Guid? schoolId)
        {
            var a = await _repo.GetWithScoresAsync(id, trackChanges: false);
            if (a == null) return null;
            if (schoolId.HasValue && a.TenantId != schoolId.Value) return null;
            return FormativeAssessmentMapper.ToDto(a);
        }

        public async Task<IEnumerable<FormativeAssessmentDto>> GetByClassAsync(
            Guid classId, Guid? schoolId)
        {
            var list = await _repo.GetByClassAsync(classId, trackChanges: false);
            return Filter(list, schoolId).Select(a => FormativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<FormativeAssessmentDto>> GetByTeacherAsync(
            Guid teacherId, Guid? schoolId)
        {
            var list = await _repo.GetByTeacherAsync(teacherId, trackChanges: false);
            return Filter(list, schoolId).Select(a => FormativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<FormativeAssessmentDto>> GetByTermAsync(
            Guid termId, Guid academicYearId, Guid? schoolId)
        {
            var list = await _repo.GetByTermAsync(termId, academicYearId, trackChanges: false);
            return Filter(list, schoolId).Select(a => FormativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<FormativeAssessmentDto>> GetByLearningOutcomeAsync(
            Guid learningOutcomeId, Guid? schoolId)
        {
            var list = await _repo.GetByLearningOutcomeAsync(learningOutcomeId, trackChanges: false);
            return Filter(list, schoolId).Select(a => FormativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<FormativeAssessmentDto>> GetPublishedAsync(
            Guid classId, Guid termId, Guid? schoolId)
        {
            var list = await _repo.GetPublishedAsync(classId, termId, trackChanges: false);
            return Filter(list, schoolId).Select(a => FormativeAssessmentMapper.ToDto(a));
        }

        // ── Write ───────────────────────────────────────────────────────

        public async Task<FormativeAssessmentDto> CreateAsync(
            CreateFormativeAssessmentRequest req, Guid schoolId)
        {
            var entity = new FormativeAssessment
            {
                Id = Guid.NewGuid(),
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                AssessmentType = "Formative",
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

                FormativeType = req.FormativeType?.Trim(),
                CompetencyArea = req.CompetencyArea?.Trim(),
                LearningOutcomeId = req.LearningOutcomeId,
                FeedbackTemplate = req.FeedbackTemplate?.Trim(),
                RequiresRubric = req.RequiresRubric,
                Strand = req.Strand?.Trim(),
                SubStrand = req.SubStrand?.Trim(),
                Criteria = req.Criteria?.Trim(),
                Instructions = req.Instructions?.Trim(),
                AssessmentWeight = req.AssessmentWeight,
            };

            _repo.Create(entity);
            await _unitOfWork.SaveAsync();

            // Re-fetch with navigations for a complete DTO
            var created = await _repo.GetByIdAsync(entity.Id, trackChanges: false);
            return FormativeAssessmentMapper.ToDto(created!);
        }

        public async Task<FormativeAssessmentDto> UpdateAsync(
            Guid id, UpdateFormativeAssessmentRequest req, Guid? schoolId)
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

            entity.FormativeType = req.FormativeType?.Trim();
            entity.CompetencyArea = req.CompetencyArea?.Trim();
            entity.LearningOutcomeId = req.LearningOutcomeId;
            entity.FeedbackTemplate = req.FeedbackTemplate?.Trim();
            entity.RequiresRubric = req.RequiresRubric;
            entity.Strand = req.Strand?.Trim();
            entity.SubStrand = req.SubStrand?.Trim();
            entity.Criteria = req.Criteria?.Trim();
            entity.Instructions = req.Instructions?.Trim();
            entity.AssessmentWeight = req.AssessmentWeight;

            _repo.Update(entity);
            await _unitOfWork.SaveAsync();

            var updated = await _repo.GetByIdAsync(entity.Id, trackChanges: false);
            return FormativeAssessmentMapper.ToDto(updated!);
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

        // ── Private helpers ─────────────────────────────────────────────

        private static IEnumerable<FormativeAssessment> Filter(
            IEnumerable<FormativeAssessment> list, Guid? schoolId)
            => schoolId.HasValue ? list.Where(a => a.TenantId == schoolId.Value) : list;

        private async Task<FormativeAssessment> GetEntityOrThrowAsync(
            Guid id, Guid? schoolId, bool trackChanges)
        {
            var entity = await _repo.GetByIdAsync(id, trackChanges);
            if (entity == null)
                throw new KeyNotFoundException($"FormativeAssessment {id} not found.");
            if (schoolId.HasValue && entity.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this assessment.");
            return entity;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // FORMATIVE ASSESSMENT SCORE — Service Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class FormativeAssessmentScoreService : IFormativeAssessmentScoreService
    {
        private readonly IFormativeAssessmentScoreRepository _scoreRepo;
        private readonly IFormativeAssessmentRepository _assessmentRepo;
        private readonly IRepositoryManager _unitOfWork;

        public FormativeAssessmentScoreService(
            IFormativeAssessmentScoreRepository scoreRepo,
            IFormativeAssessmentRepository assessmentRepo,
            IRepositoryManager unitOfWork)
        {
            _scoreRepo = scoreRepo ?? throw new ArgumentNullException(nameof(scoreRepo));
            _assessmentRepo = assessmentRepo ?? throw new ArgumentNullException(nameof(assessmentRepo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<FormativeAssessmentScoreDto>> GetByAssessmentAsync(
            Guid assessmentId, Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(assessmentId, schoolId);
            var scores = await _scoreRepo.GetAllByAssessmentAsync(assessmentId, trackChanges: false);
            return scores.Select(ScoreMapper.ToDto);
        }

        public async Task<IEnumerable<FormativeAssessmentScoreDto>> GetByStudentAsync(
            Guid studentId, Guid? schoolId)
        {
            var scores = await _scoreRepo.GetByStudentAsync(studentId, trackChanges: false);
            return FilterScores(scores, schoolId).Select(ScoreMapper.ToDto);
        }

        public async Task<IEnumerable<FormativeAssessmentScoreDto>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, Guid? schoolId)
        {
            var scores = await _scoreRepo.GetByStudentAndTermAsync(studentId, termId, trackChanges: false);
            return FilterScores(scores, schoolId).Select(ScoreMapper.ToDto);
        }

        public async Task<FormativeAssessmentScoreDto?> GetByIdAsync(Guid id, Guid? schoolId)
        {
            var score = await _scoreRepo.GetByIdAsync(id, trackChanges: false);
            if (score == null) return null;
            if (schoolId.HasValue
                && score.FormativeAssessment != null
                && score.FormativeAssessment.TenantId != schoolId.Value) return null;
            return ScoreMapper.ToDto(score);
        }

        // ── Write ───────────────────────────────────────────────────────

        public async Task<FormativeAssessmentScoreDto> CreateAsync(
            CreateFormativeAssessmentScoreRequest req,
            Guid? gradedById,
            Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(req.FormativeAssessmentId, schoolId);

            // Prevent duplicate score for same student + assessment
            var existing = await _scoreRepo.GetByStudentAndAssessmentAsync(
                req.StudentId, req.FormativeAssessmentId, trackChanges: false);

            if (existing != null)
                throw new InvalidOperationException(
                    $"A score already exists for student {req.StudentId} " +
                    $"on assessment {req.FormativeAssessmentId}. Use Update instead.");

            var entity = new FormativeAssessmentScore
            {
                Id = Guid.NewGuid(),
                FormativeAssessmentId = req.FormativeAssessmentId,
                StudentId = req.StudentId,
                Score = req.Score,
                MaximumScore = req.MaximumScore,
                Grade = req.Grade?.Trim(),
                PerformanceLevel = req.PerformanceLevel?.Trim(),
                Feedback = req.Feedback?.Trim(),
                Strengths = req.Strengths?.Trim(),
                AreasForImprovement = req.AreasForImprovement?.Trim(),
                CompetencyArea = req.CompetencyArea?.Trim(),
                CompetencyAchieved = req.CompetencyAchieved,
                GradedById = gradedById,
                GradedDate = DateTime.UtcNow,
                IsSubmitted = false,
                CreatedOn = DateTime.UtcNow,
            };

            _scoreRepo.Create(entity);
            await _unitOfWork.SaveAsync();

            var created = await _scoreRepo.GetByIdAsync(entity.Id, trackChanges: false);
            return ScoreMapper.ToDto(created!);
        }

        public async Task<FormativeAssessmentScoreDto> UpdateAsync(
            Guid id,
            UpdateFormativeAssessmentScoreRequest req,
            Guid? gradedById,
            Guid? schoolId)
        {
            var entity = await GetScoreOrThrowAsync(id, schoolId, trackChanges: true);

            entity.Score = req.Score;
            entity.MaximumScore = req.MaximumScore;
            entity.Grade = req.Grade?.Trim();
            entity.PerformanceLevel = req.PerformanceLevel?.Trim();
            entity.Feedback = req.Feedback?.Trim();
            entity.Strengths = req.Strengths?.Trim();
            entity.AreasForImprovement = req.AreasForImprovement?.Trim();
            entity.CompetencyArea = req.CompetencyArea?.Trim();
            entity.CompetencyAchieved = req.CompetencyAchieved;
            entity.GradedById = gradedById;
            entity.GradedDate = DateTime.UtcNow;

            _scoreRepo.Update(entity);
            await _unitOfWork.SaveAsync();

            var updated = await _scoreRepo.GetByIdAsync(entity.Id, trackChanges: false);
            return ScoreMapper.ToDto(updated!);
        }

        public async Task BulkSubmitAsync(BulkSubmitFormativeScoresRequest request, Guid? schoolId)
        {
            var scoreIds = request.Scores.Select(s => s.ScoreId).ToList();
            var now = DateTime.UtcNow;

            foreach (var item in request.Scores)
            {
                var entity = await GetScoreOrThrowAsync(item.ScoreId, schoolId, trackChanges: true);
                entity.IsSubmitted = item.IsSubmitted;
                entity.SubmissionDate = item.IsSubmitted ? now : null;
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

        // ── Private helpers ─────────────────────────────────────────────

        private static IEnumerable<FormativeAssessmentScore> FilterScores(
            IEnumerable<FormativeAssessmentScore> list, Guid? schoolId)
            => schoolId.HasValue
                ? list.Where(s => s.FormativeAssessment?.TenantId == schoolId.Value)
                : list;

        private async Task EnsureAssessmentAccessAsync(Guid assessmentId, Guid? schoolId)
        {
            var a = await _assessmentRepo.GetByIdAsync(assessmentId, trackChanges: false);
            if (a == null)
                throw new KeyNotFoundException($"FormativeAssessment {assessmentId} not found.");
            if (schoolId.HasValue && a.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this assessment.");
        }

        private async Task<FormativeAssessmentScore> GetScoreOrThrowAsync(
            Guid id, Guid? schoolId, bool trackChanges)
        {
            var entity = await _scoreRepo.GetByIdAsync(id, trackChanges);
            if (entity == null)
                throw new KeyNotFoundException($"FormativeAssessmentScore {id} not found.");
            if (schoolId.HasValue
                && entity.FormativeAssessment != null
                && entity.FormativeAssessment.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this score.");
            return entity;
        }
    }
}