using System;
using System.Drawing;
using System.IO;
using DevExpress.Drawing;
using DevExpress.Drawing.Printing;
using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.Drawing;
using DevExpress.XtraReports.UI;
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Reports
{
    public abstract class BaseSchoolReportDocument : XtraReport
    {
        protected readonly School? School;
        protected readonly byte[]? LogoBytes;
        protected readonly string ReportTitle;
        protected readonly bool IsSuperAdmin;

        protected const float PageWidth = 750f;
        protected const float HeaderHeight = 120f;
        protected const float FooterHeight = 30f;
        protected const float AccentBarH = 6f;
        protected const float LogoSize = 70f;
        protected const float LogoX = 0f;
        protected const float InfoX = LogoSize + 14f;
        protected const float InfoW = PageWidth - InfoX;

        protected static Color AccentColor => Color.FromArgb(24, 72, 152);
        protected static Color AccentDark => Color.FromArgb(16, 50, 110);
        protected static Color AccentLight => Color.FromArgb(232, 239, 252);
        protected static Color DividerColor => Color.FromArgb(185, 200, 225);
        protected static Color TitleColor => Color.FromArgb(16, 50, 110);
        protected static Color SubTextColor => Color.FromArgb(95, 108, 126);
        protected static Color HeaderBg => Color.FromArgb(24, 72, 152);
        protected static Color HeaderFg => Color.White;
        protected static Color EvenRowBg => Color.White;
        protected static Color OddRowBg => Color.FromArgb(243, 247, 254);
        protected static Color BorderClr => Color.FromArgb(210, 222, 238);

        protected BaseSchoolReportDocument(
            School? school,
            byte[]? logoBytes,
            string reportTitle,
            bool isSuperAdmin = false)
        {
            School = school;
            LogoBytes = logoBytes;
            ReportTitle = reportTitle ?? throw new ArgumentNullException(nameof(reportTitle));
            IsSuperAdmin = isSuperAdmin;
        }

        protected void Build()
        {
            Margins = new DXMargins(36, 36, 36, 36);
            PaperKind = DXPaperKind.A4;
            Font = new DXFont("Segoe UI", 9);

            ApplyDiagonalWatermark();
            BuildHeader();
            BuildBody();
            BuildFooter();
        }

        // ── Watermark ──────────────────────────────────────────────────────
        private void ApplyDiagonalWatermark()
        {
            var watermarkText = (IsSuperAdmin && School == null)
                ? "SYSTEM REPORT"
                : School?.Name?.ToUpperInvariant() ?? "CONFIDENTIAL";

            Watermarks.Add(new XRWatermark
            {
                ShowBehind = true,
                Text = watermarkText,
                Font = new DXFont("Segoe UI", 56, DXFontStyle.Bold),
                ForeColor = AccentColor,
                TextTransparency = 228,
                TextDirection = DirectionMode.ForwardDiagonal,
                TextPosition = WatermarkPosition.Behind
            });

            DrawWatermark = true;
        }

        // ── Header ─────────────────────────────────────────────────────────
        private void BuildHeader()
        {
            var header = new PageHeaderBand { HeightF = HeaderHeight };

            // Two-tone top accent bar
            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(0, 0),
                SizeF = new SizeF(PageWidth * 0.65f, AccentBarH),
                BackColor = AccentColor,
                Borders = BorderSide.None
            });
            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(PageWidth * 0.65f, 0),
                SizeF = new SizeF(PageWidth * 0.35f, AccentBarH),
                BackColor = AccentDark,
                Borders = BorderSide.None
            });

            // Logo / initials
            if (LogoBytes != null)
            {
                header.Controls.Add(new XRPictureBox
                {
                    LocationF = new PointF(LogoX, AccentBarH + 10f),
                    SizeF = new SizeF(LogoSize, LogoSize),
                    Sizing = ImageSizeMode.Squeeze,
                    ImageSource = new ImageSource(Image.FromStream(new MemoryStream(LogoBytes)))
                });
            }
            else
            {
                var initials = BuildInitials(School?.Name);
                header.Controls.Add(new XRLabel
                {
                    Text = initials,
                    LocationF = new PointF(LogoX, AccentBarH + 10f),
                    SizeF = new SizeF(LogoSize, LogoSize),
                    BackColor = AccentLight,
                    ForeColor = AccentColor,
                    Font = new DXFont("Segoe UI", initials.Length > 1 ? 22 : 30, DXFontStyle.Bold),
                    TextAlignment = TextAlignment.MiddleCenter,
                    Borders = BorderSide.All,
                    BorderColor = DividerColor,
                    BorderWidth = 1.5f
                });
            }

            // ── School / system name — auto-scale font for long names ──────
            var headingText = (IsSuperAdmin && School == null)
                ? "All Schools — System Report"
                : School?.Name ?? string.Empty;

            // Scale font down gracefully:
            //  ≤ 40 chars  →  14pt (standard)
            //  41-55 chars →  11pt
            //  56-70 chars →   9pt
            //  > 70 chars  →   8pt (+ word-wrap so it never clips)
            float headingFontSize = headingText.Length switch
            {
                <= 40 => 14f,
                <= 55 => 11f,
                <= 70 => 9f,
                _ => 8f
            };

            // Taller label when text wraps (two-line names get 40px, single-line 24px)
            float headingHeight = headingText.Length > 55 ? 40f : 24f;

            header.Controls.Add(new XRLabel
            {
                Text = headingText,
                LocationF = new PointF(InfoX, AccentBarH + 8f),
                SizeF = new SizeF(InfoW, headingHeight),
                Font = new DXFont("Segoe UI", headingFontSize, DXFontStyle.Bold),
                ForeColor = TitleColor,
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.None,
                CanGrow = true,
                WordWrap = headingText.Length > 40   // wrap only when needed
            });

            // Contact / address lines — start below the heading box
            float y = AccentBarH + 8f + headingHeight;

            if (School != null)
            {
                foreach (var line in new[]
                {
                    School.Address ?? string.Empty,
                    $"{School.County}  •  {School.SubCounty}",
                    $"Tel: {School.PhoneNumber}   |   {School.Email}"
                })
                {
                    header.Controls.Add(new XRLabel
                    {
                        Text = line,
                        LocationF = new PointF(InfoX, y),
                        SizeF = new SizeF(InfoW, 15f),
                        Font = new DXFont("Segoe UI", 8),
                        ForeColor = SubTextColor,
                        TextAlignment = TextAlignment.MiddleLeft,
                        Borders = BorderSide.None,
                        CanGrow = false,
                        WordWrap = false
                    });
                    y += 16f;
                }
            }
            else
            {
                header.Controls.Add(new XRLabel
                {
                    Text = "Generated by System Administrator  •  Covers all schools in the system",
                    LocationF = new PointF(InfoX, y),
                    SizeF = new SizeF(InfoW, 15f),
                    Font = new DXFont("Segoe UI", 8, DXFontStyle.Italic),
                    ForeColor = SubTextColor,
                    TextAlignment = TextAlignment.MiddleLeft,
                    Borders = BorderSide.None
                });
            }

            // Divider
            float divY = AccentBarH + LogoSize + 16f;
            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(0, divY),
                SizeF = new SizeF(PageWidth, 1.5f),
                BackColor = DividerColor,
                Borders = BorderSide.None
            });

            // Report title banner
            header.Controls.Add(new XRLabel
            {
                Text = ReportTitle,
                LocationF = new PointF(0, divY + 3f),
                SizeF = new SizeF(PageWidth, 22f),
                Font = new DXFont("Segoe UI", 11, DXFontStyle.Bold),
                ForeColor = TitleColor,
                BackColor = AccentLight,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.None,
                Padding = new PaddingInfo(0, 0, 2, 2)
            });

            Bands.Add(header);
        }

        // ── Footer ─────────────────────────────────────────────────────────
        private void BuildFooter()
        {
            var footer = new PageFooterBand { HeightF = FooterHeight };

            // Rule
            footer.Controls.Add(new XRLabel
            {
                LocationF = new PointF(0, 0),
                SizeF = new SizeF(PageWidth, 1.5f),
                BackColor = DividerColor,
                Borders = BorderSide.None
            });

            // Left — timestamp
            footer.Controls.Add(new XRLabel
            {
                Text = $"Generated: {DateTime.Now:dd MMM yyyy  HH:mm}",
                LocationF = new PointF(0, 6f),
                SizeF = new SizeF(250f, 18f),
                Font = new DXFont("Segoe UI", 8),
                ForeColor = SubTextColor,
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.None
            });

            // Centre — school name, truncated so it never overlaps left/right slots
            // Safe zone for centre label: 250 px left margin, 130 px right → 370 px wide
            const float footerCentreX = 250f;
            const float footerCentreW = 250f;   // generous but bounded

            var footerName = School?.Name ?? "System Report";
            footerName = TruncateForFooter(footerName, maxChars: 40);

            footer.Controls.Add(new XRLabel
            {
                Text = footerName,
                LocationF = new PointF(footerCentreX, 6f),
                SizeF = new SizeF(footerCentreW, 18f),
                Font = new DXFont("Segoe UI", 8, DXFontStyle.Italic),
                ForeColor = DividerColor,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.None,
                CanGrow = false,
                WordWrap = false
            });

            // Right — page N of M
            footer.Controls.Add(new XRPageInfo
            {
                LocationF = new PointF(PageWidth - 130f, 6f),
                SizeF = new SizeF(130f, 18f),
                PageInfo = PageInfo.NumberOfTotal,
                Format = "Page {0} of {1}",
                Font = new DXFont("Segoe UI", 8),
                ForeColor = SubTextColor,
                TextAlignment = TextAlignment.MiddleRight,
                Borders = BorderSide.None
            });

            Bands.Add(footer);
        }

        // ── Abstract ───────────────────────────────────────────────────────
        protected abstract void BuildBody();

        // ── Helpers ────────────────────────────────────────────────────────
        private static string BuildInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "SM";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                : parts[0][0].ToString().ToUpperInvariant();
        }

        /// <summary>
        /// Truncates <paramref name="name"/> to <paramref name="maxChars"/> characters,
        /// appending "…" when truncation occurs. Used in space-constrained areas like
        /// the footer where layout is fixed-width.
        /// </summary>
        private static string TruncateForFooter(string name, int maxChars)
        {
            if (name.Length <= maxChars) return name;
            return string.Concat(name.AsSpan(0, maxChars - 1), "…");
        }

        protected static XRLabel MakeLabel(
            string text,
            float x, float y,
            float width, float height,
            float fontSize = 9f,
            bool bold = false,
            TextAlignment alignment = TextAlignment.MiddleLeft,
            Color? backColor = null,
            Color? foreColor = null) => new XRLabel
            {
                Text = text,
                LocationF = new PointF(x, y),
                SizeF = new SizeF(width, height),
                Font = new DXFont("Segoe UI", fontSize, bold ? DXFontStyle.Bold : DXFontStyle.Regular),
                TextAlignment = alignment,
                BackColor = backColor ?? Color.Transparent,
                ForeColor = foreColor ?? Color.FromArgb(40, 40, 40),
                Borders = BorderSide.None,
                CanGrow = true,
                WordWrap = false
            };

        public byte[] ExportToPdfBytes()
        {
            using var stream = new MemoryStream();
            ExportToPdf(stream);
            return stream.ToArray();
        }
    }
}