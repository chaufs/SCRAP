using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Technical
{
    /// <summary>
    /// Floor-operations dashboard for technical staff with visual analytics.
    /// </summary>
    [DesignerCategory("Code")]
    public class TechnicalDashboardForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;

        private Panel cardActive = null!;
        private Panel cardTeardowns = null!;
        private Panel cardRecovered = null!;
        private Label valActive = null!;
        private Label valTeardowns = null!;
        private Label valRecovered = null!;

        private BarChartControl chartBatchesByCategory = null!;
        private DonutChartControl chartYieldMaterials = null!;

        private Label lblRecent = null!;
        private Panel cardHistory = null!;
        private DataGridView dgvRecent = null!;

        public TechnicalDashboardForm()
        {
            Text = "Technical Dashboard";
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
                Text = "Floor Dashboard",
                Left = 32,
                Top = 28,
                Width = 400,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Inventory, teardown dismantling throughput, and material recovery",
                Left = 32,
                Top = 64,
                Width = 520,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 120,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadData();

            cardActive = MakeStatCard("ACTIVE DEVICES", "—", Theme.Blue);
            valActive = (Label)cardActive.Tag!;

            cardTeardowns = MakeStatCard("TEARDOWN BATCHES", "—", Theme.Green);
            valTeardowns = (Label)cardTeardowns.Tag!;

            cardRecovered = MakeStatCard("RECOVERED (kg)", "—", Theme.Blue);
            valRecovered = (Label)cardRecovered.Tag!;

            chartBatchesByCategory = new BarChartControl
            {
                Title = "Dismantled Devices by Category",
                Subtitle = "Throughput across device categories",
                ValueSuffix = " units"
            };

            chartYieldMaterials = new DonutChartControl
            {
                Title = "Recovered Materials on Hand",
                Subtitle = "Current commodity stock yield (kg)",
                CenterLabel = "Total kg"
            };

            lblRecent = Theme.CreateSectionTitle("Recent Teardown Batches", 32, 400);

            cardHistory = new Panel
            {
                BackColor = Theme.White,
                Padding = new Padding(1)
            };
            cardHistory.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, cardHistory.Width - 1, cardHistory.Height - 1);
            };

            dgvRecent = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvRecent);
            cardHistory.Controls.Add(dgvRecent);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(cardActive);
            Controls.Add(cardTeardowns);
            Controls.Add(cardRecovered);
            Controls.Add(chartBatchesByCategory);
            Controls.Add(chartYieldMaterials);
            //Controls.Add(lblRecent);
            //Controls.Add(cardHistory);

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private Panel MakeStatCard(string label, string value, Color accent)
        {
            var card = new Panel
            {
                Width = 240,
                Height = 100,
                BackColor = Theme.White
            };

            var bar = new Panel
            {
                Height = 4,
                Dock = DockStyle.Top,
                BackColor = accent
            };

            var lbl = new Label
            {
                Text = label,
                Left = 20,
                Top = 14,
                Width = Math.Max(50, card.Width - 40),
                Height = 18,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            var val = new Label
            {
                Text = value,
                Left = 20,
                Top = 34,
                Width = Math.Max(50, card.Width - 40),
                Height = 42,
                Font = Theme.StatValueFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false
            };

            card.Resize += (s, e) =>
            {
                int w = Math.Max(10, card.Width - 40);
                lbl.Width = w;
                val.Width = w;
            };

            card.Controls.Add(bar);
            card.Controls.Add(val);
            card.Controls.Add(lbl);
            card.Tag = val;
            return card;
        }

        private void LayoutControls()
        {
            btnRefresh.Left = ClientSize.Width - btnRefresh.Width - 32;
            btnRefresh.Top = 32;

            int gap = 20;
            int startX = 32;
            int y = 105;
            int availableW = Math.Max(200, ClientSize.Width - 64);
            int cardW = (availableW - (gap * 2)) / 3;

            cardActive.Left = startX;
            cardActive.Top = y;
            cardActive.Width = cardW;

            cardTeardowns.Left = startX + cardW + gap;
            cardTeardowns.Top = y;
            cardTeardowns.Width = cardW;

            cardRecovered.Left = startX + (cardW + gap) * 2;
            cardRecovered.Top = y;
            cardRecovered.Width = cardW;

            int chartY = y + 115;
            int chartH = 200;
            int chartW = (availableW - gap) / 2;

            chartBatchesByCategory.Left = startX;
            chartBatchesByCategory.Top = chartY;
            chartBatchesByCategory.Width = chartW;
            chartBatchesByCategory.Height = chartH;

            chartYieldMaterials.Left = startX + chartW + gap;
            chartYieldMaterials.Top = chartY;
            chartYieldMaterials.Width = chartW;
            chartYieldMaterials.Height = chartH;

            int tableY = chartY + chartH + 16;
            lblRecent.Top = tableY;
            lblRecent.Left = 32;

            cardHistory.Left = 32;
            cardHistory.Top = tableY + 28;
            cardHistory.Width = availableW;
            cardHistory.Height = Math.Max(160, ClientSize.Height - cardHistory.Top - 24);

            if (dgvRecent.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvRecent);
        }

        private void ConfigureHistoryColumns()
        {
            Theme.ConfigureColumns(
                dgvRecent,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["BatchCode"] = ("Batch #", 95),
                    ["DateProcessed"] = ("Date Processed", 140),
                    ["Category"] = ("Device Category", 150),
                    ["Quantity"] = ("Qty Dismantled", 110),
                    ["YieldSummary"] = ("Recovered Materials", 160),
                    ["Branch"] = ("Branch", 130),
                    ["ProcessedBy"] = ("Processed By", 120)
                },
                "Id", "DeviceCategory", "User", "RawInventories", "ProcessedByUser", "ProcessedByUserId", "DeviceCategoryId", "CategoryId",
                "DeviceCategoryNavigation", "ProcessedByUserNavigation", "Yields", "BranchId");
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadSummary(), LoadRecentTeardowns(), LoadCommodityStock());
        }

        private async Task LoadSummary()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Dashboard/summary");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<DashboardSummary>();
                    if (data != null)
                    {
                        valActive.Text = data.ActiveInventoryCount.ToString("N0");
                        valRecovered.Text = data.TotalRecoveredWeightKg.ToString("N1");
                        if (data.TeardownBatchCount > 0)
                            valTeardowns.Text = data.TeardownBatchCount.ToString("N0");
                    }
                }
            }
            catch
            {
                // Non-fatal
            }
        }

        private async Task LoadRecentTeardowns()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Teardown/history");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<TeardownBatch>>() ?? new();
                    var displayList = data.Select(b => new
                    {
                        BatchCode = $"BAT-{b.Id:D4}",
                        DateProcessed = b.DateProcessed.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        Category = b.DeviceCategory?.Name ?? $"Category #{b.DeviceCategoryId}",
                        Quantity = b.QuantityDismantled,
                        YieldSummary = b.Yields != null && b.Yields.Count > 0 ? $"{b.Yields.Count} mat ({b.Yields.Sum(y => y.WeightKg):N2} kg)" : "0 materials",
                        Branch = b.Branch?.Name ?? $"Branch #{b.BranchId}",
                        ProcessedBy = b.ProcessedByUserId > 0 ? $"User #{b.ProcessedByUserId}" : "—"
                    }).ToList();

                    dgvRecent.DataSource = displayList;
                    ConfigureHistoryColumns();

                    if (valTeardowns.Text == "—" || valTeardowns.Text == "0")
                        valTeardowns.Text = data.Count.ToString("N0");

                    // Populate Dismantled Devices by Category Bar Chart
                    var categoryPoints = data
                        .GroupBy(b => b.DeviceCategory?.Name ?? $"Category #{b.DeviceCategoryId}")
                        .Select(g => new ChartDataPoint(g.Key, g.Sum(b => b.QuantityDismantled)))
                        .OrderByDescending(p => p.Value)
                        .Take(6)
                        .ToList();
                    chartBatchesByCategory.SetData(categoryPoints);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading teardown history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadCommodityStock()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/RawInventory");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<RawInventory>>() ?? new();
                    var points = data
                        .Where(r => r.CurrentTotalWeightKg > 0)
                        .Select(r => new ChartDataPoint(r.MaterialName, r.CurrentTotalWeightKg))
                        .ToList();
                    chartYieldMaterials.SetData(points);
                }
            }
            catch
            {
                chartYieldMaterials.SetData(new List<ChartDataPoint>());
            }
        }

        private class DashboardSummary
        {
            public int ActiveInventoryCount { get; set; }
            public int DeviceCategoryCount { get; set; }
            public decimal TotalRecoveredWeightKg { get; set; }
            public int TeardownBatchCount { get; set; }
        }
    }
}