// Devken.CBC.SchoolManagement.Application/RepositoryManagers/Interfaces/Assessments/IAssessmentRepositories.cs
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Assessments
{
    // ─────────────────────────────────────────────────────────────────────────
    // FORMATIVE
    // ─────────────────────────────────────────────────────────────────────────
    public interface IFormativeAssessmentRepository
        : IRepositoryBase<FormativeAssessment, Guid>
    {
        Task<IEnumerable<FormativeAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId,
            Guid? teacherId, bool? isPublished, bool trackChanges = false);

        Task<FormativeAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

        /// <summary>Ignores global tenant filter — use for SuperAdmin.</summary>
        Task<FormativeAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false);

        Task<IEnumerable<FormativeAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false);

        Task<bool> IsPublishedAsync(Guid id);

        /// <summary>Loads navigations after Create+Save; safe for SuperAdmin (bypasses tenant filter).</summary>
        Task LoadNavigationsAsync(FormativeAssessment entity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SUMMATIVE
    // ─────────────────────────────────────────────────────────────────────────
    public interface ISummativeAssessmentRepository
        : IRepositoryBase<SummativeAssessment, Guid>
    {
        Task<IEnumerable<SummativeAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId,
            Guid? teacherId, bool? isPublished, bool trackChanges = false);

        Task<SummativeAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

        Task<SummativeAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false);

        Task<IEnumerable<SummativeAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false);

        Task<bool> IsPublishedAsync(Guid id);
        Task LoadNavigationsAsync(SummativeAssessment entity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMPETENCY
    // ─────────────────────────────────────────────────────────────────────────
    public interface ICompetencyAssessmentRepository
        : IRepositoryBase<CompetencyAssessment, Guid>
    {
        Task<IEnumerable<CompetencyAssessment>> GetAllAsync(
            Guid? classId, Guid? termId, Guid? subjectId,
            Guid? teacherId, bool? isPublished, bool trackChanges = false);

        Task<CompetencyAssessment?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

        Task<CompetencyAssessment?> GetByIdIgnoringTenantAsync(Guid id, bool trackChanges = false);

        Task<IEnumerable<CompetencyAssessment>> GetByClassAndTermAsync(
            Guid classId, Guid termId, bool trackChanges = false);

        Task<bool> IsPublishedAsync(Guid id);
        Task LoadNavigationsAsync(CompetencyAssessment entity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCORE REPOSITORIES
    // ─────────────────────────────────────────────────────────────────────────
    public interface IFormativeAssessmentScoreRepository
        : IRepositoryBase<FormativeAssessmentScore, Guid>
    {
        Task<IEnumerable<FormativeAssessmentScore>> GetByAssessmentAsync(Guid assessmentId, bool trackChanges = false);
        Task<IEnumerable<FormativeAssessmentScore>> GetByStudentAsync(Guid studentId, Guid? termId = null, bool trackChanges = false);
        Task<FormativeAssessmentScore?> GetByAssessmentAndStudentAsync(Guid assessmentId, Guid studentId, bool trackChanges = false);
    }

    public interface ISummativeAssessmentScoreRepository
        : IRepositoryBase<SummativeAssessmentScore, Guid>
    {
        Task<IEnumerable<SummativeAssessmentScore>> GetByAssessmentAsync(Guid assessmentId, bool trackChanges = false);
        Task<IEnumerable<SummativeAssessmentScore>> GetByStudentAsync(Guid studentId, Guid? termId = null, bool trackChanges = false);
        Task<SummativeAssessmentScore?> GetByAssessmentAndStudentAsync(Guid assessmentId, Guid studentId, bool trackChanges = false);
    }

    public interface ICompetencyAssessmentScoreRepository
        : IRepositoryBase<CompetencyAssessmentScore, Guid>
    {
        Task<IEnumerable<CompetencyAssessmentScore>> GetByAssessmentAsync(Guid assessmentId, bool trackChanges = false);
        Task<IEnumerable<CompetencyAssessmentScore>> GetByStudentAsync(Guid studentId, Guid? termId = null, bool trackChanges = false);
        Task<CompetencyAssessmentScore?> GetByAssessmentAndStudentAsync(Guid assessmentId, Guid studentId, bool trackChanges = false);
    }
}