using Devken.CBC.SchoolManagement.Application.DTOs.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.Finance
{
    // New DTO
    public record PaymentPagedResultDto
    {
        public IEnumerable<PaymentResponseDto> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // Stats always computed on the FULL filtered set, not just the page
        public decimal TotalCollected { get; init; }
        public decimal TotalReversed { get; init; }
        public decimal NetAvailable { get; init; }
        public int TotalReversalCount { get; init; }
        public int PendingCount { get; init; }
        public int MpesaCount { get; init; }
        public int SchoolCount { get; set; }
    }
}
