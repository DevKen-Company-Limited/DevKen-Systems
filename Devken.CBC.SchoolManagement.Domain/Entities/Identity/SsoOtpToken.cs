using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Identity
{
    public class SsoOtpToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string OtpHash { get; set; } = string.Empty; 
        public string BindingToken { get; set; } = string.Empty;  
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConsumedAt { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
