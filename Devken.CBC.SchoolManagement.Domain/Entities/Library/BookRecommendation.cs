using Devken.CBC.SchoolManagement.Domain.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Academic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Library
{
    public class BookRecommendation : TenantBaseEntity<Guid>
    {
        public Guid BookId { get; set; }

        public Guid StudentId { get; set; }

        public int Score { get; set; }

        public string Reason { get; set; } = string.Empty;
        // ── NAVIGATION PROPERTIES ──────────────────────────────────────────
        // These allow EF to perform Joins and solve the "Definition not found" error

        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
}
