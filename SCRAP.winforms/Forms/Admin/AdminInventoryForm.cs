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
    /// <summary>Admin inventory — view only (Active + Recovered Commodities tabs) with Branch filtering.</summary>
    [DesignerCategory("Code")]
    public class AdminInventoryForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;
        private ComboBox cmbBranch = null!;
        private TabControl tabs = null!;
        private DataGridView dgvActive = null!;
        private DataGridView dgvCommodities = null!;
        private Panel cardActive = null!;
        private Panel cardCommodities = null!;
        private readonly int _initialTab;
        private bool _isBranchesLoaded;

        public AdminInventoryForm(int initialTab = 0)
        {
            _initialTab = initialTab;
            Text = "Inventory";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = InitData();
        }

        public AdminInventoryForm() : this(0) { }

        private async Task InitData()
        {
            await LoadBranches();
            await LoadActive();
            if (_initialTab > 0 && tabs.TabPages.Count > _initialTab)
            {
                tabs.SelectedIndex = _initialTab;
                if (_initialTab == 1)
                    await LoadCommodities();
            }
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Inventory & Stock",
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
                Text = "Read-only overview of devices and recovered commodities by branch",
                Left = 32,
                Top = 64,
                Width = 520,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
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
                    await LoadActive();
            };

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 110,
                Height = 36
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
            dgvActive = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            Theme.StyleGrid(dgvActive);
            cardActive.Controls.Add(dgvActive);
            tabActive.Controls.Add(cardActive);

            cardCommodities = MakeCard();
            dgvCommodities = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            Theme.StyleGrid(dgvCommodities);
            cardCommodities.Controls.Add(dgvCommodities);
            tabComm.Controls.Add(cardCommodities);

            tabs.TabPages.Add(tabActive);
            tabs.TabPages.Add(tabComm);
            tabs.SelectedIndexChanged += async (s, e) =>
            {
                if (tabs.SelectedIndex == 1)
                    await LoadCommodities();
            };

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(cmbBranch);
            Controls.Add(btnRefresh);
            Controls.Add(tabs);

            Resize += (s, e) => LayoutPage();
            LayoutPage();
        }

        private Panel MakeCard()
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

            cmbBranch.Left = Math.Max(0, btnRefresh.Left - cmbBranch.Width - 12);
            cmbBranch.Top = 36;

            tabs.Left = 32;
            tabs.Top = 100;
            tabs.Width = Math.Max(400, ClientSize.Width - 64);
            tabs.Height = Math.Max(300, ClientSize.Height - 120);

            if (dgvActive.Columns.Count > 0) Theme.FillColumnsToWidth(dgvActive);
            if (dgvCommodities.Columns.Count > 0) Theme.FillColumnsToWidth(dgvCommodities);
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

        private void ConfigureActiveColumns()
        {
            Theme.ConfigureColumns(dgvActive,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("Product ID", 80),
                    ["Branch"] = ("Branch", 160),
                    ["DeviceName"] = ("Device Name", 180),
                    ["Category"] = ("Category", 120),
                    ["SerialNumber"] = ("Serial Number", 140),
                    ["Status"] = ("Status", 90),
                    ["DateReceived"] = ("Date Received", 120),
                    ["HasStorage"] = ("Storage Device", 110),
                    ["Notes"] = ("Notes", 140)
                });
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
                var selectedBranch = cmbBranch.SelectedItem as BranchOption;
                var branchId = selectedBranch?.Id ?? 0;
                string url = branchId > 0 ? $"api/Inventory?branchId={branchId}" : "api/Inventory";

                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var items = await res.Content.ReadFromJsonAsync<List<Inventory>>(ApiConfig.JsonOptions) ?? new();
                    var displayList = items.Select(x => new
                    {
                        x.Id,
                        Branch = x.Branch != null ? $"{x.Branch.Code} - {x.Branch.Name}" : "Unassigned",
                        DeviceName = x.DeviceName,
                        Category = x.DeviceCategory?.Name ?? $"Cat #{x.DeviceCategoryId}",
                        SerialNumber = x.SerialNumber ?? "",
                        Status = x.Status.ToString(),
                        DateReceived = x.DateReceived.ToString("yyyy-MM-dd"),
                        HasStorage = x.HasStorageDevice ? "Yes" : "No",
                        Notes = x.Notes ?? ""
                    }).ToList();

                    dgvActive.DataSource = displayList;
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
}