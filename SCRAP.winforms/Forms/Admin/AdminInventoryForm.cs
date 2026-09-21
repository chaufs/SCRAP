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
    /// <summary>Admin inventory — view only (Active + Recovered Commodities tabs).</summary>
    [DesignerCategory("Code")]
    public class AdminInventoryForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;
        private TabControl tabs = null!;
        private DataGridView dgvActive = null!;
        private DataGridView dgvCommodities = null!;
        private Panel cardActive = null!;
        private Panel cardCommodities = null!;
        private readonly int _initialTab;

        public AdminInventoryForm(int initialTab = 0)
        {
            _initialTab = initialTab;
            Text = "Inventory";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadActive();
            if (_initialTab > 0 && tabs.TabPages.Count > _initialTab)
            {
                tabs.SelectedIndex = _initialTab;
                if (_initialTab == 1)
                    _ = LoadCommodities();
            }
        }

        public AdminInventoryForm() : this(0) { }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Inventory & Stock",
                Left = 32,
                Top = 28,
                Width = 520,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };
            lblSubtitle = new Label
            {
                Text = "Read-only overview of devices and recovered commodities",
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
                Width = 110,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) =>
            {
                if (tabs.SelectedIndex == 0) await LoadActive();
                else await LoadCommodities();
            };

            tabs = new TabControl
            {
                Left = 32,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            var tabActive = new TabPage("Active Inventory") { BackColor = Theme.Background, Padding = new Padding(0) };
            var tabComm = new TabPage("Recovered Commodities") { BackColor = Theme.Background, Padding = new Padding(0) };

            cardActive = MakeCard();
            dgvActive = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvActive);
            cardActive.Controls.Add(dgvActive);
            tabActive.Controls.Add(cardActive);

            cardCommodities = MakeCard();
            dgvCommodities = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvCommodities);
            cardCommodities.Controls.Add(dgvCommodities);
            tabComm.Controls.Add(cardCommodities);

            tabs.TabPages.Add(tabActive);
            tabs.TabPages.Add(tabComm);
            tabs.SelectedIndexChanged += async (s, e) =>
            {
                if (tabs.SelectedIndex == 1) await LoadCommodities();
            };

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(tabs);
            Resize += (s, e) => LayoutPage();
            LayoutPage();
        }

        private static Panel MakeCard()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.White, Padding = new Padding(1) };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private void LayoutPage()
        {
            btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
            btnRefresh.Top = 36;
            tabs.Left = 32;
            tabs.Top = 100;
            tabs.Width = Math.Max(400, ClientSize.Width - 64);
            tabs.Height = Math.Max(300, ClientSize.Height - 120);
            if (dgvActive.Columns.Count > 0) Theme.FillColumnsToWidth(dgvActive);
            if (dgvCommodities.Columns.Count > 0) Theme.FillColumnsToWidth(dgvCommodities);
        }

        private void ConfigureActiveColumns()
        {
            Theme.ConfigureColumns(dgvActive,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("Product ID", 90),
                    ["DeviceName"] = ("Device Name", 180),
                    ["DeviceCategoryId"] = ("Category", 100),
                    ["SerialNumber"] = ("Serial Number", 150),
                    ["Status"] = ("Status", 100),
                    ["DateReceived"] = ("Date Received", 180),
                    ["Notes"] = ("Notes", 140),
                    ["HasStorageDrive"] = ("Has Storage Device", 160),
                    ["HasStorage"] = ("Has Storage Device", 160),
                    ["HasStorageDevice"] = ("Has Storage Device", 160)
                },
                "DeviceCategory", "DeviceCategoryNavigation", "TeardownBatches");
        }

        private void ConfigureCommodityColumns()
        {
            Theme.ConfigureColumns(dgvCommodities,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 70),
                    ["MaterialName"] = ("Material", 200),
                    ["CurrentTotalWeightKg"] = ("Current Total Weight (kg)", 200),
                    ["WeightKg"] = ("Weight (kg)", 120),
                    ["Quantity"] = ("Quantity", 100),
                    ["SourceBatchId"] = ("Batch ID", 100),
                    ["DateRecovered"] = ("Date Recovered", 180),
                    ["Notes"] = ("Notes", 180)
                },
                "TeardownBatch", "TeardownBatchNavigation");
        }

        private async Task LoadActive()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Inventory");
                if (res.IsSuccessStatusCode)
                {
                    dgvActive.DataSource = await res.Content.ReadFromJsonAsync<List<Inventory>>();
                    ConfigureActiveColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading inventory: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadCommodities()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/RawInventory");
                if (res.IsSuccessStatusCode)
                {
                    dgvCommodities.DataSource = await res.Content.ReadFromJsonAsync<List<RawInventory>>();
                    ConfigureCommodityColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading commodities: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}