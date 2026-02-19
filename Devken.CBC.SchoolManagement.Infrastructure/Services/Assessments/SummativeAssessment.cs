using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessment;
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

    internal static class SummativeAssessmentMapper
    {
        internal static SummativeAssessmentDto ToDto(
            SummativeAssessment a,
            IEnumerable<SummativeAssessmentScore>? scores = null) => new()
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

                // Summative-specific
                ExamType = a.ExamType,
                Duration = a.Duration,
                NumberOfQuestions = a.NumberOfQuestions,
                PassMark = a.PassMark,
                HasPracticalComponent = a.HasPracticalComponent,
                PracticalWeight = a.PracticalWeight,
                TheoryWeight = a.TheoryWeight,
                Instructions = a.Instructions,

                // Scores (optional)
                Scores = scores?.Select(SummativeScoreMapper.ToDto)
                      ?? a.Scores?.Select(SummativeScoreMapper.ToDto),
            };
    }

    internal static class SummativeScoreMapper
    {
        internal static SummativeAssessmentScoreDto ToDto(SummativeAssessmentScore s) => new()
        {
            Id = s.Id,
            SummativeAssessmentId = s.SummativeAssessmentId,
            AssessmentTitle = s.SummativeAssessment?.Title,
            StudentId = s.StudentId,
            StudentName = s.Student != null
                ? $"{s.Student.FirstName} {s.Student.LastName}".Trim()
                : null,
            TheoryScore = s.TheoryScore,
            PracticalScore = s.PracticalScore,
            MaximumTheoryScore = s.MaximumTheoryScore,
            MaximumPracticalScore = s.MaximumPracticalScore,
            TotalScore = s.TotalScore,
            MaximumTotalScore = s.MaximumTotalScore,
            Percentage = s.Percentage,
            PerformanceStatus = s.PerformanceStatus,
            Grade = s.Grade,
            Remarks = s.Remarks,
            PositionInClass = s.PositionInClass,
            PositionInStream = s.PositionInStream,
            IsPassed = s.IsPassed,
            Comments = s.Comments,
            GradedDate = s.GradedDate,
            GradedById = s.GradedById,
            GradedByName = s.GradedBy != null
                ? $"{s.GradedBy.FirstName} {s.GradedBy.LastName}".Trim()
                : null,
            CreatedOn = s.CreatedOn,
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT — Service Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class SummativeAssessmentService : ISummativeAssessmentService
    {
        private readonly ISummativeAssessmentRepository _repo;
        private readonly IRepositoryManager _unitOfWork;

        public SummativeAssessmentService(
            ISummativeAssessmentRepository repo,
            IRepositoryManager unitOfWork)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<SummativeAssessmentDto>> GetAllAsync(Guid? schoolId)
        {
            var list = schoolId.HasValue
                ? await _repo.GetBySchoolAsync(schoolId.Value, trackChanges: false)
                : await _repo.GetAllAsync(trackChanges: false);

            return list.Select(a => SummativeAssessmentMapper.ToDto(a));
        }

        public async Task<SummativeAssessmentDto?> GetByIdAsync(Guid id, Guid? schoolId)
        {
            var a = await _repo.GetByIdAsync(id, trackChanges: false);
            if (a == null) return null;
            if (schoolId.HasValue && a.TenantId != schoolId.Value) return null;
            return SummativeAssessmentMapper.ToDto(a);
        }

        public async Task<SummativeAssessmentDto?> GetWithScoresAsync(Guid id, Guid? schoolId)
        {
            var a = await _repo.GetWithScoresAsync(id, trackChanges: false);
            if (a == null) return null;
            if (schoolId.HasValue && a.TenantId != schoolId.Value) return null;
            return SummativeAssessmentMapper.ToDto(a);
        }

        public async Task<IEnumerable<SummativeAssessmentDto>> GetByClassAsync(
            Guid classId, Guid? schoolId)
        {
            var list = await _repo.GetByClassAsync(classId, trackChanges: false);
            return Filter(list, schoolId).Select(a => SummativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<SummativeAssessmentDto>> GetByTeacherAsync(
            Guid teacherId, Guid? schoolId)
        {
            var list = await _repo.GetByTeacherAsync(teacherId, trackChanges: false);
            return Filter(list, schoolId).Select(a => SummativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<SummativeAssessmentDto>> GetByTermAsync(
            Guid termId, Guid academicYearId, Guid? schoolId)
        {
            var list = await _repo.GetByTermAsync(termId, academicYearId, trackChanges: false);
            return Filter(list, schoolId).Select(a => SummativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<SummativeAssessmentDto>> GetByExamTypeAsync(
            string examType, Guid? schoolId)
        {
            var list = await _repo.GetByExamTypeAsync(examType, trackChanges: false);
            return Filter(list, schoolId).Select(a => SummativeAssessmentMapper.ToDto(a));
        }

        public async Task<IEnumerable<SummativeAssessmentDto>> GetPublishedAsync(
            Guid classId, Guid termId, Guid? schoolId)
        {
            var list = await _repo.GetPublishedAsync(classId, termId, trackChanges: false);
            return Filter(list, schoolId).Select(a => SummativeAssessmentMapper.ToDto(a));
        }

        // ── Write ───────────────────────────────────────────────────────

        public async Task<SummativeAssessmentDto> CreateAsync(
            CreateSummativeAssessmentRequest req, Guid schoolId)
        {
            var entity = new SummativeAssessment
            {
                Id = Guid.NewGuid(),
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                AssessmentType = "Summative",
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

                ExamType = req.ExamType?.Trim(),
                Duration = req.Duration,
                NumberOfQuestions = req.NumberOfQuestions,
                PassMark = req.PassMark,
                HasPracticalComponent = req.HasPracticalComponent,
                PracticalWeight = req.PracticalWeight,
                TheoryWeight = req.TheoryWeight,
                Instructions = req.Instructions?.Trim(),
            };

            _repo.Create(entity);
            await _unitOfWork.SaveAsync();

            var created = await _repo.GetByIdAsync(entity.Id, trackChanges: false);
            return SummativeAssessmentMapper.ToDto(created!);
        }

        public async Task<SummativeAssessmentDto> UpdateAsync(
            Guid id, UpdateSummativeAssessmentRequest req, Guid? schoolId)
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

            entity.ExamType = req.ExamType?.Trim();
            entity.Duration = req.Duration;
            entity.NumberOfQuestions = req.NumberOfQuestions;
            entity.PassMark = req.PassMark;
            entity.HasPracticalComponent = req.HasPracticalComponent;
            entity.PracticalWeight = req.PracticalWeight;
            entity.TheoryWeight = req.TheoryWeight;
            entity.Instructions = req.Instructions?.Trim();

            _repo.Update(entity);
            await _unitOfWork.SaveAsync();

            var updated = await _repo.GetByIdAsync(entity.Id, trackChanges: false);
            return SummativeAssessmentMapper.ToDto(updated!);
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

        private static IEnumerable<SummativeAssessment> Filter(
            IEnumerable<SummativeAssessment> list, Guid? schoolId)
            => schoolId.HasValue ? list.Where(a => a.TenantId == schoolId.Value) : list;

        private async Task<SummativeAssessment> GetEntityOrThrowAsync(
            Guid id, Guid? schoolId, bool trackChanges)
        {
            var entity = await _repo.GetByIdAsync(id, trackChanges);
            if (entity == null)
                throw new KeyNotFoundException($"SummativeAssessment {id} not found.");
            if (schoolId.HasValue && entity.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this assessment.");
            return entity;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUMMATIVE ASSESSMENT SCORE — Service Implementation
    // ═══════════════════════════════════════════════════════════════════

    public class SummativeAssessmentScoreService : ISummativeAssessmentScoreService
    {
        private readonly ISummativeAssessmentScoreRepository _scoreRepo;
        private readonly ISummativeAssessmentRepository _assessmentRepo;
        private readonly IRepositoryManager _unitOfWork;

        public SummativeAssessmentScoreService(
            ISummativeAssessmentScoreRepository scoreRepo,
            ISummativeAssessmentRepository assessmentRepo,
            IRepositoryManager unitOfWork)
        {
            _scoreRepo = scoreRepo ?? throw new ArgumentNullException(nameof(scoreRepo));
            _assessmentRepo = assessmentRepo ?? throw new ArgumentNullException(nameof(assessmentRepo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ── Read ────────────────────────────────────────────────────────

        public async Task<IEnumerable<SummativeAssessmentScoreDto>> GetByAssessmentAsync(
            Guid assessmentId, Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(assessmentId, schoolId);
            var scores = await _scoreRepo.GetAllByAssessmentAsync(assessmentId, trackChanges: false);
            return scores.Select(SummativeScoreMapper.ToDto);
        }

        public async Task<IEnumerable<SummativeAssessmentScoreDto>> GetByStudentAsync(
            Guid studentId, Guid? schoolId)
        {
            var scores = await _scoreRepo.GetByStudentAsync(studentId, trackChanges: false);
            return FilterScores(scores, schoolId).Select(SummativeScoreMapper.ToDto);
        }

        public async Task<IEnumerable<SummativeAssessmentScoreDto>> GetByStudentAndTermAsync(
            Guid studentId, Guid termId, Guid? schoolId)
        {
            var scores = await _scoreRepo.GetByStudentAndTermAsync(studentId, termId, trackChanges: false);
            return FilterScores(scores, schoolId).Select(SummativeScoreMapper.ToDto);
        }

        public async Task<SummativeAssessmentScoreDto?> GetByIdAsync(Guid id, Guid? schoolId)
        {
            var score = await _scoreRepo.GetByIdAsync(id, trackChanges: false);
            if (score == null) return null;
            if (schoolId.HasValue
                && score.SummativeAssessment != null
                && score.SummativeAssessment.TenantId != schoolId.Value) return null;
            return SummativeScoreMapper.ToDto(score);
        }

        // ── Write ───────────────────────────────────────────────────────

        public async Task<SummativeAssessmentScoreDto> CreateAsync(
            CreateSummativeAssessmentScoreRequest req,
            Guid? gradedById,
            Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(req.SummativeAssessmentId, schoolId);

            // Prevent duplicate score for same student + assessment
            var existing = await _scoreRepo.GetByStudentAndAssessmentAsync(
                req.StudentId, req.SummativeAssessmentId, trackChanges: false);

            if (existing != null)
                throw new InvalidOperationException(
                    $"A score already exists for student {req.StudentId} " +
                    $"on assessment {req.SummativeAssessmentId}. Use Update instead.");

            // Retrieve assessment to check PassMark
            var assessment = await _assessmentRepo.GetByIdAsync(
                req.SummativeAssessmentId, trackChanges: false);

            var theoryScore = req.TheoryScore;
            var practicalScore = req.PracticalScore ?? 0m;
            var maxTheory = req.MaximumTheoryScore;
            var maxPractical = req.MaximumPracticalScore ?? 0m;
            var totalScore = theoryScore + practicalScore;
            var maxTotal = maxTheory + maxPractical;
            var percentage = maxTotal > 0 ? (totalScore / maxTotal) * 100 : 0m;

            var entity = new SummativeAssessmentScore
            {
                Id = Guid.NewGuid(),
                SummativeAssessmentId = req.SummativeAssessmentId,
                StudentId = req.StudentId,
                TheoryScore = theoryScore,
                PracticalScore = req.PracticalScore,
                MaximumTheoryScore = maxTheory,
                MaximumPracticalScore = req.MaximumPracticalScore,
                Grade = req.Grade?.Trim(),
                Remarks = req.Remarks?.Trim(),
                PositionInClass = req.PositionInClass,
                PositionInStream = req.PositionInStream,
                IsPassed = assessment != null && percentage >= assessment.PassMark,
                Comments = req.Comments?.Trim(),
                GradedById = gradedById,
                GradedDate = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
            };

            _scoreRepo.Create(entity);
            await _unitOfWork.SaveAsync();

            var created = await _scoreRepo.GetByIdAsync(entity.Id, trackChanges: false);
            return SummativeScoreMapper.ToDto(created!);
        }

        public async Task<SummativeAssessmentScoreDto> UpdateAsync(
            Guid id,
            UpdateSummativeAssessmentScoreRequest req,
            Guid? gradedById,
            Guid? schoolId)
        {
            var entity = await GetScoreOrThrowAsync(id, schoolId, trackChanges: true);

            // Retrieve assessment to re-check PassMark
            var assessment = entity.SummativeAssessmentId.HasValue
                ? await _assessmentRepo.GetByIdAsync(entity.SummativeAssessmentId.Value, trackChanges: false)
                : null;

            var theoryScore = req.TheoryScore;
            var practicalScore = req.PracticalScore ?? 0m;
            var maxTheory = req.MaximumTheoryScore;
            var maxPractical = req.MaximumPracticalScore ?? 0m;
            var maxTotal = maxTheory + maxPractical;
            var totalScore = theoryScore + practicalScore;
            var percentage = maxTotal > 0 ? (totalScore / maxTotal) * 100 : 0m;

            entity.TheoryScore = theoryScore;
            entity.PracticalScore = req.PracticalScore;
            entity.MaximumTheoryScore = maxTheory;
            entity.MaximumPracticalScore = req.MaximumPracticalScore;
            entity.Grade = req.Grade?.Trim();
            entity.Remarks = req.Remarks?.Trim();
            entity.PositionInClass = req.PositionInClass;
            entity.PositionInStream = req.PositionInStream;
            entity.IsPassed = assessment != null && percentage >= assessment.PassMark;
            entity.Comments = req.Comments?.Trim();
            entity.GradedById = gradedById;
            entity.GradedDate = DateTime.UtcNow;

            _scoreRepo.Update(entity);
            await _unitOfWork.SaveAsync();

            var updated = await _scoreRepo.GetByIdAsync(entity.Id, trackChanges: false);
            return SummativeScoreMapper.ToDto(updated!);
        }

        public async Task RecalculatePositionsAsync(Guid assessmentId, Guid? schoolId)
        {
            await EnsureAssessmentAccessAsync(assessmentId, schoolId);

            var scores = (await _scoreRepo.GetAllByAssessmentAsync(assessmentId, trackChanges: true))
                .OrderByDescending(s => s.TotalScore)
                .ToList();

            for (int i = 0; i < scores.Count; i++)
            {
                scores[i].PositionInClass = i + 1;
                _scoreRepo.Update(scores[i]);
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

        private static IEnumerable<SummativeAssessmentScore> FilterScores(
            IEnumerable<SummativeAssessmentScore> list, Guid? schoolId)
            => schoolId.HasValue
                ? list.Where(s => s.SummativeAssessment?.TenantId == schoolId.Value)
                : list;

        private async Task EnsureAssessmentAccessAsync(Guid assessmentId, Guid? schoolId)
        {
            var a = await _assessmentRepo.GetByIdAsync(assessmentId, trackChanges: false);
            if (a == null)
                throw new KeyNotFoundException($"SummativeAssessment {assessmentId} not found.");
            if (schoolId.HasValue && a.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this assessment.");
        }

        private async Task<SummativeAssessmentScore> GetScoreOrThrowAsync(
            Guid id, Guid? schoolId, bool trackChanges)
        {
            var entity = await _scoreRepo.GetByIdAsync(id, trackChanges);
            if (entity == null)
                throw new KeyNotFoundException($"SummativeAssessmentScore {id} not found.");
            if (schoolId.HasValue
                && entity.SummativeAssessment != null
                && entity.SummativeAssessment.TenantId != schoolId.Value)
                throw new UnauthorizedAccessException("Access denied to this score.");
            return entity;
        }
    }
}