using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers;

public class Subject(

    string Name,
    string Code,
    CBCLevel Level
) : TenantBaseEntity<Guid>
{
    #region Core Properties

    [Required, MaxLength(100)]
    public string Name { get; } = Name;

    [Required, MaxLength(20)]
    public string Code { get; } = Code;

    public CBCLevel Level { get; } = Level;

    [MaxLength(20)]
    public string? SubjectType { get; set; } // Core, Optional, Elective

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Classes where this subject is taught
    /// </summary>
    public ICollection<Class> Classes { get; set; } = new HashSet<Class>();

    /// <summary>
    /// Grades for this subject
    /// </summary>
    public ICollection<Grade> Grades { get; set; } = new HashSet<Grade>();

    /// <summary>
    /// Teachers who teach this subject
    /// </summary>
    public ICollection<Teacher> Teachers { get; set; } = new HashSet<Teacher>();

    #endregion
}
