using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class DashboardForm : Form
    {
        /// <summary>Raised when user clicks a dashboard card. Parent shell should switch views.</summary>
        public Action<DashboardNavTarget>? NavigateRequested;

        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private ComboBox cmbBranch = null!;
        private Button btnRefresh = null!;
        private Panel cardInventory = null!;
        private Panel cardCategories = null!;
        private Panel cardRecovered = null!;
        private Panel cardRevenue = null!;
        private Label valInventory = null!;
        private Label valCategories = null!;
        private Label valRecovered = null!;
        private Label valRevenue = null!;

        private BarChartControl chartCategories = null!;
        private DonutChartControl chartCommodities = null!;
        private LineChartControl lineChartRevenue = null!;

        private bool _isBranchesLoaded;

        public DashboardForm()
        {
            Text = "Dashboard";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            AutoScroll = true;
            InitializeComponent();
            _ = InitData();
        }

        private async Task InitData()
        {
            await LoadBranches();
            await LoadData();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Dashboard",
                Left = 32,
                Top = 28,
                Width = 420,
                Height = 40,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = false
            };

            lblSubtitle = new Label
            {
                Text = "Overview of recovery operations by branch",
                Left = 32,
                Top = 68,
                Width = 520,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            cmbBranch = new ComboBox
            {
                Width = 200,
                Height = 36,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cmbBranch);
            cmbBranch.SelectedIndexChanged += async (s, e) =>
            {
                if (_isBranchesLoaded)
                    await LoadData();
            };

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 110,
                Height = 36
            };
            btnRefresh.Click += async (s, e) => await LoadData();
            Theme.StyleOutlineButton(btnRefresh);

            cardInventory = MakeStatCard("ACTIVE INVENTORY", "—", Theme.Blue, DashboardNavTarget.Inventory);
            valInventory = (Label)cardInventory.Tag!;

            cardCategories = MakeStatCard("DEVICE CATEGORIES", "—", Theme.Green, DashboardNavTarget.DeviceCategories);
            valCategories = (Label)cardCategories.Tag!;

            cardRecovered = MakeStatCard("TOTAL RECOVERED (kg)", "—", Theme.Blue, DashboardNavTarget.RecoveredCommodities);
            valRecovered = (Label)cardRecovered.Tag!;

            cardRevenue = MakeStatCard("TOTAL SALES", "—", Theme.Green, DashboardNavTarget.Reports);
            valRevenue = (Label)cardRevenue.Tag!;

            chartCategories = new BarChartControl
            {
                Title = "Inventory by Device Category",
                Subtitle = "Available stock units per category",
                ValueSuffix = " units"
            };

            chartCommodities = new DonutChartControl
            {
                Title = "Recovered Materials Breakdown",
                Subtitle = "Distribution of recovered stock (kg)",
                CenterLabel = "Total kg"
            };

            lineChartRevenue = new LineChartControl
            {
                Title = "Branch Revenue Comparison by Month",
                Subtitle = "Monthly revenue performance comparison across active branches",
                XAxisTitle = "Timeline (Month)",
                YAxisTitle = "Revenue (₱)",
                ValuePrefix = "₱",
                LegendInTopLeftBox = true
            };

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(cmbBranch);
            Controls.Add(btnRefresh);
            Controls.Add(cardInventory);
            Controls.Add(cardCategories);
            Controls.Add(cardRecovered);
            Controls.Add(cardRevenue);
            Controls.Add(lineChartRevenue);
            Controls.Add(chartCategories);
            Controls.Add(chartCommodities);

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private Panel MakeStatCard(string label, string value, Color accent, DashboardNavTarget target)
        {
            var card = new Panel
            {
                Width = 240,
                Height = 110,
                BackColor = Theme.White,
                Cursor = Cursors.Hand
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
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };

            var val = new Label
            {
                Text = value,
                Left = 20,
                Top = 34,
                Width = Math.Max(50, card.Width - 40),
                Height = 40,
                Font = Theme.StatValueFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false
            };

            var hint = new Label
            {
                Text = "Click to view →",
                Left = 20,
                Top = 80,
                Width = Math.Max(50, card.Width - 40),
                Height = 16,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };

            card.Resize += (s, e) =>
            {
                int w = Math.Max(10, card.Width - 40);
                lbl.Width = w;
                val.Width = w;
                hint.Width = w;
            };

            void CardClicked(object? s, EventArgs e) => NavigateRequested?.Invoke(target);

            card.Click += CardClicked;
            bar.Click += CardClicked;
            lbl.Click += CardClicked;
            val.Click += CardClicked;
            hint.Click += CardClicked;

            card.MouseEnter += (s, e) => card.BackColor = Theme.SoftBlue;
            card.MouseLeave += (s, e) => card.BackColor = Theme.White;
            lbl.MouseEnter += (s, e) => card.BackColor = Theme.SoftBlue;
            val.MouseEnter += (s, e) => card.BackColor = Theme.SoftBlue;
            hint.MouseEnter += (s, e) => card.BackColor = Theme.SoftBlue;

            card.Controls.Add(bar);
            card.Controls.Add(lbl);
            card.Controls.Add(val);
            card.Controls.Add(hint);

            card.Tag = val;
            return card;
        }

        private void LayoutControls()
        {
            btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
            btnRefresh.Top = 32;

            cmbBranch.Left = Math.Max(0, btnRefresh.Left - cmbBranch.Width - 12);
            cmbBranch.Top = 32;

            int pad = 32;
            int gap = 20;
            int availableW = Math.Max(300, ClientSize.Width - (pad * 2));
            int cardW = Math.Max(180, (availableW - (gap * 3)) / 4);
            int y = 105;

            cardInventory.Left = pad;
            cardInventory.Top = y;
            cardInventory.Width = cardW;

            cardCategories.Left = pad + cardW + gap;
            cardCategories.Top = y;
            cardCategories.Width = cardW;

            cardRecovered.Left = pad + (cardW + gap) * 2;
            cardRecovered.Top = y;
            cardRecovered.Width = cardW;

            cardRevenue.Left = pad + (cardW + gap) * 3;
            cardRevenue.Top = y;
            cardRevenue.Width = cardW;

            int lineChartY = y + 124;
            int lineChartH = 300;

            lineChartRevenue.Left = pad;
            lineChartRevenue.Top = lineChartY;
            lineChartRevenue.Width = availableW;
            lineChartRevenue.Height = lineChartH;

            int secondaryChartY = lineChartY + lineChartH + gap;
            int secondaryChartH = 260;
            int chartGap = 20;
            int chartW = (availableW - chartGap) / 2;

            chartCategories.Left = pad;
            chartCategories.Top = secondaryChartY;
            chartCategories.Width = chartW;
            chartCategories.Height = secondaryChartH;

            chartCommodities.Left = pad + chartW + chartGap;
            chartCommodities.Top = secondaryChartY;
            chartCommodities.Width = chartW;
            chartCommodities.Height = secondaryChartH;

            AutoScrollMinSize = new Size(950, secondaryChartY + secondaryChartH + 32);
        }

        private async Task LoadBranches()
        {
            try
            {
                var branches = await ApiConfig.Http.GetFromJsonAsync<List<BranchOptionDto>>(
                    "api/Branches", ApiConfig.JsonOptions) ?? new();

                cmbBranch.Items.Clear();
                cmbBranch.Items.Add(new BranchOption { Id = 0, Name = "All Branches" });
                foreach (var b in branches)
                {
                    cmbBranch.Items.Add(new BranchOption { Id = b.Id, Name = $"{b.Code} — {b.Name}" });
                }
                cmbBranch.SelectedIndex = 0;
                _isBranchesLoaded = true;
            }
            catch
            {
                cmbBranch.Items.Clear();
                cmbBranch.Items.Add(new BranchOption { Id = 0, Name = "All Branches" });
                cmbBranch.SelectedIndex = 0;
                _isBranchesLoaded = true;
            }
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadSummary(), LoadRevenue(), LoadCategoryChart(), LoadCommoditiesChart(), LoadBranchRevenueChart());
        }

        private async Task LoadBranchRevenueChart()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Branches/revenue-comparison?months=6");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<BranchRevenueComparisonDto>(ApiConfig.JsonOptions);
                    if (data != null && data.Series != null && data.Series.Count > 0)
                    {
                        var branchId = (cmbBranch?.SelectedItem as BranchOption)?.Id ?? 0;
                        var seriesList = new List<LineSeries>();

                        foreach (var s in data.Series)
                        {
                            if (branchId > 0 && s.BranchId != branchId) continue;

                            Color c;
                            try { c = ColorTranslator.FromHtml(s.Color); } catch { c = Theme.Green; }
                            seriesList.Add(new LineSeries
                            {
                                Name = s.Name,
                                Color = c,
                                Values = s.Values
                            });
                        }

                        lineChartRevenue.SetData(data.XLabels, seriesList);
                    }
                    else
                    {
                        lineChartRevenue.SetData(new List<string>(), new List<LineSeries>());
                    }
                }
            }
            catch
            {
                lineChartRevenue.SetData(new List<string>(), new List<LineSeries>());
            }
        }

        private async Task LoadSummary()
        {
            try
            {
                var branchId = (cmbBranch?.SelectedItem as BranchOption)?.Id ?? 0;
                var url = branchId > 0 ? $"api/Dashboard/summary?branchId={branchId}" : "api/Dashboard/summary";

                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<DashboardSummary>();
                    if (data != null)
                    {
                        valInventory.Text = data.ActiveInventoryCount.ToString("N0");
                        valCategories.Text = data.DeviceCategoryCount.ToString("N0");
                        valRecovered.Text = data.TotalRecoveredWeightKg.ToString("N1");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadRevenue()
        {
            try
            {
                var branchId = (cmbBranch?.SelectedItem as BranchOption)?.Id ?? 0;
                var url = branchId > 0 ? $"api/CommoditySales?branchId={branchId}" : "api/CommoditySales";

                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CommoditySale>>()
                               ?? new List<CommoditySale>();
                    decimal revenue = data.Sum(s =>
                        s.TotalAmount != 0 ? s.TotalAmount : s.QuantityKg * s.PricePerKg);
                    valRevenue.Text = "₱" + revenue.ToString("N2");
                }
                else
                {
                    valRevenue.Text = "₱0.00";
                }
            }
            catch
            {
                valRevenue.Text = "—";
            }
        }

        private async Task LoadCategoryChart()
        {
            try
            {
                var branchId = (cmbBranch?.SelectedItem as BranchOption)?.Id ?? 0;
                var url = branchId > 0 ? $"api/Inventory/summary-by-category?branchId={branchId}" : "api/Inventory/summary-by-category";
                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CategorySummaryDto>>() ?? new();
                    var points = data.Select(c => new ChartDataPoint(c.CategoryName, c.AvailableCount)).ToList();
                    chartCategories.SetData(points);
                }
            }
            catch
            {
                chartCategories.SetData(new List<ChartDataPoint>());
            }
        }

        private async Task LoadCommoditiesChart()
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
                    chartCommodities.SetData(points);
                }
            }
            catch
            {
                chartCommodities.SetData(new List<ChartDataPoint>());
            }
        }

        private class CategorySummaryDto
        {
            public string CategoryName { get; set; } = string.Empty;
            public int AvailableCount { get; set; }
        }

        private class DashboardSummary
        {
            public int ActiveInventoryCount { get; set; }
            public int DeviceCategoryCount { get; set; }
            public decimal TotalRecoveredWeightKg { get; set; }
        }

        private class BranchOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public override string ToString() => Name;
        }

        private class BranchOptionDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
    }

    public enum DashboardNavTarget
    {
        Inventory,
        DeviceCategories,
        RecoveredCommodities,
        Reports
    }
}