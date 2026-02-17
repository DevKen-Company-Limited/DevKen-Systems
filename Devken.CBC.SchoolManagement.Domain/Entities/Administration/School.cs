using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Administration
{
    public class School : BaseEntity<Guid>
    {
        public string SlugName { get; set; } = null!;
        public string Name { get; set; } = null!;

        public string? RegistrationNumber { get; set; }
        public string? KnecCenterCode { get; set; }
        public string? KraPin { get; set; }

        public string? Address { get; set; }
        public string? County { get; set; }
        public string? SubCounty { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }

        public SchoolType SchoolType { get; set; }
        public SchoolCategory Category { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<AcademicYear> AcademicYears { get; set; } = new List<AcademicYear>();
        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
    }

}
