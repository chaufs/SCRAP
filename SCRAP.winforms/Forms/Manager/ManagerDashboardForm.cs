using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    /// <summary>
    /// Manager dashboard: pending destructions, certificates issued, stock snapshot.
    /// </summary>
    [DesignerCategory("Code")]
    public class ManagerDashboardForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;

        private Panel cardPending = null!;
        private Panel cardCertificates = null!;
        private Panel cardInventory = null!;
        private Label valPending = null!;
        private Label valCertificates = null!;
        private Label valInventory = null!;

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
                Text = "Destruction certificates and facility overview",
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

            cardPending = MakeStatCard("PENDING DESTRUCTIONS", "—", Theme.Blue);
            valPending = (Label)cardPending.Tag!;

            cardCertificates = MakeStatCard("CERTIFICATES ISSUED", "—", Theme.Green);
            valCertificates = (Label)cardCertificates.Tag!;

            cardInventory = MakeStatCard("ACTIVE INVENTORY", "—", Theme.Blue);
            valInventory = (Label)cardInventory.Tag!;

            lblRecent = Theme.CreateSectionTitle("Recent Certificates", 32, 260);

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
            Controls.Add(cardPending);
            Controls.Add(cardCertificates);
            Controls.Add(cardInventory);
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

            cardPending.Left = startX;
            cardPending.Top = y;
            cardPending.Width = cardW;

            cardCertificates.Left = startX + cardW + gap;
            cardCertificates.Top = y;
            cardCertificates.Width = cardW;

            cardInventory.Left = startX + (cardW + gap) * 2;
            cardInventory.Top = y;
            cardInventory.Width = cardW;

            lblRecent.Top = 260;
            lblRecent.Left = 32;

            cardHistory.Left = 32;
            cardHistory.Top = 292;
            cardHistory.Width = Math.Max(400, ClientSize.Width - 64);
            cardHistory.Height = Math.Max(180, ClientSize.Height - 320);

            if (dgvRecent.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvRecent);
        }

        private void ConfigureCertificateColumns()
        {
            Theme.ConfigureColumns(
                dgvRecent,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 60),
                    ["CertificateNumber"] = ("Certificate #", 200),
                    ["DestructionDateTime"] = ("Destroyed On", 170),
                    ["OrganizationName"] = ("Organization", 200),
                    ["ProviderName"] = ("Provider", 180),
                    ["Method"] = ("Method", 140),
                    ["SecurityStandard"] = ("Standard", 160),
                    ["VerifiedByName"] = ("Verified By", 160)
                },
                "Items", "OrganizationAddress", "ProviderAddress", "SoftwareToolName",
                "SoftwareToolVersion", "ManagerUserId", "VerifiedDate", "Notes");
        }

        private async Task LoadData()
        {
            await Task.WhenAll(LoadPendingCount(), LoadCertificates(), LoadInventoryCount());
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
                    dgvRecent.DataSource = data;
                    if (dgvRecent.Columns.Count > 0)
                        ConfigureCertificateColumns();
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

        private class DashboardSummary
        {
            public int ActiveInventoryCount { get; set; }
            public int DeviceCategoryCount { get; set; }
            public decimal TotalRecoveredWeightKg { get; set; }
        }
    }
}
