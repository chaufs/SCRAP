using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Sales
{
    /// <summary>
    /// Sales-focused dashboard: commodities on hand, recent sales, revenue snapshot, and visual graphs.
    /// </summary>
    [DesignerCategory("Code")]
    public class SalesDashboardForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;

        private Panel cardMaterials = null!;
        private Panel cardSalesCount = null!;
        private Panel cardRevenue = null!;
        private Label valMaterials = null!;
        private Label valSalesCount = null!;
        private Label valRevenue = null!;

        private BarChartControl chartRevenueByMaterial = null!;
        private DonutChartControl chartStockWeight = null!;

        private Label lblRecent = null!;
        private Panel cardHistory = null!;
        private DataGridView dgvRecent = null!;

        public SalesDashboardForm()
        {
            Text = "Sales Dashboard";
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
                Text = "Sales Dashboard",
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
                Text = "Recovered stock, sales performance, and commodity revenue analytics",
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

            cardMaterials = MakeStatCard("MATERIALS ON HAND", "—", Theme.Blue);
            valMaterials = (Label)cardMaterials.Tag!;

            cardSalesCount = MakeStatCard("TOTAL SALES", "—", Theme.Green);
            valSalesCount = (Label)cardSalesCount.Tag!;

            cardRevenue = MakeStatCard("REVENUE", "—", Theme.Blue);
            valRevenue = (Label)cardRevenue.Tag!;

            chartRevenueByMaterial = new BarChartControl
            {
                Title = "Sales Revenue by Commodity",
                Subtitle = "Revenue generated per material (₱)",
                ValuePrefix = "₱"
            };

            chartStockWeight = new DonutChartControl
            {
                Title = "Available Commodity Stock",
                Subtitle = "Weight breakdown of recovered stock (kg)",
                CenterLabel = "Total kg"
            };

            lblRecent = Theme.CreateSectionTitle("Recent Sales", 32, 400);

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
            Controls.Add(cardMaterials);
            Controls.Add(cardSalesCount);
            Controls.Add(cardRevenue);
            Controls.Add(chartRevenueByMaterial);
            Controls.Add(chartStockWeight);
            Controls.Add(lblRecent);
            Controls.Add(cardHistory);

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

            cardMaterials.Left = startX;
            cardMaterials.Top = y;
            cardMaterials.Width = cardW;

            cardSalesCount.Left = startX + cardW + gap;
            cardSalesCount.Top = y;
            cardSalesCount.Width = cardW;

            cardRevenue.Left = startX + (cardW + gap) * 2;
            cardRevenue.Top = y;
            cardRevenue.Width = cardW;

            int chartY = y + 115;
            int chartH = 200;
            int chartW = (availableW - gap) / 2;

            chartRevenueByMaterial.Left = startX;
            chartRevenueByMaterial.Top = chartY;
            chartRevenueByMaterial.Width = chartW;
            chartRevenueByMaterial.Height = chartH;

            chartStockWeight.Left = startX + chartW + gap;
            chartStockWeight.Top = chartY;
            chartStockWeight.Width = chartW;
            chartStockWeight.Height = chartH;

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

        private void ConfigureSalesColumns()
        {
            Theme.ConfigureColumns(
                dgvRecent,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 70),
                    ["MaterialName"] = ("Material", 160),
                    ["BuyerName"] = ("Buyer", 160),
                    ["QuantityKg"] = ("Qty (kg)", 100),
                    ["PricePerKg"] = ("Price / kg", 110),
                    ["TotalAmount"] = ("Total", 110),
                    ["InvoiceNumber"] = ("Invoice #", 130),
                    ["SaleDate"] = ("Sale Date", 160),
                    ["Notes"] = ("Notes", 160)
                },
                "Branch", "BranchId");
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadCommoditiesSummary(), LoadSales());
        }

        private async Task LoadCommoditiesSummary()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/RawInventory");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<RawInventory>>() ?? new();
                    var distinct = data.Select(x => x.MaterialName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Count();
                    valMaterials.Text = distinct.ToString("N0");

                    // Populate stock weight donut chart
                    var stockPoints = data
                        .Where(r => r.CurrentTotalWeightKg > 0)
                        .Select(r => new ChartDataPoint(r.MaterialName, r.CurrentTotalWeightKg))
                        .ToList();
                    chartStockWeight.SetData(stockPoints);
                }
            }
            catch
            {
                chartStockWeight.SetData(new List<ChartDataPoint>());
            }
        }

        private async Task LoadSales()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/CommoditySales");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CommoditySale>>() ?? new();
                    dgvRecent.DataSource = data;
                    ConfigureSalesColumns();

                    decimal revenue = data.Sum(s => s.TotalAmount != 0 ? s.TotalAmount : s.QuantityKg * s.PricePerKg);
                    valRevenue.Text = "₱" + revenue.ToString("N2");
                    valSalesCount.Text = data.Count.ToString("N0");

                    // Populate Revenue by Commodity Bar Chart
                    var revenuePoints = data
                        .GroupBy(s => s.MaterialName)
                        .Select(g => new ChartDataPoint(g.Key, g.Sum(s => s.TotalAmount != 0 ? s.TotalAmount : s.QuantityKg * s.PricePerKg)))
                        .OrderByDescending(p => p.Value)
                        .Take(7)
                        .ToList();
                    chartRevenueByMaterial.SetData(revenuePoints);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sales: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}