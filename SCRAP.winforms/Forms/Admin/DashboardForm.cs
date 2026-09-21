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
    [DesignerCategory("Code")]
    public class DashboardForm : Form
    {
        /// <summary>Raised when user clicks a dashboard card. Parent shell should switch views.</summary>
        public Action<DashboardNavTarget>? NavigateRequested;

        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;
        private Panel cardInventory = null!;
        private Panel cardCategories = null!;
        private Panel cardRecovered = null!;
        private Panel cardRevenue = null!;
        private Label valInventory = null!;
        private Label valCategories = null!;
        private Label valRecovered = null!;
        private Label valRevenue = null!;

        public DashboardForm()
        {
            Text = "Dashboard";
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
                Text = "Dashboard",
                Left = 32,
                Top = 28,
                Width = 520,
                Height = 40,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = false
            };

            lblSubtitle = new Label
            {
                Text = "Overview of recovery operations",
                Left = 32,
                Top = 68,
                Width = 520,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 120,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.Click += async (s, e) => await LoadData();
            Theme.StyleOutlineButton(btnRefresh);

            cardInventory = MakeStatCard("ACTIVE INVENTORY", "—", Theme.Blue, DashboardNavTarget.Inventory);
            valInventory = (Label)cardInventory.Tag!;

            cardCategories = MakeStatCard("DEVICE CATEGORIES", "—", Theme.Green, DashboardNavTarget.DeviceCategories);
            valCategories = (Label)cardCategories.Tag!;

            cardRecovered = MakeStatCard("TOTAL RECOVERED (kg)", "—", Theme.Blue, DashboardNavTarget.RecoveredCommodities);
            valRecovered = (Label)cardRecovered.Tag!;

            cardRevenue = MakeStatCard("TOTAL REVENUE", "—", Theme.Green, DashboardNavTarget.Reports);
            valRevenue = (Label)cardRevenue.Tag!;

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(cardInventory);
            Controls.Add(cardCategories);
            Controls.Add(cardRecovered);
            Controls.Add(cardRevenue);

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private Panel MakeStatCard(string label, string value, Color accent, DashboardNavTarget target)
        {
            var card = new Panel
            {
                Width = 240,
                Height = 120,
                BackColor = Theme.White,
                Cursor = Cursors.Hand
            };

            var bar = new Panel
            {
                Height = 3,
                Dock = DockStyle.Top,
                BackColor = accent,
                Cursor = Cursors.Hand
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
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
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
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            void OnClick(object? s, EventArgs e) => NavigateRequested?.Invoke(target);

            card.Click += OnClick;
            bar.Click += OnClick;
            val.Click += OnClick;
            lbl.Click += OnClick;

            // Subtle hover
            void Enter(object? s, EventArgs e) => card.BackColor = Theme.SoftBlue;
            void Leave(object? s, EventArgs e) => card.BackColor = Theme.White;
            card.MouseEnter += Enter;
            card.MouseLeave += Leave;
            val.MouseEnter += Enter;
            val.MouseLeave += Leave;
            lbl.MouseEnter += Enter;
            lbl.MouseLeave += Leave;
            bar.MouseEnter += Enter;
            bar.MouseLeave += Leave;

            card.Controls.Add(bar);
            card.Controls.Add(val);
            card.Controls.Add(lbl);
            card.Tag = val;
            return card;
        }

        private void LayoutControls()
        {
            if (btnRefresh != null)
            {
                btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
                btnRefresh.Top = 32;
            }

            int gap = 20;
            int startX = 32;
            int y = 120;
            int cardW = Math.Max(180, (ClientSize.Width - 64 - gap * 3) / 4);

            cardInventory.Left = startX;
            cardInventory.Top = y;
            cardInventory.Width = cardW;

            cardCategories.Left = startX + (cardW + gap);
            cardCategories.Top = y;
            cardCategories.Width = cardW;

            cardRecovered.Left = startX + (cardW + gap) * 2;
            cardRecovered.Top = y;
            cardRecovered.Width = cardW;

            cardRevenue.Left = startX + (cardW + gap) * 3;
            cardRevenue.Top = y;
            cardRevenue.Width = cardW;
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadSummary(), LoadRevenue());
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
                var res = await ApiConfig.Http.GetAsync("api/CommoditySales");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CommoditySale>>()
                               ?? new List<CommoditySale>();
                    decimal revenue = data.Sum(s =>
                        s.TotalAmount != 0 ? s.TotalAmount : s.QuantityKg * s.PricePerKg);
                    valRevenue.Text = revenue.ToString("N2");
                }
                else
                {
                    valRevenue.Text = "0.00";
                }
            }
            catch
            {
                valRevenue.Text = "—";
            }
        }

        private class DashboardSummary
        {
            public int ActiveInventoryCount { get; set; }
            public int DeviceCategoryCount { get; set; }
            public decimal TotalRecoveredWeightKg { get; set; }
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