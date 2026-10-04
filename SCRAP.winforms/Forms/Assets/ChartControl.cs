#pragma warning disable WFO1000
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace SCRAP.winforms
{
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public Color? Color { get; set; }

        public ChartDataPoint() { }
        public ChartDataPoint(string label, decimal value, Color? color = null)
        {
            Label = label;
            Value = value;
            Color = color;
        }
    }

    /// <summary>
    /// Modern, responsive Bar Chart control with smooth anti-aliased rendering.
    /// Supports both vertical and horizontal bar layouts.
    /// </summary>
    [DesignerCategory("Code")]
    public class BarChartControl : UserControl
    {
        private readonly List<ChartDataPoint> _data = new();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitle { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ValuePrefix { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ValueSuffix { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsHorizontal { get; set; } = false;

        private static readonly Color[] DefaultPalette = new[]
        {
            ColorTranslator.FromHtml("#0069E5"), // Blue
            ColorTranslator.FromHtml("#00AD4C"), // Green
            ColorTranslator.FromHtml("#8B5CF6"), // Purple
            ColorTranslator.FromHtml("#0EA5E9"), // Sky Blue
            ColorTranslator.FromHtml("#F59E0B"), // Amber
            ColorTranslator.FromHtml("#EC4899"), // Pink
            ColorTranslator.FromHtml("#10B981"), // Emerald
            ColorTranslator.FromHtml("#6366F1"), // Indigo
            ColorTranslator.FromHtml("#14B8A6"), // Teal
            ColorTranslator.FromHtml("#F97316")  // Orange
        };

        public BarChartControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.White;
            Font = Theme.LabelFont;
        }

        public void SetData(IEnumerable<ChartDataPoint> points)
        {
            _data.Clear();
            if (points != null)
                _data.AddRange(points);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Card border
            using (var borderPen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            int topOffset = 18;
            int pad = 20;

            // Draw Title
            if (!string.IsNullOrWhiteSpace(Title))
            {
                using var brush = new SolidBrush(Theme.DarkText);
                g.DrawString(Title, Theme.SectionFont, brush, pad, topOffset);
                topOffset += 24;
            }

            // Draw Subtitle
            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                using var brush = new SolidBrush(Theme.MutedText);
                g.DrawString(Subtitle, Theme.StatLabelFont, brush, pad, topOffset);
                topOffset += 20;
            }

            topOffset += 8;

            var plotRect = new Rectangle(pad, topOffset, Width - (pad * 2), Height - topOffset - pad);

            if (_data.Count == 0 || _data.All(d => d.Value <= 0))
            {
                DrawEmptyState(g, plotRect);
                return;
            }

            if (IsHorizontal)
                DrawHorizontalBars(g, plotRect);
            else
                DrawVerticalBars(g, plotRect);
        }

        private void DrawEmptyState(Graphics g, Rectangle r)
        {
            using var brush = new SolidBrush(Theme.MutedText);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("No data available to display", Theme.SubtitleFont, brush, r, sf);
        }

        private void DrawVerticalBars(Graphics g, Rectangle r)
        {
            decimal maxVal = _data.Max(d => d.Value);
            if (maxVal <= 0) maxVal = 1;

            int count = _data.Count;
            int bottomLabelH = 26;
            int chartH = r.Height - bottomLabelH;
            int barAreaW = r.Width;
            int slotW = barAreaW / count;
            int barW = Math.Min(60, (int)(slotW * 0.65));

            // Grid lines (3 horizontal reference lines)
            using (var gridPen = new Pen(Theme.GridLine, 1) { DashStyle = DashStyle.Dash })
            using (var gridFont = new Font("Segoe UI", 7.5f))
            using (var gridBrush = new SolidBrush(Theme.MutedText))
            {
                for (int i = 1; i <= 3; i++)
                {
                    int lineY = r.Top + (int)(chartH * (1.0 - (i / 3.0)));
                    g.DrawLine(gridPen, r.Left, lineY, r.Right, lineY);
                    decimal refVal = (maxVal / 3.0m) * i;
                    string refText = FormatVal(refVal);
                    g.DrawString(refText, gridFont, gridBrush, r.Left, lineY - 14);
                }
            }

            // Draw baseline
            using (var basePen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawLine(basePen, r.Left, r.Top + chartH, r.Right, r.Top + chartH);
            }

            for (int i = 0; i < count; i++)
            {
                var pt = _data[i];
                int slotX = r.Left + (i * slotW);
                int barX = slotX + (slotW - barW) / 2;

                int barH = (int)((pt.Value / maxVal) * (chartH - 24));
                if (barH < 4 && pt.Value > 0) barH = 4;
                int barY = r.Top + chartH - barH;

                var color = pt.Color ?? DefaultPalette[i % DefaultPalette.Length];

                // Bar fill with rounded top
                using (var path = GetTopRoundedRect(new Rectangle(barX, barY, barW, barH), 5))
                using (var brush = new SolidBrush(color))
                {
                    g.FillPath(brush, path);
                }

                // Value label on top
                using (var valFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (var valBrush = new SolidBrush(Theme.DarkText))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far })
                {
                    g.DrawString(FormatVal(pt.Value), valFont, valBrush, new RectangleF(barX - 10, barY - 18, barW + 20, 16), sf);
                }

                // Category label on bottom
                using (var lblBrush = new SolidBrush(Theme.DarkText))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    g.DrawString(pt.Label, Theme.StatLabelFont, lblBrush, new RectangleF(slotX, r.Top + chartH + 6, slotW, bottomLabelH), sf);
                }
            }
        }

        private void DrawHorizontalBars(Graphics g, Rectangle r)
        {
            decimal maxVal = _data.Max(d => d.Value);
            if (maxVal <= 0) maxVal = 1;

            int count = _data.Count;
            int labelW = Math.Min(120, (int)(r.Width * 0.28));
            int valueW = 80;
            int barAreaW = r.Width - labelW - valueW - 10;
            int slotH = Math.Min(40, r.Height / count);
            int barH = Math.Max(12, (int)(slotH * 0.55));

            for (int i = 0; i < count; i++)
            {
                var pt = _data[i];
                int y = r.Top + (i * slotH);
                int barY = y + (slotH - barH) / 2;

                // Category Label (left)
                using (var lblBrush = new SolidBrush(Theme.DarkText))
                using (var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    g.DrawString(pt.Label, Theme.StatLabelFont, lblBrush, new RectangleF(r.Left, y, labelW, slotH), sf);
                }

                // Background track
                int barX = r.Left + labelW + 10;
                using (var trackBrush = new SolidBrush(Theme.SoftBlue))
                using (var trackPath = GetRightRoundedRect(new Rectangle(barX, barY, barAreaW, barH), 4))
                {
                    g.FillPath(trackBrush, trackPath);
                }

                // Bar fill
                int barW = (int)((pt.Value / maxVal) * barAreaW);
                if (barW < 4 && pt.Value > 0) barW = 4;

                var color = pt.Color ?? DefaultPalette[i % DefaultPalette.Length];
                using (var brush = new SolidBrush(color))
                using (var barPath = GetRightRoundedRect(new Rectangle(barX, barY, barW, barH), 4))
                {
                    g.FillPath(brush, barPath);
                }

                // Value text (right)
                using (var valFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var valBrush = new SolidBrush(Theme.DarkText))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    g.DrawString(FormatVal(pt.Value), valFont, valBrush, new RectangleF(barX + barAreaW + 10, y, valueW, slotH), sf);
                }
            }
        }

        private string FormatVal(decimal val)
        {
            string numStr = val >= 1000 ? $"{val:N0}" : (val % 1 == 0 ? $"{val:0}" : $"{val:0.##}");
            return $"{ValuePrefix}{numStr}{ValueSuffix}";
        }

        private static GraphicsPath GetTopRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (rect.Height <= 0 || rect.Width <= 0) return path;
            int r2 = radius * 2;
            path.AddArc(rect.X, rect.Y, r2, r2, 180, 90);
            path.AddArc(rect.Right - r2, rect.Y, r2, r2, 270, 90);
            path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath GetRightRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (rect.Height <= 0 || rect.Width <= 0) return path;
            int r2 = Math.Min(rect.Height, radius * 2);
            path.AddLine(rect.X, rect.Y, rect.Right - r2, rect.Y);
            path.AddArc(rect.Right - r2, rect.Y, r2, r2, 270, 90);
            path.AddArc(rect.Right - r2, rect.Bottom - r2, r2, r2, 0, 90);
            path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Modern Donut / Pie Chart control with legend and center statistics.
    /// </summary>
    [DesignerCategory("Code")]
    public class DonutChartControl : UserControl
    {
        private readonly List<ChartDataPoint> _data = new();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitle { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CenterLabel { get; set; } = string.Empty;

        private static readonly Color[] DefaultPalette = new[]
        {
            ColorTranslator.FromHtml("#00AD4C"), // Green
            ColorTranslator.FromHtml("#0069E5"), // Blue
            ColorTranslator.FromHtml("#8B5CF6"), // Purple
            ColorTranslator.FromHtml("#F59E0B"), // Amber
            ColorTranslator.FromHtml("#EC4899"), // Pink
            ColorTranslator.FromHtml("#0EA5E9"), // Sky Blue
            ColorTranslator.FromHtml("#10B981"), // Emerald
            ColorTranslator.FromHtml("#6366F1")  // Indigo
        };

        public DonutChartControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.White;
            Font = Theme.LabelFont;
        }

        public void SetData(IEnumerable<ChartDataPoint> points)
        {
            _data.Clear();
            if (points != null)
                _data.AddRange(points);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Card border
            using (var borderPen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            int topOffset = 18;
            int pad = 20;

            if (!string.IsNullOrWhiteSpace(Title))
            {
                using var brush = new SolidBrush(Theme.DarkText);
                g.DrawString(Title, Theme.SectionFont, brush, pad, topOffset);
                topOffset += 24;
            }

            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                using var brush = new SolidBrush(Theme.MutedText);
                g.DrawString(Subtitle, Theme.StatLabelFont, brush, pad, topOffset);
                topOffset += 20;
            }

            topOffset += 10;
            var plotRect = new Rectangle(pad, topOffset, Width - (pad * 2), Height - topOffset - pad);

            decimal total = _data.Sum(d => d.Value);
            if (total <= 0)
            {
                using var brush = new SolidBrush(Theme.MutedText);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("No data available to display", Theme.SubtitleFont, brush, plotRect, sf);
                return;
            }

            // Donut layout: Donut circle on left, Legend on right
            int donutSize = Math.Min(plotRect.Width / 2, plotRect.Height - 10);
            donutSize = Math.Max(80, Math.Min(donutSize, 180));
            var donutRect = new Rectangle(plotRect.Left + 10, plotRect.Top + (plotRect.Height - donutSize) / 2, donutSize, donutSize);

            // Draw donut slices
            float startAngle = -90f;
            for (int i = 0; i < _data.Count; i++)
            {
                var pt = _data[i];
                if (pt.Value <= 0) continue;
                float sweepAngle = (float)(pt.Value / total) * 360f;
                var color = pt.Color ?? DefaultPalette[i % DefaultPalette.Length];

                using (var brush = new SolidBrush(color))
                {
                    g.FillPie(brush, donutRect, startAngle, sweepAngle);
                }
                startAngle += sweepAngle;
            }

            // Inner hole
            int holeRatio = (int)(donutSize * 0.58);
            var holeRect = new Rectangle(
                donutRect.Left + (donutSize - holeRatio) / 2,
                donutRect.Top + (donutSize - holeRatio) / 2,
                holeRatio, holeRatio);

            using (var holeBrush = new SolidBrush(BackColor))
            {
                g.FillEllipse(holeBrush, holeRect);
            }

            // Center text (Total count / center label)
            using (var countFont = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var lblFont = new Font("Segoe UI", 7.5f))
            using (var textBrush = new SolidBrush(Theme.DarkText))
            using (var mutedBrush = new SolidBrush(Theme.MutedText))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                string topStr = total >= 1000 ? $"{total:N0}" : $"{total:0}";
                string bottomStr = string.IsNullOrWhiteSpace(CenterLabel) ? "Total" : CenterLabel;

                g.DrawString(topStr, countFont, textBrush, new RectangleF(holeRect.Left, holeRect.Top + 6, holeRect.Width, holeRect.Height / 2), sf);
                g.DrawString(bottomStr, lblFont, mutedBrush, new RectangleF(holeRect.Left, holeRect.Top + (holeRect.Height / 2) - 2, holeRect.Width, holeRect.Height / 2), sf);
            }

            // Legend on right
            int legendLeft = donutRect.Right + 24;
            int legendW = plotRect.Right - legendLeft;
            int legendItemH = Math.Min(28, plotRect.Height / Math.Max(1, _data.Count));
            int legendTop = plotRect.Top + Math.Max(0, (plotRect.Height - (_data.Count * legendItemH)) / 2);

            for (int i = 0; i < _data.Count; i++)
            {
                var pt = _data[i];
                int itemY = legendTop + (i * legendItemH);
                var color = pt.Color ?? DefaultPalette[i % DefaultPalette.Length];

                // Dot
                using (var dotBrush = new SolidBrush(color))
                {
                    g.FillEllipse(dotBrush, legendLeft, itemY + (legendItemH - 10) / 2, 10, 10);
                }

                // Label & percentage
                float pct = (float)((pt.Value / total) * 100m);
                string text = $"{pt.Label} ({pct:0.#}%)";

                using (var brush = new SolidBrush(Theme.DarkText))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    g.DrawString(text, Theme.StatLabelFont, brush, new RectangleF(legendLeft + 16, itemY, legendW - 16, legendItemH), sf);
                }
            }
        }
    }

    public class LineSeries
    {
        public string Name { get; set; } = string.Empty;
        public Color Color { get; set; } = Color.DodgerBlue;
        public List<decimal> Values { get; set; } = new();
    }

    public class BranchRevenueComparisonDto
    {
        public List<string> XLabels { get; set; } = new();
        public List<BranchRevenueSeriesDto> Series { get; set; } = new();
    }

    public class BranchRevenueSeriesDto
    {
        public int BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#15803D";
        public List<decimal> Values { get; set; } = new();
    }

    /// <summary>
    /// Modern multi-series line chart with smooth anti-aliased curves, gradient fills, and value points.
    /// </summary>
    [DesignerCategory("Code")]
    public class LineChartControl : UserControl
    {
        private readonly List<string> _xLabels = new();
        private readonly List<LineSeries> _series = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitle { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ValuePrefix { get; set; } = "₱";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string XAxisTitle { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string YAxisTitle { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowVerticalGridLines { get; set; } = true;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowAreaGradient { get; set; } = false;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool LegendInTopLeftBox { get; set; } = true;

        public LineChartControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.White;
            Font = Theme.LabelFont;
        }

        public void SetData(List<string> xLabels, List<LineSeries> series)
        {
            _xLabels.Clear();
            if (xLabels != null) _xLabels.AddRange(xLabels);

            _series.Clear();
            if (series != null) _series.AddRange(series);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Outer Card Border
            using (var borderPen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            int pad = 20;
            int topOffset = 18;

            // Title
            if (!string.IsNullOrWhiteSpace(Title))
            {
                using var brush = new SolidBrush(Theme.DarkText);
                g.DrawString(Title, Theme.SectionFont, brush, pad, topOffset);
                topOffset += 24;
            }

            // Subtitle
            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                using var brush = new SolidBrush(Theme.MutedText);
                g.DrawString(Subtitle, Theme.StatLabelFont, brush, pad, topOffset);
                topOffset += 20;
            }

            // Draw Top-Right Legend if not inside plot box
            if (!LegendInTopLeftBox)
            {
                int legendX = Width - pad;
                int legendY = 18;
                for (int i = _series.Count - 1; i >= 0; i--)
                {
                    var s = _series[i];
                    var textSize = g.MeasureString(s.Name, Theme.StatLabelFont);
                    int itemW = (int)textSize.Width + 28;
                    legendX -= itemW;

                    int lineY = legendY + 8;
                    using (var pen = new Pen(s.Color, 2.5f))
                    {
                        g.DrawLine(pen, legendX, lineY, legendX + 16, lineY);
                    }
                    using (var brush = new SolidBrush(s.Color))
                    {
                        g.FillEllipse(brush, legendX + 4, lineY - 4, 8, 8);
                    }
                    using (var brush = new SolidBrush(Theme.DarkText))
                    {
                        g.DrawString(s.Name, Theme.StatLabelFont, brush, legendX + 22, legendY);
                    }
                    legendX -= 12;
                }
            }

            topOffset += 12;

            int leftMargin = !string.IsNullOrWhiteSpace(YAxisTitle) ? 82 : 68;
            int bottomMargin = !string.IsNullOrWhiteSpace(XAxisTitle) ? 46 : 32;
            int rightMargin = 28;
            var plotRect = new Rectangle(
                pad + leftMargin,
                topOffset,
                Math.Max(10, Width - pad * 2 - leftMargin - rightMargin),
                Math.Max(10, Height - topOffset - bottomMargin - pad)
            );

            // Draw Y-Axis Title if provided (rotated 90 degrees)
            if (!string.IsNullOrWhiteSpace(YAxisTitle))
            {
                var state = g.Save();
                g.TranslateTransform(16, plotRect.Top + (plotRect.Height / 2f));
                g.RotateTransform(-90);
                using var sfY = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var brushY = new SolidBrush(Theme.DarkText);
                using var fontY = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                g.DrawString(YAxisTitle, fontY, brushY, 0, 0, sfY);
                g.Restore(state);
            }

            // Calculate Max Value
            decimal maxVal = 0;
            foreach (var s in _series)
            {
                foreach (var v in s.Values)
                {
                    if (v > maxVal) maxVal = v;
                }
            }

            bool hasData = maxVal > 0 && _xLabels.Count > 0;
            if (!hasData)
            {
                using var brush = new SolidBrush(Theme.MutedText);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("No financial transactions recorded for this period", Theme.SubtitleFont, brush, plotRect, sf);
                return;
            }

            maxVal = NiceCeiling(maxVal);

            // Grid Pen & Box Pen
            using var gridPen = new Pen(Color.FromArgb(226, 232, 240), 1);
            using var boxPen = new Pen(Color.FromArgb(203, 213, 225), 1.2f);
            using var yLabelBrush = new SolidBrush(Theme.MutedText);
            using var yLabelSf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            // Horizontal Grid Lines & Y-axis Labels
            int gridSteps = 5;
            for (int i = 0; i <= gridSteps; i++)
            {
                decimal v = (maxVal / gridSteps) * i;
                float y = plotRect.Bottom - (float)(v / maxVal) * plotRect.Height;

                g.DrawLine(gridPen, plotRect.Left, y, plotRect.Right, y);

                string yText = FormatCompactVal(v);
                var labelRect = new RectangleF(pad, y - 10, leftMargin - 12, 20);
                g.DrawString(yText, Theme.StatLabelFont, yLabelBrush, labelRect, yLabelSf);
            }

            int count = _xLabels.Count;

            // Vertical Grid Lines
            if (ShowVerticalGridLines && count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = count > 1
                        ? plotRect.Left + ((float)i / (count - 1)) * plotRect.Width
                        : plotRect.Left + (plotRect.Width / 2f);
                    g.DrawLine(gridPen, x, plotRect.Top, x, plotRect.Bottom);
                }
            }

            // Draw outer plot rectangle (bounding box like mathematical graph)
            g.DrawRectangle(boxPen, plotRect);

            // Draw Series Lines and Filled Markers
            if (count > 0)
            {
                foreach (var s in _series)
                {
                    if (s.Values.Count == 0) continue;
                    var pts = new List<PointF>();

                    for (int i = 0; i < count; i++)
                    {
                        decimal val = i < s.Values.Count ? s.Values[i] : 0m;
                        float x = count > 1
                            ? plotRect.Left + ((float)i / (count - 1)) * plotRect.Width
                            : plotRect.Left + (plotRect.Width / 2f);

                        float y = plotRect.Bottom - (float)(Math.Min(maxVal, Math.Max(0, val)) / maxVal) * plotRect.Height;
                        pts.Add(new PointF(x, y));
                    }

                    if (pts.Count > 1)
                    {
                        // Optional Area Gradient Fill
                        if (ShowAreaGradient)
                        {
                            using var path = new GraphicsPath();
                            path.AddLine(pts[0].X, plotRect.Bottom, pts[0].X, pts[0].Y);
                            path.AddLines(pts.ToArray());
                            path.AddLine(pts[^1].X, pts[^1].Y, pts[^1].X, plotRect.Bottom);
                            path.CloseFigure();

                            using var fillBrush = new LinearGradientBrush(
                                new PointF(0, plotRect.Top),
                                new PointF(0, plotRect.Bottom),
                                Color.FromArgb(32, s.Color),
                                Color.FromArgb(2, s.Color));
                            g.FillPath(fillBrush, path);
                        }

                        // Connecting Line
                        using (var linePen = new Pen(s.Color, 2.5f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
                        {
                            g.DrawLines(linePen, pts.ToArray());
                        }

                        // Prominent Solid Filled Circles on Every Point
                        foreach (var p in pts)
                        {
                            using var dotBrush = new SolidBrush(s.Color);
                            using var borderDotPen = new Pen(Color.White, 1.5f);
                            g.FillEllipse(dotBrush, p.X - 4.5f, p.Y - 4.5f, 9f, 9f);
                            g.DrawEllipse(borderDotPen, p.X - 4.5f, p.Y - 4.5f, 9f, 9f);
                        }
                    }
                    else if (pts.Count == 1)
                    {
                        using var brush = new SolidBrush(s.Color);
                        g.FillEllipse(brush, pts[0].X - 5f, pts[0].Y - 5f, 10f, 10f);
                    }
                }

                // X-axis Time Labels
                int maxLabels = Math.Max(2, plotRect.Width / 75);
                int step = Math.Max(1, (int)Math.Ceiling((double)count / maxLabels));

                using var xLabelBrush = new SolidBrush(Theme.MutedText);
                using var xLabelSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

                for (int i = 0; i < count; i += step)
                {
                    float x = count > 1
                        ? plotRect.Left + ((float)i / (count - 1)) * plotRect.Width
                        : plotRect.Left + (plotRect.Width / 2f);

                    string xText = _xLabels[i];
                    var rect = new RectangleF(x - 45, plotRect.Bottom + 6, 90, 20);
                    g.DrawString(xText, new Font("Segoe UI", 8.2f, FontStyle.Bold), xLabelBrush, rect, xLabelSf);
                }

                // Draw X-Axis Title Centered at Bottom
                if (!string.IsNullOrWhiteSpace(XAxisTitle))
                {
                    using var brushX = new SolidBrush(Theme.DarkText);
                    using var fontX = new Font("Segoe UI", 9f, FontStyle.Bold);
                    using var sfX = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                    g.DrawString(XAxisTitle, fontX, brushX, new RectangleF(plotRect.Left, plotRect.Bottom + 26, plotRect.Width, 20), sfX);
                }
            }

            // Draw Top-Left Legend Box (just like in the reference image!)
            if (LegendInTopLeftBox && _series.Count > 0)
            {
                int boxX = plotRect.Left + 12;
                int boxY = plotRect.Top + 12;
                int itemH = 20;
                int boxH = (_series.Count * itemH) + 12;
                int maxW = 120;

                using var legendFont = new Font("Segoe UI", 8.2f);
                foreach (var s in _series)
                {
                    int w = (int)g.MeasureString(s.Name, legendFont).Width + 44;
                    if (w > maxW) maxW = w;
                }

                var legendBoxRect = new Rectangle(boxX, boxY, maxW, boxH);
                using (var bgBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                using (var boxBorder = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    g.FillRectangle(bgBrush, legendBoxRect);
                    g.DrawRectangle(boxBorder, legendBoxRect);
                }

                for (int i = 0; i < _series.Count; i++)
                {
                    var s = _series[i];
                    int rowY = boxY + 6 + (i * itemH);

                    // Legend line and marker dot
                    int lineY = rowY + (itemH / 2);
                    using (var pen = new Pen(s.Color, 2f))
                    {
                        g.DrawLine(pen, boxX + 8, lineY, boxX + 24, lineY);
                    }
                    using (var dotBrush = new SolidBrush(s.Color))
                    {
                        g.FillEllipse(dotBrush, boxX + 13, lineY - 3, 6, 6);
                    }

                    // Series Name
                    using (var textBrush = new SolidBrush(Theme.DarkText))
                    {
                        g.DrawString(s.Name, legendFont, textBrush, boxX + 30, rowY + 2);
                    }
                }
            }
        }

        private static decimal NiceCeiling(decimal val)
        {
            if (val <= 10) return 10;
            if (val <= 50) return 50;
            if (val <= 100) return 100;
            if (val <= 500) return 500;
            if (val <= 1000) return 1000;
            if (val <= 5000) return 5000;
            if (val <= 10000) return 10000;
            if (val <= 50000) return 50000;
            if (val <= 100000) return 100000;
            if (val <= 500000) return 500000;
            if (val <= 1000000) return 1000000;

            decimal factor = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)val)));
            return Math.Ceiling(val / factor) * factor;
        }

        private string FormatCompactVal(decimal val)
        {
            if (val >= 1_000_000) return $"{ValuePrefix}{val / 1_000_000:0.#}M";
            if (val >= 1_000) return $"{ValuePrefix}{val / 1_000:0.#}k";
            return $"{ValuePrefix}{val:0}";
        }
    }
}
