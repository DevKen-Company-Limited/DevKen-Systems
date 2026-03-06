using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Identity
{
    public class SsoSetupResult(bool success, string? message = null)
    {
        public bool Success { get; } = success;
        public string? Message { get; } = message;
        public Guid UserId { get; init; }
        public Guid TenantId { get; init; }
    }
}
