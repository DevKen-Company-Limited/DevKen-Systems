using System;
using System.Drawing;
using System.IO;
using DevExpress.Drawing;                  // DXFont, DXFontStyle
using DevExpress.Drawing.Printing;         // DXPaperKind, DXMargins
using DevExpress.XtraPrinting;             // BorderSide, TextAlignment, ImageSizeMode, PageInfo, PaddingInfo
using DevExpress.XtraPrinting.Drawing;     // ImageSource, WatermarkPosition, DirectionMode
using DevExpress.XtraReports.UI;           // XtraReport, all bands and XR controls
using Devken.CBC.SchoolManagement.Domain.Entities.Administration;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Reports
{
    public abstract class BaseSchoolReportDocument(
        School school,
        byte[]? logoBytes,
        string reportTitle,
        bool showWatermark = false) : XtraReport
    {
        // ── Fields ─────────────────────────────────────────────────────────
        protected School School = school ?? throw new ArgumentNullException(nameof(school));
        protected byte[]? LogoBytes = logoBytes;
        protected string ReportTitle = reportTitle ?? throw new ArgumentNullException(nameof(reportTitle));
        protected bool ShowWatermark = showWatermark;

        // ── Layout constants ───────────────────────────────────────────────
        protected const float PageWidth = 750f;
        protected const float HeaderHeight = 110f;
        protected const float FooterHeight = 28f;
        protected const float AccentBarH = 5f;
        protected const float LogoSize = 72f;
        protected const float LogoX = 0f;
        protected const float InfoX = LogoSize + 12f;
        protected const float InfoW = PageWidth - InfoX;

        // ── Brand colours ──────────────────────────────────────────────────
        protected static Color AccentColor => Color.FromArgb(30, 80, 160);
        protected static Color AccentLight => Color.FromArgb(235, 240, 250);
        protected static Color DividerColor => Color.FromArgb(180, 195, 220);
        protected static Color TitleColor => Color.FromArgb(20, 55, 120);
        protected static Color SubTextColor => Color.FromArgb(90, 100, 115);
        protected static Color HeaderBg => Color.FromArgb(30, 80, 160);
        protected static Color HeaderFg => Color.White;
        protected static Color EvenRowBg => Color.White;
        protected static Color OddRowBg => Color.FromArgb(245, 248, 253);
        protected static Color BorderClr => Color.FromArgb(210, 220, 235);

        // ── Called last in every subclass constructor ──────────────────────
        protected void Build()
        {
            Margins = new DXMargins(36, 36, 36, 36);
            PaperKind = DXPaperKind.A4;
            Font = new DXFont("Segoe UI", 9);

            ApplyOverlay();
            BuildHeader();
            BuildBody();
            BuildFooter();
        }

        // ── Overlay watermark ──────────────────────────────────────────────
        private void ApplyOverlay()
        {
            var overlay = new XRWatermark { ShowBehind = true };

            if (LogoBytes != null)
            {
                // ✅ Correct: convert byte[] → System.Drawing.Image → new ImageSource(image)
                //    ImageSource has NO FromStream method — constructor takes Image directly
                var img = Image.FromStream(new MemoryStream(LogoBytes));
                overlay.ImageSource = new ImageSource(img);
                overlay.ImageAlign = ContentAlignment.MiddleCenter;
                overlay.ImageTiling = false;
                overlay.ImageTransparency = 210;   // 0 = opaque, 255 = invisible
                overlay.ImagePosition = WatermarkPosition.Behind;
            }
            else
            {
                // Text overlay when no logo
                overlay.Text = School.Name.ToUpperInvariant();
                overlay.Font = new DXFont("Segoe UI", 52, DXFontStyle.Bold);
                overlay.ForeColor = AccentColor;
                overlay.TextTransparency = 225;
                overlay.TextDirection = DirectionMode.ForwardDiagonal;
                overlay.TextPosition = WatermarkPosition.Behind;
            }

            Watermarks.Add(overlay);
            DrawWatermark = true;
        }

        // ── Header ─────────────────────────────────────────────────────────
        private void BuildHeader()
        {
            var header = new PageHeaderBand { HeightF = HeaderHeight };

            // Accent bar
            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(0, 0),
                SizeF = new SizeF(PageWidth, AccentBarH),
                BackColor = AccentColor,
                Borders = BorderSide.None
            });

            // Logo / monogram
            if (LogoBytes != null)
            {
                // ✅ Same pattern: byte[] → Image → new ImageSource(image)
                var img = Image.FromStream(new MemoryStream(LogoBytes));
                header.Controls.Add(new XRPictureBox
                {
                    LocationF = new PointF(LogoX, AccentBarH + 8f),
                    SizeF = new SizeF(LogoSize, LogoSize),
                    Sizing = ImageSizeMode.Squeeze,
                    ImageSource = new ImageSource(img)
                });
            }
            else
            {
                // Monogram fallback
                header.Controls.Add(new XRLabel
                {
                    Text = School.Name.Length > 0
                                        ? School.Name[0].ToString().ToUpperInvariant()
                                        : "S",
                    LocationF = new PointF(LogoX, AccentBarH + 8f),
                    SizeF = new SizeF(LogoSize, LogoSize),
                    BackColor = AccentLight,
                    ForeColor = AccentColor,
                    Font = new DXFont("Segoe UI", 30, DXFontStyle.Bold),
                    TextAlignment = TextAlignment.MiddleCenter,
                    Borders = BorderSide.All,
                    BorderColor = DividerColor,
                    BorderWidth = 1
                });
            }

            // School name
            header.Controls.Add(new XRLabel
            {
                Text = School.Name,
                LocationF = new PointF(InfoX, AccentBarH + 6f),
                SizeF = new SizeF(InfoW, 22f),
                Font = new DXFont("Segoe UI", 14, DXFontStyle.Bold),
                ForeColor = TitleColor,
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.None
            });

            // Address lines
            float y = AccentBarH + 30f;
            foreach (var line in new[]
            {
                School.Address ?? "",
                School.County + "  •  " + School.SubCounty,
                "Tel: " + School.PhoneNumber + "   |   " + School.Email
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

            // Divider line
            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(0, AccentBarH + LogoSize + 12f),
                SizeF = new SizeF(PageWidth, 1.5f),
                BackColor = DividerColor,
                Borders = BorderSide.None
            });

            // Report title band
            float titleY = AccentBarH + LogoSize + 16f;
            header.Controls.Add(new XRLabel
            {
                Text = ReportTitle,
                LocationF = new PointF(0, titleY),
                SizeF = new SizeF(PageWidth, 20f),
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

            // Top rule
            footer.Controls.Add(new XRLabel
            {
                LocationF = new PointF(0, 0),
                SizeF = new SizeF(PageWidth, 1.5f),
                BackColor = DividerColor,
                Borders = BorderSide.None
            });

            // Date — left
            footer.Controls.Add(new XRLabel
            {
                Text = "Generated: " + DateTime.Now.ToString("dd MMM yyyy  HH:mm"),
                LocationF = new PointF(0, 5f),
                SizeF = new SizeF(300f, 18f),
                Font = new DXFont("Segoe UI", 8),
                ForeColor = SubTextColor,
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.None
            });

            // School name — centre (subtle)
            footer.Controls.Add(new XRLabel
            {
                Text = School.Name,
                LocationF = new PointF(200f, 5f),
                SizeF = new SizeF(350f, 18f),
                Font = new DXFont("Segoe UI", 8, DXFontStyle.Italic),
                ForeColor = DividerColor,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.None
            });

            // Page N of M — right
            footer.Controls.Add(new XRPageInfo
            {
                LocationF = new PointF(PageWidth - 130f, 5f),
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

        protected abstract void BuildBody();

        // ── Shared label helper ────────────────────────────────────────────
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