using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using Devken.CBC.SchoolManagement.Application.DTOs.Assessments;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;
using Devken.CBC.SchoolManagement.Infrastructure.Services.Reports;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Reports.Assessment
{
    /// <summary>
    /// Assessments List PDF report.
    ///
    /// Columns (normal school view):
    ///   #  |  Title  |  Type  |  Subject  |  Class  |  Term  |  Date  |  Max Score  |  Scores  |  Status
    ///
    /// When <paramref name="isSuperAdmin"/> is <c>true</c> and <paramref name="school"/> is
    /// <c>null</c> an extra "School" column is inserted after "Title" so the SuperAdmin
    /// can see which school each assessment belongs to.
    /// </summary>
    public class AssessmentsListReportDocument : BaseSchoolReportDocument
    {
        private readonly IEnumerable<AssessmentReportDto> _assessments;
        private readonly bool _showSchoolColumn;

        // ── Column widths ────────────────────────────────────────────────────
        // Total must equal PageWidth (750 f).
        //
        // Normal    (9 cols):  35 | 170 | 72 | 85 | 65 | 65 | 68 | 60 | 60 | 70  = 750
        // SuperAdmin (10 cols): 30 | 130 | 100 | 65 | 72 | 58 | 58 | 65 | 52 | 52 | 68 = 750
        private readonly float[] _colWidths;

        private static readonly Color FormativeFg = Color.FromArgb(30, 130, 60);   // green
        private static readonly Color SummativeFg = Color.FromArgb(24, 72, 152);   // brand blue
        private static readonly Color CompetencyFg = Color.FromArgb(150, 70, 10);   // amber

        public AssessmentsListReportDocument(
            School? school,
            IEnumerable<AssessmentReportDto> assessments,
            byte[]? logoBytes,
            bool isSuperAdmin = false)
            : base(school, logoBytes, "ASSESSMENTS LIST REPORT", isSuperAdmin)
        {
            _assessments = assessments ?? throw new ArgumentNullException(nameof(assessments));
            _showSchoolColumn = isSuperAdmin && school == null;

            _colWidths = _showSchoolColumn
                ? [30f, 130f, 100f, 65f, 72f, 58f, 58f, 65f, 52f, 52f, 68f]  // 11 cols (including School) = 750
                : [35f, 170f, 72f, 85f, 65f, 65f, 68f, 60f, 60f, 70f];        // 10 cols = 750

            Build();
        }

        // ── Body ─────────────────────────────────────────────────────────────
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
                Font = new DXFont("Segoe UI", 8.5f)
            };

            table.BeginInit();
            table.Rows.Add(BuildHeaderRow());

            int rowIndex = 0;
            foreach (var a in _assessments)
                table.Rows.Add(BuildDataRow(a, rowIndex++));

            if (rowIndex == 0)
                table.Rows.Add(BuildEmptyRow());

            table.AdjustSize();
            table.EndInit();

            detail.Controls.Add(table);
            detail.HeightF = table.HeightF + 10f;
            Bands.Add(detail);
        }

        // ── Header row ────────────────────────────────────────────────────────
        private XRTableRow BuildHeaderRow()
        {
            var row = new XRTableRow { HeightF = 28f };
            int c = 0;

            row.Cells.Add(HeaderCell("#", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Title", _colWidths[c++]));

            if (_showSchoolColumn)
                row.Cells.Add(HeaderCell("School", _colWidths[c++]));

            row.Cells.Add(HeaderCell("Type", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Subject", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Class", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Term", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Date", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Max Score", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Scores", _colWidths[c++]));
            row.Cells.Add(HeaderCell("Status", _colWidths[c]));

            return row;
        }

        // ── Data row ──────────────────────────────────────────────────────────
        private XRTableRow BuildDataRow(AssessmentReportDto a, int idx)
        {
            bool even = idx % 2 == 0;
            var row = new XRTableRow { HeightF = 22f };
            int c = 0;

            // # (row number)
            row.Cells.Add(DataCell(
                (idx + 1).ToString(),
                _colWidths[c++],
                even,
                alignment: TextAlignment.MiddleCenter,
                foreOverride: Color.FromArgb(130, 130, 130)));

            // Title
            row.Cells.Add(DataCell(a.Title, _colWidths[c++], even));

            // School (SuperAdmin cross-school only)
            if (_showSchoolColumn)
                row.Cells.Add(DataCell(
                    a.SchoolName ?? string.Empty,
                    _colWidths[c++],
                    even,
                    foreOverride: Color.FromArgb(24, 72, 152)));

            // Assessment Type — colour-coded
            var typeFg = a.AssessmentType switch
            {
                AssessmentTypeDto.Formative => FormativeFg,
                AssessmentTypeDto.Summative => SummativeFg,
                AssessmentTypeDto.Competency => CompetencyFg,
                _ => Color.FromArgb(40, 40, 40)
            };
            row.Cells.Add(DataCell(a.AssessmentTypeLabel, _colWidths[c++], even, foreOverride: typeFg));

            // Subject
            row.Cells.Add(DataCell(a.SubjectName, _colWidths[c++], even));

            // Class
            row.Cells.Add(DataCell(a.ClassName, _colWidths[c++], even));

            // Term
            row.Cells.Add(DataCell(a.TermName, _colWidths[c++], even));

            // Assessment Date
            row.Cells.Add(DataCell(
                a.AssessmentDate.ToString("dd MMM yyyy"),
                _colWidths[c++],
                even,
                alignment: TextAlignment.MiddleCenter));

            // Maximum Score
            row.Cells.Add(DataCell(
                a.MaximumScore.ToString("N0"),
                _colWidths[c++],
                even,
                alignment: TextAlignment.MiddleCenter));

            // Score Count
            row.Cells.Add(DataCell(
                a.ScoreCount.ToString(),
                _colWidths[c++],
                even,
                alignment: TextAlignment.MiddleCenter,
                foreOverride: a.ScoreCount > 0
                    ? Color.FromArgb(24, 72, 152)
                    : Color.FromArgb(170, 170, 170)));

            // Published / Draft status
            bool published = a.IsPublished;
            row.Cells.Add(DataCell(
                published ? "Published" : "Draft",
                _colWidths[c],
                even,
                foreOverride: published
                    ? Color.FromArgb(30, 130, 60)
                    : Color.FromArgb(180, 40, 40)));

            return row;
        }

        // ── Empty state ───────────────────────────────────────────────────────
        private XRTableRow BuildEmptyRow()
        {
            var row = new XRTableRow { HeightF = 36f };
            int cols = _showSchoolColumn ? 11 : 10;
            float span = PageWidth / cols;

            for (int i = 0; i < cols; i++)
            {
                row.Cells.Add(new XRTableCell
                {
                    Text = i == 0 ? "No assessments found." : string.Empty,
                    WidthF = span,
                    Font = new DXFont("Segoe UI", 9, DXFontStyle.Italic),
                    TextAlignment = TextAlignment.MiddleLeft,
                    Padding = new PaddingInfo(8, 4, 0, 0),
                    BackColor = EvenRowBg,
                    ForeColor = Color.FromArgb(150, 150, 150),
                    Borders = BorderSide.All,
                    BorderColor = BorderClr
                });
            }
            return row;
        }

        // ── Cell factories ────────────────────────────────────────────────────
        private static XRTableCell HeaderCell(string text, float width) =>
            new XRTableCell
            {
                Text = text,
                WidthF = width,
                Font = new DXFont("Segoe UI", 8.5f, DXFontStyle.Bold),
                TextAlignment = TextAlignment.MiddleLeft,
                Padding = new PaddingInfo(5, 4, 0, 0),
                BackColor = HeaderBg,
                ForeColor = HeaderFg,
                Borders = BorderSide.All,
                BorderColor = BorderClr,
                CanGrow = true
            };

        private static XRTableCell DataCell(
            string text,
            float width,
            bool isEven,
            TextAlignment alignment = TextAlignment.MiddleLeft,
            Color? foreOverride = null) =>
            new XRTableCell
            {
                Text = text,
                WidthF = width,
                Font = new DXFont("Segoe UI", 8.5f),
                TextAlignment = alignment,
                Padding = new PaddingInfo(5, 4, 0, 0),
                BackColor = isEven ? EvenRowBg : OddRowBg,
                ForeColor = foreOverride ?? Color.FromArgb(40, 40, 40),
                Borders = BorderSide.All,
                BorderColor = BorderClr,
                CanGrow = true
            };
    }
}