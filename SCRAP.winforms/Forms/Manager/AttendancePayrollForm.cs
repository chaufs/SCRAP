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
    [DesignerCategory("Code")]
    public sealed class AttendancePayrollForm : Form
    {
        private readonly DataGridView _employeesGrid = new();
        private readonly DateTimePicker _datePicker = new();
        private readonly Button _presentButton = new();
        private readonly Button _absentButton = new();
        private readonly Button _payrollButton = new();
        private List<Employee> _employees = new();

        public AttendancePayrollForm()
        {
            Text = "Attendance and Payroll";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadEmployees();
        }

        private void InitializeComponent()
        {
            var title = new Label
            {
                Text = "Attendance and Payroll",
                Left = 32,
                Top = 28,
                Width = 500,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };

            var subtitle = new Label
            {
                Text = "Manager-only attendance monitoring and payroll review",
                Left = 32,
                Top = 64,
                Width = 600,
                Height = 24,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            _datePicker.Left = 32;
            _datePicker.Top = 105;
            _datePicker.Width = 150;
            _datePicker.Format = DateTimePickerFormat.Short;
            _datePicker.Value = DateTime.Today;

            var refresh = new Button { Text = "Refresh", Left = 195, Top = 103, Width = 100, Height = 32 };
            Theme.StyleOutlineButton(refresh);
            refresh.Click += async (s, e) => await LoadEmployees();

            _presentButton.Text = "Mark Present";
            _presentButton.Left = 315;
            _presentButton.Top = 103;
            _presentButton.Width = 125;
            _presentButton.Height = 32;
            Theme.StylePrimaryButton(_presentButton);
            _presentButton.Click += async (s, e) => await MarkAttendance(AttendanceStatus.Present);

            _absentButton.Text = "Mark Absent";
            _absentButton.Left = 450;
            _absentButton.Top = 103;
            _absentButton.Width = 115;
            _absentButton.Height = 32;
            Theme.StyleOutlineButton(_absentButton);
            _absentButton.Click += async (s, e) => await MarkAttendance(AttendanceStatus.Absent);

            _payrollButton.Text = "View Payroll";
            _payrollButton.Left = 575;
            _payrollButton.Top = 103;
            _payrollButton.Width = 115;
            _payrollButton.Height = 32;
            Theme.StyleOutlineButton(_payrollButton);
            _payrollButton.Click += async (s, e) => await LoadPayroll();

            _employeesGrid.Left = 32;
            _employeesGrid.Top = 155;
            _employeesGrid.Width = 900;
            _employeesGrid.Height = 450;
            _employeesGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _employeesGrid.ReadOnly = true;
            _employeesGrid.AutoGenerateColumns = true;
            _employeesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _employeesGrid.MultiSelect = false;
            Theme.StyleGrid(_employeesGrid);

            Controls.AddRange(new Control[] { title, subtitle, _datePicker, refresh, _presentButton, _absentButton, _payrollButton, _employeesGrid });
        }

        private async Task LoadEmployees()
        {
            try
            {
                var response = await ApiConfig.Http.GetAsync("api/Employees");
                if (!response.IsSuccessStatusCode)
                {
                    await ShowApiError(response, "Unable to load employees");
                    return;
                }

                _employees = await response.Content.ReadFromJsonAsync<List<Employee>>(ApiConfig.JsonOptions) ?? new();
                _employeesGrid.DataSource = _employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load employees: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task MarkAttendance(AttendanceStatus status)
        {
            if (_employeesGrid.CurrentRow?.DataBoundItem is not Employee employee)
            {
                MessageBox.Show("Select an employee first.", "Attendance", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var request = new
            {
                EmployeeId = employee.Id,
                AttendanceDate = _datePicker.Value.Date,
                Status = status
            };

            var response = await ApiConfig.Http.PostAsJsonAsync("api/hr/attendance", request);
            if (!response.IsSuccessStatusCode)
            {
                await ShowApiError(response, "Unable to save attendance");
                return;
            }

            MessageBox.Show($"{employee.FirstName} {employee.LastName}: {status}", "Attendance saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task LoadPayroll()
        {
            var start = new DateTime(_datePicker.Value.Year, _datePicker.Value.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var response = await ApiConfig.Http.GetAsync($"api/hr/payroll?periodStart={start:yyyy-MM-dd}&periodEnd={end:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode)
            {
                await ShowApiError(response, "Unable to load payroll");
                return;
            }

            var rows = await response.Content.ReadFromJsonAsync<List<PayrollRow>>(ApiConfig.JsonOptions) ?? new();
            using var dialog = new Form { Text = "Payroll", Width = 1000, Height = 550, StartPosition = FormStartPosition.CenterParent };
            var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, DataSource = rows };
            dialog.Controls.Add(grid);
            dialog.ShowDialog(this);
        }

        private static async Task ShowApiError(HttpResponseMessage response, string title)
        {
            var body = await response.Content.ReadAsStringAsync();
            var message = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                ? "Manager access is required for this operation."
                : $"{response.StatusCode}\n{body}";
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private sealed class PayrollRow
        {
            public string EmployeeName { get; set; } = string.Empty;
            public decimal PeriodBasePay { get; set; }
            public decimal PresentDays { get; set; }
            public decimal PaidLeaveDays { get; set; }
            public decimal UnpaidAbsenceDays { get; set; }
            public decimal AbsenceDeduction { get; set; }
            public decimal NetPay { get; set; }
        }
    }
}
