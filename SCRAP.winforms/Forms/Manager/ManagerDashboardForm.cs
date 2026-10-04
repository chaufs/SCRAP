using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    /// <summary>
    /// Manager dashboard: pending destructions, certificates issued, stock snapshot, and visual analytics.
    /// </summary>
    [DesignerCategory("Code")]
    public class ManagerDashboardForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<string>? NavigateRequested { get; set; }

        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;

        private Panel cardPending = null!;
        private Panel cardCertificates = null!;
        private Panel cardInventory = null!;
        private Label valPending = null!;
        private Label valCertificates = null!;
        private Label valInventory = null!;

        private DonutChartControl chartMethods = null!;
        private BarChartControl chartInventory = null!;

        private Label lblRecent = null!;
        private Panel cardHistory = null!;
        private DataGridView dgvRecent = null!;

        public ManagerDashboardForm()
        {
            Text = "Manager Dashboard";
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
                Text = "Manager Dashboard",
                Left = 32,
                Top = 28,
                Width = 420,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Destruction certificates, facility operations, and analytics",
                Left = 32,
                Top = 64,
                Width = 480,
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

            cardPending = MakeStatCard("PENDING DESTRUCTIONS", "—", Theme.Blue, () => NavigateRequested?.Invoke("Certificates"));
            valPending = (Label)cardPending.Tag!;

            cardCertificates = MakeStatCard("CERTIFICATES ISSUED", "—", Theme.Green, () => NavigateRequested?.Invoke("Certificates"));
            valCertificates = (Label)cardCertificates.Tag!;

            cardInventory = MakeStatCard("ACTIVE INVENTORY", "—", Theme.Blue, () => NavigateRequested?.Invoke("Inventory"));
            valInventory = (Label)cardInventory.Tag!;

            chartMethods = new DonutChartControl
            {
                Title = "Destruction Methods",
                Subtitle = "Breakdown by destruction technique",
                CenterLabel = "Certs"
            };

            chartInventory = new BarChartControl
            {
                Title = "Branch Inventory by Category",
                Subtitle = "Stock available in this facility",
                ValueSuffix = " units",
                IsHorizontal = true
            };

            lblRecent = Theme.CreateSectionTitle("Recent Certificates", 32, 400);

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
            dgvRecent.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(dgvRecent);
            dgvRecent.CellDoubleClick += (s, e) => NavigateRequested?.Invoke("Certificates");
            cardHistory.Controls.Add(dgvRecent);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(cardPending);
            Controls.Add(cardCertificates);
            Controls.Add(cardInventory);
            Controls.Add(chartMethods);
            Controls.Add(chartInventory);
            Controls.Add(lblRecent);
            Controls.Add(cardHistory);

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private Panel MakeStatCard(string label, string value, Color accent, Action? onClick = null)
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

            if (onClick != null)
            {
                card.Cursor = Cursors.Hand;
                lbl.Cursor = Cursors.Hand;
                val.Cursor = Cursors.Hand;
                bar.Cursor = Cursors.Hand;

                card.Click += (s, e) => onClick();
                lbl.Click += (s, e) => onClick();
                val.Click += (s, e) => onClick();
                bar.Click += (s, e) => onClick();
            }

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

            cardPending.Left = startX;
            cardPending.Top = y;
            cardPending.Width = cardW;

            cardCertificates.Left = startX + cardW + gap;
            cardCertificates.Top = y;
            cardCertificates.Width = cardW;

            cardInventory.Left = startX + (cardW + gap) * 2;
            cardInventory.Top = y;
            cardInventory.Width = cardW;

            int chartY = y + 115;
            int chartH = 200;
            int chartW = (availableW - gap) / 2;

            chartMethods.Left = startX;
            chartMethods.Top = chartY;
            chartMethods.Width = chartW;
            chartMethods.Height = chartH;

            chartInventory.Left = startX + chartW + gap;
            chartInventory.Top = chartY;
            chartInventory.Width = chartW;
            chartInventory.Height = chartH;

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

        private static string FormatMethod(DestructionMethod method) => method switch
        {
            DestructionMethod.PhysicalShredding => "Physical Shredding",
            DestructionMethod.Crushing => "Crushing",
            DestructionMethod.Degaussing => "Degaussing",
            DestructionMethod.Incineration => "Incineration",
            DestructionMethod.Disintegration => "Disintegration",
            _ => method.ToString()
        };

        private void ConfigureCertificateColumns()
        {
            Theme.ConfigureColumns(
                dgvRecent,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 60),
                    ["CertificateNumber"] = ("Certificate #", 200),
                    ["DestructionDate"] = ("Destroyed On", 170),
                    ["OrganizationName"] = ("Organization", 200),
                    ["ProviderName"] = ("Provider", 180),
                    ["Method"] = ("Method", 150),
                    ["SecurityStandard"] = ("Standard", 160),
                    ["VerifiedByName"] = ("Verified By", 160)
                });
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadPendingCount(), LoadCertificates(), LoadInventoryCount(), LoadCategoryStock());
        }

        private async Task LoadPendingCount()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/StorageDestruction/pending");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<StorageDestructionRecord>>(ApiConfig.JsonOptions)
                               ?? new List<StorageDestructionRecord>();
                    valPending.Text = data.Count.ToString("N0");
                }
            }
            catch
            {
                valPending.Text = "—";
            }
        }

        private async Task LoadCertificates()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/CertificateOfDestruction");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CertificateOfDestruction>>(ApiConfig.JsonOptions)
                               ?? new List<CertificateOfDestruction>();
                    valCertificates.Text = data.Count.ToString("N0");

                    var rows = data.ConvertAll(c => new
                    {
                        c.Id,
                        c.CertificateNumber,
                        DestructionDate = c.DestructionDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        c.OrganizationName,
                        c.ProviderName,
                        Method = FormatMethod(c.Method),
                        c.SecurityStandard,
                        c.VerifiedByName
                    });

                    dgvRecent.DataSource = rows;
                    if (dgvRecent.Columns.Count > 0)
                        ConfigureCertificateColumns();

                    // Populate Methods Donut Chart
                    var methodPoints = data
                        .GroupBy(c => FormatMethod(c.Method))
                        .Select(g => new ChartDataPoint(g.Key, g.Count()))
                        .ToList();
                    chartMethods.SetData(methodPoints);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading certificates: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadInventoryCount()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Dashboard/summary");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<DashboardSummary>();
                    if (data != null)
                        valInventory.Text = data.ActiveInventoryCount.ToString("N0");
                }
            }
            catch
            {
                valInventory.Text = "—";
            }
        }

        private async Task LoadCategoryStock()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Inventory/summary-by-category");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CategorySummaryDto>>() ?? new();
                    var points = data.Select(c => new ChartDataPoint(c.CategoryName, c.AvailableCount)).ToList();
                    chartInventory.SetData(points);
                }
            }
            catch
            {
                chartInventory.SetData(new List<ChartDataPoint>());
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
    }
}
