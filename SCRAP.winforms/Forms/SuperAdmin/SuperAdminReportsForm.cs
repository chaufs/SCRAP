using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace SCRAP.winforms.Forms
{
    public class SuperAdminReportsForm : Form
    {
        // Metric KPI Card Labels
        private Label lblTotalRevenue = null!;
        private Label lblMrr = null!;
        private Label lblArr = null!;
        private Label lblRetention = null!;
        private Label lblTotalUsers = null!;
        private Label lblTotalBranches = null!;

        // Plan distribution & Telemetry strip
        private Label lblBasicCount = null!;
        private Label lblStandardCount = null!;
        private Label lblEnterpriseCount = null!;
        private Label lblCloudDbCount = null!;
        private Label lblExpiringAlert = null!;

        private TabControl tabReports = null!;

        // Tab 1: Executive BI Visualizations
        private RevenueTrendChartPanel pnlRevenueTrend = null!;
        private PlanDonutChartPanel pnlPlanDonut = null!;
        private ModuleAdoptionPanel pnlModuleAdoption = null!;
        private PlatformHealthAlertsPanel pnlHealthAlerts = null!;

        // Tab 2: Tenant Health & Intelligence Matrix
        private DataGridView dgvReport = null!;
        private TextBox txtTenantSearch = null!;
        private ComboBox cboHealthFilter = null!;
        private ComboBox cboPlanFilter = null!;
        private Label lblTenantMatrixCount = null!;
        private Button btnManageTenant = null!;

        // Tab 3: Subscription History Ledger
        private DataGridView dgvHistory = null!;
        private TextBox txtHistorySearch = null!;
        private Label lblHistoryCount = null!;

        // Top Actions
        private Button btnRefresh = null!;
        private Button btnExportPdf = null!;

        // Data caches
        private PlatformSummaryResponse? _summaryData;
        private List<TenantReportItem> _reportItems = new();
        private List<TenantReportItem> _filteredReportItems = new();
        private List<SubscriptionHistoryItem> _historyItems = new();
        private List<SubscriptionHistoryItem> _filteredHistoryItems = new();

        public SuperAdminReportsForm()
        {
            Text = "Business Intelligence & Platform Analytics";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;

            BuildUi();
            _ = LoadReportData();
        }

        private void BuildUi()
        {
            // Top Header with Title and KPI Cards
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 168,
                BackColor = Theme.Background,
                Padding = new Padding(32, 14, 32, 0)
            };

            var lblTitle = new Label
            {
                Text = "📊  Platform Business Intelligence & Executive Analytics",
                UseMnemonic = false,
                Left = 32,
                Top = 12,
                Width = 850,
                Height = 32,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };

            var lblSub = new Label
            {
                Text = "Cross-tenant revenue velocity in Philippine Peso (₱), ARR/MRR tracking, module adoption, and real-time multi-tenant telemetry.",
                UseMnemonic = false,
                Left = 32,
                Top = 46,
                Width = 1000,
                Height = 20,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);

            // 6 Executive KPI Metric Cards
            var c1 = CreateMetricCard("TOTAL REVENUE GENERATED", out lblTotalRevenue, ColorTranslator.FromHtml("#059669"));
            var c2 = CreateMetricCard("EST. MONTHLY REVENUE (MRR)", out lblMrr, Theme.Green);
            var c3 = CreateMetricCard("EST. ANNUAL RUN-RATE (ARR)", out lblArr, ColorTranslator.FromHtml("#7C3AED"));
            var c4 = CreateMetricCard("SUBSCRIBER RETENTION", out lblRetention, ColorTranslator.FromHtml("#2563EB"));
            var c5 = CreateMetricCard("GLOBAL PLATFORM USERS", out lblTotalUsers, ColorTranslator.FromHtml("#4F46E5"));
            var c6 = CreateMetricCard("HOSTED BRANCHES", out lblTotalBranches, ColorTranslator.FromHtml("#0284C7"));

            var cards = new[] { c1, c2, c3, c4, c5, c6 };
            foreach (var card in cards) header.Controls.Add(card);

            header.Resize += (s, e) =>
            {
                int padLeft = 32;
                int padRight = 32;
                int gap = 10;
                int count = cards.Length;
                int availWidth = header.ClientSize.Width - padLeft - padRight;
                int cardW = Math.Max(150, (availWidth - (gap * (count - 1))) / count);
                int top = 74;

                for (int i = 0; i < count; i++)
                {
                    cards[i].Width = cardW;
                    cards[i].Height = 64;
                    cards[i].Left = padLeft + i * (cardW + gap);
                    cards[i].Top = top;
                }
            };

            // Telemetry & Action Strip
            var planStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Background,
                Padding = new Padding(32, 2, 32, 2)
            };

            var stripCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.White
            };
            stripCard.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, stripCard.Width - 1, stripCard.Height - 1);
            };

            var lblDistTitle = new Label
            {
                Text = "TIERS:",
                Left = 14,
                Top = 13,
                Width = 50,
                Height = 20,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };

            lblBasicCount = new Label
            {
                Text = "Basic: 0",
                Left = 66,
                Top = 13,
                Width = 90,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#3B82F6")
            };

            lblStandardCount = new Label
            {
                Text = "Standard: 0",
                Left = 160,
                Top = 13,
                Width = 105,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Green
            };

            lblEnterpriseCount = new Label
            {
                Text = "Enterprise: 0",
                Left = 270,
                Top = 13,
                Width = 115,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#7C3AED")
            };

            lblCloudDbCount = new Label
            {
                Text = "☁️ Cloud DBs: 0",
                Left = 390,
                Top = 13,
                Width = 135,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0284C7")
            };

            lblExpiringAlert = new Label
            {
                Text = "⚠️ Expiring Soon: 0",
                Left = 530,
                Top = 13,
                Width = 145,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#DC2626")
            };

            btnExportPdf = new Button
            {
                Text = "📄  Export to PDF",
                Top = 7,
                Width = 135,
                Height = 32
            };
            Theme.StylePrimaryButton(btnExportPdf);
            btnExportPdf.Click += BtnExportPdf_Click;

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Top = 7,
                Width = 95,
                Height = 32
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadReportData();

            stripCard.Controls.Add(lblDistTitle);
            stripCard.Controls.Add(lblBasicCount);
            stripCard.Controls.Add(lblStandardCount);
            stripCard.Controls.Add(lblEnterpriseCount);
            stripCard.Controls.Add(lblCloudDbCount);
            stripCard.Controls.Add(lblExpiringAlert);
            stripCard.Controls.Add(btnExportPdf);
            stripCard.Controls.Add(btnRefresh);

            stripCard.Resize += (s, e) =>
            {
                btnRefresh.Left = stripCard.ClientSize.Width - 14 - btnRefresh.Width;
                btnExportPdf.Left = btnRefresh.Left - 8 - btnExportPdf.Width;
            };

            planStrip.Controls.Add(stripCard);

            // Tabbed Body Wrapper
            var bodyWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(32, 10, 32, 20)
            };

            tabReports = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Point(14, 8)
            };

            // TAB 1: EXECUTIVE BI VISUALIZATIONS
            var tabBi = new TabPage("  📊  Executive BI Visualizations  ")
            {
                BackColor = Theme.Background,
                Padding = new Padding(0)
            };
            BuildBiVisualizationsTab(tabBi);

            // TAB 2: TENANT HEALTH & INTELLIGENCE MATRIX
            var tabTenants = new TabPage("  🏢  Tenant Health & Intelligence Matrix  ")
            {
                BackColor = Theme.White,
                Padding = new Padding(8)
            };
            BuildTenantMatrixTab(tabTenants);

            // TAB 3: SUBSCRIPTION REVENUE LEDGER
            var tabHistory = new TabPage("  📜  Subscription Revenue Ledger  ")
            {
                BackColor = Theme.White,
                Padding = new Padding(8)
            };
            BuildLedgerTab(tabHistory);

            tabReports.TabPages.Add(tabBi);
            tabReports.TabPages.Add(tabTenants);
            tabReports.TabPages.Add(tabHistory);

            bodyWrapper.Controls.Add(tabReports);

            Controls.Add(bodyWrapper);
            Controls.Add(planStrip);
            Controls.Add(header);
        }

        private Panel CreateMetricCard(string label, out Label valLabel, Color accent)
        {
            var pnl = new Panel
            {
                BackColor = Theme.White
            };
            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            var lblT = new Label
            {
                Text = label,
                Left = 10,
                Top = 6,
                Width = 220,
                Height = 14,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.MutedText
            };

            var targetVal = new Label
            {
                Text = "—",
                Left = 10,
                Top = 23,
                Width = 220,
                Height = 32,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = accent
            };
            valLabel = targetVal;

            pnl.Controls.Add(lblT);
            pnl.Controls.Add(targetVal);

            pnl.Resize += (s, e) =>
            {
                lblT.Width = pnl.Width - 20;
                targetVal.Width = pnl.Width - 20;
            };

            return pnl;
        }

        private void BuildBiVisualizationsTab(TabPage tab)
        {
            var gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Theme.Background,
                Padding = new Padding(0)
            };

            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            pnlRevenueTrend = new RevenueTrendChartPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 6)
            };

            pnlPlanDonut = new PlanDonutChartPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 6)
            };

            pnlModuleAdoption = new ModuleAdoptionPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 6, 0)
            };

            pnlHealthAlerts = new PlatformHealthAlertsPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 6, 0, 0)
            };

            pnlHealthAlerts.OnViewExpiringClicked += () =>
            {
                tabReports.SelectedIndex = 1;
                cboHealthFilter.SelectedIndex = 2; // Expiring Soon
            };

            gridLayout.Controls.Add(pnlRevenueTrend, 0, 0);
            gridLayout.Controls.Add(pnlPlanDonut, 1, 0);
            gridLayout.Controls.Add(pnlModuleAdoption, 0, 1);
            gridLayout.Controls.Add(pnlHealthAlerts, 1, 1);

            tab.Controls.Add(gridLayout);
        }

        private void BuildTenantMatrixTab(TabPage tab)
        {
            var filterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.White,
                Padding = new Padding(4, 6, 4, 6)
            };

            txtTenantSearch = new TextBox
            {
                PlaceholderText = "🔍 Search tenant code, name, or db...",
                Left = 4,
                Top = 8,
                Width = 260,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtTenantSearch.TextChanged += (s, e) => ApplyTenantFilters();

            cboHealthFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 274,
                Top = 8,
                Width = 150,
                Height = 28,
                Font = new Font("Segoe UI", 9f)
            };
            cboHealthFilter.Items.AddRange(new object[] { "All Statuses", "Active (Healthy)", "Expiring Soon", "Suspended" });
            cboHealthFilter.SelectedIndex = 0;
            cboHealthFilter.SelectedIndexChanged += (s, e) => ApplyTenantFilters();

            cboPlanFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 432,
                Top = 8,
                Width = 125,
                Height = 28,
                Font = new Font("Segoe UI", 9f)
            };
            cboPlanFilter.Items.AddRange(new object[] { "All Plans", "Basic", "Standard", "Enterprise" });
            cboPlanFilter.SelectedIndex = 0;
            cboPlanFilter.SelectedIndexChanged += (s, e) => ApplyTenantFilters();

            lblTenantMatrixCount = new Label
            {
                Text = "Showing 0 tenants",
                Left = 570,
                Top = 13,
                Width = 180,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Theme.MutedText
            };

            btnManageTenant = new Button
            {
                Text = "Manage Tenant",
                Top = 7,
                Width = 135,
                Height = 32
            };
            Theme.StyleOutlineButton(btnManageTenant);
            btnManageTenant.Click += (s, e) => OpenSelectedTenantManager();

            filterBar.Controls.Add(txtTenantSearch);
            filterBar.Controls.Add(cboHealthFilter);
            filterBar.Controls.Add(cboPlanFilter);
            filterBar.Controls.Add(lblTenantMatrixCount);
            filterBar.Controls.Add(btnManageTenant);

            filterBar.Resize += (s, e) =>
            {
                btnManageTenant.Left = filterBar.ClientSize.Width - 4 - btnManageTenant.Width;
            };

            dgvReport = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvReport);
            SetupTenantColumns();

            dgvReport.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedTenantManager();
            };

            tab.Controls.Add(dgvReport);
            tab.Controls.Add(filterBar);
        }

        private void BuildLedgerTab(TabPage tab)
        {
            var filterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.White,
                Padding = new Padding(4, 6, 4, 6)
            };

            txtHistorySearch = new TextBox
            {
                PlaceholderText = "🔍 Search history by company, plan, notes...",
                Left = 4,
                Top = 8,
                Width = 300,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtHistorySearch.TextChanged += (s, e) => ApplyHistoryFilters();

            lblHistoryCount = new Label
            {
                Text = "Total Ledger Records: 0",
                Left = 315,
                Top = 13,
                Width = 350,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Theme.MutedText
            };

            filterBar.Controls.Add(txtHistorySearch);
            filterBar.Controls.Add(lblHistoryCount);

            dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvHistory);
            SetupHistoryColumns();

            tab.Controls.Add(dgvHistory);
            tab.Controls.Add(filterBar);
        }

        private void SetupTenantColumns()
        {
            dgvReport.Columns.Clear();
            dgvReport.AutoGenerateColumns = false;

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CompanyCode",
                HeaderText = "Code",
                Width = 75,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CompanyName",
                HeaderText = "Company Name",
                Width = 175
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "HealthScoreDisplay",
                HeaderText = "Subscription Status",
                Width = 145,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SubscriptionPlan",
                HeaderText = "Plan Tier",
                Width = 90,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "UserCountFormatted",
                HeaderText = "Users",
                Width = 75,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "BranchCountFormatted",
                HeaderText = "Branches",
                Width = 75,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "MonthlyRateFormatted",
                HeaderText = "Rate / mo",
                Width = 95,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalMoneyGeneratedFormatted",
                HeaderText = "Total Paid (₱)",
                Width = 120,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CloudStatusFormatted",
                HeaderText = "Cloud DB",
                Width = 95,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DatabaseName",
                HeaderText = "Local Database",
                Width = 135
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DaysRemainingFormatted",
                HeaderText = "Days Left",
                Width = 85,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreatedAtFormatted",
                HeaderText = "Onboarded",
                Width = 95
            });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ExpiresAtFormatted",
                HeaderText = "Expires",
                Width = 95
            });

            dgvReport.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;

                string colName = dgvReport.Columns[e.ColumnIndex].DataPropertyName;

                if (colName == "HealthScoreDisplay" && e.Value != null)
                {
                    string health = e.Value.ToString() ?? "";
                    if (health.Contains("Healthy") || health.Contains("Active"))
                    {
                        e.CellStyle.ForeColor = Theme.Green;
                    }
                    else if (health.Contains("Expiring"))
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#DC2626");
                    }
                    else
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#991B1B");
                    }
                }
                else if (colName == "TotalMoneyGeneratedFormatted")
                {
                    e.CellStyle.ForeColor = ColorTranslator.FromHtml("#059669");
                }
                else if (colName == "CloudStatusFormatted" && e.Value != null)
                {
                    string cloud = e.Value.ToString() ?? "";
                    if (cloud.Contains("Cloud"))
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#0284C7");
                        e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    }
                }
                else if (colName == "DaysRemainingFormatted" && e.Value != null)
                {
                    string text = e.Value.ToString() ?? "";
                    if (int.TryParse(text.Replace("d", "").Trim(), out int days))
                    {
                        if (days <= 7)
                        {
                            e.CellStyle.ForeColor = ColorTranslator.FromHtml("#DC2626");
                            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                        else if (days <= 30)
                        {
                            e.CellStyle.ForeColor = ColorTranslator.FromHtml("#D97706");
                            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                    }
                }
            };
        }

        private void SetupHistoryColumns()
        {
            dgvHistory.Columns.Clear();
            dgvHistory.AutoGenerateColumns = false;

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreatedAtFormatted",
                HeaderText = "Date",
                Width = 95
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CompanyDisplayName",
                HeaderText = "Company",
                Width = 190
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PlanName",
                HeaderText = "Plan",
                Width = 95,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "BillingCycle",
                HeaderText = "Cycle",
                Width = 85,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AmountFormatted",
                HeaderText = "Amount Paid (₱)",
                Width = 120,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 85,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PeriodFormatted",
                HeaderText = "Coverage Period",
                Width = 190
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Notes",
                HeaderText = "Notes / Ledger Remarks",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvHistory.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;

                if (dgvHistory.Columns[e.ColumnIndex].DataPropertyName == "AmountFormatted")
                {
                    e.CellStyle.ForeColor = ColorTranslator.FromHtml("#059669");
                }
                else if (dgvHistory.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? "";
                    if (status.Equals("Active", StringComparison.OrdinalIgnoreCase) || status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Theme.Green;
                        e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    }
                }
            };
        }

        private async Task LoadReportData()
        {
            try
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "↻ Loading...";

                var summary = await ApiConfig.Http.GetFromJsonAsync<PlatformSummaryResponse>("api/superadmin/tenants/reports/platform-summary");
                if (summary != null)
                {
                    _summaryData = summary;

                    // Update Top KPI Cards
                    lblTotalRevenue.Text = $"₱{summary.TotalCollectedRevenue:N2}";
                    lblMrr.Text = $"₱{summary.EstimatedMonthlyRevenue:N2}";
                    lblArr.Text = $"₱{summary.EstimatedAnnualRevenue:N2}";
                    lblRetention.Text = $"{summary.RetentionRate:0.0}%";
                    lblTotalUsers.Text = summary.TotalPlatformUsers.ToString("N0");
                    lblTotalBranches.Text = summary.TotalPlatformBranches.ToString("N0");

                    // Update Strip
                    lblBasicCount.Text = $"Basic: {summary.BasicCount}";
                    lblStandardCount.Text = $"Standard: {summary.StandardCount}";
                    lblEnterpriseCount.Text = $"Enterprise: {summary.EnterpriseCount}";
                    lblCloudDbCount.Text = $"☁️ Cloud DBs: {summary.CloudDatabasesCount} ({summary.CloudAdoptionRate:0.0}%)";
                    lblExpiringAlert.Text = $"⚠️ Expiring Soon: {summary.ExpiringSoonCount}";
                    lblExpiringAlert.ForeColor = summary.ExpiringSoonCount > 0 ? ColorTranslator.FromHtml("#DC2626") : Theme.Green;

                    // Update BI Charts
                    pnlRevenueTrend.SetData(summary.RevenueTrends);
                    pnlPlanDonut.SetData(summary.BasicCount, summary.StandardCount, summary.EnterpriseCount, summary.CustomCount);
                    pnlModuleAdoption.SetData(summary.ModuleAdoptions, summary.TotalSubscribers);
                    pnlHealthAlerts.SetData(summary);

                    // Update Tenant Matrix
                    _reportItems = summary.Tenants?.Select(t => new TenantReportItem
                    {
                        CompanyId = t.CompanyId,
                        CompanyCode = t.CompanyCode,
                        CompanyName = t.CompanyName,
                        ContactEmail = t.ContactEmail,
                        SubscriptionPlan = t.SubscriptionPlan,
                        IsActive = t.IsActive,
                        CreatedAt = t.CreatedAt,
                        SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                        EnabledModules = t.EnabledModules,
                        DatabaseName = t.DatabaseName,
                        ServerName = t.ServerName,
                        CloudDatabaseName = t.CloudDatabaseName,
                        CloudServerName = t.CloudServerName,
                        TotalMoneyGenerated = t.TotalMoneyGenerated,
                        UserCount = t.UserCount,
                        BranchCount = t.BranchCount,
                        HealthScore = t.HealthScore
                    }).ToList() ?? new List<TenantReportItem>();

                    // Update History Ledger
                    _historyItems = summary.SubscriptionHistories?.Select(h => new SubscriptionHistoryItem
                    {
                        Id = h.Id,
                        CompanyId = h.CompanyId,
                        CompanyCode = h.CompanyCode,
                        CompanyName = h.CompanyName,
                        PlanName = h.PlanName,
                        Amount = h.Amount,
                        BillingCycle = h.BillingCycle,
                        StartDate = h.StartDate,
                        EndDate = h.EndDate,
                        Status = h.Status,
                        Notes = h.Notes,
                        CreatedAt = h.CreatedAt
                    }).OrderByDescending(h => h.CreatedAt).ToList() ?? new List<SubscriptionHistoryItem>();

                    ApplyTenantFilters();
                    ApplyHistoryFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading business intelligence reports: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = "↻  Refresh";
            }
        }

        private void ApplyTenantFilters()
        {
            string search = txtTenantSearch.Text.Trim().ToLowerInvariant();
            string healthFilter = cboHealthFilter.SelectedItem?.ToString() ?? "All Health Statuses";
            string planFilter = cboPlanFilter.SelectedItem?.ToString() ?? "All Plans";

            _filteredReportItems = _reportItems.Where(t =>
            {
                if (!string.IsNullOrEmpty(search))
                {
                    bool matchCode = (t.CompanyCode ?? "").ToLowerInvariant().Contains(search);
                    bool matchName = (t.CompanyName ?? "").ToLowerInvariant().Contains(search);
                    bool matchDb = (t.DatabaseName ?? "").ToLowerInvariant().Contains(search) || (t.CloudDatabaseName ?? "").ToLowerInvariant().Contains(search);
                    if (!matchCode && !matchName && !matchDb) return false;
                }

                if (healthFilter != "All Statuses" && healthFilter != "All Health Statuses")
                {
                    if (healthFilter.Contains("Healthy") && !t.HealthScore.Equals("Healthy", StringComparison.OrdinalIgnoreCase)) return false;
                    if (healthFilter.Contains("Expiring") && !t.HealthScore.Equals("Expiring Soon", StringComparison.OrdinalIgnoreCase)) return false;
                    if (healthFilter.Contains("Suspended") && !t.HealthScore.Equals("Suspended", StringComparison.OrdinalIgnoreCase)) return false;
                }

                if (planFilter != "All Plans")
                {
                    if (!(t.SubscriptionPlan ?? "").Equals(planFilter, StringComparison.OrdinalIgnoreCase)) return false;
                }

                return true;
            }).ToList();

            dgvReport.DataSource = null;
            dgvReport.DataSource = _filteredReportItems;
            lblTenantMatrixCount.Text = $"Showing {_filteredReportItems.Count} of {_reportItems.Count} tenants";
        }

        private void ApplyHistoryFilters()
        {
            string search = txtHistorySearch.Text.Trim().ToLowerInvariant();

            _filteredHistoryItems = _historyItems.Where(h =>
            {
                if (!string.IsNullOrEmpty(search))
                {
                    bool matchCode = (h.CompanyCode ?? "").ToLowerInvariant().Contains(search);
                    bool matchName = (h.CompanyName ?? "").ToLowerInvariant().Contains(search);
                    bool matchPlan = (h.PlanName ?? "").ToLowerInvariant().Contains(search);
                    bool matchNotes = (h.Notes ?? "").ToLowerInvariant().Contains(search);
                    if (!matchCode && !matchName && !matchPlan && !matchNotes) return false;
                }
                return true;
            }).ToList();

            dgvHistory.DataSource = null;
            dgvHistory.DataSource = _filteredHistoryItems;
            decimal totalPaid = _filteredHistoryItems.Sum(h => h.Amount);
            lblHistoryCount.Text = $"Showing {_filteredHistoryItems.Count} of {_historyItems.Count} records | Filtered Total: ₱{totalPaid:N2}";
        }

        private void OpenSelectedTenantManager()
        {
            if (dgvReport.CurrentRow?.DataBoundItem is not TenantReportItem selected)
            {
                MessageBox.Show("Please select a tenant record to manage.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var vm = new TenantViewModel
            {
                CompanyId = selected.CompanyId,
                CompanyCode = selected.CompanyCode,
                CompanyName = selected.CompanyName,
                ContactEmail = selected.ContactEmail,
                SubscriptionPlan = selected.SubscriptionPlan,
                SubscriptionExpiresAt = selected.SubscriptionExpiresAt,
                EnabledModules = selected.EnabledModules,
                IsActive = selected.IsActive,
                CreatedAt = selected.CreatedAt,
                Databases = new List<TenantDatabaseViewModel>()
            };

            if (!string.IsNullOrEmpty(selected.DatabaseName) && selected.DatabaseName != "N/A")
            {
                vm.Databases.Add(new TenantDatabaseViewModel
                {
                    CompanyDatabaseId = 1,
                    DatabaseType = "Local",
                    DatabaseName = selected.DatabaseName,
                    ServerName = selected.ServerName ?? "localhost",
                    IsActive = true
                });
            }

            if (!string.IsNullOrEmpty(selected.CloudDatabaseName))
            {
                vm.Databases.Add(new TenantDatabaseViewModel
                {
                    CompanyDatabaseId = 2,
                    DatabaseType = "Cloud",
                    DatabaseName = selected.CloudDatabaseName,
                    ServerName = selected.CloudServerName ?? "remote",
                    IsActive = true
                });
            }

            using var dlg = new ManageCompanyDialog(vm, initialTab: 0);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _ = LoadReportData();
            }
        }



        private async void BtnExportPdf_Click(object? sender, EventArgs e)
        {
            if (_reportItems == null || _reportItems.Count == 0)
            {
                MessageBox.Show("No report data available to export.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"SCRAP_Platform_BI_Report_{DateTime.Now:yyyyMMdd}.pdf",
                Title = "Save Platform Business Intelligence & Subscription PDF Report"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            btnExportPdf.Enabled = false;
            btnExportPdf.Text = "⏳  Exporting...";

            try
            {
                var res = await ApiConfig.Http.GetAsync("api/superadmin/tenants/reports/platform-summary/pdf");
                if (res.IsSuccessStatusCode)
                {
                    var bytes = await res.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(sfd.FileName, bytes);

                    var openNow = MessageBox.Show(
                        "Executive BI PDF report generated and saved successfully!\n\nWould you like to open it now?",
                        "Export Complete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (openNow == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(sfd.FileName)
                        {
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    MessageBox.Show("Failed to generate PDF: " + res.StatusCode, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to export PDF: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExportPdf.Enabled = true;
                btnExportPdf.Text = "📄  Export PDF";
            }
        }
    }

    #region Custom GDI+ BI Chart Controls

    public class RevenueTrendChartPanel : Panel
    {
        private List<MonthlyTrendPointDto> _trends = new();

        public RevenueTrendChartPanel()
        {
            DoubleBuffered = true;
            BackColor = Theme.White;
            Padding = new Padding(16);
        }

        public void SetData(List<MonthlyTrendPointDto>? trends)
        {
            _trends = trends ?? new();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Card border
            using (var borderPen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header Title
            using (var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Theme.DarkText))
            {
                g.DrawString("Monthly Revenue Velocity (₱)", titleFont, titleBrush, 16, 12);
            }

            using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var subBrush = new SolidBrush(Theme.MutedText))
            {
                g.DrawString("Trailing 6-month subscription revenue trends in Philippine Peso", subFont, subBrush, 16, 32);
            }

            if (_trends.Count == 0)
            {
                using var msgFont = new Font("Segoe UI", 9.5f, FontStyle.Italic);
                using var msgBrush = new SolidBrush(Theme.MutedText);
                g.DrawString("No revenue trend data available.", msgFont, msgBrush, 16, 70);
                return;
            }

            int chartLeft = 55;
            int chartRight = Width - 24;
            int chartTop = 64;
            int chartBottom = Height - 40;
            int chartWidth = chartRight - chartLeft;
            int chartHeight = chartBottom - chartTop;

            if (chartWidth <= 0 || chartHeight <= 0) return;

            decimal maxRev = _trends.Max(t => t.Revenue);
            if (maxRev <= 0) maxRev = 1000m;
            decimal ceilingRev = Math.Max(1000m, Math.Ceiling(maxRev / 500m) * 500m);

            // Draw horizontal grid lines
            using (var gridPen = new Pen(ColorTranslator.FromHtml("#E2E8F0"), 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            using (var axisFont = new Font("Segoe UI", 8f))
            using (var axisBrush = new SolidBrush(Theme.MutedText))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = chartBottom - (int)(chartHeight * (i / 4.0));
                    g.DrawLine(gridPen, chartLeft, y, chartRight, y);
                    decimal val = ceilingRev * (i / 4.0m);
                    string valText = val >= 1000 ? $"₱{val / 1000:0.#}k" : $"₱{val:0}";
                    g.DrawString(valText, axisFont, axisBrush, 4, y - 7);
                }
            }

            // Draw Bars
            int count = _trends.Count;
            int slotWidth = chartWidth / count;
            int barWidth = Math.Min(48, Math.Max(18, slotWidth - 18));

            for (int i = 0; i < count; i++)
            {
                var item = _trends[i];
                int centerX = chartLeft + i * slotWidth + slotWidth / 2;
                int barX = centerX - barWidth / 2;

                int barH = (int)(chartHeight * (double)(item.Revenue / ceilingRev));
                barH = Math.Max(4, Math.Min(chartHeight, barH));
                int barY = chartBottom - barH;

                var barRect = new Rectangle(barX, barY, barWidth, barH);

                using (var gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    barRect,
                    ColorTranslator.FromHtml("#10B981"),
                    ColorTranslator.FromHtml("#047857"),
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillRectangle(gradBrush, barRect);
                }

                using (var barBorderPen = new Pen(ColorTranslator.FromHtml("#059669"), 1))
                {
                    g.DrawRectangle(barBorderPen, barRect);
                }

                // Bar value on top
                using (var valFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (var valBrush = new SolidBrush(ColorTranslator.FromHtml("#065F46")))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                {
                    string text = $"₱{item.Revenue:N0}";
                    g.DrawString(text, valFont, valBrush, centerX, Math.Max(chartTop, barY - 15), sf);
                }

                // Month label at bottom
                using (var lblFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var lblBrush = new SolidBrush(Theme.DarkText))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                {
                    g.DrawString(item.MonthLabel, lblFont, lblBrush, centerX, chartBottom + 5, sf);
                }
            }
        }
    }

    public class PlanDonutChartPanel : Panel
    {
        private int _basic;
        private int _standard;
        private int _enterprise;
        private int _custom;

        public PlanDonutChartPanel()
        {
            DoubleBuffered = true;
            BackColor = Theme.White;
            Padding = new Padding(16);
        }

        public void SetData(int basic, int standard, int enterprise, int custom)
        {
            _basic = basic;
            _standard = standard;
            _enterprise = enterprise;
            _custom = custom;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Card border
            using (var borderPen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header Title
            using (var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Theme.DarkText))
            {
                g.DrawString("Subscription Plan Tier Distribution", titleFont, titleBrush, 16, 12);
            }

            using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var subBrush = new SolidBrush(Theme.MutedText))
            {
                g.DrawString("Active market share by subscription tier", subFont, subBrush, 16, 32);
            }

            int total = _basic + _standard + _enterprise + _custom;

            int donutDiameter = Math.Min(160, Math.Min(Width / 2 - 10, Height - 80));
            donutDiameter = Math.Max(90, donutDiameter);
            int donutX = 20;
            int donutY = 56 + (Height - 70 - donutDiameter) / 2;
            var donutRect = new Rectangle(donutX, donutY, donutDiameter, donutDiameter);

            if (total == 0)
            {
                using var emptyBrush = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0"));
                g.FillEllipse(emptyBrush, donutRect);

                int holeD = (int)(donutDiameter * 0.6);
                var holeRect = new Rectangle(donutX + (donutDiameter - holeD) / 2, donutY + (donutDiameter - holeD) / 2, holeD, holeD);
                using var bgBrush = new SolidBrush(Theme.White);
                g.FillEllipse(bgBrush, holeRect);

                using var msgFont = new Font("Segoe UI", 8.5f, FontStyle.Italic);
                using var msgBrush = new SolidBrush(Theme.MutedText);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("No Tenants", msgFont, msgBrush, holeRect, sf);
            }
            else
            {
                var slices = new (string Name, int Count, Color Color)[]
                {
                    ("Basic (₱99)", _basic, ColorTranslator.FromHtml("#3B82F6")),
                    ("Standard (₱249)", _standard, ColorTranslator.FromHtml("#10B981")),
                    ("Enterprise (₱599)", _enterprise, ColorTranslator.FromHtml("#7C3AED")),
                    ("Custom Tier", _custom, ColorTranslator.FromHtml("#F59E0B"))
                };

                float startAngle = -90f;
                foreach (var slice in slices)
                {
                    if (slice.Count == 0) continue;
                    float sweepAngle = (float)slice.Count / total * 360f;
                    using (var brush = new SolidBrush(slice.Color))
                    {
                        g.FillPie(brush, donutRect, startAngle, sweepAngle);
                    }
                    startAngle += sweepAngle;
                }

                // Donut hole
                int holeD = (int)(donutDiameter * 0.62);
                var holeRect = new Rectangle(donutX + (donutDiameter - holeD) / 2, donutY + (donutDiameter - holeD) / 2, holeD, holeD);
                using (var bgBrush = new SolidBrush(Theme.White))
                {
                    g.FillEllipse(bgBrush, holeRect);
                }

                // Donut center text
                using (var numFont = new Font("Segoe UI", 15f, FontStyle.Bold))
                using (var numBrush = new SolidBrush(Theme.DarkText))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(total.ToString(), numFont, numBrush, new Rectangle(holeRect.X, holeRect.Y - 6, holeRect.Width, holeRect.Height / 2 + 6), sf);
                }

                using (var subLblFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                using (var subLblBrush = new SolidBrush(Theme.MutedText))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("SUBSCRIBERS", subLblFont, subLblBrush, new Rectangle(holeRect.X, holeRect.Y + holeRect.Height / 2 - 4, holeRect.Width, holeRect.Height / 2), sf);
                }
            }

            // Legend on Right side
            int legendX = donutX + donutDiameter + 20;
            int legendY = 60;
            int itemH = 26;

            var legendItems = new (string Name, int Count, Color Color)[]
            {
                ("Basic (₱99)", _basic, ColorTranslator.FromHtml("#3B82F6")),
                ("Standard (₱249)", _standard, ColorTranslator.FromHtml("#10B981")),
                ("Enterprise (₱599)", _enterprise, ColorTranslator.FromHtml("#7C3AED")),
                ("Custom Tier", _custom, ColorTranslator.FromHtml("#F59E0B"))
            };

            for (int i = 0; i < legendItems.Length; i++)
            {
                var item = legendItems[i];
                int y = legendY + i * itemH;

                using (var b = new SolidBrush(item.Color))
                {
                    g.FillEllipse(b, legendX, y + 4, 10, 10);
                }

                double pct = total > 0 ? (item.Count / (double)total) * 100.0 : 0.0;
                string text = $"{item.Name}";
                string countText = $"{item.Count} ({pct:0.0}%)";

                using (var nameFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var nameBrush = new SolidBrush(Theme.DarkText))
                {
                    g.DrawString(text, nameFont, nameBrush, legendX + 16, y + 1);
                }

                using (var pctFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var pctBrush = new SolidBrush(item.Color))
                {
                    g.DrawString(countText, pctFont, pctBrush, legendX + 135, y + 1);
                }
            }
        }
    }

    public class ModuleAdoptionPanel : Panel
    {
        private List<ModuleAdoptionDto> _adoptions = new();
        private int _totalSubscribers = 0;

        public ModuleAdoptionPanel()
        {
            DoubleBuffered = true;
            BackColor = Theme.White;
            Padding = new Padding(16);
        }

        public void SetData(List<ModuleAdoptionDto>? adoptions, int totalSubscribers)
        {
            _adoptions = adoptions ?? new();
            _totalSubscribers = totalSubscribers;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Card border
            using (var borderPen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header Title
            using (var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Theme.DarkText))
            {
                g.DrawString("Module Feature Adoption Across Tenants", titleFont, titleBrush, 16, 12);
            }

            using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var subBrush = new SolidBrush(Theme.MutedText))
            {
                g.DrawString("Active tenant enablement across all 8 core platform modules", subFont, subBrush, 16, 32);
            }

            if (_adoptions.Count == 0)
            {
                using var msgFont = new Font("Segoe UI", 9f, FontStyle.Italic);
                using var msgBrush = new SolidBrush(Theme.MutedText);
                g.DrawString("No module adoption metrics available.", msgFont, msgBrush, 16, 60);
                return;
            }

            int startY = 58;
            int rowH = 24;
            int maxRows = Math.Min(_adoptions.Count, Math.Max(1, (Height - startY - 8) / rowH));
            int barStartX = 125;
            int barWidth = Math.Max(50, Width - barStartX - 110);

            for (int i = 0; i < maxRows; i++)
            {
                var mod = _adoptions[i];
                int y = startY + i * rowH;

                using (var nameFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var nameBrush = new SolidBrush(Theme.DarkText))
                {
                    g.DrawString(mod.ModuleName, nameFont, nameBrush, 16, y + 2);
                }

                var trackRect = new Rectangle(barStartX, y + 5, barWidth, 10);
                using (var trackBrush = new SolidBrush(ColorTranslator.FromHtml("#F1F5F9")))
                {
                    g.FillRectangle(trackBrush, trackRect);
                }

                int fillW = (int)(barWidth * (double)(mod.AdoptionPercentage / 100m));
                fillW = Math.Max(0, Math.Min(barWidth, fillW));
                if (fillW > 0)
                {
                    var fillRect = new Rectangle(barStartX, y + 5, fillW, 10);
                    Color barColor = mod.AdoptionPercentage >= 75m ? ColorTranslator.FromHtml("#10B981")
                        : (mod.AdoptionPercentage >= 40m ? ColorTranslator.FromHtml("#3B82F6") : ColorTranslator.FromHtml("#F59E0B"));

                    using (var fillBrush = new SolidBrush(barColor))
                    {
                        g.FillRectangle(fillBrush, fillRect);
                    }
                }

                using (var statFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (var statBrush = new SolidBrush(Theme.DarkText))
                {
                    string statText = $"{mod.AdoptionPercentage:0.0}% ({mod.TenantCount}/{_totalSubscribers})";
                    g.DrawString(statText, statFont, statBrush, barStartX + barWidth + 8, y + 2);
                }
            }
        }
    }

    public class PlatformHealthAlertsPanel : Panel
    {
        private PlatformSummaryResponse? _summary;
        public event Action? OnViewExpiringClicked;

        public PlatformHealthAlertsPanel()
        {
            DoubleBuffered = true;
            BackColor = Theme.White;
            Padding = new Padding(16);
            Cursor = Cursors.Hand;
            MouseClick += (s, e) => OnViewExpiringClicked?.Invoke();
        }

        public void SetData(PlatformSummaryResponse? summary)
        {
            _summary = summary;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Card border
            using (var borderPen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header Title
            using (var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Theme.DarkText))
            {
                g.DrawString("Platform Health & Infrastructure Telemetry", titleFont, titleBrush, 16, 12);
            }

            using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var subBrush = new SolidBrush(Theme.MutedText))
            {
                g.DrawString("Predictive renewal risk assessment and multi-tenant hosting state", subFont, subBrush, 16, 32);
            }

            if (_summary == null) return;

            int boxY = 56;
            int boxW = Width - 32;

            // 1. Cloud Infrastructure Box
            var cloudRect = new Rectangle(16, boxY, boxW, 52);
            using (var cloudBg = new SolidBrush(ColorTranslator.FromHtml("#F0F9FF")))
            using (var cloudBorder = new Pen(ColorTranslator.FromHtml("#BAE6FD"), 1))
            {
                g.FillRectangle(cloudBg, cloudRect);
                g.DrawRectangle(cloudBorder, cloudRect);
            }

            using (var boldFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var textFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var primaryBrush = new SolidBrush(ColorTranslator.FromHtml("#0369A1")))
            using (var textBrush = new SolidBrush(ColorTranslator.FromHtml("#0C4A6E")))
            {
                g.DrawString("☁️ Cloud Database Infrastructure", boldFont, primaryBrush, 26, boxY + 7);
                g.DrawString($"{_summary.CloudDatabasesCount} of {_summary.TotalSubscribers} tenants running hosted Cloud Databases ({_summary.CloudAdoptionRate:0.0}% adoption)", textFont, textBrush, 26, boxY + 27);
            }

            // 2. Renewal Health / Alerts Box
            boxY += 60;
            int alertH = Math.Max(70, Height - boxY - 14);
            var alertRect = new Rectangle(16, boxY, boxW, alertH);

            if (_summary.ExpiringSoonCount > 0)
            {
                using (var alertBg = new SolidBrush(ColorTranslator.FromHtml("#FEF2F2")))
                using (var alertBorder = new Pen(ColorTranslator.FromHtml("#FECACA"), 1))
                {
                    g.FillRectangle(alertBg, alertRect);
                    g.DrawRectangle(alertBorder, alertRect);
                }

                using (var warnFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (var warnBrush = new SolidBrush(ColorTranslator.FromHtml("#DC2626")))
                {
                    g.DrawString($"⚠️ Renewal Warning: {_summary.ExpiringSoonCount} Subscription(s) Expiring Soon", warnFont, warnBrush, 26, boxY + 8);
                }

                var expiringNames = _summary.Tenants?
                    .Where(t => t.IsActive && t.DaysRemaining.HasValue && t.DaysRemaining.Value <= 30)
                    .Select(t => $"{t.CompanyName} ({t.DaysRemaining}d left)")
                    .Take(2)
                    .ToList() ?? new List<string>();

                string detailText = string.Join(", ", expiringNames);
                if (_summary.ExpiringSoonCount > 2) detailText += $" and {_summary.ExpiringSoonCount - 2} more...";

                using (var detFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var detBrush = new SolidBrush(ColorTranslator.FromHtml("#991B1B")))
                {
                    g.DrawString(detailText, detFont, detBrush, 26, boxY + 30);
                }

                using (var hintFont = new Font("Segoe UI", 8f, FontStyle.Italic))
                using (var hintBrush = new SolidBrush(ColorTranslator.FromHtml("#B91C1C")))
                {
                    g.DrawString("Review and extend contracts in the 'Tenant Health & Intelligence Matrix' tab.", hintFont, hintBrush, 26, boxY + 50);
                }
            }
            else
            {
                using (var safeBg = new SolidBrush(ColorTranslator.FromHtml("#F0FDF4")))
                using (var safeBorder = new Pen(ColorTranslator.FromHtml("#BBF7D0"), 1))
                {
                    g.FillRectangle(safeBg, alertRect);
                    g.DrawRectangle(safeBorder, alertRect);
                }

                using (var safeFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (var safeBrush = new SolidBrush(ColorTranslator.FromHtml("#16A34A")))
                {
                    g.DrawString("✅ Subscriptions Healthy & Stable", safeFont, safeBrush, 26, boxY + 8);
                }

                using (var detFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var detBrush = new SolidBrush(ColorTranslator.FromHtml("#166534")))
                {
                    g.DrawString($"All {_summary.ActiveSubscribers} active tenant organizations have > 30 days remaining on their subscription terms.", detFont, detBrush, 26, boxY + 30);
                }

                using (var hintFont = new Font("Segoe UI", 8f, FontStyle.Italic))
                using (var hintBrush = new SolidBrush(ColorTranslator.FromHtml("#15803D")))
                {
                    g.DrawString($"Subscriber retention rate currently sustained at {_summary.RetentionRate:0.0}%.", hintFont, hintBrush, 26, boxY + 50);
                }
            }
        }
    }

    #endregion

    #region Data Transfer Objects & View Models

    public class PlatformSummaryResponse
    {
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public int SuspendedSubscribers { get; set; }
        public int BasicCount { get; set; }
        public int StandardCount { get; set; }
        public int EnterpriseCount { get; set; }
        public int CustomCount { get; set; }
        public decimal EstimatedMonthlyRevenue { get; set; }
        public decimal TotalCollectedRevenue { get; set; }
        public decimal EstimatedAnnualRevenue { get; set; }
        public decimal RetentionRate { get; set; }
        public int TotalPlatformUsers { get; set; }
        public int TotalPlatformBranches { get; set; }
        public int CloudDatabasesCount { get; set; }
        public decimal CloudAdoptionRate { get; set; }
        public int ExpiringSoonCount { get; set; }

        public List<ModuleAdoptionDto>? ModuleAdoptions { get; set; }
        public List<MonthlyTrendPointDto>? RevenueTrends { get; set; }
        public List<TenantReportItemDto>? Tenants { get; set; }
        public List<SubscriptionHistoryReportDto>? SubscriptionHistories { get; set; }
    }

    public class ModuleAdoptionDto
    {
        public string ModuleName { get; set; } = string.Empty;
        public int TenantCount { get; set; }
        public decimal AdoptionPercentage { get; set; }
    }

    public class MonthlyTrendPointDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class TenantReportItemDto
    {
        public int CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactEmail { get; set; }
        public string? SubscriptionPlan { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public string? EnabledModules { get; set; }
        public string? DatabaseName { get; set; }
        public string? ServerName { get; set; }
        public string? CloudDatabaseName { get; set; }
        public string? CloudServerName { get; set; }
        public decimal TotalMoneyGenerated { get; set; }
        public int UserCount { get; set; }
        public int BranchCount { get; set; }
        public string HealthScore { get; set; } = "Healthy";
        public int? DaysRemaining { get; set; }
    }

    public class SubscriptionHistoryReportDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? PlanName { get; set; }
        public decimal Amount { get; set; }
        public string? BillingCycle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TenantReportItem
    {
        public int CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactEmail { get; set; }
        public string? SubscriptionPlan { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public string? EnabledModules { get; set; }
        public string? DatabaseName { get; set; }
        public string? ServerName { get; set; }
        public string? CloudDatabaseName { get; set; }
        public string? CloudServerName { get; set; }
        public decimal TotalMoneyGenerated { get; set; }
        public int UserCount { get; set; }
        public int BranchCount { get; set; }
        public string HealthScore { get; set; } = "Healthy";
        public string HealthScoreDisplay => !IsActive ? "Suspended" : (HealthScore == "Expiring Soon" ? "Expiring Soon" : "Active (Healthy)");

        public string StatusText => IsActive ? "Active" : "Suspended";
        public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd");
        public string ExpiresAtFormatted => SubscriptionExpiresAt.HasValue ? SubscriptionExpiresAt.Value.ToString("yyyy-MM-dd") : "N/A";
        public string TotalMoneyGeneratedFormatted => $"₱{TotalMoneyGenerated:N2}";
        public string UserCountFormatted => $"{UserCount} user{(UserCount == 1 ? "" : "s")}";
        public string BranchCountFormatted => $"{BranchCount} branch{(BranchCount == 1 ? "" : "es")}";
        public string CloudStatusFormatted => !string.IsNullOrEmpty(CloudDatabaseName) ? "Active ☁️" : "—";

        public string DaysRemainingFormatted
        {
            get
            {
                if (!SubscriptionExpiresAt.HasValue) return "—";
                var days = (int)Math.Max(0, (SubscriptionExpiresAt.Value.Date - DateTime.UtcNow.Date).TotalDays);
                return $"{days}d";
            }
        }

        public string MonthlyRateFormatted
        {
            get
            {
                return (SubscriptionPlan?.ToLowerInvariant()) switch
                {
                    "basic" => "₱99.00",
                    "standard" => "₱249.00",
                    "enterprise" => "₱599.00",
                    _ => "₱199.00"
                };
            }
        }
    }

    public class SubscriptionHistoryItem
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? PlanName { get; set; }
        public decimal Amount { get; set; }
        public string? BillingCycle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public string CompanyDisplayName => !string.IsNullOrEmpty(CompanyName) ? $"{CompanyName} ({CompanyCode})" : (CompanyCode ?? $"ID #{CompanyId}");
        public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd");
        public string AmountFormatted => $"₱{Amount:N2}";
        public string PeriodFormatted => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
    }

    #endregion
}
