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
            dgvEmployees.AutoGenerateColumns = false;
            BuildColumns();

            dgvEmployees.CellFormatting += DgvEmployees_CellFormatting;
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
            dgvEmployees.AutoGenerateColumns = false;

            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Employee.EmployeeCode),
                HeaderText = "Code",
                Width = 100,
                MinimumWidth = 90
            });

            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFullName",
                HeaderText = "Full Name",
                Width = 170,
                MinimumWidth = 130
            });

            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Employee.Department),
                HeaderText = "Department",
                Width = 120,
                MinimumWidth = 100
            });

            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Employee.Position),
                HeaderText = "Position",
                Width = 180,
                MinimumWidth = 140
            });

            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Employee.ContactNumber),
                HeaderText = "Contact",
                Width = 120,
                MinimumWidth = 100
            });

            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Employee.City),
                HeaderText = "City",
                Width = 110,
                MinimumWidth = 90
            });

            dgvEmployees.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "Status",
                Width = 90,
                MinimumWidth = 80
            });

            dgvEmployees.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colManage",
                HeaderText = "",
                Text = "Manage",
                UseColumnTextForButtonValue = true,
                Width = 90
            });

            dgvEmployees.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colToggleStatus",
                HeaderText = "System Access",
                Width = 115
            });
        }

        private void DgvEmployees_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvEmployees.Rows.Count) return;
            if (dgvEmployees.Rows[e.RowIndex].DataBoundItem is not Employee emp) return;

            var colName = dgvEmployees.Columns[e.ColumnIndex].Name;

            if (colName == "colFullName")
            {
                string middle = string.IsNullOrWhiteSpace(emp.MiddleName) ? "" : $"{emp.MiddleName} ";
                e.Value = $"{emp.FirstName} {middle}{emp.LastName}";
                e.FormattingApplied = true;
            }
            else if (colName == "colStatus")
            {
                e.Value = emp.Status == EmploymentStatus.Active ? "Active" : "Inactive";
                if (emp.Status == EmploymentStatus.Active)
                {
                    e.CellStyle!.ForeColor = ColorTranslator.FromHtml("#00AD4C");
                    e.CellStyle!.Font = new Font(dgvEmployees.Font, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle!.ForeColor = ColorTranslator.FromHtml("#DC2626");
                }
                e.FormattingApplied = true;
            }
            else if (colName == "colToggleStatus")
            {
                e.Value = emp.Status == EmploymentStatus.Active ? "Deactivate" : "Activate";
                e.FormattingApplied = true;
            }
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
                    dgvEmployees.AutoGenerateColumns = false;
                    dgvEmployees.DataSource = null;
                    dgvEmployees.AutoGenerateColumns = false;
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
                using var dialog = new EmployeeEditDialog(emp);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    await LoadEmployees();
                }
            }
            else if (columnName == "colToggleStatus")
            {
                bool isActive = (emp.Status == EmploymentStatus.Active);
                string prompt = isActive
                    ? $"Are you sure you want to DEACTIVATE {emp.FirstName} {emp.LastName}?\n\nDeactivating this employee will immediately revoke their access and prevent them from logging into the system."
                    : $"Are you sure you want to ACTIVATE {emp.FirstName} {emp.LastName}?\n\nThis will restore their active status and re-enable their system access.";

                var result = MessageBox.Show(prompt, "S.C.R.A.P — Employee System Access",
                    MessageBoxButtons.YesNo,
                    isActive ? MessageBoxIcon.Warning : MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        var res = await ApiConfig.Http.PostAsync($"api/Employees/{emp.Id}/toggle-status", null);
                        if (res.IsSuccessStatusCode)
                        {
                            string resultMsg = isActive
                                ? $"{emp.FirstName} {emp.LastName} is now Inactive. System access has been REVOKED."
                                : $"{emp.FirstName} {emp.LastName} is now Active. System access has been RESTORED.";
                            MessageBox.Show(resultMsg, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            await LoadEmployees();
                        }
                        else
                        {
                            var err = await res.Content.ReadAsStringAsync();
                            MessageBox.Show("Failed to update status: " + err, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
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