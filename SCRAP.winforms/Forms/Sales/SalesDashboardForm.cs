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
    /// Sales-focused dashboard: commodities on hand, recent sales, revenue snapshot.
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
                Text = "Recovered stock and sales performance",
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

            cardMaterials = MakeStatCard("MATERIALS ON HAND", "—", Theme.Blue);
            valMaterials = (Label)cardMaterials.Tag!;

            cardSalesCount = MakeStatCard("TOTAL SALES", "—", Theme.Green);
            valSalesCount = (Label)cardSalesCount.Tag!;

            cardRevenue = MakeStatCard("REVENUE", "—", Theme.Blue);
            valRevenue = (Label)cardRevenue.Tag!;

            lblRecent = Theme.CreateSectionTitle("Recent Sales", 32, 260);

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

            cardMaterials.Left = startX;
            cardMaterials.Top = y;
            cardMaterials.Width = cardW;

            cardSalesCount.Left = startX + cardW + gap;
            cardSalesCount.Top = y;
            cardSalesCount.Width = cardW;

            cardRevenue.Left = startX + (cardW + gap) * 2;
            cardRevenue.Top = y;
            cardRevenue.Width = cardW;

            lblRecent.Top = 260;
            lblRecent.Left = 32;

            cardHistory.Left = 32;
            cardHistory.Top = 292;
            cardHistory.Width = Math.Max(400, ClientSize.Width - 64);
            cardHistory.Height = Math.Max(180, ClientSize.Height - 320);

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
                });
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
                }
            }
            catch
            {
                // non-fatal
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

                    valSalesCount.Text = data.Count.ToString("N0");

                    decimal revenue = data.Sum(s =>
                        s.TotalAmount != 0 ? s.TotalAmount : s.QuantityKg * s.PricePerKg);
                    valRevenue.Text = revenue.ToString("N2");
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