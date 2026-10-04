using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public sealed class CompanyFinanceMonitorForm : Form
    {
        // ── Controls ──────────────────────────────────────────────────────────
        private Label _lblTitle = null!;
        private Label _lblSubtitle = null!;
        private ComboBox _cmbBranch = null!;
        private ComboBox _cmbPreset = null!;
        private DateTimePicker _dtpStart = null!;
        private DateTimePicker _dtpEnd = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportPdf = null!;

        // 4 KPI Stat Cards
        private Panel _cardIncome = null!;
        private Panel _cardDeductions = null!;
        private Panel _cardNet = null!;
        private Panel _cardTaxes = null!;

        private Label _valIncome = null!;
        private Label _valDeductions = null!;
        private Label _valNet = null!;
        private Label _valTaxes = null!;

        // Tab navigation
        private TabControl _tabs = null!;

        // Tab 1: Financial Dashboard
        private LineChartControl _chartBranchRevenueLine = null!;
        private BarChartControl _chartBranches = null!;
        private Button _btnToggleChart = null!;
        private bool _showLineChart = true;
        private DonutChartControl _chartCategories = null!;
        private DataGridView _gridBranchSummary = null!;
        private Panel _cardBranchSummary = null!;

        // Tab 2: Transaction Ledger
        private ComboBox _cmbTypeFilter = null!;
        private TextBox _txtSearch = null!;
        private DataGridView _gridTransactions = null!;
        private Panel _cardLedger = null!;

        // Data state
        private bool _isBranchesLoaded;
        private bool _isApplyingPreset;
        private readonly List<BranchOptionDto> _branches = new();
        private List<TransactionViewItem> _allTransactions = new();
        private List<BranchSummaryRow> _branchSummaries = new();

        // Stable DataSource bindings — set once, never replaced to avoid column regeneration
        private readonly BindingList<BranchSummaryRow> _branchSummarySource = new();
        private readonly BindingList<TransactionViewItem> _ledgerSource = new();


        public CompanyFinanceMonitorForm()
        {
            Text = "Company Financial Report";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = InitData();
        }

        private async Task InitData()
        {
            await LoadBranches();
            await LoadDashboard();
        }

        private void InitializeComponent()
        {
            // ── Title & Subtitle ──────────────────────────────────────────────
            _lblTitle = new Label
            {
                Text = "Company Financial Report",
                Left = 32,
                Top = 24,
                Width = 450,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            _lblSubtitle = new Label
            {
                Text = "Consolidated executive financial dashboard & multi-branch ledger",
                Left = 32,
                Top = 60,
                Width = 650,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            // ── Top Action Buttons & Filters ──────────────────────────────────
            _btnExportPdf = new Button
            {
                Text = "📥  Export PDF",
                Width = 130,
                Height = 34
            };
            Theme.StylePrimaryButton(_btnExportPdf);
            _btnExportPdf.Click += async (s, e) => await ExportReportPdf();

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 100,
                Height = 34
            };
            Theme.StyleOutlineButton(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadDashboard();

            _cmbBranch = new ComboBox
            {
                Width = 190,
                Height = 34,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(_cmbBranch);
            _cmbBranch.SelectedIndexChanged += async (s, e) =>
            {
                if (_isBranchesLoaded)
                    await LoadDashboard();
            };

            _cmbPreset = new ComboBox
            {
                Width = 130,
                Height = 34,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(_cmbPreset);
            _cmbPreset.Items.AddRange(new object[] { "This Month", "Previous Month", "Last 30 Days", "This Year", "All Records" });
            _cmbPreset.SelectedIndex = 0;
            _cmbPreset.SelectedIndexChanged += async (s, e) =>
            {
                ApplyPreset(_cmbPreset.SelectedIndex);
                if (_isBranchesLoaded)
                    await LoadDashboard();
            };

            _dtpStart = new DateTimePicker
            {
                Width = 110,
                Height = 30,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            _dtpStart.ValueChanged += async (s, e) =>
            {
                if (_isBranchesLoaded && !_isApplyingPreset)
                    await LoadDashboard();
            };

            _dtpEnd = new DateTimePicker
            {
                Width = 110,
                Height = 30,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
            };
            _dtpEnd.ValueChanged += async (s, e) =>
            {
                if (_isBranchesLoaded && !_isApplyingPreset)
                    await LoadDashboard();
            };

            // ── 4 KPI Stat Cards ──────────────────────────────────────────────
            _cardIncome = MakeStatCard("TOTAL REVENUE / INCOME", "₱0.00", "Commodity sales & operations", Theme.Green);
            _valIncome = (Label)_cardIncome.Tag!;

            _cardDeductions = MakeStatCard("TOTAL EXPENDITURE", "₱0.00", "Procurement, payroll, & ops", ColorTranslator.FromHtml("#EF4444"));
            _valDeductions = (Label)_cardDeductions.Tag!;

            _cardNet = MakeStatCard("NET OPERATING BALANCE", "₱0.00", "Consolidated net cash flow", Theme.DarkText);
            _valNet = (Label)_cardNet.Tag!;

            _cardTaxes = MakeStatCard("PAYROLL TAXES", "₱0.00", "Government remittances & taxes", ColorTranslator.FromHtml("#8B5CF6"));
            _valTaxes = (Label)_cardTaxes.Tag!;

            // ── Tabs ──────────────────────────────────────────────────────────
            _tabs = new TabControl
            {
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _tabs.TabPages.Add(BuildDashboardTab());
            _tabs.TabPages.Add(BuildLedgerTab());

            // Add main controls
            Controls.AddRange(new Control[]
            {
                _lblTitle, _lblSubtitle,
                _btnExportPdf, _btnRefresh, _cmbBranch, _cmbPreset, _dtpStart, _dtpEnd,
                _cardIncome, _cardDeductions, _cardNet, _cardTaxes,
                _tabs
            });

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private TabPage BuildDashboardTab()
        {
            var tab = new TabPage("Financial Dashboard")
            {
                BackColor = Theme.Background,
                Padding = new Padding(12)
            };

            _chartBranchRevenueLine = new LineChartControl
            {
                Title = "Branch Revenue Comparison by Month",
                Subtitle = "Multi-branch revenue performance comparison over time (₱)",
                XAxisTitle = "Timeline (Month)",
                YAxisTitle = "Revenue (₱)",
                ValuePrefix = "₱",
                LegendInTopLeftBox = true
            };

            _chartBranches = new BarChartControl
            {
                Title = "Revenue by Branch (Bar Summary)",
                Subtitle = "Gross income generated across active branches",
                ValuePrefix = "₱",
                Visible = false
            };

            _btnToggleChart = new Button
            {
                Text = "📊 Bar Summary",
                Width = 140,
                Height = 28,
                Cursor = Cursors.Hand
            };
            Theme.StyleOutlineButton(_btnToggleChart);
            _btnToggleChart.Click += (s, e) =>
            {
                _showLineChart = !_showLineChart;
                _chartBranchRevenueLine.Visible = _showLineChart;
                _chartBranches.Visible = !_showLineChart;
                _btnToggleChart.Text = _showLineChart ? "📊 Bar Summary" : "📈 Line Trend";
            };

            _chartCategories = new DonutChartControl
            {
                Title = "Expenditure Breakdown",
                Subtitle = "Distribution of deductions and expenses by category",
                CenterLabel = "Expenses"
            };

            _cardBranchSummary = MakeCard();
            var lblTableTitle = new Label
            {
                Text = "Branch Financial Performance Summary",
                Left = 16,
                Top = 12,
                Width = 400,
                Height = 24,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            _gridBranchSummary = new DataGridView
            {
                Left = 1,
                Top = 44,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _gridBranchSummary.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(_gridBranchSummary);
            SetupBranchSummaryColumns();
            _gridBranchSummary.DataSource = _branchSummarySource;


            _gridBranchSummary.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _gridBranchSummary.Rows.Count) return;
                var row = _gridBranchSummary.Rows[e.RowIndex];
                if (row.DataBoundItem is BranchSummaryRow item)
                {
                    row.Cells["NetBalanceFormatted"].Style.ForeColor = item.NetBalance >= 0 ? Theme.Green : ColorTranslator.FromHtml("#EF4444");
                    row.Cells["StatusFormatted"].Style.ForeColor = item.NetBalance >= 0 ? Theme.Green : ColorTranslator.FromHtml("#EF4444");
                }
            };

            _cardBranchSummary.Controls.Add(lblTableTitle);
            _cardBranchSummary.Controls.Add(_gridBranchSummary);

            tab.Controls.AddRange(new Control[] { _chartBranchRevenueLine, _chartBranches, _btnToggleChart, _chartCategories, _cardBranchSummary });

            tab.Resize += (s, e) => LayoutDashboardTab(tab);
            return tab;
        }

        private void LayoutDashboardTab(TabPage tab)
        {
            int pad = 12;
            int gap = 16;
            int availW = Math.Max(300, tab.ClientSize.Width - (pad * 2));
            int availH = Math.Max(300, tab.ClientSize.Height - (pad * 2));

            int chartH = Math.Max(180, (int)(availH * 0.46));
            int chartW = (availW - gap) / 2;

            _chartBranchRevenueLine.SetBounds(pad, pad, chartW, chartH);
            _chartBranches.SetBounds(pad, pad, chartW, chartH);
            _btnToggleChart.SetBounds(pad + chartW - _btnToggleChart.Width - 14, pad + 14, _btnToggleChart.Width, _btnToggleChart.Height);
            _btnToggleChart.BringToFront();

            _chartCategories.SetBounds(pad + chartW + gap, pad, chartW, chartH);

            int tableY = pad + chartH + gap;
            int tableH = Math.Max(120, availH - chartH - gap);
            _cardBranchSummary.SetBounds(pad, tableY, availW, tableH);
            _gridBranchSummary.SetBounds(1, 44, _cardBranchSummary.Width - 2, _cardBranchSummary.Height - 46);

            if (_gridBranchSummary.Columns.Count > 0)
                Theme.FillColumnsToWidth(_gridBranchSummary);
        }

        private TabPage BuildLedgerTab()
        {
            var tab = new TabPage("Transaction Ledger")
            {
                BackColor = Theme.Background,
                Padding = new Padding(12)
            };

            var filterPanel = new Panel
            {
                Left = 12,
                Top = 12,
                Height = 40,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblShow = new Label
            {
                Text = "Classification:",
                Left = 0,
                Top = 8,
                AutoSize = true,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.DarkText
            };

            _cmbTypeFilter = new ComboBox
            {
                Left = 90,
                Top = 4,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(_cmbTypeFilter);
            _cmbTypeFilter.Items.AddRange(new object[]
            {
                "All Transactions",
                "Operating Revenue / Income",
                "Expenses / Deductions",
                "Investments & Grants"
            });
            _cmbTypeFilter.SelectedIndex = 0;
            _cmbTypeFilter.SelectedIndexChanged += (s, e) => FilterLedgerGrid();

            var lblSearch = new Label
            {
                Text = "Search:",
                Left = 310,
                Top = 8,
                AutoSize = true,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.DarkText
            };

            _txtSearch = new TextBox
            {
                Left = 365,
                Top = 4,
                Width = 260,
                PlaceholderText = "Search branch, category, notes..."
            };
            Theme.StyleTextBox(_txtSearch);
            _txtSearch.TextChanged += (s, e) => FilterLedgerGrid();

            filterPanel.Controls.AddRange(new Control[] { lblShow, _cmbTypeFilter, lblSearch, _txtSearch });

            _cardLedger = MakeCard();
            _cardLedger.Left = 12;
            _cardLedger.Top = 58;
            _cardLedger.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _gridTransactions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _gridTransactions.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(_gridTransactions);
            SetupLedgerColumns();
            _gridTransactions.DataSource = _ledgerSource;


            _gridTransactions.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _gridTransactions.Rows.Count) return;
                var row = _gridTransactions.Rows[e.RowIndex];
                if (row.DataBoundItem is TransactionViewItem item)
                {
                    if (item.IsDeduction)
                    {
                        row.Cells["AmountFormatted"].Style.ForeColor = ColorTranslator.FromHtml("#EF4444");
                        row.Cells["Classification"].Style.ForeColor = ColorTranslator.FromHtml("#EF4444");
                    }
                    else if (item.IsInvestmentOrDonation)
                    {
                        row.Cells["AmountFormatted"].Style.ForeColor = ColorTranslator.FromHtml("#0069E5");
                        row.Cells["Classification"].Style.ForeColor = ColorTranslator.FromHtml("#0069E5");
                    }
                    else
                    {
                        row.Cells["AmountFormatted"].Style.ForeColor = Theme.Green;
                        row.Cells["Classification"].Style.ForeColor = Theme.Green;
                    }
                }
            };

            _cardLedger.Controls.Add(_gridTransactions);

            tab.Controls.AddRange(new Control[] { filterPanel, _cardLedger });

            tab.Resize += (s, e) =>
            {
                filterPanel.Width = tab.ClientSize.Width - 24;
                _cardLedger.SetBounds(12, 58, tab.ClientSize.Width - 24, Math.Max(100, tab.ClientSize.Height - 70));
                if (_gridTransactions.Columns.Count > 0)
                    Theme.FillColumnsToWidth(_gridTransactions);
            };

            return tab;
        }

        private void LayoutControls()
        {
            // Position top-right action controls
            _btnExportPdf.Left = Math.Max(0, ClientSize.Width - _btnExportPdf.Width - 32);
            _btnExportPdf.Top = 26;

            _btnRefresh.Left = Math.Max(0, _btnExportPdf.Left - _btnRefresh.Width - 10);
            _btnRefresh.Top = 26;

            _cmbBranch.Left = Math.Max(0, _btnRefresh.Left - _cmbBranch.Width - 10);
            _cmbBranch.Top = 26;

            _cmbPreset.Left = Math.Max(0, _cmbBranch.Left - _cmbPreset.Width - 10);
            _cmbPreset.Top = 26;

            _dtpEnd.Left = Math.Max(0, _cmbPreset.Left - _dtpEnd.Width - 8);
            _dtpEnd.Top = 28;

            _dtpStart.Left = Math.Max(0, _dtpEnd.Left - _dtpStart.Width - 6);
            _dtpStart.Top = 28;

            _lblSubtitle.Width = Math.Max(250, _dtpStart.Left - 44);

            // 4 KPI Cards
            int pad = 32;
            int gap = 16;
            int y = 92;
            int availW = Math.Max(400, ClientSize.Width - (pad * 2));
            int cardW = Math.Max(180, (availW - (gap * 3)) / 4);
            int cardH = 98;

            _cardIncome.SetBounds(pad, y, cardW, cardH);
            _cardDeductions.SetBounds(pad + cardW + gap, y, cardW, cardH);
            _cardNet.SetBounds(pad + (cardW + gap) * 2, y, cardW, cardH);
            _cardTaxes.SetBounds(pad + (cardW + gap) * 3, y, cardW, cardH);

            // TabControl below KPI cards
            int tabY = y + cardH + 16;
            int tabH = Math.Max(250, ClientSize.Height - tabY - 24);
            _tabs.SetBounds(pad, tabY, availW, tabH);

            if (_tabs.SelectedTab != null)
            {
                if (_tabs.SelectedIndex == 0) LayoutDashboardTab(_tabs.TabPages[0]);
            }
        }

        private void SetupBranchSummaryColumns()
        {
            _gridBranchSummary.AutoGenerateColumns = false;
            _gridBranchSummary.Columns.Clear();

            _gridBranchSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BranchCode",
                HeaderText = "Code",
                DataPropertyName = "BranchCode",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 9.5f) }
            });

            _gridBranchSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BranchName",
                HeaderText = "Branch Name",
                DataPropertyName = "BranchName",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 160
            });

            _gridBranchSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TotalIncomeFormatted",
                HeaderText = "Total Income",
                DataPropertyName = "TotalIncomeFormatted",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = Theme.Green, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });

            _gridBranchSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TotalExpensesFormatted",
                HeaderText = "Total Expenses",
                DataPropertyName = "TotalExpensesFormatted",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, ForeColor = ColorTranslator.FromHtml("#EF4444"), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });

            _gridBranchSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NetBalanceFormatted",
                HeaderText = "Net Cash Flow",
                DataPropertyName = "NetBalanceFormatted",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 10f, FontStyle.Bold) }
            });

            _gridBranchSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TransactionCount",
                HeaderText = "Txns",
                DataPropertyName = "TransactionCount",
                Width = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            _gridBranchSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StatusFormatted",
                HeaderText = "Status",
                DataPropertyName = "StatusFormatted",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 9f) }
            });
        }

        private void SetupLedgerColumns()
        {
            _gridTransactions.AutoGenerateColumns = false;
            _gridTransactions.Columns.Clear();

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RefCode",
                HeaderText = "Ref #",
                DataPropertyName = "RefCode",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DateFormatted",
                HeaderText = "Date",
                DataPropertyName = "DateFormatted",
                Width = 105,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BranchName",
                HeaderText = "Branch",
                DataPropertyName = "BranchName",
                Width = 150
            });

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Classification",
                HeaderText = "Classification",
                DataPropertyName = "Classification",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 9f) }
            });

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Category",
                DataPropertyName = "Category",
                Width = 180
            });

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description / Reference",
                DataPropertyName = "Description",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AmountFormatted",
                HeaderText = "Amount",
                DataPropertyName = "AmountFormatted",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });
        }

        private void ApplyPreset(int index)
        {
            _isApplyingPreset = true;
            try
            {
                var now = DateTime.Today;
                if (index == 0) // This Month
                {
                    _dtpStart.Value = new DateTime(now.Year, now.Month, 1);
                    _dtpEnd.Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                }
                else if (index == 1) // Previous Month
                {
                    var prev = now.AddMonths(-1);
                    _dtpStart.Value = new DateTime(prev.Year, prev.Month, 1);
                    _dtpEnd.Value = new DateTime(prev.Year, prev.Month, DateTime.DaysInMonth(prev.Year, prev.Month));
                }
                else if (index == 2) // Last 30 Days
                {
                    _dtpStart.Value = now.AddDays(-30);
                    _dtpEnd.Value = now;
                }
                else if (index == 3) // This Year
                {
                    _dtpStart.Value = new DateTime(now.Year, 1, 1);
                    _dtpEnd.Value = new DateTime(now.Year, 12, 31);
                }
                else if (index == 4) // All Records
                {
                    _dtpStart.Value = new DateTime(2020, 1, 1);
                    _dtpEnd.Value = now.AddDays(30);
                }
            }
            finally
            {
                _isApplyingPreset = false;
            }
        }

        private async Task LoadBranches()
        {
            try
            {
                var list = await ApiConfig.Http.GetFromJsonAsync<List<BranchOptionDto>>(
                    "api/Branches", ApiConfig.JsonOptions) ?? new();

                _branches.Clear();
                _branches.AddRange(list);

                _cmbBranch.Items.Clear();
                _cmbBranch.Items.Add(new BranchOption { Id = 0, Name = "All Branches (Consolidated)" });
                foreach (var b in _branches)
                {
                    _cmbBranch.Items.Add(new BranchOption { Id = b.Id, Name = $"{b.Code} — {b.Name}" });
                }
                _cmbBranch.SelectedIndex = 0;
                _isBranchesLoaded = true;
            }
            catch
            {
                _cmbBranch.Items.Clear();
                _cmbBranch.Items.Add(new BranchOption { Id = 0, Name = "All Branches (Consolidated)" });
                _cmbBranch.SelectedIndex = 0;
                _isBranchesLoaded = true;
            }
        }

        private async Task LoadDashboard()
        {
            try
            {
                var branchId = (_cmbBranch.SelectedItem as BranchOption)?.Id ?? 0;
                var start = _dtpStart.Value.ToString("yyyy-MM-dd");
                var end = _dtpEnd.Value.ToString("yyyy-MM-dd");

                string url = branchId > 0
                    ? $"api/finance/dashboard?branchId={branchId}&periodStart={start}&periodEnd={end}"
                    : $"api/finance/dashboard?periodStart={start}&periodEnd={end}";

                var response = await ApiConfig.Http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(msg, "Unable to load company finance", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var dashboard = await response.Content.ReadFromJsonAsync<FinanceDashboardDto>(ApiConfig.JsonOptions);
                if (dashboard is null) return;

                // 1. Update Top KPI Cards
                _valIncome.Text = $"₱{dashboard.TotalIncome:N2}";
                _valDeductions.Text = $"₱{dashboard.TotalDeductions:N2}";
                _valTaxes.Text = $"₱{dashboard.PayrollTaxes:N2}";

                decimal net = dashboard.CompanyBalance;
                _valNet.Text = (net >= 0 ? "+" : "") + $"₱{net:N2}";
                _valNet.ForeColor = net >= 0 ? Theme.Green : ColorTranslator.FromHtml("#EF4444");

                var txns = dashboard.Transactions ?? new List<CompanyFinanceTransaction>();

                // 2. Prepare Ledger View Items
                _allTransactions = txns.Select(t =>
                {
                    bool isDeduction = t.Type == FinanceTransactionType.Deduction;
                    bool isInvest = !isDeduction && (
                        t.Category.Contains("Invest", StringComparison.OrdinalIgnoreCase) ||
                        t.Category.Contains("Donat", StringComparison.OrdinalIgnoreCase) ||
                        t.Category.Contains("Grant", StringComparison.OrdinalIgnoreCase) ||
                        t.Category.Contains("Capital", StringComparison.OrdinalIgnoreCase) ||
                        t.Description.Contains("Invest", StringComparison.OrdinalIgnoreCase) ||
                        t.Description.Contains("Donat", StringComparison.OrdinalIgnoreCase));

                    string classification = isDeduction ? "Expense / Deduction" :
                        (isInvest ? "Investment / Capital" : "Operating Revenue");

                    string prefix = isDeduction ? "-₱" : "+₱";

                    return new TransactionViewItem
                    {
                        Id = t.Id,
                        RefCode = $"TXN-{t.Id:D4}",
                        DateFormatted = t.TransactionDate.ToLocalTime().ToString("yyyy-MM-dd"),
                        Classification = classification,
                        Category = t.Category,
                        Description = t.Description,
                        Amount = t.Amount,
                        AmountFormatted = $"{prefix}{t.Amount:N2}",
                        IsDeduction = isDeduction,
                        IsInvestmentOrDonation = isInvest,
                        BranchId = t.BranchId,
                        BranchName = t.Branch != null ? $"{t.Branch.Code} - {t.Branch.Name}" : "Main / Unassigned"
                    };
                }).ToList();

                // 3. Compute Per-Branch Financial Summaries
                _branchSummaries.Clear();

                if (_branches.Count > 0)
                {
                    foreach (var b in _branches)
                    {
                        if (branchId > 0 && b.Id != branchId) continue;

                        var bTxns = txns.Where(t => t.BranchId == b.Id).ToList();
                        decimal inc = bTxns.Where(t => t.Type == FinanceTransactionType.Income).Sum(t => t.Amount);
                        decimal ded = bTxns.Where(t => t.Type == FinanceTransactionType.Deduction).Sum(t => t.Amount);
                        decimal bNet = inc - ded;

                        _branchSummaries.Add(new BranchSummaryRow
                        {
                            BranchId = b.Id,
                            BranchCode = b.Code,
                            BranchName = b.Name,
                            TotalIncome = inc,
                            TotalIncomeFormatted = $"₱{inc:N2}",
                            TotalExpenses = ded,
                            TotalExpensesFormatted = $"₱{ded:N2}",
                            NetBalance = bNet,
                            NetBalanceFormatted = (bNet >= 0 ? "+" : "") + $"₱{bNet:N2}",
                            TransactionCount = bTxns.Count,
                            StatusFormatted = bNet > 0 ? "● Profitable" : (bNet == 0 ? "● Neutral" : "● Deficit")
                        });
                    }
                }

                // Check for transactions with unassigned / other branch
                var unassignedTxns = txns.Where(t => !_branches.Any(b => b.Id == t.BranchId)).ToList();
                if (unassignedTxns.Count > 0 && branchId == 0)
                {
                    decimal inc = unassignedTxns.Where(t => t.Type == FinanceTransactionType.Income).Sum(t => t.Amount);
                    decimal ded = unassignedTxns.Where(t => t.Type == FinanceTransactionType.Deduction).Sum(t => t.Amount);
                    decimal bNet = inc - ded;

                    _branchSummaries.Add(new BranchSummaryRow
                    {
                        BranchId = 0,
                        BranchCode = "MAIN",
                        BranchName = "Main / Unassigned",
                        TotalIncome = inc,
                        TotalIncomeFormatted = $"₱{inc:N2}",
                        TotalExpenses = ded,
                        TotalExpensesFormatted = $"₱{ded:N2}",
                        NetBalance = bNet,
                        NetBalanceFormatted = (bNet >= 0 ? "+" : "") + $"₱{bNet:N2}",
                        TransactionCount = unassignedTxns.Count,
                        StatusFormatted = bNet > 0 ? "● Profitable" : (bNet == 0 ? "● Neutral" : "● Deficit")
                    });
                }

                // Bind Branch Summary Grid — update BindingList in-place, never replace DataSource
                _branchSummarySource.RaiseListChangedEvents = false;
                _branchSummarySource.Clear();
                foreach (var row in _branchSummaries.OrderByDescending(x => x.TotalIncome))
                    _branchSummarySource.Add(row);
                _branchSummarySource.RaiseListChangedEvents = true;
                _branchSummarySource.ResetBindings();
                try
                {
                    if (_gridBranchSummary.HorizontalScrollingOffset > 0)
                        _gridBranchSummary.HorizontalScrollingOffset = 0;
                }
                catch { }


                // 4. Update Charts
                UpdateCharts(branchId, txns);

                // 5. Filter & update ledger
                FilterLedgerGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading financial dashboard: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCharts(int branchId, List<CompanyFinanceTransaction> txns)
        {
            // 1. Line Chart: Multi-Branch Revenue Comparison
            _ = LoadLineChartData(branchId);

            // 2. Bar Chart: Branch Comparison (or Income vs Expense for single branch)
            if (branchId == 0 && _branchSummaries.Count > 0)
            {
                _chartBranches.Title = "Revenue by Branch";
                _chartBranches.Subtitle = "Gross income generated across active branches";
                var branchPoints = _branchSummaries
                    .Where(b => b.TotalIncome > 0 || b.TotalExpenses > 0)
                    .Select(b => new ChartDataPoint
                    {
                        Label = b.BranchCode,
                        Value = b.TotalIncome,
                        Color = b.NetBalance >= 0 ? Theme.Green : ColorTranslator.FromHtml("#EF4444")
                    }).ToList();
                _chartBranches.SetData(branchPoints);
            }
            else
            {
                _chartBranches.Title = "Financial Performance";
                _chartBranches.Subtitle = "Income vs Expenditure for selected branch";
                decimal inc = txns.Where(t => t.Type == FinanceTransactionType.Income).Sum(t => t.Amount);
                decimal ded = txns.Where(t => t.Type == FinanceTransactionType.Deduction).Sum(t => t.Amount);
                _chartBranches.SetData(new List<ChartDataPoint>
                {
                    new ChartDataPoint("Total Income", inc, Theme.Green),
                    new ChartDataPoint("Total Deductions", ded, ColorTranslator.FromHtml("#EF4444")),
                    new ChartDataPoint("Net Balance", Math.Max(0, inc - ded), Theme.Blue)
                });
            }

            // Donut Chart: Top 6 Expense Categories
            var expenseCats = txns
                .Where(t => t.Type == FinanceTransactionType.Deduction)
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "General Ops" : t.Category.Trim())
                .Select(g => new ChartDataPoint(g.Key, g.Sum(x => x.Amount)))
                .OrderByDescending(p => p.Value)
                .Take(6)
                .ToList();

            if (expenseCats.Count == 0)
            {
                // If no deductions, show income categories
                var incomeCats = txns
                    .Where(t => t.Type == FinanceTransactionType.Income)
                    .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Commodity Sales" : t.Category.Trim())
                    .Select(g => new ChartDataPoint(g.Key, g.Sum(x => x.Amount)))
                    .OrderByDescending(p => p.Value)
                    .Take(6)
                    .ToList();

                _chartCategories.Title = "Income Sources";
                _chartCategories.Subtitle = "Breakdown of revenue and capital inflows";
                _chartCategories.CenterLabel = "Inflow";
                _chartCategories.SetData(incomeCats);
            }
            else
            {
                _chartCategories.Title = "Expenditure Breakdown";
                _chartCategories.Subtitle = "Distribution of deductions and expenses by category";
                _chartCategories.CenterLabel = "Expenses";
                _chartCategories.SetData(expenseCats);
            }
        }

        private async Task LoadLineChartData(int branchId)
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Branches/revenue-comparison?months=6");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<BranchRevenueComparisonDto>(ApiConfig.JsonOptions);
                    if (data != null && data.Series != null && data.Series.Count > 0)
                    {
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
                        _chartBranchRevenueLine.SetData(data.XLabels, seriesList);
                    }
                    else
                    {
                        _chartBranchRevenueLine.SetData(new List<string>(), new List<LineSeries>());
                    }
                }
            }
            catch
            {
                _chartBranchRevenueLine.SetData(new List<string>(), new List<LineSeries>());
            }
        }

        private void FilterLedgerGrid()
        {
            var filtered = _allTransactions.AsEnumerable();

            int typeFilter = _cmbTypeFilter?.SelectedIndex ?? 0;
            if (typeFilter == 1) // Operating Revenue
                filtered = filtered.Where(x => !x.IsDeduction && !x.IsInvestmentOrDonation);
            else if (typeFilter == 2) // Deductions
                filtered = filtered.Where(x => x.IsDeduction);
            else if (typeFilter == 3) // Investment / Grant
                filtered = filtered.Where(x => x.IsInvestmentOrDonation);

            string search = _txtSearch?.Text.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(x =>
                    x.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.BranchName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.RefCode.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Update ledger BindingList in-place — never replace DataSource to preserve column styling
            _ledgerSource.RaiseListChangedEvents = false;
            _ledgerSource.Clear();
            foreach (var item in filtered)
                _ledgerSource.Add(item);
            _ledgerSource.RaiseListChangedEvents = true;
            _ledgerSource.ResetBindings();
            Theme.FillColumnsToWidth(_gridTransactions);
        }

        private async Task ExportReportPdf()
        {
            try
            {
                var branchId = (_cmbBranch.SelectedItem as BranchOption)?.Id ?? 0;
                var endpoint = branchId > 0 ? $"api/Reports/overall/pdf?branchId={branchId}" : "api/Reports/overall/pdf";

                var res = await ApiConfig.Http.GetAsync(endpoint);
                if (res.IsSuccessStatusCode)
                {
                    var bytes = await res.Content.ReadAsByteArrayAsync();
                    var path = Path.Combine(Path.GetTempPath(), $"CompanyFinanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    await File.WriteAllBytesAsync(path, bytes);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Failed to generate PDF report: " + res.StatusCode, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating report PDF: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Panel MakeCard()
        {
            var p = new Panel { BackColor = Theme.White };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private static Panel MakeStatCard(string label, string value, string subtext, Color accent)
        {
            var card = new Panel { BackColor = Theme.White };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
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
                Left = 14,
                Top = 12,
                Width = Math.Max(50, card.Width - 28),
                Height = 18,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            var val = new Label
            {
                Text = value,
                Left = 14,
                Top = 32,
                Width = Math.Max(50, card.Width - 28),
                Height = 36,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false
            };

            var sub = new Label
            {
                Text = subtext,
                Left = 14,
                Top = 70,
                Width = Math.Max(50, card.Width - 28),
                Height = 18,
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            card.Resize += (s, e) =>
            {
                int w = Math.Max(10, card.Width - 28);
                lbl.Width = w;
                val.Width = w;
                sub.Width = w;
            };

            card.Controls.Add(bar);
            card.Controls.Add(sub);
            card.Controls.Add(val);
            card.Controls.Add(lbl);
            card.Tag = val;
            return card;
        }

        // ── Helper Models ──────────────────────────────────────────────────────
        private sealed class FinanceDashboardDto
        {
            public decimal TotalIncome { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal PayrollTaxes { get; set; }
            public decimal CompanyBalance { get; set; }
            public decimal TotalInvestedOrDonated { get; set; }
            public decimal OperatingRevenue { get; set; }
            public List<CompanyFinanceTransaction>? Transactions { get; set; }
        }

        private sealed class BranchSummaryRow
        {
            public int BranchId { get; set; }
            public string BranchCode { get; set; } = string.Empty;
            public string BranchName { get; set; } = string.Empty;
            public decimal TotalIncome { get; set; }
            public string TotalIncomeFormatted { get; set; } = string.Empty;
            public decimal TotalExpenses { get; set; }
            public string TotalExpensesFormatted { get; set; } = string.Empty;
            public decimal NetBalance { get; set; }
            public string NetBalanceFormatted { get; set; } = string.Empty;
            public int TransactionCount { get; set; }
            public string StatusFormatted { get; set; } = string.Empty;
        }

        private sealed class TransactionViewItem
        {
            public int Id { get; set; }
            public string RefCode { get; set; } = string.Empty;
            public string DateFormatted { get; set; } = string.Empty;
            public string Classification { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string AmountFormatted { get; set; } = string.Empty;
            public bool IsDeduction { get; set; }
            public bool IsInvestmentOrDonation { get; set; }
            public int BranchId { get; set; }
            public string BranchName { get; set; } = string.Empty;
        }

        private sealed class BranchOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public override string ToString() => Name;
        }

        private sealed class BranchOptionDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
    }
}
