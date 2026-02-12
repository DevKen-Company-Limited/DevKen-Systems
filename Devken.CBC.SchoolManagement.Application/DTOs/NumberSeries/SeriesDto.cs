using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.DTOs.NumberSeries
{
    public class CreateDocumentNumberSeriesDto
    {
        public string EntityName { get; set; } = default!;
        public string Prefix { get; set; } = default!;
        public int Padding { get; set; } = 5;
        public bool ResetEveryYear { get; set; }
        public Guid TenantId { get; set; }
    }

    public class UpdateDocumentNumberSeriesDto
    {
        public string Prefix { get; set; } = default!;
        public int Padding { get; set; }
        public bool ResetEveryYear { get; set; }
    }


}
