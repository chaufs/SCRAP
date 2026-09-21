using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Technical
{
    /// <summary>
    /// Floor-operations dashboard for technical staff.
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
                Text = "Today’s inventory and teardown activity",
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

            cardActive = MakeStatCard("ACTIVE DEVICES", "—", Theme.Blue);
            valActive = (Label)cardActive.Tag!;

            cardTeardowns = MakeStatCard("TEARDOWN BATCHES", "—", Theme.Green);
            valTeardowns = (Label)cardTeardowns.Tag!;

            cardRecovered = MakeStatCard("RECOVERED (kg)", "—", Theme.Blue);
            valRecovered = (Label)cardRecovered.Tag!;

            lblRecent = Theme.CreateSectionTitle("Recent Teardown Batches", 32, 260);

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
                Height = 120,
                BackColor = Theme.White
            };

            var bar = new Panel
            {
                Height = 3,
                Dock = DockStyle.Top,
                BackColor = accent
            };

            var val = new Label
            {
                Text = value,
                Left = 20,
                Top = 28,
                Width = 200,
                Height = 40,
                Font = Theme.StatValueFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = label,
                Left = 20,
                Top = 78,
                Width = 200,
                Height = 22,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
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
            int y = 110;
            int cardW = Math.Max(200, (ClientSize.Width - 64 - gap * 2) / 3);

            cardActive.Left = startX;
            cardActive.Top = y;
            cardActive.Width = cardW;

            cardTeardowns.Left = startX + cardW + gap;
            cardTeardowns.Top = y;
            cardTeardowns.Width = cardW;

            cardRecovered.Left = startX + (cardW + gap) * 2;
            cardRecovered.Top = y;
            cardRecovered.Width = cardW;

            lblRecent.Top = 260;
            lblRecent.Left = 32;

            cardHistory.Left = 32;
            cardHistory.Top = 292;
            cardHistory.Width = Math.Max(400, ClientSize.Width - 64);
            cardHistory.Height = Math.Max(180, ClientSize.Height - 320);

            if (dgvRecent.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvRecent);
        }

        private void ConfigureHistoryColumns()
        {
            Theme.ConfigureColumns(
                dgvRecent,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("Batch ID", 90),
                    ["ProcessedByUserId"] = ("Processed By", 120),
                    ["ProcessedBy"] = ("Processed By", 120),
                    ["DeviceCategoryId"] = ("Category ID", 110),
                    ["CategoryId"] = ("Category ID", 110),
                    ["Quantity"] = ("Quantity", 100),
                    ["QuantityDismantled"] = ("Quantity", 100),
                    ["DateProcessed"] = ("Date Processed", 180),
                    ["ProcessedAt"] = ("Date Processed", 180),
                    ["TotalWeightKg"] = ("Total Weight (kg)", 140),
                    ["Notes"] = ("Notes", 180)
                },
                "DeviceCategory", "User", "RawInventories", "ProcessedByUser",
                "DeviceCategoryNavigation", "ProcessedByUserNavigation");
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadSummary(), LoadRecentTeardowns());
        }

        private async Task LoadSummary()
        {
            try
            {
                // Prefer technical/floor-oriented summary if available; fall back to general dashboard
                var res = await ApiConfig.Http.GetAsync("api/Dashboard/summary");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<DashboardSummary>();
                    if (data != null)
                    {
                        valActive.Text = data.ActiveInventoryCount.ToString("N0");
                        valRecovered.Text = data.TotalRecoveredWeightKg.ToString("N1");
                        // Teardown count may not be on this DTO — filled from history below if needed
                        if (data.TeardownBatchCount > 0)
                            valTeardowns.Text = data.TeardownBatchCount.ToString("N0");
                    }
                }
            }
            catch
            {
                // Non-fatal; history still loads
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
                    dgvRecent.DataSource = data;
                    ConfigureHistoryColumns();

                    // If summary didn't provide batch count, use history length
                    if (valTeardowns.Text == "—" || valTeardowns.Text == "0")
                        valTeardowns.Text = data.Count.ToString("N0");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading teardown history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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