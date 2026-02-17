using System;
using System.Collections.Generic;
using System.Drawing;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Reports.Student
{
    public class StudentsListReportDocument : BaseSchoolReportDocument
    {
        private readonly IEnumerable<StudentDto> _students;

        private static readonly float[] ColWidths = [150f, 250f, 175f, 175f];

        public StudentsListReportDocument(
            School school,
            IEnumerable<StudentDto> students,
            byte[]? logoBytes)
            : base(school, logoBytes, "STUDENTS LIST REPORT", showWatermark: false)
        {
            _students = students ?? throw new ArgumentNullException(nameof(students));
            Build();
        }

        protected override void BuildBody()
        {
            var detail = new DetailBand { HeightF = 0 };

            var table = new XRTable
            {
                WidthF = PageWidth,
                LocationF = new PointF(0, 6f),
                Borders = BorderSide.All,
                BorderColor = BorderClr,
                BorderWidth = 1,
                Font = new DXFont("Segoe UI", 9)
            };

            table.BeginInit();
            table.Rows.Add(BuildHeaderRow());

            var rowIndex = 0;
            foreach (var student in _students)
                table.Rows.Add(BuildDataRow(student, rowIndex++));

            table.AdjustSize();
            table.EndInit();

            detail.Controls.Add(table);
            detail.HeightF = table.HeightF + 10f;

            Bands.Add(detail);
        }

        private XRTableRow BuildHeaderRow()
        {
            var row = new XRTableRow { HeightF = 26f };
            row.Cells.Add(MakeCell("Admission No", ColWidths[0], bold: true, isHeader: true));
            row.Cells.Add(MakeCell("Full Name", ColWidths[1], bold: true, isHeader: true));
            row.Cells.Add(MakeCell("Class", ColWidths[2], bold: true, isHeader: true));
            row.Cells.Add(MakeCell("Status", ColWidths[3], bold: true, isHeader: true));
            return row;
        }

        private XRTableRow BuildDataRow(StudentDto s, int idx)
        {
            bool even = idx % 2 == 0;
            var row = new XRTableRow { HeightF = 22f };

            row.Cells.Add(MakeCell(s.AdmissionNumber ?? "", ColWidths[0], isEven: even));
            row.Cells.Add(MakeCell(s.FullName ?? "", ColWidths[1], isEven: even));
            row.Cells.Add(MakeCell(s.CurrentClassName ?? "", ColWidths[2], isEven: even));
            row.Cells.Add(MakeCell(
                s.IsActive ? "Active" : "Inactive",
                ColWidths[3],
                isEven: even,
                foreOverride: s.IsActive
                    ? Color.FromArgb(30, 130, 60)   // green
                    : Color.FromArgb(180, 40, 40))); // red

            return row;
        }

        private static XRTableCell MakeCell(
            string text,
            float width,
            bool bold = false,
            bool isHeader = false,
            bool isEven = true,
            Color? foreOverride = null) => new XRTableCell
            {
                Text = text,
                WidthF = width,
                Font = new DXFont("Segoe UI", 9, bold ? DXFontStyle.Bold : DXFontStyle.Regular),
                TextAlignment = TextAlignment.MiddleLeft,
                Padding = new PaddingInfo(6, 4, 0, 0),
                BackColor = isHeader ? HeaderBg
                          : isEven ? EvenRowBg
                                    : OddRowBg,
                ForeColor = isHeader ? HeaderFg
                          : foreOverride.HasValue ? foreOverride.Value
                                                   : Color.FromArgb(40, 40, 40),
                Borders = BorderSide.All,
                BorderColor = BorderClr,
                CanGrow = true
            };
    }
}