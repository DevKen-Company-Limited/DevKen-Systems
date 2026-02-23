using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Application.Exceptions;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Application.Service.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Assessments
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IRepositoryManager _repo;

        public AssessmentService(IRepositoryManager repo)
            => _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        // ── GET ALL ──────────────────────────────────────────────────────────
        public async Task<IEnumerable<AssessmentListItem>> GetAllAsync(
            AssessmentTypeDto? type, Guid? classId, Guid? termId,
            Guid? subjectId, Guid? teacherId, bool? isPublished,
            Guid? userSchoolId, bool isSuperAdmin)
        {
            var results = new List<AssessmentListItem>();

            bool doFormative = !type.HasValue || type == AssessmentTypeDto.Formative;
            bool doSummative = !type.HasValue || type == AssessmentTypeDto.Summative;
            bool doCompetency = !type.HasValue || type == AssessmentTypeDto.Competency;

            if (doFormative)
            {
                var items = await _repo.FormativeAssessment.GetAllAsync(
                    classId, termId, subjectId, teacherId, isPublished);
                results.AddRange(items.Select(f => new AssessmentListItem
                {
                    Id = f.Id,
                    Title = f.Title,
                    AssessmentType = AssessmentTypeDto.Formative,
                    TeacherName = $"{f.Teacher?.FirstName} {f.Teacher?.LastName}".Trim(),
                    SubjectName = f.Subject?.Name ?? string.Empty,
                    ClassName = f.Class?.Name ?? string.Empty,
                    TermName = f.Term?.Name ?? string.Empty,
                    AssessmentDate = f.AssessmentDate,
                    MaximumScore = f.MaximumScore,
                    IsPublished = f.IsPublished,
                    ScoreCount = f.Scores?.Count ?? 0,
                }));
            }

            if (doSummative)
            {
                var items = await _repo.SummativeAssessment.GetAllAsync(
                    classId, termId, subjectId, teacherId, isPublished);
                results.AddRange(items.Select(s => new AssessmentListItem
                {
                    Id = s.Id,
                    Title = s.Title,
                    AssessmentType = AssessmentTypeDto.Summative,
                    TeacherName = $"{s.Teacher?.FirstName} {s.Teacher?.LastName}".Trim(),
                    SubjectName = s.Subject?.Name ?? string.Empty,
                    ClassName = s.Class?.Name ?? string.Empty,
                    TermName = s.Term?.Name ?? string.Empty,
                    AssessmentDate = s.AssessmentDate,
                    MaximumScore = s.MaximumScore,
                    IsPublished = s.IsPublished,
                    ScoreCount = s.Scores?.Count ?? 0,
                }));
            }

            if (doCompetency)
            {
                var items = await _repo.CompetencyAssessment.GetAllAsync(
                    classId, termId, subjectId, teacherId, isPublished);
                results.AddRange(items.Select(c => new AssessmentListItem
                {
                    Id = c.Id,
                    Title = c.Title,
                    AssessmentType = AssessmentTypeDto.Competency,
                    TeacherName = $"{c.Teacher?.FirstName} {c.Teacher?.LastName}".Trim(),
                    SubjectName = c.Subject?.Name ?? string.Empty,
                    ClassName = c.Class?.Name ?? string.Empty,
                    TermName = c.Term?.Name ?? string.Empty,
                    AssessmentDate = c.AssessmentDate,
                    MaximumScore = c.MaximumScore,
                    IsPublished = c.IsPublished,
                    ScoreCount = c.Scores?.Count ?? 0,
                }));
            }

            return results.OrderByDescending(r => r.AssessmentDate);
        }

        // ── GET BY ID ────────────────────────────────────────────────────────
        // FIX: For SuperAdmin, use IgnoreQueryFilters path so the tenant filter
        //      does not silently block records belonging to other schools.
        public async Task<AssessmentResponse> GetByIdAsync(
            Guid id, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            return type switch
            {
                AssessmentTypeDto.Formative => await GetFormativeAsync(id, isSuperAdmin),
                AssessmentTypeDto.Summative => await GetSummativeAsync(id, isSuperAdmin),
                AssessmentTypeDto.Competency => await GetCompetencyAsync(id, isSuperAdmin),
                _ => throw new NotFoundException($"Assessment {id} not found.")
            };
        }

        /// <summary>
        /// Fetches a FormativeAssessment with full navigations.
        /// SuperAdmin bypasses the tenant query filter via IgnoreQueryFilters.
        /// </summary>
        private async Task<AssessmentResponse> GetFormativeAsync(Guid id, bool isSuperAdmin = false)
        {
            FormativeAssessment? entity;

            if (isSuperAdmin)
            {
                entity = await _repo.FormativeAssessment.GetByIdIgnoringTenantAsync(id)
                    ?? throw new NotFoundException($"Formative Assessment {id} not found.");
            }
            else
            {
                entity = await _repo.FormativeAssessment.GetByIdWithDetailsAsync(id)
                    ?? throw new NotFoundException($"Formative Assessment {id} not found.");
            }

            return MapFormativeResponse(entity);
        }

        /// <summary>
        /// Fetches a SummativeAssessment with full navigations.
        /// SuperAdmin bypasses the tenant query filter via IgnoreQueryFilters.
        /// </summary>
        private async Task<AssessmentResponse> GetSummativeAsync(Guid id, bool isSuperAdmin = false)
        {
            SummativeAssessment? entity;

            if (isSuperAdmin)
            {
                entity = await _repo.SummativeAssessment.GetByIdIgnoringTenantAsync(id)
                    ?? throw new NotFoundException($"Summative Assessment {id} not found.");
            }
            else
            {
                entity = await _repo.SummativeAssessment.GetByIdWithDetailsAsync(id)
                    ?? throw new NotFoundException($"Summative Assessment {id} not found.");
            }

            return MapSummativeResponse(entity);
        }

        /// <summary>
        /// Fetches a CompetencyAssessment with full navigations.
        /// SuperAdmin bypasses the tenant query filter via IgnoreQueryFilters.
        /// </summary>
        private async Task<AssessmentResponse> GetCompetencyAsync(Guid id, bool isSuperAdmin = false)
        {
            CompetencyAssessment? entity;

            if (isSuperAdmin)
            {
                entity = await _repo.CompetencyAssessment.GetByIdIgnoringTenantAsync(id)
                    ?? throw new NotFoundException($"Competency Assessment {id} not found.");
            }
            else
            {
                entity = await _repo.CompetencyAssessment.GetByIdWithDetailsAsync(id)
                    ?? throw new NotFoundException($"Competency Assessment {id} not found.");
            }

            return MapCompetencyResponse(entity);
        }

        // ── CREATE ───────────────────────────────────────────────────────────
        // After SaveAsync, call LoadNavigationsAsync on the already-tracked entity
        // to populate navigation properties without triggering the tenant filter.
        // This is safe for both regular users and SuperAdmin.
        public async Task<AssessmentResponse> CreateAsync(
            CreateAssessmentRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            ValidateCreateRequest(request, isSuperAdmin);

            switch (request.AssessmentType)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var entity = new FormativeAssessment();
                        ApplyShared(entity, request, userSchoolId, isSuperAdmin);
                        ApplyFormative(entity, request);
                        entity.AssessmentType = "Formative";
                        _repo.FormativeAssessment.Create(entity);
                        await _repo.SaveAsync();

                        // Load navigations on the tracked entity — bypasses tenant filter
                        await _repo.FormativeAssessment.LoadNavigationsAsync(entity);
                        return MapFormativeResponse(entity);
                    }

                case AssessmentTypeDto.Summative:
                    {
                        var entity = new SummativeAssessment();
                        ApplyShared(entity, request, userSchoolId, isSuperAdmin);
                        ApplySummative(entity, request);
                        entity.AssessmentType = "Summative";
                        _repo.SummativeAssessment.Create(entity);
                        await _repo.SaveAsync();

                        await _repo.SummativeAssessment.LoadNavigationsAsync(entity);
                        return MapSummativeResponse(entity);
                    }

                case AssessmentTypeDto.Competency:
                    {
                        if (string.IsNullOrWhiteSpace(request.CompetencyName))
                            throw new ValidationException("CompetencyName is required for Competency assessments.");

                        var entity = new CompetencyAssessment();
                        ApplyShared(entity, request, userSchoolId, isSuperAdmin);
                        ApplyCompetency(entity, request);
                        entity.AssessmentType = "Competency";
                        _repo.CompetencyAssessment.Create(entity);
                        await _repo.SaveAsync();

                        await _repo.CompetencyAssessment.LoadNavigationsAsync(entity);
                        return MapCompetencyResponse(entity);
                    }

                default:
                    throw new ValidationException("Invalid AssessmentType.");
            }
        }

        // ── UPDATE ───────────────────────────────────────────────────────────
        // FIX: After updating, re-fetch via the SuperAdmin-aware helper so the
        //      response is populated correctly regardless of TenantContext.
        public async Task<AssessmentResponse> UpdateAsync(
            Guid id, UpdateAssessmentRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (request.AssessmentType)
            {
                case AssessmentTypeDto.Formative:
                    {
                        // For fetch-before-update, also use IgnoreQueryFilters for SuperAdmin
                        var entity = isSuperAdmin
                            ? await _repo.FormativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Formative Assessment {id} not found.")
                            : await _repo.FormativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Formative Assessment {id} not found.");

                        ApplyShared(entity, request, userSchoolId, isSuperAdmin);
                        ApplyFormative(entity, request);
                        _repo.FormativeAssessment.Update(entity);
                        await _repo.SaveAsync();

                        // Re-fetch with navigations via the SuperAdmin-aware helper
                        return await GetFormativeAsync(id, isSuperAdmin);
                    }

                case AssessmentTypeDto.Summative:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.SummativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Summative Assessment {id} not found.")
                            : await _repo.SummativeAssessment.GetByIdWithDetailsAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Summative Assessment {id} not found.");

                        ApplyShared(entity, request, userSchoolId, isSuperAdmin);
                        ApplySummative(entity, request);
                        _repo.SummativeAssessment.Update(entity);
                        await _repo.SaveAsync();

                        return await GetSummativeAsync(id, isSuperAdmin);
                    }

                case AssessmentTypeDto.Competency:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.CompetencyAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Competency Assessment {id} not found.")
                            : await _repo.CompetencyAssessment.GetByIdWithDetailsAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Competency Assessment {id} not found.");

                        ApplyShared(entity, request, userSchoolId, isSuperAdmin);
                        ApplyCompetency(entity, request);
                        _repo.CompetencyAssessment.Update(entity);
                        await _repo.SaveAsync();

                        return await GetCompetencyAsync(id, isSuperAdmin);
                    }

                default:
                    throw new ValidationException("Invalid AssessmentType.");
            }
        }

        // ── PUBLISH ──────────────────────────────────────────────────────────
        // FIX: SuperAdmin uses IgnoreQueryFilters path for fetching before publish.
        public async Task PublishAsync(
            Guid id, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (type)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.FormativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Formative Assessment {id} not found.")
                            : await _repo.FormativeAssessment.GetByIdAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Formative Assessment {id} not found.");

                        if (entity.IsPublished)
                            throw new ConflictException("Assessment is already published.");

                        entity.IsPublished = true;
                        entity.PublishedDate = DateTime.UtcNow;
                        _repo.FormativeAssessment.Update(entity);
                        break;
                    }

                case AssessmentTypeDto.Summative:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.SummativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Summative Assessment {id} not found.")
                            : await _repo.SummativeAssessment.GetByIdAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Summative Assessment {id} not found.");

                        if (entity.IsPublished)
                            throw new ConflictException("Assessment is already published.");

                        entity.IsPublished = true;
                        entity.PublishedDate = DateTime.UtcNow;
                        _repo.SummativeAssessment.Update(entity);
                        break;
                    }

                case AssessmentTypeDto.Competency:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.CompetencyAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Competency Assessment {id} not found.")
                            : await _repo.CompetencyAssessment.GetByIdAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Competency Assessment {id} not found.");

                        if (entity.IsPublished)
                            throw new ConflictException("Assessment is already published.");

                        entity.IsPublished = true;
                        entity.PublishedDate = DateTime.UtcNow;
                        _repo.CompetencyAssessment.Update(entity);
                        break;
                    }
            }

            await _repo.SaveAsync();
        }

        // ── DELETE ───────────────────────────────────────────────────────────
        // FIX: SuperAdmin uses IgnoreQueryFilters path for fetching before delete.
        public async Task DeleteAsync(
            Guid id, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (type)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.FormativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Formative Assessment {id} not found.")
                            : await _repo.FormativeAssessment.GetByIdAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Formative Assessment {id} not found.");

                        if (entity.IsPublished)
                            throw new ConflictException("Cannot delete a published assessment. Unpublish it first.");

                        _repo.FormativeAssessment.Delete(entity);
                        break;
                    }

                case AssessmentTypeDto.Summative:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.SummativeAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Summative Assessment {id} not found.")
                            : await _repo.SummativeAssessment.GetByIdAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Summative Assessment {id} not found.");

                        if (entity.IsPublished)
                            throw new ConflictException("Cannot delete a published assessment. Unpublish it first.");

                        _repo.SummativeAssessment.Delete(entity);
                        break;
                    }

                case AssessmentTypeDto.Competency:
                    {
                        var entity = isSuperAdmin
                            ? await _repo.CompetencyAssessment.GetByIdIgnoringTenantAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Competency Assessment {id} not found.")
                            : await _repo.CompetencyAssessment.GetByIdAsync(id, trackChanges: true)
                                  ?? throw new NotFoundException($"Competency Assessment {id} not found.");

                        if (entity.IsPublished)
                            throw new ConflictException("Cannot delete a published assessment. Unpublish it first.");

                        _repo.CompetencyAssessment.Delete(entity);
                        break;
                    }
            }

            await _repo.SaveAsync();
        }

        // ── SCORES ───────────────────────────────────────────────────────────
        public async Task<IEnumerable<AssessmentScoreResponse>> GetScoresAsync(
            Guid assessmentId, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            return type switch
            {
                AssessmentTypeDto.Formative => (await _repo.FormativeAssessmentScore
                    .GetByAssessmentAsync(assessmentId)).Select(MapFormativeScore),

                AssessmentTypeDto.Summative => (await _repo.SummativeAssessmentScore
                    .GetByAssessmentAsync(assessmentId)).Select(MapSummativeScore),

                AssessmentTypeDto.Competency => (await _repo.CompetencyAssessmentScore
                    .GetByAssessmentAsync(assessmentId)).Select(MapCompetencyScore),

                _ => throw new ValidationException("Invalid AssessmentType.")
            };
        }

        public async Task<AssessmentScoreResponse> UpsertScoreAsync(
            UpsertScoreRequest request, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (request.AssessmentType)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var existing = await _repo.FormativeAssessmentScore
                            .GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId, trackChanges: true);

                        if (existing == null)
                        {
                            var score = new FormativeAssessmentScore
                            {
                                FormativeAssessmentId = request.AssessmentId,
                                StudentId = request.StudentId,
                            };
                            ApplyFormativeScore(score, request);
                            _repo.FormativeAssessmentScore.Create(score);
                            await _repo.SaveAsync();
                            var created = await _repo.FormativeAssessmentScore
                                .GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId);
                            return MapFormativeScore(created!);
                        }

                        ApplyFormativeScore(existing, request);
                        _repo.FormativeAssessmentScore.Update(existing);
                        await _repo.SaveAsync();
                        return MapFormativeScore(existing);
                    }

                case AssessmentTypeDto.Summative:
                    {
                        var existing = await _repo.SummativeAssessmentScore
                            .GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId, trackChanges: true);

                        if (existing == null)
                        {
                            var score = new SummativeAssessmentScore
                            {
                                SummativeAssessmentId = request.AssessmentId,
                                StudentId = request.StudentId,
                            };
                            ApplySummativeScore(score, request);
                            _repo.SummativeAssessmentScore.Create(score);
                            await _repo.SaveAsync();
                            var created = await _repo.SummativeAssessmentScore
                                .GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId);
                            return MapSummativeScore(created!);
                        }

                        ApplySummativeScore(existing, request);
                        _repo.SummativeAssessmentScore.Update(existing);
                        await _repo.SaveAsync();
                        return MapSummativeScore(existing);
                    }

                case AssessmentTypeDto.Competency:
                    {
                        var existing = await _repo.CompetencyAssessmentScore
                            .GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId, trackChanges: true);

                        if (existing == null)
                        {
                            var score = new CompetencyAssessmentScore
                            {
                                CompetencyAssessmentId = request.AssessmentId,
                                StudentId = request.StudentId,
                                Rating = request.Rating ?? "Not Assessed",
                            };
                            ApplyCompetencyScore(score, request);
                            _repo.CompetencyAssessmentScore.Create(score);
                            await _repo.SaveAsync();
                            var created = await _repo.CompetencyAssessmentScore
                                .GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId);
                            return MapCompetencyScore(created!);
                        }

                        ApplyCompetencyScore(existing, request);
                        _repo.CompetencyAssessmentScore.Update(existing);
                        await _repo.SaveAsync();
                        return MapCompetencyScore(existing);
                    }

                default:
                    throw new ValidationException("Invalid AssessmentType.");
            }
        }

        public async Task DeleteScoreAsync(
            Guid scoreId, AssessmentTypeDto type, Guid? userSchoolId, bool isSuperAdmin)
        {
            switch (type)
            {
                case AssessmentTypeDto.Formative:
                    {
                        var score = await _repo.FormativeAssessmentScore.GetByIdAsync(scoreId, trackChanges: true)
                            ?? throw new NotFoundException($"Formative score {scoreId} not found.");
                        _repo.FormativeAssessmentScore.Delete(score);
                        break;
                    }
                case AssessmentTypeDto.Summative:
                    {
                        var score = await _repo.SummativeAssessmentScore.GetByIdAsync(scoreId, trackChanges: true)
                            ?? throw new NotFoundException($"Summative score {scoreId} not found.");
                        _repo.SummativeAssessmentScore.Delete(score);
                        break;
                    }
                case AssessmentTypeDto.Competency:
                    {
                        var score = await _repo.CompetencyAssessmentScore.GetByIdAsync(scoreId, trackChanges: true)
                            ?? throw new NotFoundException($"Competency score {scoreId} not found.");
                        _repo.CompetencyAssessmentScore.Delete(score);
                        break;
                    }
            }

            await _repo.SaveAsync();
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE — Validation & Apply helpers
        // ═════════════════════════════════════════════════════════════════════

        private static void ValidateCreateRequest(CreateAssessmentRequest r, bool isSuperAdmin)
        {
            if (isSuperAdmin && (r.TenantId == null || r.TenantId == Guid.Empty))
                throw new ValidationException("TenantId is required for SuperAdmin.");
        }

        private static void ApplyShared(Assessment1 e, CreateAssessmentRequest r,
            Guid? userSchoolId, bool isSuperAdmin)
        {
            e.Title = r.Title;
            e.Description = r.Description;
            e.TeacherId = r.TeacherId;
            e.SubjectId = r.SubjectId;
            e.ClassId = r.ClassId;
            e.TermId = r.TermId;
            e.AcademicYearId = r.AcademicYearId;
            e.AssessmentDate = r.AssessmentDate;
            e.MaximumScore = r.MaximumScore;
            e.IsPublished = r.IsPublished;

            if (e.TenantId == Guid.Empty)
                e.TenantId = isSuperAdmin ? r.TenantId!.Value : userSchoolId!.Value;
        }

        private static void ApplyFormative(FormativeAssessment e, CreateAssessmentRequest r)
        {
            e.FormativeType = r.FormativeType;
            e.CompetencyArea = r.CompetencyArea;
            e.LearningOutcomeId = r.LearningOutcomeId;
            e.Strand = r.FormativeStrand;
            e.SubStrand = r.FormativeSubStrand;
            e.Criteria = r.Criteria;
            e.Instructions = r.FormativeInstructions;
            e.FeedbackTemplate = r.FeedbackTemplate;
            e.RequiresRubric = r.RequiresRubric;
            e.AssessmentWeight = r.AssessmentWeight;
        }

        private static void ApplySummative(SummativeAssessment e, CreateAssessmentRequest r)
        {
            e.ExamType = r.ExamType;
            e.Duration = r.Duration;
            e.NumberOfQuestions = r.NumberOfQuestions;
            e.PassMark = r.PassMark;
            e.HasPracticalComponent = r.HasPracticalComponent;
            e.PracticalWeight = r.PracticalWeight;
            e.TheoryWeight = r.TheoryWeight;
            e.Instructions = r.SummativeInstructions;
        }

        private static void ApplyCompetency(CompetencyAssessment e, CreateAssessmentRequest r)
        {
            e.CompetencyName = r.CompetencyName!;
            e.Strand = r.CompetencyStrand;
            e.SubStrand = r.CompetencySubStrand;
            e.TargetLevel = r.TargetLevel;
            e.PerformanceIndicators = r.PerformanceIndicators;
            e.AssessmentMethod = r.AssessmentMethod;
            e.RatingScale = r.RatingScale;
            e.IsObservationBased = r.IsObservationBased;
            e.ToolsRequired = r.ToolsRequired;
            e.Instructions = r.CompetencyInstructions;
            e.SpecificLearningOutcome = r.SpecificLearningOutcome;
        }

        private static void ApplyFormativeScore(FormativeAssessmentScore s, UpsertScoreRequest r)
        {
            s.Score = r.Score ?? 0;
            s.MaximumScore = r.MaximumScore ?? 0;
            s.Grade = r.Grade;
            s.PerformanceLevel = r.PerformanceLevel;
            s.Feedback = r.Feedback;
            s.Strengths = r.Strengths;
            s.AreasForImprovement = r.AreasForImprovement;
            s.IsSubmitted = r.IsSubmitted;
            s.SubmissionDate = r.SubmissionDate;
            s.CompetencyArea = r.CompetencyArea;
            s.CompetencyAchieved = r.CompetencyAchieved;
            s.GradedById = r.GradedById;
            s.GradedDate = DateTime.UtcNow;
        }

        private static void ApplySummativeScore(SummativeAssessmentScore s, UpsertScoreRequest r)
        {
            s.TheoryScore = r.TheoryScore ?? 0;
            s.PracticalScore = r.PracticalScore;
            s.MaximumTheoryScore = r.MaximumTheoryScore ?? 0;
            s.MaximumPracticalScore = r.MaximumPracticalScore;
            s.Grade = r.Grade;
            s.Remarks = r.Remarks;
            s.PositionInClass = r.PositionInClass;
            s.PositionInStream = r.PositionInStream;
            s.IsPassed = r.IsPassed;
            s.Comments = r.Comments;
            s.GradedById = r.GradedById;
            s.GradedDate = DateTime.UtcNow;
        }

        private static void ApplyCompetencyScore(CompetencyAssessmentScore s, UpsertScoreRequest r)
        {
            s.Rating = r.Rating ?? "Not Assessed";
            s.ScoreValue = r.ScoreValue;
            s.Evidence = r.Evidence;
            s.AssessmentDate = DateTime.UtcNow;
            s.AssessmentMethod = r.AssessmentMethod;
            s.ToolsUsed = r.ToolsUsed;
            s.Feedback = r.Feedback;
            s.AreasForImprovement = r.AreasForImprovement;
            s.IsFinalized = r.IsFinalized;
            s.Strand = r.Strand;
            s.SubStrand = r.SubStrand;
            s.SpecificLearningOutcome = r.SpecificLearningOutcome;
            s.AssessorId = r.AssessorId;
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE — Response mappers
        // ═════════════════════════════════════════════════════════════════════

        private static AssessmentResponse MapShared(Assessment1 a) => new()
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            TeacherId = a.TeacherId,
            TeacherName = $"{a.Teacher?.FirstName} {a.Teacher?.LastName}".Trim(),
            SubjectId = a.SubjectId,
            SubjectName = a.Subject?.Name ?? string.Empty,
            ClassId = a.ClassId,
            ClassName = a.Class?.Name ?? string.Empty,
            TermId = a.TermId,
            TermName = a.Term?.Name ?? string.Empty,
            AcademicYearId = a.AcademicYearId,
            AcademicYearName = a.AcademicYear?.Name ?? string.Empty,
            AssessmentDate = a.AssessmentDate,
            MaximumScore = a.MaximumScore,
            IsPublished = a.IsPublished,
            PublishedDate = a.PublishedDate,
            CreatedOn = a.CreatedOn,
        };

        private static AssessmentResponse MapFormativeResponse(FormativeAssessment f)
        {
            var r = MapShared(f);
            r.AssessmentType = AssessmentTypeDto.Formative;
            r.ScoreCount = f.Scores?.Count ?? 0;
            r.FormativeType = f.FormativeType;
            r.CompetencyArea = f.CompetencyArea;
            r.LearningOutcomeId = f.LearningOutcomeId;
            r.LearningOutcomeName = f.LearningOutcome?.Outcome;
            r.FormativeStrand = f.Strand;
            r.FormativeSubStrand = f.SubStrand;
            r.Criteria = f.Criteria;
            r.FeedbackTemplate = f.FeedbackTemplate;
            r.RequiresRubric = f.RequiresRubric;
            r.AssessmentWeight = f.AssessmentWeight;
            r.FormativeInstructions = f.Instructions;
            return r;
        }

        private static AssessmentResponse MapSummativeResponse(SummativeAssessment s)
        {
            var r = MapShared(s);
            r.AssessmentType = AssessmentTypeDto.Summative;
            r.ScoreCount = s.Scores?.Count ?? 0;
            r.ExamType = s.ExamType;
            r.Duration = s.Duration;
            r.NumberOfQuestions = s.NumberOfQuestions;
            r.PassMark = s.PassMark;
            r.HasPracticalComponent = s.HasPracticalComponent;
            r.PracticalWeight = s.PracticalWeight;
            r.TheoryWeight = s.TheoryWeight;
            r.SummativeInstructions = s.Instructions;
            return r;
        }

        private static AssessmentResponse MapCompetencyResponse(CompetencyAssessment c)
        {
            var r = MapShared(c);
            r.AssessmentType = AssessmentTypeDto.Competency;
            r.ScoreCount = c.Scores?.Count ?? 0;
            r.CompetencyName = c.CompetencyName;
            r.CompetencyStrand = c.Strand;
            r.CompetencySubStrand = c.SubStrand;
            r.TargetLevel = c.TargetLevel;
            r.PerformanceIndicators = c.PerformanceIndicators;
            r.AssessmentMethod = c.AssessmentMethod;
            r.RatingScale = c.RatingScale;
            r.IsObservationBased = c.IsObservationBased;
            r.ToolsRequired = c.ToolsRequired;
            r.CompetencyInstructions = c.Instructions;
            r.SpecificLearningOutcome = c.SpecificLearningOutcome;
            return r;
        }

        private static AssessmentScoreResponse MapFormativeScore(FormativeAssessmentScore s) => new()
        {
            Id = s.Id,
            AssessmentType = AssessmentTypeDto.Formative,
            AssessmentId = s.FormativeAssessmentId,
            AssessmentTitle = s.FormativeAssessment?.Title ?? string.Empty,
            StudentId = s.StudentId,
            StudentName = $"{s.Student?.FirstName} {s.Student?.LastName}".Trim(),
            StudentAdmissionNo = s.Student?.AdmissionNumber ?? string.Empty,
            AssessmentDate = s.FormativeAssessment?.AssessmentDate ?? default,
            Score = s.Score,
            MaximumScore = s.MaximumScore,
            Percentage = s.MaximumScore > 0 ? (s.Score / s.MaximumScore) * 100 : 0,
            Grade = s.Grade,
            PerformanceLevel = s.PerformanceLevel,
            Feedback = s.Feedback,
            Strengths = s.Strengths,
            CompetencyAchieved = s.CompetencyAchieved,
            IsSubmitted = s.IsSubmitted,
            GradedByName = $"{s.GradedBy?.FirstName} {s.GradedBy?.LastName}".Trim(),
        };

        private static AssessmentScoreResponse MapSummativeScore(SummativeAssessmentScore s) => new()
        {
            Id = s.Id,
            AssessmentType = AssessmentTypeDto.Summative,
            AssessmentId = s.SummativeAssessmentId,
            AssessmentTitle = s.SummativeAssessment?.Title ?? string.Empty,
            StudentId = s.StudentId,
            StudentName = $"{s.Student?.FirstName} {s.Student?.LastName}".Trim(),
            StudentAdmissionNo = s.Student?.AdmissionNumber ?? string.Empty,
            AssessmentDate = s.SummativeAssessment?.AssessmentDate ?? default,
            TheoryScore = s.TheoryScore,
            PracticalScore = s.PracticalScore,
            TotalScore = s.TheoryScore + (s.PracticalScore ?? 0),
            MaximumTotalScore = s.MaximumTheoryScore + (s.MaximumPracticalScore ?? 0),
            Grade = s.Grade,
            Remarks = s.Remarks,
            PositionInClass = s.PositionInClass,
            IsPassed = s.IsPassed,
            Comments = s.Comments,
            GradedByName = $"{s.GradedBy?.FirstName} {s.GradedBy?.LastName}".Trim(),
            PerformanceStatus = s.MaximumTheoryScore > 0
                ? ((s.TheoryScore + (s.PracticalScore ?? 0)) /
                   (s.MaximumTheoryScore + (s.MaximumPracticalScore ?? 0)) * 100) switch
                {
                    var pct when pct >= 80 => "Excellent",
                    var pct when pct >= 70 => "Very Good",
                    var pct when pct >= 60 => "Good",
                    var pct when pct >= 50 => "Average",
                    var pct when pct >= 40 => "Below Average",
                    _ => "Poor"
                }
                : "N/A",
        };

        private static AssessmentScoreResponse MapCompetencyScore(CompetencyAssessmentScore s) => new()
        {
            Id = s.Id,
            AssessmentType = AssessmentTypeDto.Competency,
            AssessmentId = s.CompetencyAssessmentId,
            AssessmentTitle = s.CompetencyAssessment?.Title ?? string.Empty,
            StudentId = s.StudentId,
            StudentName = $"{s.Student?.FirstName} {s.Student?.LastName}".Trim(),
            StudentAdmissionNo = s.Student?.AdmissionNumber ?? string.Empty,
            AssessmentDate = s.AssessmentDate,
            Rating = s.Rating,
            CompetencyLevel = s.CompetencyLevel,
            Evidence = s.Evidence,
            IsFinalized = s.IsFinalized,
            Feedback = s.Feedback,
            Strand = s.Strand,
            SubStrand = s.SubStrand,
            AssessorName = $"{s.Assessor?.FirstName} {s.Assessor?.LastName}".Trim(),
        };
    }
}