//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DevExpress.Drawing;
//using DevExpress.XtraPrinting;
//using DevExpress.XtraReports.UI;
//using Devken.CBC.SchoolManagement.Application.DTOs.Academics;
//using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
//using Devken.CBC.SchoolManagement.Infrastructure.Services.Administration.Subject;
//using Devken.CBC.SchoolManagement.Infrastructure.Services.Reports;

//namespace Devken.CBC.SchoolManagement.Infrastructure.Utilities
//{
//    /// <summary>
//    /// Subjects List report.
//    ///
//    /// When <paramref name="isSuperAdmin"/> is <c>true</c> and <paramref name="school"/> is
//    /// <c>null</c>, an extra "School" column is included so a SuperAdmin can see which
//    /// school each subject belongs to across the entire system.
//    /// </summary>
//    public class SubjectsListReportDocument : BaseSchoolReportDocument
//    {
//        private readonly IEnumerable<SubjectReportDto> _subjects;
//        private readonly bool _showSchoolColumn;

//        // Column widths — two layouts
//        //  Normal    : No. | Code | Name          | Level | Type     | Status
//        //  SuperAdmin: No. | Code | Name     | School | Level | Type | Status
//        private readonly float[] _colWidths;

//        public SubjectsListReportDocument(
//            School? school,
//            IEnumerable<SubjectReportDto> subjects,
//            byte[]? logoBytes,
//            bool isSuperAdmin = false)
//            : base(school, logoBytes, "SUBJECTS LIST REPORT", isSuperAdmin)
//        {
//            _subjects = subjects ?? throw new ArgumentNullException(nameof(subjects));
//            _showSchoolColumn = isSuperAdmin && school == null;

//            _colWidths = _showSchoolColumn
//                ? [40f, 90f, 165f, 145f, 110f, 110f, 90f]  // 7 cols = 750
//                : [40f, 100f, 215f, 140f, 150f, 105f];       // 6 cols = 750

//            Build();
//        }

//        // ── Body ───────────────────────────────────────────────────────────
//        protected override void BuildBody()
//        {
//            var detail = new DetailBand { HeightF = 0 };

//            var table = new XRTable
//            {
//                WidthF = PageWidth,
//                LocationF = new PointF(0, 6f),
//                Borders = BorderSide.All,
//                BorderColor = BorderClr,
//                BorderWidth = 1,
//                Font = new DXFont("Segoe UI", 9)
//            };

//            table.BeginInit();
//            table.Rows.Add(BuildHeaderRow());

//            int rowIndex = 0;
//            foreach (var subject in _subjects)
//                table.Rows.Add(BuildDataRow(subject, rowIndex++));

//            if (rowIndex == 0)
//                table.Rows.Add(BuildEmptyRow());

//            table.AdjustSize();
//            table.EndInit();

//            detail.Controls.Add(table);
//            detail.HeightF = table.HeightF + 10f;
//            Bands.Add(detail);
//        }

//        // ── Header row ─────────────────────────────────────────────────────
//        private XRTableRow BuildHeaderRow()
//        {
//            var row = new XRTableRow { HeightF = 28f };
//            int c = 0;

//            row.Cells.Add(HeaderCell("#", _colWidths[c++]));
//            row.Cells.Add(HeaderCell("Code", _colWidths[c++]));
//            row.Cells.Add(HeaderCell("Name", _colWidths[c++]));

//            if (_showSchoolColumn)
//                row.Cells.Add(HeaderCell("School", _colWidths[c++]));

//            row.Cells.Add(HeaderCell("Level", _colWidths[c++]));
//            row.Cells.Add(HeaderCell("Type", _colWidths[c++]));
//            row.Cells.Add(HeaderCell("Status", _colWidths[c]));

//            return row;
//        }

//        // ── Data row ───────────────────────────────────────────────────────
//        private XRTableRow BuildDataRow(SubjectReportDto s, int idx)
//        {
//            bool even = idx % 2 == 0;
//            var row = new XRTableRow { HeightF = 22f };
//            int c = 0;

//            row.Cells.Add(DataCell(
//                (idx + 1).ToString(),
//                _colWidths[c++],
//                even,
//                alignment: TextAlignment.MiddleCenter,
//                foreOverride: Color.FromArgb(130, 130, 130)));

//            row.Cells.Add(DataCell(s.Code, _colWidths[c++], even));
//            row.Cells.Add(DataCell(s.Name, _colWidths[c++], even));

//            if (_showSchoolColumn)
//                row.Cells.Add(DataCell(
//                    s.SchoolName ?? string.Empty,
//                    _colWidths[c++],
//                    even,
//                    foreOverride: Color.FromArgb(24, 72, 152)));

//            row.Cells.Add(DataCell(s.Level, _colWidths[c++], even));
//            row.Cells.Add(DataCell(s.SubjectType, _colWidths[c++], even));

//            bool active = s.IsActive;
//            row.Cells.Add(DataCell(
//                active ? "Active" : "Inactive",
//                _colWidths[c],
//                even,
//                foreOverride: active
//                    ? Color.FromArgb(30, 130, 60)
//                    : Color.FromArgb(180, 40, 40)));

//            return row;
//        }

//        // ── Empty state ────────────────────────────────────────────────────
//        private XRTableRow BuildEmptyRow()
//        {
//            var row = new XRTableRow { HeightF = 36f };
//            int cols = _showSchoolColumn ? 7 : 6;
//            float span = PageWidth / cols;

//            for (int i = 0; i < cols; i++)
//            {
//                row.Cells.Add(new XRTableCell
//                {
//                    Text = i == 0 ? "No subjects found." : string.Empty,
//                    WidthF = span,
//                    Font = new DXFont("Segoe UI", 9, DXFontStyle.Italic),
//                    TextAlignment = TextAlignment.MiddleLeft,
//                    Padding = new PaddingInfo(8, 4, 0, 0),
//                    BackColor = EvenRowBg,
//                    ForeColor = Color.FromArgb(150, 150, 150),
//                    Borders = BorderSide.All,
//                    BorderColor = BorderClr
//                });
//            }
//            return row;
//        }

//        // ── Cell factories (mirror StudentsListReportDocument exactly) ─────
//        private static XRTableCell HeaderCell(string text, float width) =>
//            new XRTableCell
//            {
//                Text = text,
//                WidthF = width,
//                Font = new DXFont("Segoe UI", 9, DXFontStyle.Bold),
//                TextAlignment = TextAlignment.MiddleLeft,
//                Padding = new PaddingInfo(6, 4, 0, 0),
//                BackColor = HeaderBg,
//                ForeColor = HeaderFg,
//                Borders = BorderSide.All,
//                BorderColor = BorderClr,
//                CanGrow = true
//            };

//        private static XRTableCell DataCell(
//            string text,
//            float width,
//            bool isEven,
//            TextAlignment alignment = TextAlignment.MiddleLeft,
//            Color? foreOverride = null) =>
//            new XRTableCell
//            {
//                Text = text,
//                WidthF = width,
//                Font = new DXFont("Segoe UI", 9),
//                TextAlignment = alignment,
//                Padding = new PaddingInfo(6, 4, 0, 0),
//                BackColor = isEven ? EvenRowBg : OddRowBg,
//                ForeColor = foreOverride ?? Color.FromArgb(40, 40, 40),
//                Borders = BorderSide.All,
//                BorderColor = BorderClr,
//                CanGrow = true
//            };
//    }
//}

