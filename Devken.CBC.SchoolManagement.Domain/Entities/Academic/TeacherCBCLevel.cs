using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Academic
{
    public class TeacherCBCLevel : TenantBaseEntity<Guid>
    {
        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public CBCLevel Level { get; set; }
    }
}
