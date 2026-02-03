using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Finance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Helpers
{
    public class Staff : TenantBaseEntity<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(50)]
        public string StaffNumber { get; set; } = null!;

        // Navigation Properties
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<Payment> ReceivedPayments { get; set; } = new List<Payment>();
    }
}
