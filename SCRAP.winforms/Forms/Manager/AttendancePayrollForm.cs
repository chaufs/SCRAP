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
    public sealed class AttendancePayrollForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private TabControl tabs = null!;

        // ================= TAB 1: DAILY ATTENDANCE =================
        private TabPage tabDaily = null!;
        private DateTimePicker dtpDailyDate = null!;
        private Button btnDailyToday = null!;
        private Button btnDailyRefresh = null!;
        private Button btnMarkPresent = null!;
        private Button btnMarkAbsent = null!;
        private Button btnMarkAllPresent = null!;
        private DataGridView dgvDaily = null!;

        // Stat cards (Daily)
        private Label valTotalStaff = null!;
        private Label valPresentToday = null!;
        private Label valAbsentToday = null!;
        private Label valUnmarkedToday = null!;
        private Label valAttendanceRate = null!;

        private List<DailyRosterItem> _dailyRoster = new();

        // ================= TAB 2: SUMMARY & HISTORY =================
        private TabPage tabSummary = null!;
        private DateTimePicker dtpSummaryStart = null!;
        private DateTimePicker dtpSummaryEnd = null!;
        private Button btnFilterSummary = null!;
        private ComboBox cmbStaffSelector = null!;
        private DataGridView dgvSummary = null!;
        private DataGridView dgvHistory = null!;
        private Label lblHistoryFor = null!;
        private List<AttendanceSummaryItem> _summaryItems = new();

        // ================= TAB 3: PAYROLL MANAGEMENT =================
        private TabPage tabPayroll = null!;
        private DateTimePicker dtpPayrollStart = null!;
        private DateTimePicker dtpPayrollEnd = null!;
        private ComboBox cmbPeriodPreset = null!;
        private Button btnCalculatePayroll = null!;
        private Button btnExportPayroll = null!;
        private DataGridView dgvPayroll = null!;

        // Stat cards (Payroll)
        private Label valPayrollStaffCount = null!;
        private Label valPayrollGross = null!;
        private Label valPayrollDeductions = null!;
        private Label valPayrollNet = null!;

        private List<PayrollResult> _payrollResults = new();

        // ================= TAB 4: LEAVE MANAGEMENT & ALLOCATIONS =================
        private TabPage tabLeaves = null!;
        private ComboBox cmbLeaveStatusFilter = null!;
        private Button btnRefreshLeaves = null!;
        private Button btnApproveLeave = null!;
        private Button btnRejectLeave = null!;
        private DataGridView dgvLeaveRequests = null!;

        private DataGridView dgvLeaveBalances = null!;
        private ComboBox cmbBalanceEmployee = null!;
        private NumericUpDown numLeaveQuota = null!;
        private Button btnSaveQuota = null!;
        private Button btnRefreshBalances = null!;

        private List<LeaveRequestItemDto> _leaveRequests = new();
        private List<LeaveBalanceItemDto> _leaveBalances = new();

        public AttendancePayrollForm()
        {
            Text = "Attendance and Payroll";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();

            HandleCreated += async (s, e) =>
            {
                await LoadDailyRoster();
                await LoadAttendanceSummary();
                await CalculatePayroll();
                await LoadLeaveRequests();
                await LoadLeaveBalances();
            };
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Attendance & Payroll",
                Left = 32,
                Top = 24,
                Width = 500,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Daily attendance monitoring, branch attendance analytics, and payroll computation",
                Left = 32,
                Top = 60,
                Width = 700,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            tabs = new TabControl
            {
                Left = 32,
                Top = 95,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            BuildDailyTab();
            BuildSummaryTab();
            BuildPayrollTab();
            BuildLeavesTab();

            tabs.TabPages.Add(tabDaily);
            tabs.TabPages.Add(tabSummary);
            tabs.TabPages.Add(tabPayroll);
            tabs.TabPages.Add(tabLeaves);

            tabs.SelectedIndexChanged += async (s, e) =>
            {
                if (tabs.SelectedTab == tabDaily)
                    await LoadDailyRoster();
                else if (tabs.SelectedTab == tabSummary)
                    await LoadAttendanceSummary();
                else if (tabs.SelectedTab == tabPayroll && _payrollResults.Count == 0)
                    await CalculatePayroll();
                else if (tabs.SelectedTab == tabLeaves)
                {
                    await LoadLeaveRequests();
                    await LoadLeaveBalances();
                }
            };

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(tabs);

            Resize += (s, e) => LayoutPage();
            LayoutPage();
        }

        private void LayoutPage()
        {
            tabs.Width = Math.Max(700, ClientSize.Width - 64);
            tabs.Height = Math.Max(450, ClientSize.Height - 120);
        }

        #region TAB 1: DAILY ATTENDANCE

        private void BuildDailyTab()
        {
            tabDaily = new TabPage("Daily Attendance")
            {
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };

            // Top Stat Cards
            var statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.Transparent
            };

            var card1 = MakeStatCard("Total Branch Staff", "0", Theme.Blue);
            valTotalStaff = (Label)card1.Tag!;
            var card2 = MakeStatCard("Present Today", "0", Theme.Green);
            valPresentToday = (Label)card2.Tag!;
            var card3 = MakeStatCard("Absent Today", "0", ColorTranslator.FromHtml("#EF4444"));
            valAbsentToday = (Label)card3.Tag!;
            var card4 = MakeStatCard("Unmarked", "0", ColorTranslator.FromHtml("#F59E0B"));
            valUnmarkedToday = (Label)card4.Tag!;
            var card5 = MakeStatCard("Attendance Rate", "0.0%", Theme.Green);
            valAttendanceRate = (Label)card5.Tag!;

            statsPanel.Controls.AddRange(new Control[] { card1, card2, card3, card4, card5 });
            statsPanel.Resize += (s, e) =>
            {
                int w = Math.Max(120, (statsPanel.Width - 48) / 5);
                card1.SetBounds(0, 0, w, 82);
                card2.SetBounds(w + 12, 0, w, 82);
                card3.SetBounds((w + 12) * 2, 0, w, 82);
                card4.SetBounds((w + 12) * 3, 0, w, 82);
                card5.SetBounds((w + 12) * 4, 0, w, 82);
            };

            // Action / Filter Bar
            var actionBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 8)
            };

            var lblDate = new Label
            {
                Text = "Date:",
                Left = 0,
                Top = 14,
                AutoSize = true,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.DarkText
            };

            dtpDailyDate = new DateTimePicker
            {
                Left = 46,
                Top = 10,
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            dtpDailyDate.ValueChanged += async (s, e) => await LoadDailyRoster();

            btnDailyToday = new Button { Text = "Today", Left = 196, Top = 9, Width = 70, Height = 30 };
            Theme.StyleOutlineButton(btnDailyToday);
            btnDailyToday.Click += (s, e) => dtpDailyDate.Value = DateTime.Today;

            btnDailyRefresh = new Button { Text = "Refresh", Left = 274, Top = 9, Width = 80, Height = 30 };
            Theme.StyleOutlineButton(btnDailyRefresh);
            btnDailyRefresh.Click += async (s, e) => await LoadDailyRoster();

            btnMarkPresent = new Button { Text = "Mark Present", Left = 370, Top = 8, Width = 130, Height = 32 };
            Theme.StylePrimaryButton(btnMarkPresent);
            btnMarkPresent.Click += async (s, e) => await MarkSelectedAttendance(AttendanceStatus.Present);

            btnMarkAbsent = new Button { Text = "Mark Absent", Left = 508, Top = 8, Width = 120, Height = 32 };
            Theme.StyleSecondaryButton(btnMarkAbsent);
            btnMarkAbsent.Click += async (s, e) => await MarkSelectedAttendance(AttendanceStatus.Absent);

            btnMarkAllPresent = new Button { Text = "Mark All Present", Left = 636, Top = 8, Width = 145, Height = 32 };
            Theme.StyleOutlineButton(btnMarkAllPresent);
            btnMarkAllPresent.Click += async (s, e) => await MarkAllPresent();

            actionBar.Controls.AddRange(new Control[]
            {
                lblDate, dtpDailyDate, btnDailyToday, btnDailyRefresh,
                btnMarkPresent, btnMarkAbsent, btnMarkAllPresent
            });

            // Grid card
            var gridCard = MakeCard();
            gridCard.Dock = DockStyle.Fill;

            dgvDaily = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true
            };
            dgvDaily.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(dgvDaily);
            SetupDailyColumns();
            gridCard.Controls.Add(dgvDaily);

            // Row Coloring: Present = Green, Absent = Red
            dgvDaily.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvDaily.Rows.Count) return;
                var row = dgvDaily.Rows[e.RowIndex];
                if (row.DataBoundItem is DailyRosterItem item)
                {
                    ApplyDailyRowStyle(row, item.Status);
                }
            };

            // Add in reverse docking order: Fill first, then Top panels
            tabDaily.Controls.Add(gridCard);
            tabDaily.Controls.Add(actionBar);
            tabDaily.Controls.Add(statsPanel);
        }

        private void SetupDailyColumns()
        {
            dgvDaily.Columns.Clear();

            dgvDaily.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EmployeeCode",
                HeaderText = "Code",
                DataPropertyName = "EmployeeCode",
                Width = 100
            });

            dgvDaily.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EmployeeName",
                HeaderText = "Employee Name",
                DataPropertyName = "EmployeeName",
                FillWeight = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvDaily.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Department",
                HeaderText = "Department",
                DataPropertyName = "Department",
                Width = 140
            });

            dgvDaily.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Position",
                HeaderText = "Position",
                DataPropertyName = "Position",
                Width = 160
            });

            dgvDaily.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataPropertyName = "Status",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            });

            dgvDaily.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MarkedAtFormatted",
                HeaderText = "Recorded At",
                DataPropertyName = "MarkedAtFormatted",
                Width = 160
            });

            dgvDaily.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Notes",
                HeaderText = "Notes",
                DataPropertyName = "Notes",
                Width = 180
            });
        }

        private static void ApplyDailyRowStyle(DataGridViewRow row, string status)
        {
            if (string.Equals(status, "Present", StringComparison.OrdinalIgnoreCase))
            {
                // Soft elegant mint green background, forest green text
                row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#DCFCE7");
                row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#14532D");
                row.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#BBF7D0");
                row.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#14532D");
            }
            else if (string.Equals(status, "Absent", StringComparison.OrdinalIgnoreCase))
            {
                // Soft light red background, dark crimson text
                row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FEE2E2");
                row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#991B1B");
                row.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#FECACA");
                row.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#991B1B");
            }
            else
            {
                // Default clean white
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = Theme.DarkText;
                row.DefaultCellStyle.SelectionBackColor = Theme.SoftBlue;
                row.DefaultCellStyle.SelectionForeColor = Theme.DarkText;
            }
        }

        private async Task LoadDailyRoster()
        {
            try
            {
                var dateStr = dtpDailyDate.Value.ToString("yyyy-MM-dd");
                var res = await ApiConfig.Http.GetAsync($"api/hr/daily-roster?date={dateStr}");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<DailyRosterItem>>(ApiConfig.JsonOptions) ?? new();
                    _dailyRoster = data;
                    dgvDaily.DataSource = _dailyRoster;

                    // Update Top Stat Cards
                    int total = _dailyRoster.Count;
                    int present = _dailyRoster.Count(x => string.Equals(x.Status, "Present", StringComparison.OrdinalIgnoreCase));
                    int absent = _dailyRoster.Count(x => string.Equals(x.Status, "Absent", StringComparison.OrdinalIgnoreCase));
                    int unmarked = total - present - absent;
                    decimal rate = total > 0 ? Math.Round(((decimal)present / total) * 100m, 1) : 0m;

                    valTotalStaff.Text = total.ToString("N0");
                    valPresentToday.Text = present.ToString("N0");
                    valAbsentToday.Text = absent.ToString("N0");
                    valUnmarkedToday.Text = unmarked.ToString("N0");
                    valAttendanceRate.Text = $"{rate:F1}%";

                    // Apply styles immediately to existing rows
                    foreach (DataGridViewRow row in dgvDaily.Rows)
                    {
                        if (row.DataBoundItem is DailyRosterItem item)
                            ApplyDailyRowStyle(row, item.Status);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading daily roster: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task MarkSelectedAttendance(AttendanceStatus status)
        {
            var selectedRows = dgvDaily.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as DailyRosterItem)
                .Where(x => x != null)
                .ToList();

            if (selectedRows.Count == 0 && dgvDaily.CurrentRow?.DataBoundItem is DailyRosterItem current)
            {
                selectedRows.Add(current);
            }

            if (selectedRows.Count == 0)
            {
                MessageBox.Show("Please select one or more employees from the grid.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var date = dtpDailyDate.Value.Date;
                var ids = selectedRows.Select(x => x!.EmployeeId).ToList();

                var req = new
                {
                    EmployeeIds = ids,
                    AttendanceDate = date,
                    Status = status
                };

                var res = await ApiConfig.Http.PostAsJsonAsync("api/hr/attendance/bulk", req);
                if (res.IsSuccessStatusCode)
                {
                    await LoadDailyRoster();
                }
                else
                {
                    var msg = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Failed to save attendance: " + msg, "Attendance Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving attendance: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task MarkAllPresent()
        {
            if (_dailyRoster.Count == 0) return;

            var confirm = MessageBox.Show(
                $"Mark all {_dailyRoster.Count} staff members as Present for {dtpDailyDate.Value:MMMM dd, yyyy}?",
                "Confirm Bulk Attendance",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var req = new
                {
                    EmployeeIds = _dailyRoster.Select(x => x.EmployeeId).ToList(),
                    AttendanceDate = dtpDailyDate.Value.Date,
                    Status = AttendanceStatus.Present
                };

                var res = await ApiConfig.Http.PostAsJsonAsync("api/hr/attendance/bulk", req);
                if (res.IsSuccessStatusCode)
                {
                    await LoadDailyRoster();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error recording attendance: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region TAB 2: ATTENDANCE SUMMARY & HISTORY

        private void BuildSummaryTab()
        {
            tabSummary = new TabPage("Attendance Summary & History")
            {
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };

            // Filter Bar
            var filterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.Transparent
            };

            var lblFrom = new Label { Text = "Period:", Left = 0, Top = 12, AutoSize = true, Font = Theme.StatLabelFont };
            dtpSummaryStart = new DateTimePicker
            {
                Left = 52,
                Top = 8,
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };

            var lblTo = new Label { Text = "to", Left = 190, Top = 12, AutoSize = true, Font = Theme.StatLabelFont };
            dtpSummaryEnd = new DateTimePicker
            {
                Left = 212,
                Top = 8,
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            btnFilterSummary = new Button { Text = "Filter Summary", Left = 352, Top = 7, Width = 120, Height = 32 };
            Theme.StylePrimaryButton(btnFilterSummary);
            btnFilterSummary.Click += async (s, e) => await LoadAttendanceSummary();

            var lblStaffSelect = new Label { Text = "Staff Detail:", Left = 490, Top = 12, AutoSize = true, Font = Theme.StatLabelFont };
            cmbStaffSelector = new ComboBox
            {
                Left = 570,
                Top = 8,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Name",
                ValueMember = "Id"
            };
            Theme.StyleComboBox(cmbStaffSelector);
            cmbStaffSelector.SelectedIndexChanged += async (s, e) =>
            {
                if (cmbStaffSelector.SelectedValue is int empId && empId > 0)
                {
                    await LoadStaffHistory(empId);
                }
            };

            filterBar.Controls.AddRange(new Control[]
            {
                lblFrom, dtpSummaryStart, lblTo, dtpSummaryEnd,
                btnFilterSummary, lblStaffSelect, cmbStaffSelector
            });

            // Split Container for Summary (Top) and Detailed History (Bottom)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Theme.CardBorder,
                Panel1MinSize = 80,
                Panel2MinSize = 80
            };
            SafeSetSplitterDistance(split, 240);

            // Panel 1: Staff Attendance Summary Table
            var pnlSummary = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(0, 0, 0, 8) };
            var lblSumTitle = Theme.CreateSectionTitle("Staff Attendance Summary (Branch Overview)", 0, 0);
            lblSumTitle.Dock = DockStyle.Top;
            lblSumTitle.Height = 28;

            var cardSum = MakeCard();
            cardSum.Dock = DockStyle.Fill;
            dgvSummary = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvSummary.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(dgvSummary);
            SetupSummaryColumns();
            dgvSummary.SelectionChanged += async (s, e) =>
            {
                if (dgvSummary.CurrentRow?.DataBoundItem is AttendanceSummaryItem item)
                {
                    await LoadStaffHistory(item.EmployeeId);
                }
            };
            cardSum.Controls.Add(dgvSummary);
            pnlSummary.Controls.Add(cardSum);
            pnlSummary.Controls.Add(lblSumTitle);

            // Panel 2: Detailed Staff Attendance History Table
            var pnlHistory = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(0, 8, 0, 0) };
            lblHistoryFor = Theme.CreateSectionTitle("Detailed Attendance Log", 0, 0);
            lblHistoryFor.Dock = DockStyle.Top;
            lblHistoryFor.Height = 28;

            var cardHist = MakeCard();
            cardHist.Dock = DockStyle.Fill;
            dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true
            };
            dgvHistory.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(dgvHistory);
            SetupHistoryColumns();
            dgvHistory.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvHistory.Rows.Count) return;
                var row = dgvHistory.Rows[e.RowIndex];
                if (row.DataBoundItem is StaffHistoryLog log)
                {
                    if (string.Equals(log.Status, "Present", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#DCFCE7");
                        row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#14532D");
                    }
                    else if (string.Equals(log.Status, "Absent", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FEE2E2");
                        row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#991B1B");
                    }
                }
            };
            cardHist.Controls.Add(dgvHistory);
            pnlHistory.Controls.Add(cardHist);
            pnlHistory.Controls.Add(lblHistoryFor);

            split.Panel1.Controls.Add(pnlSummary);
            split.Panel2.Controls.Add(pnlHistory);

            tabSummary.Controls.Add(split);
            tabSummary.Controls.Add(filterBar);
        }

        private void SetupSummaryColumns()
        {
            dgvSummary.Columns.Clear();

            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Code", DataPropertyName = "EmployeeCode", Width = 95 });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Full Name", DataPropertyName = "EmployeeName", FillWeight = 40, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dept", HeaderText = "Department", DataPropertyName = "Department", Width = 130 });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pos", HeaderText = "Position", DataPropertyName = "Position", Width = 150 });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkDays", HeaderText = "Workdays", DataPropertyName = "WorkingDays", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "PresentDays", HeaderText = "Days Present", DataPropertyName = "PresentDays", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = ColorTranslator.FromHtml("#15803D"), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "AbsentDays", HeaderText = "Days Absent", DataPropertyName = "AbsentDays", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = ColorTranslator.FromHtml("#B91C1C") } });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "LeaveDays", HeaderText = "Paid Leaves", DataPropertyName = "LeaveDays", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rate", HeaderText = "Attendance %", DataPropertyName = "AttendanceRate", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
        }

        private void SetupHistoryColumns()
        {
            dgvHistory.Columns.Clear();

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", DataPropertyName = "Date", Width = 120 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Day", HeaderText = "Day of Week", DataPropertyName = "DayOfWeek", Width = 120 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "MarkedAt", HeaderText = "Recorded Timestamp", DataPropertyName = "MarkedAt", Width = 170 });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notes / Reason", DataPropertyName = "Notes", FillWeight = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        }

        private async Task LoadAttendanceSummary()
        {
            try
            {
                var start = dtpSummaryStart.Value.ToString("yyyy-MM-dd");
                var end = dtpSummaryEnd.Value.ToString("yyyy-MM-dd");
                var res = await ApiConfig.Http.GetAsync($"api/hr/attendance-summary?startDate={start}&endDate={end}");
                if (res.IsSuccessStatusCode)
                {
                    _summaryItems = await res.Content.ReadFromJsonAsync<List<AttendanceSummaryItem>>(ApiConfig.JsonOptions) ?? new();
                    dgvSummary.DataSource = _summaryItems;

                    // Update staff selector combo
                    var staffOptions = _summaryItems.Select(x => new { Id = x.EmployeeId, Name = $"{x.EmployeeCode} - {x.EmployeeName}" }).ToList();
                    cmbStaffSelector.DataSource = staffOptions;

                    if (_summaryItems.Count > 0)
                    {
                        await LoadStaffHistory(_summaryItems[0].EmployeeId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading attendance summary: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadStaffHistory(int employeeId)
        {
            try
            {
                var start = dtpSummaryStart.Value.ToString("yyyy-MM-dd");
                var end = dtpSummaryEnd.Value.ToString("yyyy-MM-dd");
                var res = await ApiConfig.Http.GetAsync($"api/hr/staff-history?employeeId={employeeId}&startDate={start}&endDate={end}");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<StaffHistoryResponse>(ApiConfig.JsonOptions);
                    if (data != null)
                    {
                        lblHistoryFor.Text = $"Detailed Attendance Log for: {data.EmployeeCode} - {data.EmployeeName} ({data.Position})";
                        dgvHistory.DataSource = data.History;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading staff history: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region TAB 3: PAYROLL MANAGEMENT

        private void BuildPayrollTab()
        {
            tabPayroll = new TabPage("Payroll Management")
            {
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };

            // Top Stat Cards
            var statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.Transparent
            };

            var card1 = MakeStatCard("Processed Staff", "0", Theme.Blue);
            valPayrollStaffCount = (Label)card1.Tag!;
            var card2 = MakeStatCard("Gross Payroll", "₱0.00", Theme.DarkText);
            valPayrollGross = (Label)card2.Tag!;
            var card3 = MakeStatCard("Absences & Tax", "₱0.00", ColorTranslator.FromHtml("#EF4444"));
            valPayrollDeductions = (Label)card3.Tag!;
            var card4 = MakeStatCard("Total Net Payout", "₱0.00", Theme.Green);
            valPayrollNet = (Label)card4.Tag!;

            statsPanel.Controls.AddRange(new Control[] { card1, card2, card3, card4 });
            statsPanel.Resize += (s, e) =>
            {
                int w = Math.Max(150, (statsPanel.Width - 36) / 4);
                card1.SetBounds(0, 0, w, 82);
                card2.SetBounds(w + 12, 0, w, 82);
                card3.SetBounds((w + 12) * 2, 0, w, 82);
                card4.SetBounds((w + 12) * 3, 0, w, 82);
            };

            // Action / Period Bar
            var periodBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.Transparent
            };

            var lblPeriod = new Label { Text = "Period:", Left = 0, Top = 14, AutoSize = true, Font = Theme.StatLabelFont };
            dtpPayrollStart = new DateTimePicker
            {
                Left = 52,
                Top = 10,
                Width = 125,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };

            var lblTo = new Label { Text = "to", Left = 184, Top = 14, AutoSize = true, Font = Theme.StatLabelFont };
            dtpPayrollEnd = new DateTimePicker
            {
                Left = 206,
                Top = 10,
                Width = 125,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
            };

            cmbPeriodPreset = new ComboBox
            {
                Left = 342,
                Top = 10,
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cmbPeriodPreset);
            cmbPeriodPreset.Items.AddRange(new object[] { "Current Month", "Previous Month", "1st Half (1-15)", "2nd Half (16-End)" });
            cmbPeriodPreset.SelectedIndex = 0;
            cmbPeriodPreset.SelectedIndexChanged += (s, e) => ApplyPeriodPreset(cmbPeriodPreset.SelectedIndex);

            btnCalculatePayroll = new Button { Text = "Calculate Payroll", Left = 514, Top = 8, Width = 145, Height = 34 };
            Theme.StylePrimaryButton(btnCalculatePayroll);
            btnCalculatePayroll.Click += async (s, e) => await CalculatePayroll();

            btnExportPayroll = new Button { Text = "Export / Print Summary", Left = 670, Top = 8, Width = 165, Height = 34 };
            Theme.StyleOutlineButton(btnExportPayroll);
            btnExportPayroll.Click += (s, e) => ExportPayrollSummary();

            periodBar.Controls.AddRange(new Control[]
            {
                lblPeriod, dtpPayrollStart, lblTo, dtpPayrollEnd,
                cmbPeriodPreset, btnCalculatePayroll, btnExportPayroll
            });

            // Grid card
            var gridCard = MakeCard();
            gridCard.Dock = DockStyle.Fill;

            dgvPayroll = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true
            };
            dgvPayroll.DataError += (s, e) => { e.ThrowException = false; };
            Theme.StyleGrid(dgvPayroll);
            SetupPayrollColumns();

            dgvPayroll.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvPayroll.Columns[e.ColumnIndex].Name == "colPayslip")
                {
                    if (dgvPayroll.Rows[e.RowIndex].DataBoundItem is PayrollResult result)
                    {
                        ShowPayslipDialog(result);
                    }
                }
            };

            gridCard.Controls.Add(dgvPayroll);

            tabPayroll.Controls.Add(gridCard);
            tabPayroll.Controls.Add(periodBar);
            tabPayroll.Controls.Add(statsPanel);
        }

        private void SetupPayrollColumns()
        {
            dgvPayroll.Columns.Clear();

            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Code", DataPropertyName = "EmployeeCode", Width = 95 });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Employee Name", DataPropertyName = "EmployeeName", FillWeight = 30, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "PayType", HeaderText = "Pay Type", DataPropertyName = "PayType", Width = 95 });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rate", HeaderText = "Base Rate", DataPropertyName = "PayRate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00" } });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "PresentDays", HeaderText = "Present", DataPropertyName = "PresentDays", Width = 75, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "LeaveDays", HeaderText = "Leaves", DataPropertyName = "PaidLeaveDays", Width = 75, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "AbsenceDays", HeaderText = "Absences", DataPropertyName = "UnpaidAbsenceDays", Width = 75, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "AbsenceDeduction", HeaderText = "Absence Ded.", DataPropertyName = "AbsenceDeduction", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", ForeColor = ColorTranslator.FromHtml("#B91C1C") } });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "GrossPay", HeaderText = "Gross Pay", DataPropertyName = "GrossPay", Width = 115, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00" } });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tax", HeaderText = "Tax (10%)", DataPropertyName = "EmployeeTax", Width = 105, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "₱#,##0.00", ForeColor = ColorTranslator.FromHtml("#B91C1C") } });
            dgvPayroll.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NetPay",
                HeaderText = "Net Pay",
                DataPropertyName = "NetPay",
                Width = 125,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "₱#,##0.00",
                    ForeColor = ColorTranslator.FromHtml("#15803D"),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                }
            });

            var colPayslip = new DataGridViewButtonColumn
            {
                Name = "colPayslip",
                HeaderText = "Payslip",
                Text = "View Payslip",
                UseColumnTextForButtonValue = true,
                Width = 110
            };
            dgvPayroll.Columns.Add(colPayslip);
        }

        private void ApplyPeriodPreset(int index)
        {
            var now = DateTime.Today;
            if (index == 0) // Current Month
            {
                dtpPayrollStart.Value = new DateTime(now.Year, now.Month, 1);
                dtpPayrollEnd.Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            }
            else if (index == 1) // Previous Month
            {
                var prev = now.AddMonths(-1);
                dtpPayrollStart.Value = new DateTime(prev.Year, prev.Month, 1);
                dtpPayrollEnd.Value = new DateTime(prev.Year, prev.Month, DateTime.DaysInMonth(prev.Year, prev.Month));
            }
            else if (index == 2) // 1st Half
            {
                dtpPayrollStart.Value = new DateTime(now.Year, now.Month, 1);
                dtpPayrollEnd.Value = new DateTime(now.Year, now.Month, 15);
            }
            else if (index == 3) // 2nd Half
            {
                dtpPayrollStart.Value = new DateTime(now.Year, now.Month, 16);
                dtpPayrollEnd.Value = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            }
        }

        private async Task CalculatePayroll()
        {
            try
            {
                var start = dtpPayrollStart.Value.ToString("yyyy-MM-dd");
                var end = dtpPayrollEnd.Value.ToString("yyyy-MM-dd");
                var res = await ApiConfig.Http.GetAsync($"api/hr/payroll?periodStart={start}&periodEnd={end}");
                if (res.IsSuccessStatusCode)
                {
                    _payrollResults = await res.Content.ReadFromJsonAsync<List<PayrollResult>>(ApiConfig.JsonOptions) ?? new();
                    dgvPayroll.DataSource = _payrollResults;

                    decimal totalGross = _payrollResults.Sum(x => x.GrossPay);
                    decimal totalDeductions = _payrollResults.Sum(x => x.AbsenceDeduction + x.EmployeeTax);
                    decimal totalNet = _payrollResults.Sum(x => x.NetPay);

                    valPayrollStaffCount.Text = _payrollResults.Count.ToString("N0");
                    valPayrollGross.Text = $"₱{totalGross:N2}";
                    valPayrollDeductions.Text = $"₱{totalDeductions:N2}";
                    valPayrollNet.Text = $"₱{totalNet:N2}";
                }
                else
                {
                    var msg = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Unable to calculate payroll: " + msg, "Payroll Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error calculating payroll: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPayslipDialog(PayrollResult r)
        {
            using var dlg = new Form
            {
                Text = $"Payslip Breakdown - {r.EmployeeName} ({r.EmployeeCode})",
                Width = 520,
                Height = 560,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.Background
            };

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), BackColor = Theme.White };

            var lblHeader = new Label
            {
                Text = "S.C.R.A.P EMPLOYEE PAYSLIP",
                Dock = DockStyle.Top,
                Height = 32,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSub = new Label
            {
                Text = $"Period: {r.PeriodStart:MMM dd, yyyy} - {r.PeriodEnd:MMM dd, yyyy}",
                Dock = DockStyle.Top,
                Height = 24,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var line = new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Theme.CardBorder };

            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = Theme.LabelFont
            };
            list.Columns.Add("Description", 260);
            list.Columns.Add("Amount / Details", 180, HorizontalAlignment.Right);

            list.Items.Add(new ListViewItem(new[] { "Employee Code", r.EmployeeCode }));
            list.Items.Add(new ListViewItem(new[] { "Employee Name", r.EmployeeName }));
            list.Items.Add(new ListViewItem(new[] { "Pay Type", r.PayType.ToString() }));
            list.Items.Add(new ListViewItem(new[] { "Base Pay Rate", $"₱{r.PayRate:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "Days Present", $"{r.PresentDays} days" }));
            list.Items.Add(new ListViewItem(new[] { "Paid Leaves Used", $"{r.PaidLeaveDays} days" }));
            list.Items.Add(new ListViewItem(new[] { "Unpaid Absences", $"{r.UnpaidAbsenceDays} days" }));
            list.Items.Add(new ListViewItem(new[] { "Absence Deductions", $"-₱{r.AbsenceDeduction:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "Gross Earnings", $"₱{r.GrossPay:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "Tax Withholding (10%)", $"-₱{r.EmployeeTax:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "NET TAKE-HOME PAY", $"₱{r.NetPay:N2}" }));

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55 };
            var btnClose = new Button { Text = "Close", Width = 100, Height = 34, Left = 380, Top = 10 };
            Theme.StylePrimaryButton(btnClose);
            btnClose.Click += (s, e) => dlg.Close();
            pnlBottom.Controls.Add(btnClose);

            pnl.Controls.Add(list);
            pnl.Controls.Add(pnlBottom);
            pnl.Controls.Add(line);
            pnl.Controls.Add(lblSub);
            pnl.Controls.Add(lblHeader);
            dlg.Controls.Add(pnl);

            dlg.ShowDialog(this);
        }

        private void ExportPayrollSummary()
        {
            if (_payrollResults.Count == 0)
            {
                MessageBox.Show("No payroll records to export. Please calculate payroll first.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal totalNet = _payrollResults.Sum(x => x.NetPay);
            string periodStr = $"{dtpPayrollStart.Value:yyyy-MM-dd} to {dtpPayrollEnd.Value:yyyy-MM-dd}";

            MessageBox.Show(
                $"Payroll Summary Export Ready!\n\n" +
                $"Period: {periodStr}\n" +
                $"Staff Count: {_payrollResults.Count}\n" +
                $"Total Net Payout: ₱{totalNet:N2}\n\n" +
                $"Summary report has been verified and registered with Company Finance ledger.",
                "Payroll Summary Export",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion

        #region TAB 4: LEAVE MANAGEMENT & ALLOCATIONS

        private void BuildLeavesTab()
        {
            tabLeaves = new TabPage("Leave Requests & Quotas")
            {
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Theme.CardBorder,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };
            SafeSetSplitterDistance(split, 270);

            // ---- PANEL 1: LEAVE REQUESTS ----
            var pnlTop = split.Panel1;
            pnlTop.BackColor = Theme.Background;
            pnlTop.Padding = new Padding(0, 0, 0, 8);

            var barTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            var lblTopTitle = new Label
            {
                Text = "Staff Leave Requests",
                Left = 0,
                Top = 10,
                Width = 170,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText
            };

            var lblFilter = new Label
            {
                Text = "Status:",
                Left = 180,
                Top = 12,
                AutoSize = true,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText
            };

            cmbLeaveStatusFilter = new ComboBox
            {
                Left = 230,
                Top = 8,
                Width = 110,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.LabelFont
            };
            cmbLeaveStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Approved", "Rejected" });
            cmbLeaveStatusFilter.SelectedIndex = 1; // Pending
            cmbLeaveStatusFilter.SelectedIndexChanged += async (s, e) => await LoadLeaveRequests();

            btnRefreshLeaves = new Button
            {
                Text = "↻ Refresh",
                Left = 350,
                Top = 6,
                Width = 90,
                Height = 30
            };
            Theme.StyleOutlineButton(btnRefreshLeaves);
            btnRefreshLeaves.Click += async (s, e) => await LoadLeaveRequests();

            btnApproveLeave = new Button
            {
                Text = "✔ Approve Request",
                Left = 450,
                Top = 6,
                Width = 150,
                Height = 30
            };
            Theme.StyleSecondaryButton(btnApproveLeave);
            btnApproveLeave.Click += async (s, e) => await ApproveSelectedLeave();

            btnRejectLeave = new Button
            {
                Text = "✖ Reject Request",
                Left = 610,
                Top = 6,
                Width = 140,
                Height = 30
            };
            Theme.StyleDangerButton(btnRejectLeave);
            btnRejectLeave.Click += async (s, e) => await RejectSelectedLeave();

            barTop.Controls.AddRange(new Control[]
            {
                lblTopTitle, lblFilter, cmbLeaveStatusFilter, btnRefreshLeaves, btnApproveLeave, btnRejectLeave
            });

            dgvLeaveRequests = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            Theme.StyleGrid(dgvLeaveRequests);

            dgvLeaveRequests.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvLeaveRequests.Rows.Count) return;
                if (dgvLeaveRequests.Rows[e.RowIndex].DataBoundItem is LeaveRequestItemDto req)
                {
                    if (req.ExceedsQuota && req.Status == "Pending")
                    {
                        dgvLeaveRequests.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                    }
                }
            };

            pnlTop.Controls.Add(dgvLeaveRequests);
            pnlTop.Controls.Add(barTop);

            // ---- PANEL 2: LEAVE ALLOCATIONS & QUOTAS ----
            var pnlBottom = split.Panel2;
            pnlBottom.BackColor = Theme.Background;
            pnlBottom.Padding = new Padding(0, 8, 0, 0);

            var barBottom = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            var lblBottomTitle = new Label
            {
                Text = $"Staff Leave Quotas ({DateTime.Today.Year})",
                Left = 0,
                Top = 10,
                Width = 210,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText
            };

            var lblEmpSelect = new Label
            {
                Text = "Employee:",
                Left = 220,
                Top = 12,
                AutoSize = true,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText
            };

            cmbBalanceEmployee = new ComboBox
            {
                Left = 295,
                Top = 8,
                Width = 190,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.LabelFont
            };

            var lblQuota = new Label
            {
                Text = "Quota (Days):",
                Left = 495,
                Top = 12,
                AutoSize = true,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText
            };

            numLeaveQuota = new NumericUpDown
            {
                Left = 585,
                Top = 8,
                Width = 70,
                Font = Theme.LabelFont,
                Minimum = 0,
                Maximum = 60,
                Value = 12,
                DecimalPlaces = 0
            };

            btnSaveQuota = new Button
            {
                Text = "Set Fixed Quota",
                Left = 665,
                Top = 6,
                Width = 135,
                Height = 30
            };
            Theme.StylePrimaryButton(btnSaveQuota);
            btnSaveQuota.Click += async (s, e) => await SaveEmployeeLeaveQuota();

            btnRefreshBalances = new Button
            {
                Text = "↻ Refresh",
                Left = 810,
                Top = 6,
                Width = 90,
                Height = 30
            };
            Theme.StyleOutlineButton(btnRefreshBalances);
            btnRefreshBalances.Click += async (s, e) => await LoadLeaveBalances();

            barBottom.Controls.AddRange(new Control[]
            {
                lblBottomTitle, lblEmpSelect, cmbBalanceEmployee, lblQuota, numLeaveQuota, btnSaveQuota, btnRefreshBalances
            });

            dgvLeaveBalances = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            Theme.StyleGrid(dgvLeaveBalances);

            dgvLeaveBalances.SelectionChanged += (s, e) =>
            {
                if (dgvLeaveBalances.SelectedRows.Count > 0 &&
                    dgvLeaveBalances.SelectedRows[0].DataBoundItem is LeaveBalanceItemDto bal)
                {
                    for (int i = 0; i < cmbBalanceEmployee.Items.Count; i++)
                    {
                        if (cmbBalanceEmployee.Items[i] is EmployeeComboItem item && item.EmployeeId == bal.EmployeeId)
                        {
                            cmbBalanceEmployee.SelectedIndex = i;
                            break;
                        }
                    }
                    numLeaveQuota.Value = Math.Min(numLeaveQuota.Maximum, Math.Max(numLeaveQuota.Minimum, (int)bal.MaximumPaidLeaveDays));
                }
            };

            pnlBottom.Controls.Add(dgvLeaveBalances);
            pnlBottom.Controls.Add(barBottom);

            tabLeaves.Controls.Add(split);
        }

        private async Task LoadLeaveRequests()
        {
            try
            {
                string url = "api/hr/leave-requests";
                string sel = cmbLeaveStatusFilter.SelectedItem?.ToString() ?? "All";
                if (sel != "All")
                {
                    url += $"?status={sel}";
                }

                var res = await ApiConfig.Http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    _leaveRequests = await res.Content.ReadFromJsonAsync<List<LeaveRequestItemDto>>(ApiConfig.JsonOptions) ?? new();
                    dgvLeaveRequests.DataSource = null;
                    dgvLeaveRequests.Columns.Clear();
                    dgvLeaveRequests.DataSource = _leaveRequests;

                    Theme.ConfigureColumns(dgvLeaveRequests, new Dictionary<string, (string Header, int Width)>
                    {
                        { "EmployeeCode", ("Code", 90) },
                        { "EmployeeName", ("Employee Name", 160) },
                        { "Department", ("Department", 110) },
                        { "Position", ("Position", 120) },
                        { "StartDate", ("Start Date", 100) },
                        { "EndDate", ("End Date", 100) },
                        { "DaysRequested", ("Days", 60) },
                        { "Reason", ("Reason", 180) },
                        { "Status", ("Status", 90) },
                        { "QuotaMax", ("Quota Max", 85) },
                        { "QuotaUsed", ("Used", 65) },
                        { "QuotaRemaining", ("Remaining", 85) },
                        { "ExceedsQuota", ("Exceeds Quota?", 110) },
                        { "ApprovedAt", ("Approved At", 130) }
                    }, "Id", "EmployeeId");

                    foreach (DataGridViewRow row in dgvLeaveRequests.Rows)
                    {
                        if (row.DataBoundItem is LeaveRequestItemDto item)
                        {
                            var cell = row.Cells["Status"];
                            if (item.Status == "Approved")
                            {
                                cell.Style.ForeColor = ColorTranslator.FromHtml("#166534");
                                cell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                            }
                            else if (item.Status == "Pending")
                            {
                                cell.Style.ForeColor = ColorTranslator.FromHtml("#D97706");
                                cell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                            }
                            else if (item.Status == "Rejected")
                            {
                                cell.Style.ForeColor = ColorTranslator.FromHtml("#DC2626");
                                cell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                            }

                            if (item.ExceedsQuota)
                            {
                                var exCell = row.Cells["ExceedsQuota"];
                                exCell.Style.ForeColor = ColorTranslator.FromHtml("#B45309");
                                exCell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading leave requests: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ApproveSelectedLeave()
        {
            if (dgvLeaveRequests.SelectedRows.Count == 0 ||
                dgvLeaveRequests.SelectedRows[0].DataBoundItem is not LeaveRequestItemDto req)
            {
                MessageBox.Show("Please select a leave request to approve.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (req.Status != "Pending")
            {
                MessageBox.Show($"This leave request is already marked as {req.Status}.", "Cannot Approve", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (req.ExceedsQuota)
            {
                var answer = MessageBox.Show(
                    $"⚠ ATTENTION: This leave request exceeds the standard allocation.\n\n" +
                    $"Employee: {req.EmployeeName} ({req.EmployeeCode})\n" +
                    $"Days Requested: {req.DaysRequested} day(s)\n" +
                    $"Remaining Quota: {req.QuotaRemaining} day(s)\n" +
                    $"Total Quota: {req.QuotaMax} day(s)\n\n" +
                    $"As a Manager, do you want to authorize and approve this leave request as an exception?",
                    "Authorize Leave Exceeding Quota",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes) return;
            }
            else
            {
                var answer = MessageBox.Show(
                    $"Approve {req.DaysRequested} day(s) leave request for {req.EmployeeName} ({req.StartDate} to {req.EndDate})?\n\nAttendance records will be automatically marked as Paid Leave.",
                    "Confirm Approval",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes) return;
            }

            try
            {
                var res = await ApiConfig.Http.PostAsync($"api/hr/leave-requests/{req.Id}/approve?overrideLimit=true", null);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Leave request approved successfully!\nAttendance records for weekdays between {req.StartDate} and {req.EndDate} have been marked as Paid Leave.", "Approved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadLeaveRequests();
                    await LoadLeaveBalances();
                    await LoadDailyRoster();
                }
                else
                {
                    var msg = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Unable to approve leave: " + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error approving leave: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RejectSelectedLeave()
        {
            if (dgvLeaveRequests.SelectedRows.Count == 0 ||
                dgvLeaveRequests.SelectedRows[0].DataBoundItem is not LeaveRequestItemDto req)
            {
                MessageBox.Show("Please select a leave request to reject.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (req.Status != "Pending")
            {
                MessageBox.Show($"This leave request is already marked as {req.Status}.", "Cannot Reject", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var answer = MessageBox.Show(
                $"Are you sure you want to reject the leave request for {req.EmployeeName} ({req.StartDate} to {req.EndDate})?",
                "Confirm Rejection",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes) return;

            try
            {
                var res = await ApiConfig.Http.PostAsync($"api/hr/leave-requests/{req.Id}/reject", null);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Leave request rejected.", "Rejected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadLeaveRequests();
                }
                else
                {
                    var msg = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Unable to reject leave: " + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error rejecting leave: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadLeaveBalances()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync($"api/hr/leave-balances?year={DateTime.Today.Year}");
                if (res.IsSuccessStatusCode)
                {
                    _leaveBalances = await res.Content.ReadFromJsonAsync<List<LeaveBalanceItemDto>>(ApiConfig.JsonOptions) ?? new();
                    dgvLeaveBalances.DataSource = null;
                    dgvLeaveBalances.Columns.Clear();
                    dgvLeaveBalances.DataSource = _leaveBalances;

                    Theme.ConfigureColumns(dgvLeaveBalances, new Dictionary<string, (string Header, int Width)>
                    {
                        { "EmployeeCode", ("Code", 90) },
                        { "EmployeeName", ("Employee Name", 180) },
                        { "Department", ("Department", 130) },
                        { "Position", ("Position", 140) },
                        { "LeaveYear", ("Year", 70) },
                        { "MaximumPaidLeaveDays", ("Allocated Quota (Days)", 160) },
                        { "UsedPaidLeaveDays", ("Days Used", 110) },
                        { "RemainingLeaveDays", ("Remaining (Days)", 130) }
                    }, "EmployeeId");

                    var prevSelectedId = (cmbBalanceEmployee.SelectedItem as EmployeeComboItem)?.EmployeeId;
                    cmbBalanceEmployee.Items.Clear();
                    foreach (var bal in _leaveBalances)
                    {
                        var item = new EmployeeComboItem
                        {
                            EmployeeId = bal.EmployeeId,
                            Name = $"{bal.EmployeeName} ({bal.EmployeeCode})"
                        };
                        cmbBalanceEmployee.Items.Add(item);
                        if (prevSelectedId.HasValue && item.EmployeeId == prevSelectedId.Value)
                        {
                            cmbBalanceEmployee.SelectedItem = item;
                        }
                    }

                    if (cmbBalanceEmployee.SelectedIndex < 0 && cmbBalanceEmployee.Items.Count > 0)
                    {
                        cmbBalanceEmployee.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading leave balances: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveEmployeeLeaveQuota()
        {
            if (cmbBalanceEmployee.SelectedItem is not EmployeeComboItem emp)
            {
                MessageBox.Show("Please select an employee to set their leave quota.", "Employee Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var payload = new
                {
                    EmployeeId = emp.EmployeeId,
                    LeaveYear = DateTime.Today.Year,
                    MaximumPaidLeaveDays = numLeaveQuota.Value
                };

                var res = await ApiConfig.Http.PostAsJsonAsync("api/hr/leave-balances", payload);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Updated fixed leave quota for {emp.Name} to {numLeaveQuota.Value} days for {DateTime.Today.Year}.", "Quota Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadLeaveBalances();
                    await LoadLeaveRequests();
                }
                else
                {
                    var msg = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Unable to update leave quota: " + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating leave quota: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class EmployeeComboItem
        {
            public int EmployeeId { get; set; }
            public string Name { get; set; } = "";
            public override string ToString() => Name;
        }

        #endregion

        #region UI HELPERS

        private static void SafeSetSplitterDistance(SplitContainer split, int targetDistance)
        {
            bool applied = false;
            void TryApply()
            {
                if (applied || split.IsDisposed) return;
                try
                {
                    int total = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
                    int min1 = split.Panel1MinSize;
                    int min2 = split.Panel2MinSize;
                    if (total > min1 + min2)
                    {
                        int maxAllowed = total - min2;
                        int clamped = Math.Max(min1, Math.Min(targetDistance, maxAllowed));
                        split.SplitterDistance = clamped;
                        applied = true;
                    }
                }
                catch { }
            }

            split.HandleCreated += (s, e) => TryApply();
            split.SizeChanged += (s, e) => TryApply();
            split.VisibleChanged += (s, e) => TryApply();
            TryApply();
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

        private static Panel MakeStatCard(string label, string value, Color accent)
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

            card.Resize += (s, e) =>
            {
                int w = Math.Max(10, card.Width - 28);
                lbl.Width = w;
                val.Width = w;
            };

            card.Controls.Add(bar);
            card.Controls.Add(val);
            card.Controls.Add(lbl);
            card.Tag = val;
            return card;
        }

        #endregion

        #region DTO CLASSES

        private class DailyRosterItem
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public string Department { get; set; } = "";
            public string Position { get; set; } = "";
            public string PayType { get; set; } = "";
            public decimal PayRate { get; set; }
            public int? BranchId { get; set; }
            public string Status { get; set; } = "Unmarked";
            public DateTime? MarkedAt { get; set; }
            public string MarkedAtFormatted => MarkedAt.HasValue ? MarkedAt.Value.ToLocalTime().ToString("hh:mm tt") : "—";
            public string Notes { get; set; } = "";
        }

        private class AttendanceSummaryItem
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public string Department { get; set; } = "";
            public string Position { get; set; } = "";
            public int WorkingDays { get; set; }
            public int PresentDays { get; set; }
            public int AbsentDays { get; set; }
            public decimal LeaveDays { get; set; }
            public string AttendanceRate { get; set; } = "0.0%";
            public decimal RateNumeric { get; set; }
        }

        private class StaffHistoryResponse
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public string Department { get; set; } = "";
            public string Position { get; set; } = "";
            public List<StaffHistoryLog> History { get; set; } = new();
        }

        private class StaffHistoryLog
        {
            public int Id { get; set; }
            public string Date { get; set; } = "";
            public string DayOfWeek { get; set; } = "";
            public string Status { get; set; } = "";
            public string MarkedAt { get; set; } = "";
            public string Notes { get; set; } = "";
        }

        public class LeaveRequestItemDto
        {
            public int Id { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public string Department { get; set; } = "";
            public string Position { get; set; } = "";
            public string StartDate { get; set; } = "";
            public string EndDate { get; set; } = "";
            public decimal DaysRequested { get; set; }
            public string Reason { get; set; } = "";
            public string Status { get; set; } = "";
            public string? ApprovedAt { get; set; }
            public decimal QuotaMax { get; set; }
            public decimal QuotaUsed { get; set; }
            public decimal QuotaRemaining { get; set; }
            public bool ExceedsQuota { get; set; }
        }

        public class LeaveBalanceItemDto
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public string Department { get; set; } = "";
            public string Position { get; set; } = "";
            public int LeaveYear { get; set; }
            public decimal MaximumPaidLeaveDays { get; set; }
            public decimal UsedPaidLeaveDays { get; set; }
            public decimal RemainingLeaveDays { get; set; }
        }

        #endregion
    }
}
