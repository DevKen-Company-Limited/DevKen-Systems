using Devken.CBC.SchoolManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Common
{
    public abstract class BaseEntity<TId> where TId : IEquatable<TId>
    {
        [Required]
        public TId? Id { get; set; }
        public EntityStatus Status { get; set; } = EntityStatus.Active;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}


public interface IAuditableEntity
{
    DateTime CreatedOn { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime UpdatedOn { get; set; }
    Guid? UpdatedBy { get; set; }
}


public interface ITenantEntity
{
    Guid TenantId { get; set; }
}