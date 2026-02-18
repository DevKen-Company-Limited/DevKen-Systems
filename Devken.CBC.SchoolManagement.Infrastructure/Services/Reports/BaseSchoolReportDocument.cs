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
    /// <summary>
    /// Base class for all school report documents.
    /// Provides a professional header, footer, a clean diagonal school-name watermark,
    /// and shared helpers used by every concrete report.
    ///
    /// SuperAdmin context: pass <c>school = null</c> when generating cross-school
    /// reports — header and watermark adapt automatically.
    /// </summary>
    public abstract class BaseSchoolReportDocument : XtraReport
    {
        // ── Constructor fields ─────────────────────────────────────────────
        /// <summary>
        /// The school this report belongs to. <c>null</c> for SuperAdmin all-school reports.
        /// </summary>
        protected readonly School? School;
        protected readonly byte[]? LogoBytes;
        protected readonly string ReportTitle;
        protected readonly bool IsSuperAdmin;

        // ── Layout constants ───────────────────────────────────────────────
        protected const float PageWidth = 750f;
        protected const float HeaderHeight = 120f;
        protected const float FooterHeight = 30f;
        protected const float AccentBarH = 6f;
        protected const float LogoSize = 70f;
        protected const float LogoX = 0f;
        protected const float InfoX = LogoSize + 14f;
        protected const float InfoW = PageWidth - InfoX;

        // ── Brand palette ──────────────────────────────────────────────────
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

        // ── Constructor ────────────────────────────────────────────────────
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

        // ── Entry point called by each subclass constructor ────────────────
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

        // ── Watermark: always a clean diagonal school-name text ────────────
        /// <summary>
        /// Renders the school name (or "SYSTEM REPORT" for cross-school SuperAdmin
        /// reports) diagonally across every page at very low opacity, keeping the
        /// document clean while retaining a subtle branded impression.
        /// No logo image is used as a watermark — keeping the page fully legible.
        /// </summary>
        private void ApplyDiagonalWatermark()
        {
            var watermarkText = (IsSuperAdmin && School == null)
                ? "SYSTEM REPORT"
                : School?.Name?.ToUpperInvariant() ?? "CONFIDENTIAL";

            var overlay = new XRWatermark
            {
                ShowBehind = true,
                Text = watermarkText,
                Font = new DXFont("Segoe UI", 56, DXFontStyle.Bold),
                ForeColor = AccentColor,
                // 228 keeps the text barely-visible yet clearly present when held up to light.
                // 0 = fully opaque, 255 = invisible. Tune this value to taste.
                TextTransparency = 228,
                TextDirection = DirectionMode.ForwardDiagonal,
                TextPosition = WatermarkPosition.Behind,
            };

            Watermarks.Add(overlay);
            DrawWatermark = true;
        }

        // ── Header ─────────────────────────────────────────────────────────
        private void BuildHeader()
        {
            var header = new PageHeaderBand { HeightF = HeaderHeight };

            // Two-tone top accent bar — primary | dark
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

            // ── Logo / initials box ────────────────────────────────────────
            if (LogoBytes != null)
            {
                var img = Image.FromStream(new MemoryStream(LogoBytes));
                header.Controls.Add(new XRPictureBox
                {
                    LocationF = new PointF(LogoX, AccentBarH + 10f),
                    SizeF = new SizeF(LogoSize, LogoSize),
                    Sizing = ImageSizeMode.Squeeze,
                    ImageSource = new ImageSource(img)
                });
            }
            else
            {
                // Two-letter initials when no logo is available
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

            // ── School / system name ───────────────────────────────────────
            var headingText = (IsSuperAdmin && School == null)
                ? "All Schools — System Report"
                : School?.Name ?? string.Empty;

            header.Controls.Add(new XRLabel
            {
                Text = headingText,
                LocationF = new PointF(InfoX, AccentBarH + 8f),
                SizeF = new SizeF(InfoW, 24f),
                Font = new DXFont("Segoe UI", 14, DXFontStyle.Bold),
                ForeColor = TitleColor,
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.None
            });

            // ── Contact / address lines ────────────────────────────────────
            float y = AccentBarH + 34f;

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
                        Borders = BorderSide.None
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

            // ── Divider line ───────────────────────────────────────────────
            float divY = AccentBarH + LogoSize + 16f;
            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(0, divY),
                SizeF = new SizeF(PageWidth, 1.5f),
                BackColor = DividerColor,
                Borders = BorderSide.None
            });

            // ── Report title banner ────────────────────────────────────────
            float titleY = divY + 3f;
            header.Controls.Add(new XRLabel
            {
                Text = ReportTitle,
                LocationF = new PointF(0, titleY),
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

            // Timestamp — left
            footer.Controls.Add(new XRLabel
            {
                Text = $"Generated: {DateTime.Now:dd MMM yyyy  HH:mm}",
                LocationF = new PointF(0, 6f),
                SizeF = new SizeF(300f, 18f),
                Font = new DXFont("Segoe UI", 8),
                ForeColor = SubTextColor,
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.None
            });

            // Centre organisation
            footer.Controls.Add(new XRLabel
            {
                Text = School?.Name ?? "System Report",
                LocationF = new PointF(200f, 6f),
                SizeF = new SizeF(350f, 18f),
                Font = new DXFont("Segoe UI", 8, DXFontStyle.Italic),
                ForeColor = DividerColor,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.None
            });

            // Page N of M — right
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

        // ── Abstract body ──────────────────────────────────────────────────
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

        // ── Export ─────────────────────────────────────────────────────────
        public byte[] ExportToPdfBytes()
        {
            using var stream = new MemoryStream();
            ExportToPdf(stream);
            return stream.ToArray();
        }
    }
}