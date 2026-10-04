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
    public sealed class CompanyFinanceForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnAddEntry = null!;
        private Button btnRefresh = null!;

        // Period filter controls
        private DateTimePicker dtpStart = null!;
        private DateTimePicker dtpEnd = null!;
        private ComboBox cmbPreset = null!;
        private Label lblFilterType = null!;
        private ComboBox cmbTypeFilter = null!;
        private TextBox txtSearch = null!;
        private bool _suppressDateEvents;

        // 4 KPI Stat Cards
        private Panel cardSpent = null!;
        private Panel cardRevenue = null!;
        private Panel cardInvested = null!;
        private Panel cardNet = null!;
        private Panel filterPanel = null!;

        private Label valSpent = null!;
        private Label valRevenue = null!;
        private Label valInvested = null!;
        private Label valNet = null!;

        // Tab and Trend Line Chart
        private TabControl tabs = null!;
        private TabPage tabTrend = null!;
        private TabPage tabLedger = null!;
        private LineChartControl lineChartTrend = null!;

        // Transaction Ledger Grid
        private Panel cardGrid = null!;
        private DataGridView dgvTransactions = null!;
        private List<TransactionViewItem> _allTransactions = new();

        public CompanyFinanceForm()
        {
            Text = "Company Finance";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();

            HandleCreated += async (s, e) => await LoadDashboard();
        }

        private void InitializeComponent()
        {
            // Title & Subtitle
            string branchName = !string.IsNullOrWhiteSpace(CurrentSession.BranchName)
                ? CurrentSession.BranchName
                : "Assigned Branch";

            lblTitle = new Label
            {
                Text = $"Branch Financials — {branchName}",
                Left = 32,
                Top = 22,
                Width = 560,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = $"Branch financial dashboard: expenditure, revenue, investments, and ledger strictly for {branchName}",
                Left = 32,
                Top = 58,
                Width = 750,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            // Top-right action buttons
            btnAddEntry = new Button
            {
                Text = "+ Record Finance Entry",
                Width = 220,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StylePrimaryButton(btnAddEntry);
            btnAddEntry.Click += async (s, e) =>
            {
                using var dlg = new Manager.FinanceEntryDialog();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    await LoadDashboard();
                }
            };

            btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Width = 100,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadDashboard();

            // 4 KPI Cards
            cardSpent = MakeStatCard("Operating Cost (This Month)", "₱0.00", "Procurement, payroll, & ops", ColorTranslator.FromHtml("#EF4444"));
            valSpent = (Label)cardSpent.Tag!;

            cardRevenue = MakeStatCard("Money Received (Revenue)", "₱0.00", "Commodity sales & operations", Theme.Green);
            valRevenue = (Label)cardRevenue.Tag!;

            cardInvested = MakeStatCard("Invested & Donated", "₱0.00", "Capital, grants, & donations", ColorTranslator.FromHtml("#0069E5"));
            valInvested = (Label)cardInvested.Tag!;

            cardNet = MakeStatCard("Net Cash Flow / Balance", "₱0.00", "Income + Capital - Expenses", Theme.DarkText);
            valNet = (Label)cardNet.Tag!;

            // Period Filter Bar
            filterPanel = new Panel
            {
                Left = 32,
                Top = 220,
                Height = 44,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblPeriod = new Label { Text = "Period:", Left = 0, Top = 10, AutoSize = true, Font = Theme.StatLabelFont };
            dtpStart = new DateTimePicker
            {
                Left = 48,
                Top = 6,
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            dtpStart.ValueChanged += async (s, e) => { if (!_suppressDateEvents) await LoadDashboard(); };

            var lblTo = new Label { Text = "to", Left = 176, Top = 10, AutoSize = true, Font = Theme.StatLabelFont };
            dtpEnd = new DateTimePicker
            {
                Left = 196,
                Top = 6,
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
            };
            dtpEnd.ValueChanged += async (s, e) => { if (!_suppressDateEvents) await LoadDashboard(); };

            cmbPreset = new ComboBox
            {
                Left = 328,
                Top = 6,
                Width = 135,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cmbPreset);
            cmbPreset.Items.AddRange(new object[] { "This Month", "Previous Month", "Last 30 Days", "This Year", "All Records" });
            cmbPreset.SelectedIndex = 0;
            cmbPreset.SelectedIndexChanged += (s, e) => ApplyPreset(cmbPreset.SelectedIndex);

            lblFilterType = new Label { Text = "Show:", Left = 475, Top = 10, AutoSize = true, Font = Theme.StatLabelFont };
            cmbTypeFilter = new ComboBox
            {
                Left = 518,
                Top = 6,
                Width = 175,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cmbTypeFilter);
            cmbTypeFilter.Items.AddRange(new object[] { "All Transactions", "Money Spent (Deductions)", "Money Received (Revenue)", "Invested & Donated" });
            cmbTypeFilter.SelectedIndex = 0;
            cmbTypeFilter.SelectedIndexChanged += (s, e) => FilterGrid();

            txtSearch = new TextBox
            {
                Left = 705,
                Top = 6,
                Width = 200,
                PlaceholderText = "Search category or note..."
            };
            Theme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => FilterGrid();

            filterPanel.Controls.AddRange(new Control[]
            {
                lblPeriod, dtpStart, lblTo, dtpEnd, cmbPreset,
                lblFilterType, cmbTypeFilter, txtSearch
            });

            // Grid card and Grid
            cardGrid = MakeCard();
            cardGrid.Dock = DockStyle.Fill;

            dgvTransactions = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true
            };
            dgvTransactions.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(dgvTransactions);
            SetupGridColumns();

            dgvTransactions.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvTransactions.Rows.Count) return;
                var row = dgvTransactions.Rows[e.RowIndex];
                if (row.DataBoundItem is TransactionViewItem item)
                {
                    if (item.IsDeduction)
                    {
                        row.Cells["AmountFormatted"].Style.ForeColor = ColorTranslator.FromHtml("#DC2626");
                        row.Cells["TypeBadge"].Style.ForeColor = ColorTranslator.FromHtml("#DC2626");
                    }
                    else if (item.IsInvestmentOrDonation)
                    {
                        row.Cells["AmountFormatted"].Style.ForeColor = ColorTranslator.FromHtml("#0069E5");
                        row.Cells["TypeBadge"].Style.ForeColor = ColorTranslator.FromHtml("#0069E5");
                    }
                    else
                    {
                        row.Cells["AmountFormatted"].Style.ForeColor = ColorTranslator.FromHtml("#16A34A");
                        row.Cells["TypeBadge"].Style.ForeColor = ColorTranslator.FromHtml("#16A34A");
                    }
                }
            };

            cardGrid.Controls.Add(dgvTransactions);

            // Tabs: Tab 1 (Line graph) & Tab 2 (Transaction Ledger)
            tabs = new TabControl
            {
                Left = 32,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            tabTrend = new TabPage("📈 Revenue & Operating Cost Trend")
            {
                BackColor = Theme.Background,
                Padding = new Padding(8)
            };

            lineChartTrend = new LineChartControl
            {
                Dock = DockStyle.Fill,
                Title = "Operating Cost vs. Revenue Trend",
                Subtitle = "Operating expenditures vs. gross operating revenue across the selected period",
                ValuePrefix = "₱"
            };
            tabTrend.Controls.Add(lineChartTrend);

            tabLedger = new TabPage("📑 Transaction Ledger")
            {
                BackColor = Theme.Background,
                Padding = new Padding(8)
            };
            tabLedger.Controls.Add(cardGrid);

            tabs.TabPages.Add(tabTrend);
            tabs.TabPages.Add(tabLedger);

            // Initially on Trend tab, so ledger search/filter is hidden until Ledger tab is selected
            lblFilterType.Visible = false;
            cmbTypeFilter.Visible = false;
            txtSearch.Visible = false;

            tabs.SelectedIndexChanged += (s, e) =>
            {
                bool isLedger = tabs.SelectedTab == tabLedger;
                lblFilterType.Visible = isLedger;
                cmbTypeFilter.Visible = isLedger;
                txtSearch.Visible = isLedger;

                if (isLedger && dgvTransactions != null && dgvTransactions.IsHandleCreated && dgvTransactions.Columns.Count > 0)
                {
                    Theme.FillColumnsToWidth(dgvTransactions);
                }
            };

            Controls.AddRange(new Control[]
            {
                lblTitle, lblSubtitle, btnAddEntry, btnRefresh,
                cardSpent, cardRevenue, cardInvested, cardNet,
                filterPanel, tabs
            });

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private void LayoutControls()
        {
            btnRefresh.Left = ClientSize.Width - btnRefresh.Width - 32;
            btnRefresh.Top = 28;

            btnAddEntry.Width = 220;
            btnAddEntry.Left = btnRefresh.Left - btnAddEntry.Width - 12;
            btnAddEntry.Top = 28;

            int gap = 16;
            int startX = 32;
            int y = 96;
            int availableW = Math.Max(400, ClientSize.Width - 64);
            int cardW = (availableW - (gap * 3)) / 4;
            int cardH = 104;

            cardSpent.SetBounds(startX, y, cardW, cardH);
            cardRevenue.SetBounds(startX + cardW + gap, y, cardW, cardH);
            cardInvested.SetBounds(startX + (cardW + gap) * 2, y, cardW, cardH);
            cardNet.SetBounds(startX + (cardW + gap) * 3, y, cardW, cardH);

            filterPanel.SetBounds(startX, y + cardH + 16, availableW, 44);
            int tabsTop = filterPanel.Bottom + 10;
            tabs.SetBounds(startX, tabsTop, availableW, Math.Max(160, ClientSize.Height - tabsTop - 24));

            if (tabs.SelectedTab == tabLedger && dgvTransactions != null && dgvTransactions.IsHandleCreated && dgvTransactions.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvTransactions);
        }

        private void SetupGridColumns()
        {
            dgvTransactions.Columns.Clear();

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RefCode",
                HeaderText = "Ref #",
                DataPropertyName = "RefCode",
                Width = 90
            });

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DateFormatted",
                HeaderText = "Date",
                DataPropertyName = "DateFormatted",
                Width = 110
            });

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TypeBadge",
                HeaderText = "Classification",
                DataPropertyName = "Classification",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                }
            });

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Category",
                DataPropertyName = "Category",
                Width = 180
            });

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description / Reference",
                DataPropertyName = "Description",
                FillWeight = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AmountFormatted",
                HeaderText = "Amount",
                DataPropertyName = "AmountFormatted",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            });

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BranchName",
                HeaderText = "Branch",
                DataPropertyName = "BranchName",
                Width = 150
            });
        }

        private void ApplyPreset(int index)
        {
            _suppressDateEvents = true;
            try
            {
                var now = DateTime.Today;
                if (index == 0) // This Month
                {
                    dtpStart.Value = new DateTime(now.Year, now.Month, 1);
                    dtpEnd.Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                }
                else if (index == 1) // Previous Month
                {
                    var prev = now.AddMonths(-1);
                    dtpStart.Value = new DateTime(prev.Year, prev.Month, 1);
                    dtpEnd.Value = new DateTime(prev.Year, prev.Month, DateTime.DaysInMonth(prev.Year, prev.Month));
                }
                else if (index == 2) // Last 30 Days
                {
                    dtpStart.Value = now.AddDays(-30);
                    dtpEnd.Value = now;
                }
                else if (index == 3) // This Year
                {
                    dtpStart.Value = new DateTime(now.Year, 1, 1);
                    dtpEnd.Value = new DateTime(now.Year, 12, 31);
                }
                else if (index == 4) // All Records
                {
                    dtpStart.Value = new DateTime(2020, 1, 1);
                    dtpEnd.Value = now.AddDays(30);
                }
            }
            finally
            {
                _suppressDateEvents = false;
            }
            _ = LoadDashboard();
        }

        private async Task LoadDashboard()
        {
            try
            {
                var start = dtpStart.Value.ToString("yyyy-MM-dd");
                var end = dtpEnd.Value.ToString("yyyy-MM-dd");
                string url = $"api/finance/dashboard?periodStart={start}&periodEnd={end}";
                if (CurrentSession.BranchId.HasValue && CurrentSession.BranchId.Value > 0)
                {
                    url += $"&branchId={CurrentSession.BranchId.Value}";
                }

                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<FinanceDashboardDto>(ApiConfig.JsonOptions);
                    if (data != null)
                    {
                        valSpent.Text = $"₱{data.TotalDeductions:N2}";
                        valRevenue.Text = $"₱{data.OperatingRevenue:N2}";
                        valInvested.Text = $"₱{data.TotalInvestedOrDonated:N2}";

                        decimal net = data.CompanyBalance;
                        valNet.Text = (net >= 0 ? "+" : "") + $"₱{net:N2}";
                        valNet.ForeColor = net >= 0 ? Theme.Green : ColorTranslator.FromHtml("#EF4444");

                        var txList = data.Transactions ?? new List<CompanyFinanceTransaction>();
                        if (CurrentSession.BranchId.HasValue && CurrentSession.BranchId.Value > 0)
                        {
                            txList = txList.Where(t => t.BranchId == CurrentSession.BranchId.Value).ToList();
                        }

                        _allTransactions = txList
                            .Select(t =>
                            {
                                bool isDeduction = t.Type == FinanceTransactionType.Deduction;
                                bool isInvestOrDonate = IsInvestmentOrDonation(t);

                                string classification = isDeduction ? "Expense / Deduction" :
                                    (isInvestOrDonate ? "Investment / Donation" : "Operating Revenue");

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
                                    IsInvestmentOrDonation = isInvestOrDonate,
                                    BranchId = t.BranchId,
                                    BranchName = t.Branch?.Name ?? $"Branch #{t.BranchId}"
                                };
                            }).ToList();

                        UpdateTrendChart(txList);
                        FilterGrid();

                        if (tabs.SelectedTab == tabLedger && dgvTransactions != null && dgvTransactions.IsHandleCreated && dgvTransactions.Columns.Count > 0)
                        {
                            Theme.FillColumnsToWidth(dgvTransactions);
                        }
                    }
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Unable to load finance dashboard: " + body, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading finance dashboard: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateTrendChart(List<CompanyFinanceTransaction> txList)
        {
            var seriesRevenue = new LineSeries
            {
                Name = "Operating Revenue",
                Color = Theme.Green,
                Values = new List<decimal>()
            };

            var seriesCost = new LineSeries
            {
                Name = "Operating Cost",
                Color = ColorTranslator.FromHtml("#EF4444"),
                Values = new List<decimal>()
            };

            var xLabels = new List<string>();

            if (txList == null || txList.Count == 0)
            {
                lineChartTrend.SetData(xLabels, new List<LineSeries> { seriesRevenue, seriesCost });
                return;
            }

            DateTime minDate = txList.Min(t => t.TransactionDate.Date);
            DateTime maxDate = txList.Max(t => t.TransactionDate.Date);

            DateTime rangeStart = dtpStart.Value.Date;
            DateTime rangeEnd = dtpEnd.Value.Date;

            if (rangeStart > rangeEnd)
            {
                var temp = rangeStart;
                rangeStart = rangeEnd;
                rangeEnd = temp;
            }

            // If range is expansive (e.g. 'All Records' starting at 2020), clamp to transaction span
            if ((rangeEnd - rangeStart).TotalDays > 365 && (maxDate - minDate).TotalDays <= 365)
            {
                rangeStart = new DateTime(minDate.Year, minDate.Month, 1);
                rangeEnd = new DateTime(maxDate.Year, maxDate.Month, DateTime.DaysInMonth(maxDate.Year, maxDate.Month));
            }

            int days = (int)(rangeEnd - rangeStart).TotalDays + 1;

            if (days <= 35)
            {
                for (var d = rangeStart; d <= rangeEnd; d = d.AddDays(1))
                {
                    xLabels.Add(d.ToString("MMM dd"));
                    decimal rev = txList
                        .Where(t => t.TransactionDate.Date == d && t.Type == FinanceTransactionType.Income && !IsInvestmentOrDonation(t))
                        .Sum(t => t.Amount);
                    decimal cost = txList
                        .Where(t => t.TransactionDate.Date == d && t.Type == FinanceTransactionType.Deduction)
                        .Sum(t => t.Amount);

                    seriesRevenue.Values.Add(rev);
                    seriesCost.Values.Add(cost);
                }
            }
            else
            {
                var cur = new DateTime(rangeStart.Year, rangeStart.Month, 1);
                var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
                while (cur <= endMonth)
                {
                    xLabels.Add(cur.ToString("MMM yyyy"));
                    decimal rev = txList
                        .Where(t => t.TransactionDate.Year == cur.Year && t.TransactionDate.Month == cur.Month &&
                                    t.Type == FinanceTransactionType.Income && !IsInvestmentOrDonation(t))
                        .Sum(t => t.Amount);
                    decimal cost = txList
                        .Where(t => t.TransactionDate.Year == cur.Year && t.TransactionDate.Month == cur.Month &&
                                    t.Type == FinanceTransactionType.Deduction)
                        .Sum(t => t.Amount);

                    seriesRevenue.Values.Add(rev);
                    seriesCost.Values.Add(cost);
                    cur = cur.AddMonths(1);
                }
            }

            lineChartTrend.SetData(xLabels, new List<LineSeries> { seriesRevenue, seriesCost });
        }

        private static bool IsInvestmentOrDonation(CompanyFinanceTransaction t)
        {
            if (t.Type == FinanceTransactionType.Deduction) return false;
            return (!string.IsNullOrWhiteSpace(t.Category) && (
                       t.Category.Contains("Invest", StringComparison.OrdinalIgnoreCase) ||
                       t.Category.Contains("Donat", StringComparison.OrdinalIgnoreCase) ||
                       t.Category.Contains("Grant", StringComparison.OrdinalIgnoreCase) ||
                       t.Category.Contains("Capital", StringComparison.OrdinalIgnoreCase))) ||
                   (!string.IsNullOrWhiteSpace(t.Description) && (
                       t.Description.Contains("Invest", StringComparison.OrdinalIgnoreCase) ||
                       t.Description.Contains("Donat", StringComparison.OrdinalIgnoreCase)));
        }

        private void FilterGrid()
        {
            var filtered = _allTransactions.AsEnumerable();

            int typeFilter = cmbTypeFilter.SelectedIndex;
            if (typeFilter == 1) // Deductions
                filtered = filtered.Where(x => x.IsDeduction);
            else if (typeFilter == 2) // Revenue
                filtered = filtered.Where(x => !x.IsDeduction && !x.IsInvestmentOrDonation);
            else if (typeFilter == 3) // Investment / Donation
                filtered = filtered.Where(x => x.IsInvestmentOrDonation);

            string search = txtSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(x =>
                    x.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.RefCode.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (CurrentSession.BranchId.HasValue && CurrentSession.BranchId.Value > 0)
            {
                filtered = filtered.Where(x => x.BranchId == CurrentSession.BranchId.Value);
            }

            dgvTransactions.DataSource = filtered.ToList();
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
            var card = new Panel
            {
                BackColor = Theme.White
            };
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
                Height = 38,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false
            };

            var sub = new Label
            {
                Text = subtext,
                Left = 14,
                Top = 72,
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

        private class FinanceDashboardDto
        {
            public decimal TotalIncome { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal OperatingRevenue { get; set; }
            public decimal TotalInvestedOrDonated { get; set; }
            public decimal CompanyBalance { get; set; }
            public List<CompanyFinanceTransaction>? Transactions { get; set; }
        }

        private class TransactionViewItem
        {
            public int Id { get; set; }
            public string RefCode { get; set; } = "";
            public string DateFormatted { get; set; } = "";
            public string Classification { get; set; } = "";
            public string Category { get; set; } = "";
            public string Description { get; set; } = "";
            public decimal Amount { get; set; }
            public string AmountFormatted { get; set; } = "";
            public bool IsDeduction { get; set; }
            public bool IsInvestmentOrDonation { get; set; }
            public int BranchId { get; set; }
            public string BranchName { get; set; } = "";
        }
    }
}
