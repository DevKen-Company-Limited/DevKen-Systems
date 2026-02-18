using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Assessments;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers;

public class Subject(
    string name,
    string code,
    CBCLevel level,
    SubjectType subjectType
) : TenantBaseEntity<Guid>
{
    #region Core Properties

    [Required, MaxLength(100)]
    public string Name { get; private set; } = name;

    [Required, MaxLength(20)]
    public string Code { get; private set; } = code;

    public CBCLevel Level { get; private set; } = level;


    public SubjectType SubjectType { get; private set; } = subjectType;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    #endregion

    #region Navigation Properties

    public ICollection<Class> Classes { get; set; } = new HashSet<Class>();

    public ICollection<Grade> Grades { get; set; } = new HashSet<Grade>();

    public ICollection<Teacher> Teachers { get; set; } = new HashSet<Teacher>();

    #endregion
}
