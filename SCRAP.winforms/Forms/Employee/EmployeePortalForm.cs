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
    public sealed class EmployeePortalForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblProfileBadge = null!;
        private Label lblTodayStatus = null!;
        private TabControl tabs = null!;

        // Stat cards
        private Label valAvailableLeaves = null!;
        private Label valLeavesUsed = null!;
        private Label valPresentThisMonth = null!;
        private Label valBaseRate = null!;

        // Tab 1: Request Leave
        private TabPage tabLeave = null!;
        private DateTimePicker dtpLeaveStart = null!;
        private DateTimePicker dtpLeaveEnd = null!;
        private TextBox txtLeaveReason = null!;
        private Label lblLeaveDaysCalc = null!;
        private Button btnSubmitLeave = null!;
        private Button btnRefreshLeaves = null!;
        private DataGridView dgvLeaveRequests = null!;

        // Tab 2: Attendance History
        private TabPage tabAttendance = null!;
        private Button btnRefreshAttendance = null!;
        private DataGridView dgvAttendance = null!;

        // Tab 3: Salary History
        private TabPage tabSalary = null!;
        private Button btnRefreshSalary = null!;
        private DataGridView dgvSalary = null!;

        private PortalResponse? _portalData;

        public EmployeePortalForm()
        {
            Text = "My Employee Portal";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();

            HandleCreated += async (s, e) =>
            {
                await LoadPortalData();
            };
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "My Employee Portal",
                Left = 32,
                Top = 18,
                Width = 450,
                Height = 38,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            lblSubtitle = new Label
            {
                Text = "Personal attendance, leave request management, and monthly salary history",
                Left = 32,
                Top = 58,
                Width = 650,
                Height = 24,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            lblProfileBadge = new Label
            {
                Text = "Loading profile...",
                Top = 20,
                Height = 26,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.Blue,
                BackColor = Theme.SoftBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(10, 4, 10, 4),
                AutoSize = true
            };

            lblTodayStatus = new Label
            {
                Text = "• Checking today's attendance...",
                Top = 50,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#166534"),
                BackColor = Theme.SoftGreen,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true
            };

            tabs = new TabControl
            {
                Left = 32,
                Top = 192,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            BuildLeaveTab();
            BuildAttendanceTab();
            BuildSalaryTab();

            tabs.TabPages.Add(tabLeave);
            tabs.TabPages.Add(tabAttendance);
            tabs.TabPages.Add(tabSalary);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(lblProfileBadge);
            Controls.Add(lblTodayStatus);
            Controls.Add(tabs);

            BuildStatCards();

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private Panel statsPanel = null!;
        private Panel cardAvailable = null!;
        private Panel cardUsed = null!;
        private Panel cardPresent = null!;
        private Panel cardBaseRate = null!;

        private void BuildStatCards()
        {
            statsPanel = new Panel
            {
                Left = 32,
                Top = 88,
                Height = 88,
                BackColor = Color.Transparent
            };

            cardAvailable = MakeStatCard("Available Leaves", "0 Days", Theme.Green);
            valAvailableLeaves = (Label)cardAvailable.Tag!;

            cardUsed = MakeStatCard("Leaves Taken", "0 / 0 Days", Theme.Blue);
            valLeavesUsed = (Label)cardUsed.Tag!;

            cardPresent = MakeStatCard("Present This Month", "0 Days", ColorTranslator.FromHtml("#0D9488"));
            valPresentThisMonth = (Label)cardPresent.Tag!;

            cardBaseRate = MakeStatCard("Base Salary Rate", "₱0.00", ColorTranslator.FromHtml("#6366F1"));
            valBaseRate = (Label)cardBaseRate.Tag!;

            statsPanel.Controls.AddRange(new Control[] { cardAvailable, cardUsed, cardPresent, cardBaseRate });
            Controls.Add(statsPanel);
        }

        private void LayoutControls()
        {
            if (tabs == null) return;
            tabs.Width = Math.Max(700, ClientSize.Width - 64);
            tabs.Height = Math.Max(400, ClientSize.Height - 208);

            if (statsPanel != null)
            {
                statsPanel.Width = tabs.Width;
                int cardWidth = Math.Max(120, (statsPanel.Width - 36) / 4);
                cardAvailable.SetBounds(0, 0, cardWidth, 84);
                cardUsed.SetBounds(cardWidth + 12, 0, cardWidth, 84);
                cardPresent.SetBounds((cardWidth + 12) * 2, 0, cardWidth, 84);
                cardBaseRate.SetBounds((cardWidth + 12) * 3, 0, cardWidth, 84);
            }

            lblProfileBadge.Left = Math.Max(480, ClientSize.Width - lblProfileBadge.Width - 32);
            lblTodayStatus.Left = Math.Max(480, ClientSize.Width - lblTodayStatus.Width - 32);
        }

        #region TAB 1: LEAVE REQUESTS

        private void BuildLeaveTab()
        {
            tabLeave = new TabPage("Leave Requests")
            {
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };

            // Request form panel
            var formPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 175,
                BackColor = Theme.White,
                Padding = new Padding(16)
            };
            formPanel.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, formPanel.Width - 1, formPanel.Height - 1);
            };

            var lblFormTitle = new Label
            {
                Text = "Apply for Paid Leave",
                Left = 16,
                Top = 14,
                Width = 300,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText,
                AutoSize = true
            };

            var lblStart = new Label
            {
                Text = "Start Date:",
                Left = 16,
                Top = 50,
                AutoSize = true,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText,
                UseMnemonic = false
            };

            dtpLeaveStart = new DateTimePicker
            {
                Left = 95,
                Top = 46,
                Width = 135,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(1)
            };

            var lblEnd = new Label
            {
                Text = "End Date:",
                Left = 248,
                Top = 50,
                AutoSize = true,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText,
                UseMnemonic = false
            };

            dtpLeaveEnd = new DateTimePicker
            {
                Left = 320,
                Top = 46,
                Width = 135,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(1)
            };

            lblLeaveDaysCalc = new Label
            {
                Text = "Duration: 1 working day(s)",
                Left = 475,
                Top = 50,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.Blue,
                UseMnemonic = false
            };

            var lblReason = new Label
            {
                Text = "Reason:",
                Left = 16,
                Top = 88,
                AutoSize = true,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText,
                UseMnemonic = false
            };

            txtLeaveReason = new TextBox
            {
                Left = 95,
                Top = 84,
                Width = 360,
                Font = Theme.LabelFont,
                PlaceholderText = "e.g., Vacation, Medical appointment, Family emergency"
            };

            btnSubmitLeave = new Button
            {
                Text = "Submit Leave Request",
                Left = 475,
                Top = 80,
                Width = 200,
                Height = 34
            };
            Theme.StylePrimaryButton(btnSubmitLeave);
            btnSubmitLeave.Click += async (s, e) => await SubmitLeaveRequest();

            var lblNote = new Label
            {
                Text = "ℹ Standard leave balance will be verified upon review. If your request exceeds available quota, your manager may still authorize and approve it as an exception.",
                Left = 16,
                Top = 126,
                Width = 800,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.MutedText,
                AutoSize = true
            };

            EventHandler recalcDays = (s, e) =>
            {
                if (dtpLeaveEnd.Value.Date < dtpLeaveStart.Value.Date)
                {
                    lblLeaveDaysCalc.Text = "End date is before start date!";
                    lblLeaveDaysCalc.ForeColor = ColorTranslator.FromHtml("#DC2626");
                }
                else
                {
                    int wDays = CountWeekdays(dtpLeaveStart.Value.Date, dtpLeaveEnd.Value.Date);
                    lblLeaveDaysCalc.Text = $"Duration: {wDays} working day(s)";
                    lblLeaveDaysCalc.ForeColor = Theme.Blue;
                }
            };
            dtpLeaveStart.ValueChanged += recalcDays;
            dtpLeaveEnd.ValueChanged += recalcDays;

            formPanel.Controls.AddRange(new Control[]
            {
                lblFormTitle, lblStart, dtpLeaveStart, lblEnd, dtpLeaveEnd, lblLeaveDaysCalc,
                lblReason, txtLeaveReason, btnSubmitLeave, lblNote
            });

            // Grid header bar
            var barHistory = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 4)
            };

            var lblTableTitle = new Label
            {
                Text = "My Leave Requests History",
                Left = 0,
                Top = 12,
                Width = 300,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText
            };

            btnRefreshLeaves = new Button
            {
                Text = "↻ Refresh",
                Top = 6,
                Width = 100,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefreshLeaves);
            btnRefreshLeaves.Click += async (s, e) => await LoadPortalData();

            barHistory.Controls.Add(lblTableTitle);
            barHistory.Controls.Add(btnRefreshLeaves);
            btnRefreshLeaves.Left = Math.Max(300, barHistory.ClientSize.Width - btnRefreshLeaves.Width - 8);

            // Grid
            dgvLeaveRequests = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            Theme.StyleGrid(dgvLeaveRequests);

            tabLeave.Controls.Add(dgvLeaveRequests);
            tabLeave.Controls.Add(barHistory);
            tabLeave.Controls.Add(formPanel);
        }

        private async Task SubmitLeaveRequest()
        {
            if (dtpLeaveEnd.Value.Date < dtpLeaveStart.Value.Date)
            {
                MessageBox.Show("Leave end date cannot be earlier than start date.", "Invalid Dates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int wDays = CountWeekdays(dtpLeaveStart.Value.Date, dtpLeaveEnd.Value.Date);
            if (wDays <= 0)
            {
                MessageBox.Show("The selected date range contains no weekdays (working days).", "Invalid Dates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string reason = txtLeaveReason.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Please enter a reason for the leave request.", "Reason Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLeaveReason.Focus();
                return;
            }

            btnSubmitLeave.Enabled = false;
            btnSubmitLeave.Text = "Submitting...";

            try
            {
                var payload = new
                {
                    StartDate = dtpLeaveStart.Value.Date,
                    EndDate = dtpLeaveEnd.Value.Date,
                    Reason = reason
                };

                var res = await ApiConfig.Http.PostAsJsonAsync("api/hr/my-leave-request", payload);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Leave request for {wDays} day(s) submitted successfully!\nYour manager has received the request.", "Leave Requested", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtLeaveReason.Clear();
                    await LoadPortalData();
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show($"Unable to submit leave request: {err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting leave request: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSubmitLeave.Enabled = true;
                btnSubmitLeave.Text = "Submit Leave Request";
            }
        }

        #endregion

        #region TAB 2: ATTENDANCE HISTORY

        private void BuildAttendanceTab()
        {
            tabAttendance = new TabPage("Attendance History")
            {
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };

            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            var lblHistoryTitle = new Label
            {
                Text = "My Recent Daily Attendance (Past 90 Days)",
                Left = 0,
                Top = 10,
                Width = 450,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText
            };

            btnRefreshAttendance = new Button
            {
                Text = "↻ Refresh",
                Top = 6,
                Width = 100,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefreshAttendance);
            btnRefreshAttendance.Click += async (s, e) => await LoadPortalData();

            bar.Controls.Add(lblHistoryTitle);
            bar.Controls.Add(btnRefreshAttendance);
            btnRefreshAttendance.Left = Math.Max(300, bar.ClientSize.Width - btnRefreshAttendance.Width - 8);

            dgvAttendance = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            Theme.StyleGrid(dgvAttendance);

            dgvAttendance.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvAttendance.Rows.Count) return;
                var row = dgvAttendance.Rows[e.RowIndex];
                if (row.DataBoundItem is AttendanceItem item)
                {
                    if (item.Status == "Present")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                    }
                    else if (item.Status == "PaidLeave")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(239, 246, 255);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 64, 175);
                    }
                    else if (item.Status == "Absent")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                    }
                }
            };

            tabAttendance.Controls.Add(dgvAttendance);
            tabAttendance.Controls.Add(bar);
        }

        #endregion

        #region TAB 3: SALARY HISTORY & PAYSLIP

        private void BuildSalaryTab()
        {
            tabSalary = new TabPage("Salary History")
            {
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };

            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            var lblSalaryTitle = new Label
            {
                Text = "Monthly Salary Statements & Payslips",
                Left = 0,
                Top = 10,
                Width = 450,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText
            };

            btnRefreshSalary = new Button
            {
                Text = "↻ Refresh",
                Top = 6,
                Width = 100,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefreshSalary);
            btnRefreshSalary.Click += async (s, e) => await LoadPortalData();

            bar.Controls.Add(lblSalaryTitle);
            bar.Controls.Add(btnRefreshSalary);
            btnRefreshSalary.Left = Math.Max(300, bar.ClientSize.Width - btnRefreshSalary.Width - 8);

            dgvSalary = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            Theme.StyleGrid(dgvSalary);

            dgvSalary.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvSalary.Columns[e.ColumnIndex].Name == "Action")
                {
                    if (dgvSalary.Rows[e.RowIndex].DataBoundItem is SalaryItem salary)
                    {
                        ShowMyPayslipDialog(salary);
                    }
                }
            };

            tabSalary.Controls.Add(dgvSalary);
            tabSalary.Controls.Add(bar);
        }

        private void ShowMyPayslipDialog(SalaryItem s)
        {
            using var dlg = new Form
            {
                Text = $"Payslip Breakdown - {_portalData?.FullName ?? "Employee"} ({s.PeriodName})",
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
                Text = $"Period: {s.PeriodStart} to {s.PeriodEnd} ({s.PeriodName})",
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

            list.Items.Add(new ListViewItem(new[] { "Employee Code", _portalData?.EmployeeCode ?? "—" }));
            list.Items.Add(new ListViewItem(new[] { "Employee Name", _portalData?.FullName ?? "—" }));
            list.Items.Add(new ListViewItem(new[] { "Branch", _portalData?.BranchName ?? "—" }));
            list.Items.Add(new ListViewItem(new[] { "Pay Type", s.PayType }));
            list.Items.Add(new ListViewItem(new[] { "Base Pay Rate", $"₱{s.PayRate:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "Days Present", $"{s.PresentDays} days" }));
            list.Items.Add(new ListViewItem(new[] { "Paid Leaves Used", $"{s.PaidLeaveDays} days" }));
            list.Items.Add(new ListViewItem(new[] { "Unpaid Absences", $"{s.UnpaidAbsenceDays} days" }));
            list.Items.Add(new ListViewItem(new[] { "Absence Deductions", $"-₱{s.AbsenceDeduction:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "Gross Earnings", $"₱{s.GrossPay:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "Tax Withholding (10%)", $"-₱{s.EmployeeTax:N2}" }));
            list.Items.Add(new ListViewItem(new[] { "NET TAKE-HOME PAY", $"₱{s.NetPay:N2}" }));

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 55 };
            var btnClose = new Button { Text = "Close", Width = 100, Height = 34, Left = 380, Top = 10 };
            Theme.StylePrimaryButton(btnClose);
            btnClose.Click += (_, __) => dlg.Close();
            pnlBottom.Controls.Add(btnClose);

            pnl.Controls.Add(list);
            pnl.Controls.Add(pnlBottom);
            pnl.Controls.Add(line);
            pnl.Controls.Add(lblSub);
            pnl.Controls.Add(lblHeader);
            dlg.Controls.Add(pnl);

            dlg.ShowDialog(this);
        }

        #endregion

        #region DATA LOADING

        private async Task LoadPortalData()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/hr/my-portal");
                if (!res.IsSuccessStatusCode)
                {
                    lblProfileBadge.Text = "No employee record linked to account.";
                    lblTodayStatus.Text = "Attendance self-service not enabled for this user.";
                    return;
                }

                _portalData = await res.Content.ReadFromJsonAsync<PortalResponse>(ApiConfig.JsonOptions);
                if (_portalData == null) return;

                // Update Profile Badge
                lblProfileBadge.Text = $"{_portalData.FullName}  •  {_portalData.Position}  •  {_portalData.BranchName}  •  {CurrentSession.GetCompanyDisplay()}";
                LayoutControls();
                if (_portalData.IsPresentToday)
                {
                    lblTodayStatus.Text = "✔ Auto-marked Present for today (Logged in)";
                    lblTodayStatus.ForeColor = ColorTranslator.FromHtml("#166534");
                    lblTodayStatus.BackColor = Theme.SoftGreen;
                }
                else
                {
                    lblTodayStatus.Text = "• Today's attendance status: Not logged today";
                    lblTodayStatus.ForeColor = ColorTranslator.FromHtml("#B45309");
                    lblTodayStatus.BackColor = Color.FromArgb(254, 243, 199);
                }

                // Update Stat Cards
                valAvailableLeaves.Text = $"{_portalData.RemainingLeaveDays:G29} Days";
                valLeavesUsed.Text = $"{_portalData.UsedPaidLeaveDays:G29} / {_portalData.MaximumPaidLeaveDays:G29} Days";
                valPresentThisMonth.Text = $"{_portalData.DaysPresentThisMonth} Days";
                valBaseRate.Text = $"₱{_portalData.PayRate:N2}";

                // Bind Leave Requests Grid
                dgvLeaveRequests.DataSource = null;
                dgvLeaveRequests.Columns.Clear();
                dgvLeaveRequests.DataSource = _portalData.LeaveRequests;
                Theme.ConfigureColumns(dgvLeaveRequests, new Dictionary<string, (string Header, int Width)>
                {
                    { "StartDate", ("Start Date", 120) },
                    { "EndDate", ("End Date", 120) },
                    { "DaysRequested", ("Working Days", 110) },
                    { "Reason", ("Reason", 260) },
                    { "Status", ("Status", 120) },
                    { "ApprovedAt", ("Approved / Processed At", 160) }
                }, "Id");

                // Highlight leave status cells
                foreach (DataGridViewRow row in dgvLeaveRequests.Rows)
                {
                    if (row.DataBoundItem is LeaveRequestItem req)
                    {
                        var statusCell = row.Cells["Status"];
                        if (req.Status == "Approved")
                        {
                            statusCell.Style.ForeColor = ColorTranslator.FromHtml("#166534");
                            statusCell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        }
                        else if (req.Status == "Pending")
                        {
                            statusCell.Style.ForeColor = ColorTranslator.FromHtml("#D97706");
                            statusCell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        }
                        else if (req.Status == "Rejected")
                        {
                            statusCell.Style.ForeColor = ColorTranslator.FromHtml("#DC2626");
                            statusCell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        }
                    }
                }

                // Bind Attendance History Grid
                dgvAttendance.DataSource = null;
                dgvAttendance.Columns.Clear();
                dgvAttendance.DataSource = _portalData.AttendanceHistory;
                Theme.ConfigureColumns(dgvAttendance, new Dictionary<string, (string Header, int Width)>
                {
                    { "Date", ("Date", 130) },
                    { "DayOfWeek", ("Day", 120) },
                    { "Status", ("Attendance Status", 150) },
                    { "MarkedAt", ("Time Marked", 130) },
                    { "Notes", ("Remarks / Method", 300) }
                }, "Id");

                // Bind Salary History Grid
                dgvSalary.DataSource = null;
                dgvSalary.Columns.Clear();
                dgvSalary.DataSource = _portalData.SalaryHistory;
                Theme.ConfigureColumns(dgvSalary, new Dictionary<string, (string Header, int Width)>
                {
                    { "PeriodName", ("Pay Period", 140) },
                    { "PeriodStart", ("Start Date", 110) },
                    { "PeriodEnd", ("End Date", 110) },
                    { "PayRate", ("Base Rate", 110) },
                    { "PresentDays", ("Days Worked", 100) },
                    { "PaidLeaveDays", ("Paid Leaves", 95) },
                    { "GrossPay", ("Gross Pay (₱)", 120) },
                    { "AbsenceDeduction", ("Deductions (₱)", 110) },
                    { "EmployeeTax", ("Tax (₱)", 100) },
                    { "NetPay", ("Net Pay (₱)", 130) }
                }, "BaseSalary", "UnpaidAbsenceDays", "PayType");

                // Add "Action" button column for Payslip
                var btnCol = new DataGridViewButtonColumn
                {
                    Name = "Action",
                    HeaderText = "Payslip",
                    Text = "📄 View Payslip",
                    UseColumnTextForButtonValue = true,
                    Width = 130
                };
                dgvSalary.Columns.Add(btnCol);

                LayoutControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employee portal: {ex.Message}", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region HELPERS

        private static int CountWeekdays(DateTime start, DateTime end)
        {
            if (end < start) return 0;
            int count = 0;
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    count++;
            }
            return count;
        }

        private Panel MakeStatCard(string label, string initialValue, Color accentColor)
        {
            var card = new Panel
            {
                BackColor = Theme.White,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(12)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var bar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = accentColor
            };

            var lbl = new Label
            {
                Text = label,
                Left = 14,
                Top = 8,
                Height = 18,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                AutoSize = true,
                UseMnemonic = false
            };

            var val = new Label
            {
                Text = initialValue,
                Left = 14,
                Top = 28,
                Height = 48,
                Font = new Font("Segoe UI", 16.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                UseMnemonic = false
            };

            card.Resize += (s, e) =>
            {
                int w = Math.Max(20, card.ClientSize.Width - 20);
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

        #region MODELS

        public class PortalResponse
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Department { get; set; } = "";
            public string Position { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string PayType { get; set; } = "";
            public decimal PayRate { get; set; }
            public bool IsPresentToday { get; set; }
            public int DaysPresentThisMonth { get; set; }
            public int DaysAbsentThisMonth { get; set; }
            public int LeaveYear { get; set; }
            public decimal MaximumPaidLeaveDays { get; set; }
            public decimal UsedPaidLeaveDays { get; set; }
            public decimal PendingLeaveDays { get; set; }
            public decimal RemainingLeaveDays { get; set; }
            public decimal MonthlyAllowance { get; set; }
            public List<AttendanceItem> AttendanceHistory { get; set; } = new();
            public List<LeaveRequestItem> LeaveRequests { get; set; } = new();
            public List<SalaryItem> SalaryHistory { get; set; } = new();
        }

        public class AttendanceItem
        {
            public int Id { get; set; }
            public string Date { get; set; } = "";
            public string DayOfWeek { get; set; } = "";
            public string Status { get; set; } = "";
            public string MarkedAt { get; set; } = "";
            public string Notes { get; set; } = "";
        }

        public class LeaveRequestItem
        {
            public int Id { get; set; }
            public string StartDate { get; set; } = "";
            public string EndDate { get; set; } = "";
            public decimal DaysRequested { get; set; }
            public string Reason { get; set; } = "";
            public string Status { get; set; } = "";
            public string? ApprovedAt { get; set; }
        }

        public class SalaryItem
        {
            public string PeriodName { get; set; } = "";
            public string PeriodStart { get; set; } = "";
            public string PeriodEnd { get; set; } = "";
            public decimal BaseSalary { get; set; }
            public decimal GrossPay { get; set; }
            public decimal AbsenceDeduction { get; set; }
            public decimal EmployeeTax { get; set; }
            public decimal NetPay { get; set; }
            public int PresentDays { get; set; }
            public decimal PaidLeaveDays { get; set; }
            public decimal UnpaidAbsenceDays { get; set; }
            public string PayType { get; set; } = "";
            public decimal PayRate { get; set; }
        }

        #endregion
    }
}
