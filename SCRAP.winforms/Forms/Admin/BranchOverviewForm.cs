using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class BranchOverviewForm : Form
    {
        private Label lblTitle   = null!;
        private Button btnRefresh = null!;
        private LineChartControl lineChartRevenue = null!;
        private Panel cardsPanel  = null!;

        public BranchOverviewForm()
        {
            Text = "Branch Overview";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadData();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Branch Overview",
                Left = 32, Top = 28, Width = 600, Height = 40,
                Font = Theme.TitleFont, ForeColor = Theme.DarkText,
                BackColor = Color.Transparent, AutoSize = false
            };

            var lblSub = new Label
            {
                Text = "Live executive summary & revenue comparison across all branches",
                Left = 32, Top = 68, Width = 500, Height = 22,
                Font = Theme.SubtitleFont, ForeColor = Theme.MutedText,
                BackColor = Color.Transparent, AutoSize = false
            };

            btnRefresh = new Button { Text = "↻  Refresh", Width = 120, Height = 36, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadData();

            lineChartRevenue = new LineChartControl
            {
                Title = "Branch Revenue Comparison by Month",
                Subtitle = "Comparing gross monthly revenue performance across all branches",
                XAxisTitle = "Timeline (Month)",
                YAxisTitle = "Revenue (₱)",
                ValuePrefix = "₱",
                LegendInTopLeftBox = true
            };

            cardsPanel = new Panel { Left = 32, Top = 404, BackColor = Color.Transparent, AutoScroll = true };

            Controls.Add(lblTitle);
            Controls.Add(lblSub);
            Controls.Add(btnRefresh);
            Controls.Add(lineChartRevenue);
            Controls.Add(cardsPanel);

            Resize += (s, e) =>
            {
                btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
                btnRefresh.Top  = 32;

                int chartW = Math.Max(300, ClientSize.Width - 64);
                lineChartRevenue.SetBounds(32, 105, chartW, 280);

                cardsPanel.SetBounds(32, 395, chartW, Math.Max(150, ClientSize.Height - 410));
            };
        }

        private async Task LoadData()
        {
            try
            {
                var overview = await ApiConfig.Http.GetFromJsonAsync<List<BranchOverviewDto>>(
                    "api/Branches/overview", ApiConfig.JsonOptions) ?? new();

                cardsPanel.Controls.Clear();
                int col = 0, row = 0;
                const int cardW = 300, cardH = 180, gapX = 20, gapY = 20, perRow = 3;

                foreach (var b in overview)
                {
                    var card = MakeBranchCard(b, col * (cardW + gapX), row * (cardH + gapY), cardW, cardH);
                    cardsPanel.Controls.Add(card);
                    col++;
                    if (col >= perRow) { col = 0; row++; }
                }

                try
                {
                    var comp = await ApiConfig.Http.GetFromJsonAsync<BranchRevenueComparisonDto>(
                        "api/Branches/revenue-comparison?months=6", ApiConfig.JsonOptions);
                    if (comp != null && comp.Series.Count > 0)
                    {
                        var seriesList = new List<LineSeries>();
                        foreach (var s in comp.Series)
                        {
                            Color c;
                            try { c = ColorTranslator.FromHtml(s.Color); } catch { c = Theme.Green; }
                            seriesList.Add(new LineSeries { Name = s.Name, Color = c, Values = s.Values });
                        }
                        lineChartRevenue.SetData(comp.XLabels, seriesList);
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading overview: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel MakeBranchCard(BranchOverviewDto b, int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Left = x, Top = y, Width = w, Height = h,
                BackColor = Theme.White
            };

            var bar = new Panel { Height = 4, Dock = DockStyle.Top, BackColor = b.IsActive ? Theme.Green : Theme.MutedText };

            var lblName = new Label
            {
                Text = $"{b.Code} — {b.Name}",
                Left = 16, Top = 14, Width = w - 32, Height = 24,
                Font = Theme.SubtitleFont, ForeColor = Theme.DarkText
            };

            var lblAddress = new Label
            {
                Text = b.Address ?? "No address",
                Left = 16, Top = 38, Width = w - 32, Height = 18,
                Font = Theme.StatLabelFont, ForeColor = Theme.MutedText
            };

            void Stat(string label, string val, int top)
            {
                card.Controls.Add(new Label { Text = label, Left = 16, Top = top, Width = 130, Height = 18, Font = Theme.StatLabelFont, ForeColor = Theme.MutedText, UseMnemonic = false });
                card.Controls.Add(new Label { Text = val,   Left = 146, Top = top, Width = w - 162, Height = 18, Font = Theme.StatLabelFont, ForeColor = Theme.DarkText, TextAlign = ContentAlignment.MiddleRight, UseMnemonic = false });
            }

            card.Controls.Add(bar);
            card.Controls.Add(lblName);
            card.Controls.Add(lblAddress);

            Stat("Active Inventory:",  b.ActiveInventory.ToString("N0"),     62);
            Stat("Total Revenue:",     "₱" + b.TotalRevenue.ToString("N2"),  82);
            Stat("Teardown Batches:",  b.TeardownBatches.ToString("N0"),    102);
            Stat("Staff Count:",       b.StaffCount.ToString("N0"),          122);

            var status = new Label
            {
                Text = b.IsActive ? "● Active" : "● Inactive",
                Left = 16, Top = 148, Width = 120, Height = 18,
                Font = Theme.StatLabelFont,
                ForeColor = b.IsActive ? Theme.Green : Color.Gray
            };
            card.Controls.Add(status);

            return card;
        }

        private class BranchOverviewDto
        {
            public int     Id              { get; set; }
            public string  Name            { get; set; } = string.Empty;
            public string  Code            { get; set; } = string.Empty;
            public string? Address         { get; set; }
            public bool    IsActive        { get; set; }
            public int     ActiveInventory { get; set; }
            public decimal TotalRevenue    { get; set; }
            public int     TeardownBatches { get; set; }
            public int     StaffCount      { get; set; }
        }
    }
}
