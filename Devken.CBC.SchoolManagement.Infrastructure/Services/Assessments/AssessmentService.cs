// Devken.CBC.SchoolManagement.Application/Service/Assessments/AssessmentService.cs
using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service.Assessments
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IRepositoryManager _repository;

        public AssessmentService(IRepositoryManager repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<AssessmentListItem>> GetAllAsync(
            AssessmentTypeDto? type,
            Guid? classId, Guid? termId, Guid? subjectId, Guid? teacherId,
            bool? isPublished, Guid? userSchoolId, bool isSuperAdmin)
        {
            var results = new List<AssessmentListItem>();

            bool fetchAll = type == null;
            bool fetchFormative = fetchAll || type == AssessmentTypeDto.Formative;
            bool fetchSummative = fetchAll || type == AssessmentTypeDto.Summative;
            bool fetchCompetency = fetchAll || type == AssessmentTypeDto.Competency;

            if (fetchFormative)
            {
                var entities = await _repository.FormativeAssessment
                    .GetAllAsync(classId, termId, subjectId, teacherId, isPublished);

                results.AddRange(
                    ApplyTenantFilter(entities, userSchoolId, isSuperAdmin)
                        .Select(MapFormativeToListItem));
            }

            if (fetchSummative)
            {
                var entities = await _repository.SummativeAssessment
                    .GetAllAsync(classId, termId, subjectId, teacherId, isPublished);

                results.AddRange(
                    ApplyTenantFilter(entities, userSchoolId, isSuperAdmin)
                        .Select(MapSummativeToListItem));
            }

            if (fetchCompetency)
            {
                var entities = await _repository.CompetencyAssessment
                    .GetAllAsync(classId, termId, subjectId, teacherId, isPublished);

                results.AddRange(
                    ApplyTenantFilter(entities, userSchoolId, isSuperAdmin)
                        .Select(MapCompetencyToListItem));
            }

            return results.OrderByDescending(r => r.AssessmentDate);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<AssessmentResponse> GetByIdAsync(
            Guid id, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            return type switch
            {
                AssessmentTypeDto.Formative => await GetFormativeById(id, userSchoolId, isSuperAdmin),
                AssessmentTypeDto.Summative => await GetSummativeById(id, userSchoolId, isSuperAdmin),
                AssessmentTypeDto.Competency => await GetCompetencyById(id, userSchoolId, isSuperAdmin),
                _ => throw new ValidationException($"Unknown assessment type: {type}")
            };
        }

        private async Task<AssessmentResponse> GetFormativeById(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var entity = isSuperAdmin
                ? await _repository.FormativeAssessment.GetByIdIgnoringTenantAsync(id)
                : await _repository.FormativeAssessment.GetByIdWithDetailsAsync(id);

            if (entity == null)
                throw new NotFoundException($"Formative assessment {id} not found.");

            ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);
            return MapFormativeToResponse(entity);
        }

        private async Task<AssessmentResponse> GetSummativeById(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var entity = isSuperAdmin
                ? await _repository.SummativeAssessment.GetByIdIgnoringTenantAsync(id)
                : await _repository.SummativeAssessment.GetByIdWithDetailsAsync(id);

            if (entity == null)
                throw new NotFoundException($"Summative assessment {id} not found.");

            ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);
            return MapSummativeToResponse(entity);
        }

        private async Task<AssessmentResponse> GetCompetencyById(
            Guid id, Guid? userSchoolId, bool isSuperAdmin)
        {
            var entity = isSuperAdmin
                ? await _repository.CompetencyAssessment.GetByIdIgnoringTenantAsync(id)
                : await _repository.CompetencyAssessment.GetByIdWithDetailsAsync(id);

            if (entity == null)
                throw new NotFoundException($"Competency assessment {id} not found.");

            ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);
            return MapCompetencyToResponse(entity);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<AssessmentResponse> CreateAsync(
            CreateAssessmentRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            var tenantId = ResolveTenant(request.TenantId, userSchoolId, isSuperAdmin);

            return request.AssessmentType switch
            {
                AssessmentTypeDto.Formative => await CreateFormative(request, tenantId),
                AssessmentTypeDto.Summative => await CreateSummative(request, tenantId),
                AssessmentTypeDto.Competency => await CreateCompetency(request, tenantId),
                _ => throw new ValidationException($"Unknown assessment type: {request.AssessmentType}")
            };
        }

        private async Task<AssessmentResponse> CreateFormative(
            CreateAssessmentRequest r, Guid tenantId)
        {
            var entity = new FormativeAssessment
            {
                Id = Guid.NewGuid(),
                Title = r.Title.Trim(),
                Description = r.Description?.Trim(),
                TeacherId = r.TeacherId,
                SubjectId = r.SubjectId,
                ClassId = r.ClassId,
                TermId = r.TermId,
                AcademicYearId = r.AcademicYearId,
                AssessmentDate = r.AssessmentDate,
                MaximumScore = r.MaximumScore,
                AssessmentType = AssessmentTypeDto.Formative.ToString(),
                IsPublished = false,
                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,

                FormativeType = r.FormativeType?.Trim(),
                CompetencyArea = r.CompetencyArea?.Trim(),
                StrandId = r.StrandId,
                SubStrandId = r.SubStrandId,
                LearningOutcomeId = r.LearningOutcomeId,
                Criteria = r.Criteria?.Trim(),
                FeedbackTemplate = r.FeedbackTemplate?.Trim(),
                RequiresRubric = r.RequiresRubric,
                AssessmentWeight = r.AssessmentWeight,
                Instructions = r.FormativeInstructions?.Trim(),
            };

            _repository.FormativeAssessment.Create(entity);
            await _repository.SaveAsync();
            await _repository.FormativeAssessment.LoadNavigationsAsync(entity);
            return MapFormativeToResponse(entity);
        }

        private async Task<AssessmentResponse> CreateSummative(
            CreateAssessmentRequest r, Guid tenantId)
        {
            var entity = new SummativeAssessment
            {
                Id = Guid.NewGuid(),
                Title = r.Title.Trim(),
                Description = r.Description?.Trim(),
                TeacherId = r.TeacherId,
                SubjectId = r.SubjectId,
                ClassId = r.ClassId,
                TermId = r.TermId,
                AcademicYearId = r.AcademicYearId,
                AssessmentDate = r.AssessmentDate,
                MaximumScore = r.MaximumScore,
                AssessmentType = AssessmentTypeDto.Summative.ToString(),
                IsPublished = false,
                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,

                ExamType = r.ExamType?.Trim(),
                Duration = r.Duration,
                NumberOfQuestions = r.NumberOfQuestions,
                PassMark = r.PassMark,
                HasPracticalComponent = r.HasPracticalComponent,
                PracticalWeight = r.PracticalWeight,
                TheoryWeight = r.TheoryWeight,
                Instructions = r.SummativeInstructions?.Trim(),
            };

            _repository.SummativeAssessment.Create(entity);
            await _repository.SaveAsync();
            await _repository.SummativeAssessment.LoadNavigationsAsync(entity);
            return MapSummativeToResponse(entity);
        }

        private async Task<AssessmentResponse> CreateCompetency(
            CreateAssessmentRequest r, Guid tenantId)
        {
            if (string.IsNullOrWhiteSpace(r.CompetencyName))
                throw new ValidationException("CompetencyName is required for Competency assessments.");

            var entity = new CompetencyAssessment
            {
                Id = Guid.NewGuid(),
                Title = r.Title.Trim(),
                Description = r.Description?.Trim(),
                TeacherId = r.TeacherId,
                SubjectId = r.SubjectId,
                ClassId = r.ClassId,
                TermId = r.TermId,
                AcademicYearId = r.AcademicYearId,
                AssessmentDate = r.AssessmentDate,
                MaximumScore = r.MaximumScore,
                AssessmentType = AssessmentTypeDto.Competency.ToString(),
                IsPublished = false,
                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,

                CompetencyName = r.CompetencyName.Trim(),
                CompetencyStrand = r.CompetencyStrand?.Trim(),
                CompetencySubStrand = r.CompetencySubStrand?.Trim(),
                TargetLevel = r.TargetLevel,
                PerformanceIndicators = r.PerformanceIndicators?.Trim(),
                AssessmentMethod = r.AssessmentMethod,
                RatingScale = r.RatingScale?.Trim(),
                IsObservationBased = r.IsObservationBased,
                ToolsRequired = r.ToolsRequired?.Trim(),
                Instructions = r.CompetencyInstructions?.Trim(),
                SpecificLearningOutcome = r.SpecificLearningOutcome?.Trim(),
            };

            _repository.CompetencyAssessment.Create(entity);
            await _repository.SaveAsync();
            await _repository.CompetencyAssessment.LoadNavigationsAsync(entity);
            return MapCompetencyToResponse(entity);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<AssessmentResponse> UpdateAsync(
            Guid id, UpdateAssessmentRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            return request.AssessmentType switch
            {
                AssessmentTypeDto.Formative => await UpdateFormative(id, request, userSchoolId, isSuperAdmin),
                AssessmentTypeDto.Summative => await UpdateSummative(id, request, userSchoolId, isSuperAdmin),
                AssessmentTypeDto.Competency => await UpdateCompetency(id, request, userSchoolId, isSuperAdmin),
                _ => throw new ValidationException($"Unknown assessment type: {request.AssessmentType}")
            };
        }

        private async Task<AssessmentResponse> UpdateFormative(
            Guid id, UpdateAssessmentRequest r, Guid? userSchoolId, bool isSuperAdmin)
        {
            var entity = isSuperAdmin
                ? await _repository.FormativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                : await _repository.FormativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

            if (entity == null)
                throw new NotFoundException($"Formative assessment {id} not found.");

            ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

            // Shared
            entity.Title = r.Title.Trim();
            entity.Description = r.Description?.Trim();
            entity.TeacherId = r.TeacherId;
            entity.SubjectId = r.SubjectId;
            entity.ClassId = r.ClassId;
            entity.TermId = r.TermId;
            entity.AcademicYearId = r.AcademicYearId;
            entity.AssessmentDate = r.AssessmentDate;
            entity.MaximumScore = r.MaximumScore;

            // Formative-specific
            entity.FormativeType = r.FormativeType?.Trim();
            entity.CompetencyArea = r.CompetencyArea?.Trim();
            entity.StrandId = r.StrandId;
            entity.SubStrandId = r.SubStrandId;
            entity.LearningOutcomeId = r.LearningOutcomeId;
            entity.Criteria = r.Criteria?.Trim();
            entity.FeedbackTemplate = r.FeedbackTemplate?.Trim();
            entity.RequiresRubric = r.RequiresRubric;
            entity.AssessmentWeight = r.AssessmentWeight;
            entity.Instructions = r.FormativeInstructions?.Trim();

            _repository.FormativeAssessment.Update(entity);
            await _repository.SaveAsync();
            await _repository.FormativeAssessment.LoadNavigationsAsync(entity);
            return MapFormativeToResponse(entity);
        }

        private async Task<AssessmentResponse> UpdateSummative(
            Guid id, UpdateAssessmentRequest r, Guid? userSchoolId, bool isSuperAdmin)
        {
            var entity = isSuperAdmin
                ? await _repository.SummativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                : await _repository.SummativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

            if (entity == null)
                throw new NotFoundException($"Summative assessment {id} not found.");

            ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

            entity.Title = r.Title.Trim();
            entity.Description = r.Description?.Trim();
            entity.TeacherId = r.TeacherId;
            entity.SubjectId = r.SubjectId;
            entity.ClassId = r.ClassId;
            entity.TermId = r.TermId;
            entity.AcademicYearId = r.AcademicYearId;
            entity.AssessmentDate = r.AssessmentDate;
            entity.MaximumScore = r.MaximumScore;

            entity.ExamType = r.ExamType?.Trim();
            entity.Duration = r.Duration;
            entity.NumberOfQuestions = r.NumberOfQuestions;
            entity.PassMark = r.PassMark;
            entity.HasPracticalComponent = r.HasPracticalComponent;
            entity.PracticalWeight = r.PracticalWeight;
            entity.TheoryWeight = r.TheoryWeight;
            entity.Instructions = r.SummativeInstructions?.Trim();

            _repository.SummativeAssessment.Update(entity);
            await _repository.SaveAsync();
            await _repository.SummativeAssessment.LoadNavigationsAsync(entity);
            return MapSummativeToResponse(entity);
        }

        private async Task<AssessmentResponse> UpdateCompetency(
            Guid id, UpdateAssessmentRequest r, Guid? userSchoolId, bool isSuperAdmin)
        {
            var entity = isSuperAdmin
                ? await _repository.CompetencyAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                : await _repository.CompetencyAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

            if (entity == null)
                throw new NotFoundException($"Competency assessment {id} not found.");

            ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

            entity.Title = r.Title.Trim();
            entity.Description = r.Description?.Trim();
            entity.TeacherId = r.TeacherId;
            entity.SubjectId = r.SubjectId;
            entity.ClassId = r.ClassId;
            entity.TermId = r.TermId;
            entity.AcademicYearId = r.AcademicYearId;
            entity.AssessmentDate = r.AssessmentDate;
            entity.MaximumScore = r.MaximumScore;

            entity.CompetencyName = (r.CompetencyName ?? entity.CompetencyName).Trim();
            entity.CompetencyStrand = r.CompetencyStrand?.Trim();
            entity.CompetencySubStrand = r.CompetencySubStrand?.Trim();
            entity.TargetLevel = r.TargetLevel;
            entity.PerformanceIndicators = r.PerformanceIndicators?.Trim();
            entity.AssessmentMethod = r.AssessmentMethod;
            entity.RatingScale = r.RatingScale?.Trim();
            entity.IsObservationBased = r.IsObservationBased;
            entity.ToolsRequired = r.ToolsRequired?.Trim();
            entity.Instructions = r.CompetencyInstructions?.Trim();
            entity.SpecificLearningOutcome = r.SpecificLearningOutcome?.Trim();

            _repository.CompetencyAssessment.Update(entity);
            await _repository.SaveAsync();
            await _repository.CompetencyAssessment.LoadNavigationsAsync(entity);
            return MapCompetencyToResponse(entity);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLISH
        // ─────────────────────────────────────────────────────────────────────
        public async Task PublishAsync(
            Guid id, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (type)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var entity = isSuperAdmin
                            ? await _repository.FormativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                            : await _repository.FormativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

                        if (entity == null) throw new NotFoundException($"Formative assessment {id} not found.");
                        if (entity.IsPublished) throw new ConflictException("Assessment is already published.");
                        ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

                        entity.IsPublished = true;
                        entity.PublishedDate = DateTime.UtcNow;
                        _repository.FormativeAssessment.Update(entity);
                        break;
                    }
                case AssessmentTypeDto.Summative:
                    {
                        var entity = isSuperAdmin
                            ? await _repository.SummativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                            : await _repository.SummativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

                        if (entity == null) throw new NotFoundException($"Summative assessment {id} not found.");
                        if (entity.IsPublished) throw new ConflictException("Assessment is already published.");
                        ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

                        entity.IsPublished = true;
                        entity.PublishedDate = DateTime.UtcNow;
                        _repository.SummativeAssessment.Update(entity);
                        break;
                    }
                case AssessmentTypeDto.Competency:
                    {
                        var entity = isSuperAdmin
                            ? await _repository.CompetencyAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                            : await _repository.CompetencyAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

                        if (entity == null) throw new NotFoundException($"Competency assessment {id} not found.");
                        if (entity.IsPublished) throw new ConflictException("Assessment is already published.");
                        ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

                        entity.IsPublished = true;
                        entity.PublishedDate = DateTime.UtcNow;
                        _repository.CompetencyAssessment.Update(entity);
                        break;
                    }
                default:
                    throw new ValidationException($"Unknown assessment type: {type}");
            }

            await _repository.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteAsync(
            Guid id, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (type)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var entity = isSuperAdmin
                            ? await _repository.FormativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                            : await _repository.FormativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

                        if (entity == null) throw new NotFoundException($"Formative assessment {id} not found.");
                        if (entity.IsPublished) throw new ConflictException("Cannot delete a published assessment.");
                        ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

                        _repository.FormativeAssessment.Delete(entity);
                        break;
                    }
                case AssessmentTypeDto.Summative:
                    {
                        var entity = isSuperAdmin
                            ? await _repository.SummativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                            : await _repository.SummativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

                        if (entity == null) throw new NotFoundException($"Summative assessment {id} not found.");
                        if (entity.IsPublished) throw new ConflictException("Cannot delete a published assessment.");
                        ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

                        _repository.SummativeAssessment.Delete(entity);
                        break;
                    }
                case AssessmentTypeDto.Competency:
                    {
                        var entity = isSuperAdmin
                            ? await _repository.CompetencyAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                            : await _repository.CompetencyAssessment.GetByIdWithDetailsAsync(id, trackChanges: true);

                        if (entity == null) throw new NotFoundException($"Competency assessment {id} not found.");
                        if (entity.IsPublished) throw new ConflictException("Cannot delete a published assessment.");
                        ValidateTenantAccess(entity.TenantId, userSchoolId, isSuperAdmin);

                        _repository.CompetencyAssessment.Delete(entity);
                        break;
                    }
                default:
                    throw new ValidationException($"Unknown assessment type: {type}");
            }

            await _repository.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // SCORES — GET
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<AssessmentScoreResponse>> GetScoresAsync(
            Guid assessmentId, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            // Verify the parent assessment exists and the caller may access it
            await GetByIdAsync(assessmentId, type, userSchoolId, isSuperAdmin);

            return type switch
            {
                AssessmentTypeDto.Formative => (await _repository.FormativeAssessmentScore
                    .GetByAssessmentAsync(assessmentId))
                    .Select(MapFormativeScore),

                AssessmentTypeDto.Summative => (await _repository.SummativeAssessmentScore
                    .GetByAssessmentAsync(assessmentId))
                    .Select(MapSummativeScore),

                AssessmentTypeDto.Competency => (await _repository.CompetencyAssessmentScore
                    .GetByAssessmentAsync(assessmentId))
                    .Select(MapCompetencyScore),

                _ => throw new ValidationException($"Unknown assessment type: {type}")
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // SCORES — UPSERT
        // ─────────────────────────────────────────────────────────────────────
        public async Task<AssessmentScoreResponse> UpsertScoreAsync(
            UpsertScoreRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            var assessment = await GetByIdAsync(
                request.AssessmentId, request.AssessmentType, userSchoolId, isSuperAdmin);

            if (!assessment.IsPublished)
                throw new ConflictException("Scores can only be entered for published assessments.");

            return request.AssessmentType switch
            {
                AssessmentTypeDto.Formative => await UpsertFormativeScore(request),
                AssessmentTypeDto.Summative => await UpsertSummativeScore(request),
                AssessmentTypeDto.Competency => await UpsertCompetencyScore(request),
                _ => throw new ValidationException($"Unknown assessment type: {request.AssessmentType}")
            };
        }

        private async Task<AssessmentScoreResponse> UpsertFormativeScore(UpsertScoreRequest r)
        {
            var existing = await _repository.FormativeAssessmentScore
                .GetByAssessmentAndStudentAsync(r.AssessmentId, r.StudentId, trackChanges: true);

            if (existing != null)
            {
                existing.Score = r.Score ?? existing.Score;
                existing.MaximumScore = r.MaximumScore ?? existing.MaximumScore;
                existing.Grade = r.Grade;
                existing.PerformanceLevel = r.PerformanceLevel;
                existing.Feedback = r.Feedback;
                existing.Strengths = r.Strengths;
                existing.AreasForImprovement = r.AreasForImprovement;
                existing.IsSubmitted = r.IsSubmitted;
                existing.SubmissionDate = r.SubmissionDate;
                existing.CompetencyArea = r.CompetencyArea;
                existing.CompetencyAchieved = r.CompetencyAchieved;
                existing.GradedById = r.GradedById;
                existing.GradedDate = DateTime.UtcNow;

                _repository.FormativeAssessmentScore.Update(existing);
                await _repository.SaveAsync();
                return MapFormativeScore(existing);
            }
            else
            {
                var score = new FormativeAssessmentScore
                {
                    Id = Guid.NewGuid(),
                    FormativeAssessmentId = r.AssessmentId,
                    StudentId = r.StudentId,
                    Score = r.Score ?? 0,
                    MaximumScore = r.MaximumScore ?? 0,
                    Grade = r.Grade,
                    PerformanceLevel = r.PerformanceLevel,
                    Feedback = r.Feedback,
                    Strengths = r.Strengths,
                    AreasForImprovement = r.AreasForImprovement,
                    IsSubmitted = r.IsSubmitted,
                    SubmissionDate = r.SubmissionDate,
                    CompetencyArea = r.CompetencyArea,
                    CompetencyAchieved = r.CompetencyAchieved,
                    GradedById = r.GradedById,
                    GradedDate = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow,
                };

                _repository.FormativeAssessmentScore.Create(score);
                await _repository.SaveAsync();
                return MapFormativeScore(score);
            }
        }

        private async Task<AssessmentScoreResponse> UpsertSummativeScore(UpsertScoreRequest r)
        {
            var existing = await _repository.SummativeAssessmentScore
                .GetByAssessmentAndStudentAsync(r.AssessmentId, r.StudentId, trackChanges: true);

            if (existing != null)
            {
                existing.TheoryScore = r.TheoryScore ?? existing.TheoryScore;
                existing.PracticalScore = r.PracticalScore;
                existing.MaximumTheoryScore = r.MaximumTheoryScore ?? existing.MaximumTheoryScore;
                existing.MaximumPracticalScore = r.MaximumPracticalScore;
                existing.Grade = r.Grade;
                existing.Remarks = r.Remarks;
                existing.PositionInClass = r.PositionInClass;
                existing.PositionInStream = r.PositionInStream;
                existing.IsPassed = r.IsPassed;
                existing.Comments = r.Comments;
                existing.GradedById = r.GradedById;
                existing.GradedDate = DateTime.UtcNow;

                _repository.SummativeAssessmentScore.Update(existing);
                await _repository.SaveAsync();
                return MapSummativeScore(existing);
            }
            else
            {
                var score = new SummativeAssessmentScore
                {
                    Id = Guid.NewGuid(),
                    SummativeAssessmentId = r.AssessmentId,
                    StudentId = r.StudentId,
                    TheoryScore = r.TheoryScore ?? 0,
                    PracticalScore = r.PracticalScore,
                    MaximumTheoryScore = r.MaximumTheoryScore ?? 0,
                    MaximumPracticalScore = r.MaximumPracticalScore,
                    Grade = r.Grade,
                    Remarks = r.Remarks,
                    PositionInClass = r.PositionInClass,
                    PositionInStream = r.PositionInStream,
                    IsPassed = r.IsPassed,
                    Comments = r.Comments,
                    GradedById = r.GradedById,
                    GradedDate = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow,
                };

                _repository.SummativeAssessmentScore.Create(score);
                await _repository.SaveAsync();
                return MapSummativeScore(score);
            }
        }

        private async Task<AssessmentScoreResponse> UpsertCompetencyScore(UpsertScoreRequest r)
        {
            if (string.IsNullOrWhiteSpace(r.Rating))
                throw new ValidationException("Rating is required for Competency scores.");

            var existing = await _repository.CompetencyAssessmentScore
                .GetByAssessmentAndStudentAsync(r.AssessmentId, r.StudentId, trackChanges: true);

            if (existing != null)
            {
                existing.Rating = r.Rating;
                existing.ScoreValue = r.ScoreValue;
                existing.Evidence = r.Evidence;
                existing.AssessmentMethod = r.AssessmentMethod;
                existing.ToolsUsed = r.ToolsUsed;
                existing.Feedback = r.Feedback;
                existing.AreasForImprovement = r.AreasForImprovement;
                existing.IsFinalized = r.IsFinalized;
                existing.Strand = r.Strand;
                existing.SubStrand = r.SubStrand;
                existing.SpecificLearningOutcome = r.SpecificLearningOutcome;
                existing.AssessorId = r.AssessorId;

                _repository.CompetencyAssessmentScore.Update(existing);
                await _repository.SaveAsync();
                return MapCompetencyScore(existing);
            }
            else
            {
                var score = new CompetencyAssessmentScore
                {
                    Id = Guid.NewGuid(),
                    CompetencyAssessmentId = r.AssessmentId,
                    StudentId = r.StudentId,
                    Rating = r.Rating,
                    ScoreValue = r.ScoreValue,
                    Evidence = r.Evidence,
                    AssessmentDate = DateTime.UtcNow,
                    AssessmentMethod = r.AssessmentMethod,
                    ToolsUsed = r.ToolsUsed,
                    Feedback = r.Feedback,
                    AreasForImprovement = r.AreasForImprovement,
                    IsFinalized = r.IsFinalized,
                    Strand = r.Strand,
                    SubStrand = r.SubStrand,
                    SpecificLearningOutcome = r.SpecificLearningOutcome,
                    AssessorId = r.AssessorId,
                    CreatedOn = DateTime.UtcNow,
                };

                _repository.CompetencyAssessmentScore.Create(score);
                await _repository.SaveAsync();
                return MapCompetencyScore(score);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SCORES — DELETE
        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteScoreAsync(
            Guid scoreId, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (type)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var score = await _repository.FormativeAssessmentScore
                            .GetByIdAsync(scoreId, trackChanges: true);
                        if (score == null) throw new NotFoundException($"Score {scoreId} not found.");
                        _repository.FormativeAssessmentScore.Delete(score);
                        break;
                    }
                case AssessmentTypeDto.Summative:
                    {
                        var score = await _repository.SummativeAssessmentScore
                            .GetByIdAsync(scoreId, trackChanges: true);
                        if (score == null) throw new NotFoundException($"Score {scoreId} not found.");
                        _repository.SummativeAssessmentScore.Delete(score);
                        break;
                    }
                case AssessmentTypeDto.Competency:
                    {
                        var score = await _repository.CompetencyAssessmentScore
                            .GetByIdAsync(scoreId, trackChanges: true);
                        if (score == null) throw new NotFoundException($"Score {scoreId} not found.");
                        _repository.CompetencyAssessmentScore.Delete(score);
                        break;
                    }
                default:
                    throw new ValidationException($"Unknown assessment type: {type}");
            }

            await _repository.SaveAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // MAPPERS
        // ─────────────────────────────────────────────────────────────────────
        private static AssessmentListItem MapFormativeToListItem(FormativeAssessment f) => new()
        {
            Id = f.Id,
            Title = f.Title,
            AssessmentType = AssessmentTypeDto.Formative,
            TeacherName = f.Teacher != null ? $"{f.Teacher.FirstName} {f.Teacher.LastName}".Trim() : "-",
            SubjectName = f.Subject?.Name ?? "-",
            ClassName = f.Class?.Name ?? "-",
            TermName = f.Term?.Name ?? "-",
            AssessmentDate = f.AssessmentDate,
            MaximumScore = f.MaximumScore,
            IsPublished = f.IsPublished,
            ScoreCount = f.Scores?.Count ?? 0,
            StrandName = f.Strand?.Name,
            SubStrandName = f.SubStrand?.Name,
        };

        private static AssessmentListItem MapSummativeToListItem(SummativeAssessment s) => new()
        {
            Id = s.Id,
            Title = s.Title,
            AssessmentType = AssessmentTypeDto.Summative,
            TeacherName = s.Teacher != null ? $"{s.Teacher.FirstName} {s.Teacher.LastName}".Trim() : "-",
            SubjectName = s.Subject?.Name ?? "-",
            ClassName = s.Class?.Name ?? "-",
            TermName = s.Term?.Name ?? "-",
            AssessmentDate = s.AssessmentDate,
            MaximumScore = s.MaximumScore,
            IsPublished = s.IsPublished,
            ScoreCount = s.Scores?.Count ?? 0,
        };

        private static AssessmentListItem MapCompetencyToListItem(CompetencyAssessment c) => new()
        {
            Id = c.Id,
            Title = c.Title,
            AssessmentType = AssessmentTypeDto.Competency,
            TeacherName = c.Teacher != null ? $"{c.Teacher.FirstName} {c.Teacher.LastName}".Trim() : "-",
            SubjectName = c.Subject?.Name ?? "-",
            ClassName = c.Class?.Name ?? "-",
            TermName = c.Term?.Name ?? "-",
            AssessmentDate = c.AssessmentDate,
            MaximumScore = c.MaximumScore,
            IsPublished = c.IsPublished,
            ScoreCount = c.Scores?.Count ?? 0,
        };

        private static AssessmentResponse MapFormativeToResponse(FormativeAssessment f) => new()
        {
            Id = f.Id,
            AssessmentType = AssessmentTypeDto.Formative,
            Title = f.Title,
            Description = f.Description,
            TeacherId = f.TeacherId,
            TeacherName = f.Teacher != null ? $"{f.Teacher.FirstName} {f.Teacher.LastName}".Trim() : "-",
            SubjectId = f.SubjectId,
            SubjectName = f.Subject?.Name ?? "-",
            ClassId = f.ClassId,
            ClassName = f.Class?.Name ?? "-",
            TermId = f.TermId,
            TermName = f.Term?.Name ?? "-",
            AcademicYearId = f.AcademicYearId,
            AcademicYearName = f.AcademicYear?.Name ?? "-",
            AssessmentDate = f.AssessmentDate,
            MaximumScore = f.MaximumScore,
            IsPublished = f.IsPublished,
            PublishedDate = f.PublishedDate,
            CreatedOn = f.CreatedOn,
            ScoreCount = f.Scores?.Count ?? 0,

            FormativeType = f.FormativeType,
            CompetencyArea = f.CompetencyArea,
            StrandId = f.StrandId,
            StrandName = f.Strand?.Name,
            SubStrandId = f.SubStrandId,
            SubStrandName = f.SubStrand?.Name,
            LearningOutcomeId = f.LearningOutcomeId,
            LearningOutcomeName = f.LearningOutcome?.Outcome,
            Criteria = f.Criteria,
            FeedbackTemplate = f.FeedbackTemplate,
            RequiresRubric = f.RequiresRubric,
            AssessmentWeight = f.AssessmentWeight,
            FormativeInstructions = f.Instructions,
        };

        private static AssessmentResponse MapSummativeToResponse(SummativeAssessment s) => new()
        {
            Id = s.Id,
            AssessmentType = AssessmentTypeDto.Summative,
            Title = s.Title,
            Description = s.Description,
            TeacherId = s.TeacherId,
            TeacherName = s.Teacher != null ? $"{s.Teacher.FirstName} {s.Teacher.LastName}".Trim() : "-",
            SubjectId = s.SubjectId,
            SubjectName = s.Subject?.Name ?? "-",
            ClassId = s.ClassId,
            ClassName = s.Class?.Name ?? "-",
            TermId = s.TermId,
            TermName = s.Term?.Name ?? "-",
            AcademicYearId = s.AcademicYearId,
            AcademicYearName = s.AcademicYear?.Name ?? "-",
            AssessmentDate = s.AssessmentDate,
            MaximumScore = s.MaximumScore,
            IsPublished = s.IsPublished,
            PublishedDate = s.PublishedDate,
            CreatedOn = s.CreatedOn,
            ScoreCount = s.Scores?.Count ?? 0,

            ExamType = s.ExamType,
            Duration = s.Duration,
            NumberOfQuestions = s.NumberOfQuestions,
            PassMark = s.PassMark,
            HasPracticalComponent = s.HasPracticalComponent,
            PracticalWeight = s.PracticalWeight,
            TheoryWeight = s.TheoryWeight,
            SummativeInstructions = s.Instructions,
        };

        private static AssessmentResponse MapCompetencyToResponse(CompetencyAssessment c) => new()
        {
            Id = c.Id,
            AssessmentType = AssessmentTypeDto.Competency,
            Title = c.Title,
            Description = c.Description,
            TeacherId = c.TeacherId,
            TeacherName = c.Teacher != null ? $"{c.Teacher.FirstName} {c.Teacher.LastName}".Trim() : "-",
            SubjectId = c.SubjectId,
            SubjectName = c.Subject?.Name ?? "-",
            ClassId = c.ClassId,
            ClassName = c.Class?.Name ?? "-",
            TermId = c.TermId,
            TermName = c.Term?.Name ?? "-",
            AcademicYearId = c.AcademicYearId,
            AcademicYearName = c.AcademicYear?.Name ?? "-",
            AssessmentDate = c.AssessmentDate,
            MaximumScore = c.MaximumScore,
            IsPublished = c.IsPublished,
            PublishedDate = c.PublishedDate,
            CreatedOn = c.CreatedOn,
            ScoreCount = c.Scores?.Count ?? 0,

            CompetencyName = c.CompetencyName,
            CompetencyStrand = c.CompetencyStrand,
            CompetencySubStrand = c.CompetencySubStrand,
            TargetLevel = c.TargetLevel,
            PerformanceIndicators = c.PerformanceIndicators,
            AssessmentMethod = c.AssessmentMethod,
            RatingScale = c.RatingScale,
            IsObservationBased = c.IsObservationBased,
            ToolsRequired = c.ToolsRequired,
            CompetencyInstructions = c.Instructions,
            SpecificLearningOutcome = c.SpecificLearningOutcome,
        };

        private static AssessmentScoreResponse MapFormativeScore(FormativeAssessmentScore s) => new()
        {
            Id = s.Id,
            AssessmentType = AssessmentTypeDto.Formative,
            AssessmentId = s.FormativeAssessmentId,
            AssessmentTitle = s.FormativeAssessment?.Title ?? "-",
            StudentId = s.StudentId,
            StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}".Trim() : "-",
            StudentAdmissionNo = s.Student?.AdmissionNumber ?? "-",
            AssessmentDate = s.FormativeAssessment?.AssessmentDate ?? DateTime.MinValue,
            Score = s.Score,
            MaximumScore = s.MaximumScore,
            Percentage = s.Percentage,
            Grade = s.Grade,
            PerformanceLevel = s.PerformanceLevel,
            Feedback = s.Feedback,
            Strengths = s.Strengths,
            CompetencyAchieved = s.CompetencyAchieved,
            IsSubmitted = s.IsSubmitted,
            GradedByName = s.GradedBy != null
                ? $"{s.GradedBy.FirstName} {s.GradedBy.LastName}".Trim() : null,
        };

        private static AssessmentScoreResponse MapSummativeScore(SummativeAssessmentScore s) => new()
        {
            Id = s.Id,
            AssessmentType = AssessmentTypeDto.Summative,
            AssessmentId = s.SummativeAssessmentId,
            AssessmentTitle = s.SummativeAssessment?.Title ?? "-",
            StudentId = s.StudentId,
            StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}".Trim() : "-",
            StudentAdmissionNo = s.Student?.AdmissionNumber ?? "-",
            AssessmentDate = s.SummativeAssessment?.AssessmentDate ?? DateTime.MinValue,
            TheoryScore = s.TheoryScore,
            PracticalScore = s.PracticalScore,
            TotalScore = s.TotalScore,
            MaximumTotalScore = s.MaximumTotalScore,
            Remarks = s.Remarks,
            PositionInClass = s.PositionInClass,
            IsPassed = s.IsPassed,
            PerformanceStatus = s.PerformanceStatus,
            Comments = s.Comments,
        };

        private static AssessmentScoreResponse MapCompetencyScore(CompetencyAssessmentScore s) => new()
        {
            Id = s.Id,
            AssessmentType = AssessmentTypeDto.Competency,
            AssessmentId = s.CompetencyAssessmentId,
            AssessmentTitle = s.CompetencyAssessment?.Title ?? "-",
            StudentId = s.StudentId,
            StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}".Trim() : "-",
            StudentAdmissionNo = s.Student?.AdmissionNumber ?? "-",
            AssessmentDate = s.AssessmentDate,
            Rating = s.Rating,
            CompetencyLevel = s.CompetencyLevel,
            Evidence = s.Evidence,
            IsFinalized = s.IsFinalized,
            AssessorName = s.Assessor != null
                ? $"{s.Assessor.FirstName} {s.Assessor.LastName}".Trim() : null,
            Strand = s.Strand,
            SubStrand = s.SubStrand,
        };

        // ─────────────────────────────────────────────────────────────────────
        // ACCESS CONTROL HELPERS
        // ─────────────────────────────────────────────────────────────────────
        private static Guid ResolveTenant(Guid? requestTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (!isSuperAdmin)
                return userSchoolId
                    ?? throw new UnauthorizedException("School context is missing from the token.");

            return requestTenantId
                ?? throw new ValidationException(
                    "A school must be selected (tenantId is required for SuperAdmin).");
        }

        private static void ValidateTenantAccess(
            Guid entityTenantId, Guid? userSchoolId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return;

            if (userSchoolId == null || userSchoolId != entityTenantId)
                throw new UnauthorizedException("You do not have access to this assessment.");
        }

        private static IEnumerable<T> ApplyTenantFilter<T>(
            IEnumerable<T> entities, Guid? userSchoolId, bool isSuperAdmin)
            where T : Domain.Common.TenantBaseEntity<Guid>
        {
            if (isSuperAdmin) return entities;
            if (userSchoolId == null) return Enumerable.Empty<T>();
            return entities.Where(e => e.TenantId == userSchoolId);
        }
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// IRepositoryManager — add these properties to your existing interface
// ─────────────────────────────────────────────────────────────────────────────
//
// namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common
// {
//     public interface IRepositoryManager
//     {
//         // ... existing properties (School, User, Role, UserRole, etc.) ...
//
//         IFormativeAssessmentRepository          FormativeAssessment          { get; }
//         ISummativeAssessmentRepository          SummativeAssessment          { get; }
//         ICompetencyAssessmentRepository         CompetencyAssessment         { get; }
//         IFormativeAssessmentScoreRepository     FormativeAssessmentScore     { get; }
//         ISummativeAssessmentScoreRepository     SummativeAssessmentScore     { get; }
//         ICompetencyAssessmentScoreRepository    CompetencyAssessmentScore    { get; }
//
//         Task SaveAsync();
//     }
// }
//
// ─────────────────────────────────────────────────────────────────────────────
// DI Registration (Program.cs / ServiceCollectionExtensions.cs)
// ─────────────────────────────────────────────────────────────────────────────
//
// services.AddScoped<IAssessmentService, AssessmentService>();
// (repositories are already registered via IRepositoryManager)