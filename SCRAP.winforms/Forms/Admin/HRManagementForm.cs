using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class HRManagementForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblSummary = null!;
        private Button btnRefresh = null!;
        private Button btnCreateEmployee = null!;

        private Panel cardList = null!;
        private DataGridView dgvEmployees = null!;

        private List<Employee> _employees = new();

        public HRManagementForm()
        {
            Text = "HR Management";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "HR Management",
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
                Text = "Employee records, positions, and login accounts",
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
            btnRefresh.Click += async (s, e) => await LoadEmployees();

            btnCreateEmployee = new Button
            {
                Text = "+ Create Employee",
                Width = 160,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StylePrimaryButton(btnCreateEmployee);
            btnCreateEmployee.Click += async (s, e) => await OpenCreateDialog();

            lblSummary = new Label
            {
                Left = 32,
                Top = 100,
                Width = 700,
                Height = 22,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            cardList = MakeCard();
            dgvEmployees = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvEmployees);
            BuildColumns();
            dgvEmployees.CellContentClick += DgvEmployees_CellContentClick;
            cardList.Controls.Add(dgvEmployees);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(lblSummary);
            Controls.Add(btnRefresh);
            Controls.Add(btnCreateEmployee);
            Controls.Add(cardList);

            Resize += (s, e) => LayoutPage();
            LayoutPage();

            HandleCreated += async (s, e) => await LoadEmployees();
        }

        private void BuildColumns()
        {
            dgvEmployees.Columns.Clear();
            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Employee.EmployeeCode), HeaderText = "Code", Width = 100 });
            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Employee.FullName), HeaderText = "Full Name", Width = 200 });
            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Employee.Position), HeaderText = "Position", Width = 160 });
            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Employee.Department), HeaderText = "Department", Width = 130 });
            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Employee.Status), HeaderText = "Status", Width = 110 });
            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Employee.DateHired), HeaderText = "Date Hired", Width = 130 });
            dgvEmployees.Columns.Add(new DataGridViewButtonColumn { Name = "colManage", HeaderText = "", Text = "Manage", UseColumnTextForButtonValue = true, Width = 90 });
            dgvEmployees.Columns.Add(new DataGridViewButtonColumn { Name = "colArchive", HeaderText = "", Text = "Archive", UseColumnTextForButtonValue = true, Width = 90 });
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
            btnCreateEmployee.Left = ClientSize.Width - btnCreateEmployee.Width - 32;
            btnCreateEmployee.Top = 36;

            btnRefresh.Left = btnCreateEmployee.Left - btnRefresh.Width - 10;
            btnRefresh.Top = 36;

            cardList.Left = 32;
            cardList.Top = 132;
            cardList.Width = Math.Max(500, ClientSize.Width - 64);
            cardList.Height = Math.Max(200, ClientSize.Height - 132 - 32);
        }

        private async Task LoadEmployees()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Employees");
                if (res.IsSuccessStatusCode)
                {
                    _employees = await res.Content.ReadFromJsonAsync<List<Employee>>(ApiConfig.JsonOptions) ?? new();
                    dgvEmployees.DataSource = null;
                    dgvEmployees.DataSource = _employees;
                }
                else
                {
                    MessageBox.Show("Error loading employees: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            await LoadSummary();
        }

        private async Task LoadSummary()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Employees/summary");
                if (!res.IsSuccessStatusCode) return;
                var data = await res.Content.ReadFromJsonAsync<EmployeeSummary>(ApiConfig.JsonOptions);
                if (data != null) lblSummary.Text = $"{data.Active} active of {data.Total} total employees";
            }
            catch { /* summary is decoration */ }
        }

        private async Task OpenCreateDialog()
        {
            using var dialog = new EmployeeEditDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                await LoadEmployees();
            }
        }

        private async void DgvEmployees_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvEmployees.Rows[e.RowIndex].DataBoundItem is not Employee emp) return;

            var columnName = dgvEmployees.Columns[e.ColumnIndex].Name;

            if (columnName == "colManage")
            {
                MessageBox.Show($"Manage screen for {emp.FullName} — hook up an edit dialog here next.",
                    "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // TODO: open an edit dialog pre-filled with emp's data (position, department, pay, status, and
                // account fields if emp.UserId != null), PUT to api/Employees/{id} on save.
            }
            else if (columnName == "colArchive")
            {
                var action = emp.Status == EmploymentStatus.Active ? "archive" : "reactivate";
                var confirm = MessageBox.Show($"Are you sure you want to {action} {emp.FullName}?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                var newStatus = emp.Status == EmploymentStatus.Active ? EmploymentStatus.Resigned : EmploymentStatus.Active;
                try
                {
                    var res = await ApiConfig.Http.PostAsync($"api/Employees/{emp.Id}/deactivate?status={newStatus}", null);
                    if (res.IsSuccessStatusCode)
                    {
                        await LoadEmployees();
                    }
                    else
                    {
                        MessageBox.Show("Action failed: " + res.StatusCode, "S.C.R.A.P",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private class EmployeeSummary
        {
            public int Total { get; set; }
            public int Active { get; set; }
        }
    }
}