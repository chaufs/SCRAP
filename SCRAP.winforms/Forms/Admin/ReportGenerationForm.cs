using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;
using System.Drawing;
using SCRAP.winforms.Forms.Common;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class ReportGenerationForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Panel filterPanel = null!;
        private Label lblBranch = null!;
        private ComboBox cmbBranch = null!;
        private Label lblPeriod = null!;
        private ComboBox cmbPeriod = null!;
        private DateTimePicker dtpDate = null!;
        private ComboBox cmbMonth = null!;
        private ComboBox cmbYear = null!;
        private Label lblTo = null!;
        private DateTimePicker dtpTo = null!;
        private Button btnRefresh = null!;
        private TabControl tabs = null!;
        private Button btnDownloadOverall = null!;
        private DataGridView dgvInventory = null!;
        private DataGridView dgvTeardowns = null!;
        private DataGridView dgvSales = null!;
        private DataGridView dgvProcurement = null!;

        private Button btnExportInventory = null!;
        private Button btnExportTeardowns = null!;
        private Button btnExportSales = null!;
        private Button btnExportProcurement = null!;
        private bool _isBranchesLoaded;
        private readonly Dictionary<int, string> _userMap = new();

        public ReportGenerationForm()
        {
            Text = "Report Generation";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = InitData();
        }

        private async Task InitData()
        {
            await LoadBranches();
            await LoadAll();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Report Generation",
                Left = 32,
                Top = 28,
                Width = 350,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "View and export history for inventory, teardowns, sales, and procurement by branch",
                Left = 32,
                Top = 64,
                Width = 600,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            filterPanel = new Panel
            {
                Height = 38,
                BackColor = Theme.Background
            };

            lblBranch = new Label
            {
                Text = "Branch:",
                AutoSize = true,
                Top = 8,
                Left = 0,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText
            };

            cmbBranch = new ComboBox
            {
                Width = 170,
                Height = 32,
                Top = 4,
                Left = 55,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cmbBranch);
            cmbBranch.SelectedIndexChanged += async (s, e) =>
            {
                if (_isBranchesLoaded)
                    await LoadAll();
            };

            lblPeriod = new Label
            {
                Text = "Filter By:",
                AutoSize = true,
                Top = 8,
                Left = 240,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText
            };

            cmbPeriod = new ComboBox
            {
                Width = 120,
                Height = 32,
                Top = 4,
                Left = 305,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPeriod.Items.AddRange(new object[] { "All Time", "By Day", "By Month", "By Year", "Custom Range" });
            Theme.StyleComboBox(cmbPeriod);
            cmbPeriod.SelectedIndex = 0;
            cmbPeriod.SelectedIndexChanged += async (s, e) =>
            {
                UpdateFilterVisibility();
                if (_isBranchesLoaded)
                    await LoadAll();
            };

            dtpDate = new DateTimePicker
            {
                Width = 120,
                Height = 30,
                Top = 4,
                Left = 435,
                Format = DateTimePickerFormat.Short,
                Font = Theme.LabelFont,
                Value = DateTime.Today,
                Visible = false
            };
            dtpDate.ValueChanged += async (s, e) =>
            {
                if (_isBranchesLoaded)
                    await LoadAll();
            };

            cmbMonth = new ComboBox
            {
                Width = 115,
                Height = 32,
                Top = 4,
                Left = 435,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            cmbMonth.Items.AddRange(new object[] {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            });
            Theme.StyleComboBox(cmbMonth);
            cmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            cmbMonth.SelectedIndexChanged += async (s, e) =>
            {
                if (_isBranchesLoaded)
                    await LoadAll();
            };

            cmbYear = new ComboBox
            {
                Width = 85,
                Height = 32,
                Top = 4,
                Left = 558,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            int currentYear = DateTime.Today.Year;
            for (int y = currentYear - 3; y <= currentYear + 1; y++)
                cmbYear.Items.Add(y);
            Theme.StyleComboBox(cmbYear);
            cmbYear.SelectedItem = currentYear;
            cmbYear.SelectedIndexChanged += async (s, e) =>
            {
                if (_isBranchesLoaded)
                    await LoadAll();
            };

            lblTo = new Label
            {
                Text = "to",
                AutoSize = true,
                Top = 8,
                Left = 562,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText,
                Visible = false
            };

            dtpTo = new DateTimePicker
            {
                Width = 120,
                Height = 30,
                Top = 4,
                Left = 585,
                Format = DateTimePickerFormat.Short,
                Font = Theme.LabelFont,
                Value = DateTime.Today,
                Visible = false
            };
            dtpTo.ValueChanged += async (s, e) =>
            {
                if (_isBranchesLoaded)
                    await LoadAll();
            };

            filterPanel.Controls.Add(lblBranch);
            filterPanel.Controls.Add(cmbBranch);
            filterPanel.Controls.Add(lblPeriod);
            filterPanel.Controls.Add(cmbPeriod);
            filterPanel.Controls.Add(dtpDate);
            filterPanel.Controls.Add(cmbMonth);
            filterPanel.Controls.Add(cmbYear);
            filterPanel.Controls.Add(lblTo);
            filterPanel.Controls.Add(dtpTo);

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 100,
                Height = 36
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadAll();

            btnDownloadOverall = new Button
            {
                Text = "Download Overall Report",
                Width = 180,
                Height = 36
            };
            Theme.StylePrimaryButton(btnDownloadOverall);
            btnDownloadOverall.Click += async (s, e) =>
            {
                var endpoint = BuildUrl("api/Reports/overall/pdf");
                await DownloadPdf(endpoint, "OverallReport");
            };

            Controls.Add(btnDownloadOverall);
            Controls.Add(btnRefresh);
            Controls.Add(filterPanel);

            tabs = new TabControl
            {
                Left = 32,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // ---------------- Inventory tab ----------------
            var tabInventory = new TabPage("Inventory History") { BackColor = Theme.Background, Padding = new Padding(8) };

            var cardInventory = MakeCard();
            cardInventory.Dock = DockStyle.Fill;
            dgvInventory = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };
            Theme.StyleGrid(dgvInventory);
            SetupInventoryColumns();
            cardInventory.Controls.Add(dgvInventory);

            var inventoryFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Background };
            btnExportInventory = new Button { Text = "Download PDF", Width = 140, Height = 32, Top = 8, Left = 0 };
            Theme.StylePrimaryButton(btnExportInventory);
            btnExportInventory.Click += async (s, e) =>
            {
                var endpoint = BuildUrl("api/Reports/inventory-history/pdf");
                await DownloadPdf(endpoint, "InventoryHistory");
            };
            inventoryFooter.Controls.Add(btnExportInventory);

            tabInventory.Controls.Add(cardInventory);
            tabInventory.Controls.Add(inventoryFooter);

            // ---------------- Teardown tab ----------------
            var tabTeardowns = new TabPage("Teardown History") { BackColor = Theme.Background, Padding = new Padding(8) };

            var cardTeardowns = MakeCard();
            cardTeardowns.Dock = DockStyle.Fill;
            dgvTeardowns = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };
            Theme.StyleGrid(dgvTeardowns);
            SetupTeardownColumns();
            cardTeardowns.Controls.Add(dgvTeardowns);

            var teardownFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Background };
            btnExportTeardowns = new Button { Text = "Download PDF", Width = 140, Height = 32, Top = 8, Left = 0 };
            Theme.StylePrimaryButton(btnExportTeardowns);
            btnExportTeardowns.Click += async (s, e) =>
            {
                var endpoint = BuildUrl("api/Reports/teardown-history/pdf");
                await DownloadPdf(endpoint, "TeardownHistory");
            };
            teardownFooter.Controls.Add(btnExportTeardowns);

            tabTeardowns.Controls.Add(cardTeardowns);
            tabTeardowns.Controls.Add(teardownFooter);

            // ---------------- Sales tab ----------------
            var tabSales = new TabPage("Sales History") { BackColor = Theme.Background, Padding = new Padding(8) };

            var cardSales = MakeCard();
            cardSales.Dock = DockStyle.Fill;
            dgvSales = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };
            Theme.StyleGrid(dgvSales);
            SetupSalesColumns();
            cardSales.Controls.Add(dgvSales);

            var salesFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Background };
            btnExportSales = new Button { Text = "Download PDF", Width = 140, Height = 32, Top = 8, Left = 0 };
            Theme.StylePrimaryButton(btnExportSales);
            btnExportSales.Click += async (s, e) =>
            {
                var endpoint = BuildUrl("api/Reports/commodity-sales-history/pdf");
                await DownloadPdf(endpoint, "SalesHistory");
            };
            salesFooter.Controls.Add(btnExportSales);

            tabSales.Controls.Add(cardSales);
            tabSales.Controls.Add(salesFooter);

            // ---------------- Procurement tab ----------------
            var tabProcurement = new TabPage("Procurement History") { BackColor = Theme.Background, Padding = new Padding(8) };

            var cardProcurement = MakeCard();
            cardProcurement.Dock = DockStyle.Fill;
            dgvProcurement = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvProcurement);
            dgvProcurement.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "#", Width = 55, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { DataPropertyName = "RequestedDate",   HeaderText = "Date",             Width = 95, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "SupplierCompany", HeaderText = "Vendor / Supplier",Width = 190 },
                new DataGridViewTextBoxColumn { DataPropertyName = "DeviceName",      HeaderText = "Device",           Width = 190 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Quantity",        HeaderText = "Qty",              Width = 60,  DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { DataPropertyName = "TotalCostFormatted", HeaderText = "Total Cost",    Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { DataPropertyName = "Status",          HeaderText = "Status",           Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewButtonColumn
                {
                    Name = "ColViewProcurement",
                    HeaderText = "Action",
                    Text = "🔍 View",
                    UseColumnTextForButtonValue = true,
                    Width = 90,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 9f) }
                }
            });
            dgvProcurement.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedReportProcurementDetails();
            };
            dgvProcurement.CellContentClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgvProcurement.Columns[e.ColumnIndex].Name == "ColViewProcurement")
                    OpenSelectedReportProcurementDetails();
            };
            cardProcurement.Controls.Add(dgvProcurement);

            var procurementFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Background };
            btnExportProcurement = new Button { Text = "Download PDF", Width = 140, Height = 32, Top = 8, Left = 0 };
            Theme.StylePrimaryButton(btnExportProcurement);
            btnExportProcurement.Click += async (s, e) =>
            {
                var endpoint = BuildUrl("api/Reports/procurement-history/pdf");
                await DownloadPdf(endpoint, "ProcurementHistory");
            };
            procurementFooter.Controls.Add(btnExportProcurement);

            var btnViewProcurement = new Button { Text = "🔍  View Details", Width = 130, Height = 32, Top = 8, Left = 150 };
            Theme.StyleOutlineButton(btnViewProcurement);
            btnViewProcurement.Click += (_, _) => OpenSelectedReportProcurementDetails();
            procurementFooter.Controls.Add(btnViewProcurement);

            tabProcurement.Controls.Add(cardProcurement);
            tabProcurement.Controls.Add(procurementFooter);

            tabs.TabPages.Add(tabInventory);
            tabs.TabPages.Add(tabTeardowns);
            tabs.TabPages.Add(tabSales);
            tabs.TabPages.Add(tabProcurement);

            tabs.SelectedIndexChanged += (s, e) =>
            {
                var currentGrid = tabs.SelectedIndex switch
                {
                    0 => dgvInventory,
                    1 => dgvTeardowns,
                    2 => dgvSales,
                    3 => dgvProcurement,
                    _ => null
                };
                if (currentGrid != null && currentGrid.IsHandleCreated && currentGrid.Columns.Count > 0)
                {
                    Theme.FillColumnsToWidth(currentGrid);
                }
            };

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(tabs);

            Resize += (s, e) => LayoutPage();
            LayoutPage();
        }

        private Panel MakeCard()
        {
            var p = new Panel { BackColor = Theme.White };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private void LayoutPage()
        {
            btnDownloadOverall.Left = ClientSize.Width - btnDownloadOverall.Width - 32;
            btnDownloadOverall.Top = 36;

            btnRefresh.Left = btnDownloadOverall.Left - btnRefresh.Width - 10;
            btnRefresh.Top = 36;

            filterPanel.Left = 32;
            filterPanel.Top = 96;
            filterPanel.Width = Math.Max(750, ClientSize.Width - 64);

            tabs.Left = 32;
            tabs.Top = 142;
            tabs.Width = Math.Max(400, ClientSize.Width - 64);
            tabs.Height = Math.Max(300, ClientSize.Height - 162);
        }

        private void UpdateFilterVisibility()
        {
            var period = cmbPeriod.SelectedItem?.ToString() ?? "All Time";
            dtpDate.Visible = false;
            cmbMonth.Visible = false;
            cmbYear.Visible = false;
            lblTo.Visible = false;
            dtpTo.Visible = false;

            switch (period)
            {
                case "By Day":
                    dtpDate.Visible = true;
                    dtpDate.Left = 435;
                    break;
                case "By Month":
                    cmbMonth.Visible = true;
                    cmbMonth.Left = 435;
                    cmbYear.Visible = true;
                    cmbYear.Left = 558;
                    break;
                case "By Year":
                    cmbYear.Visible = true;
                    cmbYear.Left = 435;
                    break;
                case "Custom Range":
                    dtpDate.Visible = true;
                    dtpDate.Left = 435;
                    lblTo.Visible = true;
                    lblTo.Left = 562;
                    dtpTo.Visible = true;
                    dtpTo.Left = 585;
                    break;
                case "All Time":
                default:
                    break;
            }
        }

        private (DateTime? from, DateTime? to) GetDateFilterRange()
        {
            var period = cmbPeriod.SelectedItem?.ToString() ?? "All Time";
            switch (period)
            {
                case "By Day":
                {
                    var day = dtpDate.Value.Date;
                    return (day, day.AddDays(1).AddTicks(-1));
                }
                case "By Month":
                {
                    int month = cmbMonth.SelectedIndex + 1;
                    if (month < 1 || month > 12) month = DateTime.Today.Month;
                    int year = cmbYear.SelectedItem is int y ? y : DateTime.Today.Year;
                    var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
                    var end = start.AddMonths(1).AddTicks(-1);
                    return (start, end);
                }
                case "By Year":
                {
                    int year = cmbYear.SelectedItem is int y ? y : DateTime.Today.Year;
                    var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local);
                    var end = new DateTime(year, 12, 31, 23, 59, 59, 999, DateTimeKind.Local);
                    return (start, end);
                }
                case "Custom Range":
                {
                    var start = dtpDate.Value.Date;
                    var end = dtpTo.Value.Date.AddDays(1).AddTicks(-1);
                    return (start, end);
                }
                default:
                    return (null, null);
            }
        }

        private string BuildUrl(string basePath)
        {
            var branchId = (cmbBranch?.SelectedItem as BranchOption)?.Id ?? 0;
            var (from, to) = GetDateFilterRange();
            var query = new List<string>();

            if (branchId > 0)
                query.Add($"branchId={branchId}");

            if (from.HasValue)
                query.Add($"from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");

            if (to.HasValue)
                query.Add($"to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");

            if (query.Count > 0)
                return $"{basePath}?{string.Join("&", query)}";

            return basePath;
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

        private void SetupInventoryColumns()
        {
            dgvInventory.Columns.Clear();
            dgvInventory.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "DateReceived", HeaderText = "Date Received", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "DeviceName",   HeaderText = "Device Name",   Width = 160 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Category",     HeaderText = "Category",      Width = 140 },
                new DataGridViewTextBoxColumn { DataPropertyName = "SerialNumber", HeaderText = "Serial / Batch", Width = 130 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Status",       HeaderText = "Status",        Width = 100 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Branch",       HeaderText = "Branch",        Width = 130 },
                new DataGridViewTextBoxColumn { DataPropertyName = "HasStorage",   HeaderText = "Storage?",      Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { DataPropertyName = "RecordedBy",   HeaderText = "Recorded By",   Width = 120 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Notes",        HeaderText = "Notes",         Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });
        }

        private void SetupTeardownColumns()
        {
            dgvTeardowns.Columns.Clear();
            dgvTeardowns.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "BatchCode",          HeaderText = "Batch #",             Width = 100 },
                new DataGridViewTextBoxColumn { DataPropertyName = "DateProcessed",      HeaderText = "Date Processed",      Width = 140 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Category",           HeaderText = "Device Category",     Width = 160 },
                new DataGridViewTextBoxColumn { DataPropertyName = "QuantityDismantled", HeaderText = "Qty Dismantled",     Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { DataPropertyName = "YieldSummary",       HeaderText = "Recovered Materials", Width = 170 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Branch",             HeaderText = "Branch",              Width = 140 },
                new DataGridViewTextBoxColumn { DataPropertyName = "ProcessedBy",        HeaderText = "Processed By",        Width = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });
        }

        private void SetupSalesColumns()
        {
            dgvSales.Columns.Clear();
            dgvSales.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNumber", HeaderText = "Invoice #",           Width = 120 },
                new DataGridViewTextBoxColumn { DataPropertyName = "SaleDate",      HeaderText = "Sale Date",           Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "MaterialName",  HeaderText = "Commodity / Material", Width = 160 },
                new DataGridViewTextBoxColumn { DataPropertyName = "BuyerName",     HeaderText = "Buyer",               Width = 150 },
                new DataGridViewTextBoxColumn { DataPropertyName = "QuantityKg",    HeaderText = "Weight (kg)",         Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { DataPropertyName = "PricePerKg",    HeaderText = "Price / kg",          Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { DataPropertyName = "TotalAmount",   HeaderText = "Total Amount",        Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { DataPropertyName = "Branch",        HeaderText = "Branch",              Width = 130 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Notes",         HeaderText = "Notes",               Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });
        }

        private async Task EnsureUsersLoaded()
        {
            if (_userMap.Count > 0) return;
            try
            {
                var users = await ApiConfig.Http.GetFromJsonAsync<List<UserManagement>>("api/Users");
                if (users != null)
                {
                    foreach (var u in users)
                    {
                        var fullName = $"{u.FirstName} {u.LastName}".Trim();
                        var name = !string.IsNullOrWhiteSpace(fullName) ? fullName : u.Username;
                        _userMap[u.Id] = name;
                    }
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        private string ResolveUserName(int userId)
        {
            if (userId <= 0) return "—";
            if (_userMap.TryGetValue(userId, out var name) && !string.IsNullOrWhiteSpace(name))
                return name;
            return $"User #{userId}";
        }

        private async Task LoadAll()
        {
            await EnsureUsersLoaded();
            await LoadInventory();
            await LoadTeardowns();
            await LoadSales();
            await LoadProcurement();
        }

        private async Task LoadInventory()
        {
            try
            {
                var url = BuildUrl("api/Reports/inventory-history");
                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<Inventory>>(ApiConfig.JsonOptions) ?? new List<Inventory>();
                    var (from, to) = GetDateFilterRange();
                    if (from.HasValue) data = data.Where(x => x.DateReceived >= from.Value).ToList();
                    if (to.HasValue) data = data.Where(x => x.DateReceived <= to.Value).ToList();

                    var displayList = data.Select(i => new
                    {
                        DateReceived = i.DateReceived,
                        DeviceName = !string.IsNullOrWhiteSpace(i.DeviceName) ? i.DeviceName : "—",
                        Category = i.DeviceCategory?.Name ?? $"Category #{i.DeviceCategoryId}",
                        SerialNumber = !string.IsNullOrWhiteSpace(i.SerialNumber) ? i.SerialNumber : (!string.IsNullOrWhiteSpace(i.BatchCode) ? i.BatchCode : "—"),
                        Status = i.Status.ToString(),
                        Branch = i.Branch?.Name ?? $"Branch #{i.BranchId}",
                        HasStorage = i.HasStorageDevice ? "Yes" : "No",
                        RecordedBy = !string.IsNullOrWhiteSpace(i.RecordedByUsername) ? i.RecordedByUsername : "—",
                        Notes = i.Notes ?? ""
                    }).ToList();

                    dgvInventory.DataSource = null;
                    dgvInventory.DataSource = displayList;
                    Theme.FillColumnsToWidth(dgvInventory);
                }
                else
                {
                    MessageBox.Show("Error loading inventory history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading inventory history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadTeardowns()
        {
            try
            {
                var url = BuildUrl("api/Reports/teardown-history");
                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<TeardownBatch>>(ApiConfig.JsonOptions) ?? new List<TeardownBatch>();
                    var (from, to) = GetDateFilterRange();
                    if (from.HasValue) data = data.Where(b => b.DateProcessed >= from.Value).ToList();
                    if (to.HasValue) data = data.Where(b => b.DateProcessed <= to.Value).ToList();

                    var displayList = data.Select(b =>
                    {
                        string yieldText = "0 materials";
                        if (b.Yields != null && b.Yields.Count > 0)
                        {
                            decimal totalKg = b.Yields.Sum(y => y.WeightKg);
                            yieldText = $"{b.Yields.Count} mat ({totalKg:N2} kg)";
                        }

                        return new
                        {
                            BatchCode = $"BAT-{b.Id:D4}",
                            DateProcessed = b.DateProcessed.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                            Category = b.DeviceCategory?.Name ?? $"Category #{b.DeviceCategoryId}",
                            QuantityDismantled = b.QuantityDismantled,
                            YieldSummary = yieldText,
                            Branch = b.Branch?.Name ?? $"Branch #{b.BranchId}",
                            ProcessedBy = ResolveUserName(b.ProcessedByUserId)
                        };
                    }).ToList();

                    dgvTeardowns.DataSource = null;
                    dgvTeardowns.DataSource = displayList;
                    Theme.FillColumnsToWidth(dgvTeardowns);
                }
                else
                {
                    MessageBox.Show("Error loading teardown history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading teardown history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadSales()
        {
            try
            {
                var url = BuildUrl("api/Reports/commodity-sales-history");
                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CommoditySale>>(ApiConfig.JsonOptions) ?? new List<CommoditySale>();
                    var (from, to) = GetDateFilterRange();
                    if (from.HasValue) data = data.Where(s => s.SaleDate >= from.Value).ToList();
                    if (to.HasValue) data = data.Where(s => s.SaleDate <= to.Value).ToList();

                    var displayList = data.Select(s => new
                    {
                        InvoiceNumber = !string.IsNullOrWhiteSpace(s.InvoiceNumber) ? s.InvoiceNumber : $"INV-{s.Id:D4}",
                        SaleDate = s.SaleDate,
                        MaterialName = s.MaterialName,
                        BuyerName = !string.IsNullOrWhiteSpace(s.BuyerName) ? s.BuyerName : "—",
                        QuantityKg = s.QuantityKg,
                        PricePerKg = s.PricePerKg,
                        TotalAmount = s.TotalAmount,
                        Branch = s.Branch?.Name ?? $"Branch #{s.BranchId}",
                        Notes = s.Notes ?? ""
                    }).ToList();

                    dgvSales.DataSource = null;
                    dgvSales.DataSource = displayList;
                    Theme.FillColumnsToWidth(dgvSales);
                }
                else
                {
                    MessageBox.Show("Error loading sales history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sales history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadProcurement()
        {
            try
            {
                var url = BuildUrl("api/Reports/procurement-history");
                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<ProcurementRequest>>(ApiConfig.JsonOptions) ?? new List<ProcurementRequest>();
                    var (from, to) = GetDateFilterRange();
                    if (from.HasValue) data = data.Where(p => p.RequestedAtUtc.ToLocalTime() >= from.Value).ToList();
                    if (to.HasValue) data = data.Where(p => p.RequestedAtUtc.ToLocalTime() <= to.Value).ToList();

                    var displayList = data.Select(p =>
                    {
                        var reqBy = !string.IsNullOrWhiteSpace(p.RequestedByFullName)
                            ? p.RequestedByFullName
                            : (!string.IsNullOrWhiteSpace(p.RequestedByUserName) ? p.RequestedByUserName : "—");

                        var accBy = !string.IsNullOrWhiteSpace(p.ReviewedByFullName)
                            ? p.ReviewedByFullName
                            : (!string.IsNullOrWhiteSpace(p.ReviewedByUserName) ? p.ReviewedByUserName : "—");

                        var tech = !string.IsNullOrWhiteSpace(p.AssignedTechStaffFullName)
                            ? p.AssignedTechStaffFullName
                            : (!string.IsNullOrWhiteSpace(p.AssignedTechStaffUserName) ? p.AssignedTechStaffUserName : "—");

                        var code = !string.IsNullOrWhiteSpace(p.SerialNumber) ? p.SerialNumber : (p.BatchCode ?? "—");

                        return new
                        {
                            Id = p.Id,
                            RequestedDate = p.RequestedAtUtc.ToLocalTime(),
                            SupplierCompany = !string.IsNullOrWhiteSpace(p.SupplierCompany) ? p.SupplierCompany : "—",
                            DeviceName = p.DeviceName,
                            CategoryName = p.DeviceCategory?.Name ?? "—",
                            Quantity = p.Quantity,
                            CostPerDevice = p.CostPerDevice,
                            TotalCost = p.TotalCost,
                            TotalCostFormatted = $"₱{p.TotalCost:N2}",
                            Status = p.Status.ToString(),
                            RequestedBy = reqBy,
                            AcceptedBy = accBy,
                            AssignedTech = tech,
                            SerialNumber = code,
                            Notes = !string.IsNullOrWhiteSpace(p.RejectionReason) 
                                ? $"Rejected: {p.RejectionReason}" 
                                : (p.Notes ?? "")
                        };
                    }).ToList();

                    dgvProcurement.AutoGenerateColumns = false;
                    dgvProcurement.DataSource = null;
                    dgvProcurement.DataSource = displayList;
                    Theme.FillColumnsToWidth(dgvProcurement);
                }
                else
                {
                    MessageBox.Show("Error loading procurement history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading procurement history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSelectedReportProcurementDetails()
        {
            if (dgvProcurement.CurrentRow?.DataBoundItem != null)
            {
                dynamic item = dgvProcurement.CurrentRow.DataBoundItem;
                int id = item.Id;
                using var dlg = new ProcurementDetailsDialog(id);
                dlg.ShowDialog(this);
            }
        }

        private async Task DownloadPdf(string endpoint, string filePrefix)
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync(endpoint);
                if (res.IsSuccessStatusCode)
                {
                    var bytes = await res.Content.ReadAsByteArrayAsync();
                    var path = Path.Combine(Path.GetTempPath(), $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    await File.WriteAllBytesAsync(path, bytes);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Failed to generate PDF: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating PDF: " + ex.Message, "S.C.R.A.P",
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