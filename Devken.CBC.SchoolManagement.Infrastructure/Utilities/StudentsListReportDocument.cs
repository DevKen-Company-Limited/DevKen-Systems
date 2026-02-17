//using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;
//using System.ComponentModel;

//namespace Devken.CBC.SchoolManagement.Infrastructure.Utilities
//{


//    public class StudentsListReportDocument : IDocument
//    {
//        private readonly School _school;
//        private readonly IEnumerable<dynamic> _students;
//        private readonly byte[]? _logoBytes;

//        public StudentsListReportDocument(
//            School school,
//            IEnumerable<dynamic> students,
//            byte[]? logoBytes)
//        {
//            _school = school;
//            _students = students;
//            _logoBytes = logoBytes;
//        }

//        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

//        public void Compose(IDocumentContainer container)
//        {
//            container.Page(page =>
//            {
//                page.Margin(30);

//                page.Header().Element(ComposeHeader);

//                page.Content().Element(ComposeContent);

//                page.Footer()
//                    .AlignCenter()
//                    .Text(x =>
//                    {
//                        x.Span("Generated on ");
//                        x.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"));
//                    });
//            });
//        }

//        private void ComposeHeader(IContainer container)
//        {
//            container.Row(row =>
//            {
//                row.RelativeItem(1).Height(80).AlignMiddle().Element(c =>
//                {
//                    if (_logoBytes != null)
//                        c.Image(_logoBytes);
//                });

//                row.RelativeItem(4).Column(column =>
//                {
//                    column.Item().Text(_school.Name)
//                        .FontSize(18)
//                        .Bold();

//                    column.Item().Text(_school.Address ?? "");
//                    column.Item().Text($"{_school.County} - {_school.SubCounty}");
//                    column.Item().Text($"Phone: {_school.PhoneNumber} | Email: {_school.Email}");
//                });
//            });

//            container.PaddingVertical(10)
//                .LineHorizontal(1);
//        }

//        private void ComposeContent(IContainer container)
//        {
//            container.Column(column =>
//            {
//                column.Item().Text("STUDENTS LIST REPORT")
//                    .FontSize(16)
//                    .SemiBold()
//                    .AlignCenter();

//                column.Item().PaddingTop(15).Table(table =>
//                {
//                    table.ColumnsDefinition(columns =>
//                    {
//                        columns.RelativeColumn(2);
//                        columns.RelativeColumn(3);
//                        columns.RelativeColumn(2);
//                        columns.RelativeColumn(2);
//                    });

//                    table.Header(header =>
//                    {
//                        header.Cell().Text("Admission No").Bold();
//                        header.Cell().Text("Full Name").Bold();
//                        header.Cell().Text("Class").Bold();
//                        header.Cell().Text("Status").Bold();
//                    });

//                    foreach (var student in _students)
//                    {
//                        table.Cell().Text(student.AdmissionNumber);
//                        table.Cell().Text(student.FullName);
//                        table.Cell().Text(student.CurrentClassName);
//                        table.Cell().Text(student.IsActive ? "Active" : "Inactive");
//                    }
//                });
//            });
//        }
//    }

//}
