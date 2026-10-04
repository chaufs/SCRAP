using System;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class EmployeeEditDialog : Form
    {
        private readonly Employee? _existing;

        private TextBox txtFirstName  = null!;
        private TextBox txtMiddleName = null!;
        private TextBox txtLastName   = null!;
        private TextBox txtPosition   = null!;
        private ComboBox cmbDepartment = null!;
        private TextBox txtContact    = null!;
        private TextBox txtEmail      = null!;

        // Split address fields
        private TextBox txtStreet    = null!;
        private TextBox txtBarangay  = null!;
        private TextBox txtCity      = null!;
        private TextBox txtProvince  = null!;
        private TextBox txtCountry   = null!;

        private DateTimePicker dtpHired  = null!;
        private ComboBox cmbPayType      = null!;
        private TextBox txtPayRate       = null!;

        // Status & Access management
        private ComboBox cmbStatus        = null!;
        private CheckBox chkAccountAccess = null!;

        // Account creation for new employee
        private CheckBox chkCreateAccount = null!;
        private Panel accountPanel        = null!;
        private TextBox txtUsername       = null!;
        private TextBox txtAccountEmail   = null!;
        private TextBox txtPassword       = null!;

        private Button btnSave   = null!;
        private Button btnCancel = null!;

        public EmployeeEditDialog(Employee? existing = null)
        {
            _existing = existing;
            Text = _existing == null ? "New Employee" : $"Manage Employee — {_existing.EmployeeCode} ({_existing.FirstName} {_existing.LastName})";
            Width = 490;
            Height = 840;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            AutoScroll = true;
            InitializeComponent();
            if (_existing != null)
            {
                PopulateExisting();
            }
        }

        private void InitializeComponent()
        {
            int top = 20;

            // ── Name row ──────────────────────────────────────────────────
            var lblFirst = MakeLabel("First Name", top);
            txtFirstName = new TextBox { Left = 20, Top = top + 20, Width = 190 };
            Theme.StyleTextBox(txtFirstName);
            AttachNameFilter(txtFirstName);

            var lblMiddle = MakeLabel("Middle Name", top, 220);
            txtMiddleName = new TextBox { Left = 220, Top = top + 20, Width = 100, PlaceholderText = "optional" };
            Theme.StyleTextBox(txtMiddleName);
            AttachNameFilter(txtMiddleName);

            var lblLast = MakeLabel("Last Name", top, 330);
            txtLastName = new TextBox { Left = 330, Top = top + 20, Width = 110 };
            Theme.StyleTextBox(txtLastName);
            AttachNameFilter(txtLastName);
            top += 56;

            // ── Position ──────────────────────────────────────────────────
            var lblPos = MakeLabel("Position", top);
            txtPosition = MakeText(top + 20);
            top += 56;

            // ── Department ────────────────────────────────────────────────
            var lblDept = MakeLabel("Department", top);
            cmbDepartment = new ComboBox { Left = 20, Top = top + 20, Width = 420, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbDepartment);
            cmbDepartment.Items.AddRange(new object[] { "Technical", "Operations", "Sales", "Management", "Human Resources" });
            if (cmbDepartment.Items.Count > 0) cmbDepartment.SelectedIndex = 0;
            top += 56;

            // ── Contact ───────────────────────────────────────────────────
            var lblContact = MakeLabel("Contact Number", top);
            txtContact = MakeText(top + 20);
            top += 56;

            // ── Email ─────────────────────────────────────────────────────
            var lblEmail = MakeLabel("Email Address", top);
            txtEmail = MakeText(top + 20);
            top += 56;

            // ── Address (split) ───────────────────────────────────────────
            var lblAddrSection = MakeLabel("Address", top);
            top += 22;

            var lblStreet = MakeLabel("Street", top);
            txtStreet = MakeText(top + 18);
            top += 50;

            var lblBarangay = MakeLabel("Barangay", top);
            txtBarangay = MakeText(top + 18);
            top += 50;

            // City + Province side by side
            var lblCity = MakeLabel("City", top);
            txtCity = new TextBox { Left = 20, Top = top + 18, Width = 195 };
            Theme.StyleTextBox(txtCity);

            var lblProvince = MakeLabel("Province", top, 225);
            txtProvince = new TextBox { Left = 225, Top = top + 18, Width = 215 };
            Theme.StyleTextBox(txtProvince);
            top += 50;

            var lblCountry = MakeLabel("Country", top);
            txtCountry = MakeText(top + 18);
            txtCountry.Text = "Philippines";
            top += 50;

            // ── Date Hired ────────────────────────────────────────────────
            var lblHired = MakeLabel("Date Hired", top);
            dtpHired = new DateTimePicker { Left = 20, Top = top + 18, Width = 200, Format = DateTimePickerFormat.Short };
            top += 50;

            // ── Pay ───────────────────────────────────────────────────────
            var lblPayType = MakeLabel("Pay Type", top);
            cmbPayType = new ComboBox { Left = 20, Top = top + 18, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbPayType);
            cmbPayType.DataSource = Enum.GetValues(typeof(PayType));

            var lblRate = MakeLabel("Pay Rate (Monthly/Rate)", top, 240);
            txtPayRate = new TextBox { Left = 240, Top = top + 18, Width = 200, PlaceholderText = "0.00" };
            Theme.StyleTextBox(txtPayRate);
            top += 56;

            // ── Status & System Access Section ────────────────────────────
            var lblStatus = MakeLabel("Employment Status & System Access", top);
            cmbStatus = new ComboBox { Left = 20, Top = top + 20, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbStatus);
            cmbStatus.Items.AddRange(new object[] { "Active", "Inactive (Resigned/Terminated)" });
            cmbStatus.SelectedIndex = 0;

            chkAccountAccess = new CheckBox
            {
                Text = "System Login Access Enabled",
                Left = 230,
                Top = top + 20,
                Width = 220,
                Checked = true
            };
            cmbStatus.SelectedIndexChanged += (s, e) =>
            {
                bool isActive = cmbStatus.SelectedIndex == 0;
                chkAccountAccess.Checked = isActive;
                chkAccountAccess.Enabled = isActive;
            };

            var lblStatusHint = new Label
            {
                Text = "Note: Inactive employees are immediately blocked from logging into the system.",
                Left = 20,
                Top = top + 52,
                Width = 430,
                Height = 32,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.MutedText
            };
            top += 88;

            // ── Create Account (for new employees only) ───────────────────
            chkCreateAccount = new CheckBox { Text = "Create a login account for this employee", Left = 20, Top = top, Width = 400 };
            chkCreateAccount.CheckedChanged += (s, e) => accountPanel.Visible = chkCreateAccount.Checked;

            accountPanel = new Panel { Left = 20, Top = top + 28, Width = 420, Height = 130, Visible = false };
            var lblUser = new Label { Text = "Username", Left = 0, Top = 0, Width = 200, ForeColor = Theme.MutedText };
            txtUsername = new TextBox { Left = 0, Top = 20, Width = 200 };
            Theme.StyleTextBox(txtUsername);

            var lblAcctEmail = new Label { Text = "Account Email (optional)", Left = 220, Top = 0, Width = 200, ForeColor = Theme.MutedText };
            txtAccountEmail = new TextBox { Left = 220, Top = 20, Width = 200, PlaceholderText = "Uses employee email if blank" };
            Theme.StyleTextBox(txtAccountEmail);

            var lblPass = new Label { Text = "Password", Left = 0, Top = 56, Width = 200, ForeColor = Theme.MutedText };
            txtPassword = new TextBox { Left = 0, Top = 76, Width = 200, PasswordChar = '•' };
            Theme.StyleTextBox(txtPassword);

            accountPanel.Controls.AddRange(new Control[] { lblUser, txtUsername, lblAcctEmail, txtAccountEmail, lblPass, txtPassword });

            if (_existing != null)
            {
                chkCreateAccount.Visible = false;
                accountPanel.Visible = false;
            }
            else
            {
                top += 160;
            }

            // ── Buttons ───────────────────────────────────────────────────
            btnSave = new Button { Text = _existing == null ? "Save" : "Save Changes", Left = 230, Top = top + 10, Width = 110, Height = 36 };
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += async (s, e) => await Save();

            btnCancel = new Button { Text = "Cancel", Left = 350, Top = top + 10, Width = 90, Height = 36 };
            Theme.StyleOutlineButton(btnCancel);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                lblFirst, txtFirstName, lblMiddle, txtMiddleName, lblLast, txtLastName,
                lblPos, txtPosition, lblDept, cmbDepartment,
                lblContact, txtContact, lblEmail, txtEmail,
                lblAddrSection,
                lblStreet, txtStreet,
                lblBarangay, txtBarangay,
                lblCity, txtCity, lblProvince, txtProvince,
                lblCountry, txtCountry,
                lblHired, dtpHired, lblPayType, cmbPayType, lblRate, txtPayRate,
                lblStatus, cmbStatus, chkAccountAccess, lblStatusHint,
                chkCreateAccount, accountPanel, btnSave, btnCancel
            });

            Height = top + 90;
        }

        private void PopulateExisting()
        {
            if (_existing == null) return;
            txtFirstName.Text = _existing.FirstName;
            txtMiddleName.Text = _existing.MiddleName ?? "";
            txtLastName.Text = _existing.LastName;
            txtPosition.Text = _existing.Position;

            // Match Department
            for (int i = 0; i < cmbDepartment.Items.Count; i++)
            {
                if (string.Equals(cmbDepartment.Items[i]?.ToString(), _existing.Department, StringComparison.OrdinalIgnoreCase))
                {
                    cmbDepartment.SelectedIndex = i;
                    break;
                }
            }

            txtContact.Text = _existing.ContactNumber ?? "";
            txtEmail.Text = _existing.EmailAddress ?? "";
            txtStreet.Text = _existing.Street ?? "";
            txtBarangay.Text = _existing.Barangay ?? "";
            txtCity.Text = _existing.City ?? "";
            txtProvince.Text = _existing.Province ?? "";
            txtCountry.Text = _existing.Country ?? "Philippines";

            if (_existing.DateHired != default)
                dtpHired.Value = _existing.DateHired;

            cmbPayType.SelectedItem = _existing.PayType;
            txtPayRate.Text = _existing.PayRate.ToString("0.00");

            bool isActive = _existing.Status == EmploymentStatus.Active;
            cmbStatus.SelectedIndex = isActive ? 0 : 1;
            chkAccountAccess.Checked = isActive;
            chkAccountAccess.Enabled = isActive;
        }

        private static void AttachNameFilter(TextBox tb)
        {
            tb.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (char.IsLetter(e.KeyChar) || e.KeyChar == ' ' || e.KeyChar == '-' || e.KeyChar == '\'')
                    return;
                e.Handled = true;
            };
        }

        private Label MakeLabel(string text, int top, int left = 20) => new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 400,
            Height = 18,
            ForeColor = Theme.MutedText,
            Font = Theme.StatLabelFont
        };

        private TextBox MakeText(int top)
        {
            var t = new TextBox { Left = 20, Top = top, Width = 420 };
            Theme.StyleTextBox(t);
            return t;
        }

        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtPosition.Text))
            {
                MessageBox.Show("First name, last name, and position are required.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (cmbDepartment.SelectedItem is null)
            {
                MessageBox.Show("Select a department.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!decimal.TryParse(txtPayRate.Text, out var rate) || rate < 0)
            {
                MessageBox.Show("Enter a valid pay rate.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnSave.Enabled = false;

            try
            {
                if (_existing != null)
                {
                    // ── UPDATE EXISTING EMPLOYEE ──────────────────────────────
                    var updatePayload = new
                    {
                        FirstName     = txtFirstName.Text.Trim(),
                        MiddleName    = string.IsNullOrWhiteSpace(txtMiddleName.Text) ? null : txtMiddleName.Text.Trim(),
                        LastName      = txtLastName.Text.Trim(),
                        Position      = txtPosition.Text.Trim(),
                        Department    = cmbDepartment.SelectedItem.ToString()!,
                        ContactNumber = txtContact.Text.Trim(),
                        EmailAddress  = txtEmail.Text.Trim(),
                        Street        = txtStreet.Text.Trim(),
                        Barangay      = txtBarangay.Text.Trim(),
                        City          = txtCity.Text.Trim(),
                        Province      = txtProvince.Text.Trim(),
                        Country       = txtCountry.Text.Trim(),
                        PayType       = (PayType)(cmbPayType.SelectedItem ?? PayType.Monthly),
                        PayRate       = rate,
                        Status        = cmbStatus.SelectedIndex == 0 ? EmploymentStatus.Active : EmploymentStatus.Resigned,
                        IsActive      = chkAccountAccess.Checked && cmbStatus.SelectedIndex == 0,
                        Notes         = _existing.Notes
                    };

                    var res = await ApiConfig.Http.PutAsJsonAsync($"api/Employees/{_existing.Id}", updatePayload);
                    if (res.IsSuccessStatusCode)
                    {
                        string accessMsg = updatePayload.IsActive
                            ? "Employee updated successfully. System access remains Active."
                            : "Employee updated successfully. System access has been DEACTIVATED.";
                        MessageBox.Show(accessMsg, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        var body = await res.Content.ReadAsStringAsync();
                        MessageBox.Show("Update failed: " + body, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    // ── CREATE NEW EMPLOYEE ──────────────────────────────────
                    if (chkCreateAccount.Checked)
                    {
                        if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                        {
                            MessageBox.Show("Username and password are required to create an account.", "S.C.R.A.P",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            btnSave.Enabled = true;
                            return;
                        }
                    }

                    var createPayload = new
                    {
                        FirstName     = txtFirstName.Text.Trim(),
                        MiddleName    = string.IsNullOrWhiteSpace(txtMiddleName.Text) ? null : txtMiddleName.Text.Trim(),
                        LastName      = txtLastName.Text.Trim(),
                        Position      = txtPosition.Text.Trim(),
                        Department    = cmbDepartment.SelectedItem.ToString()!,
                        ContactNumber = txtContact.Text.Trim(),
                        EmailAddress  = txtEmail.Text.Trim(),
                        Street        = txtStreet.Text.Trim(),
                        Barangay      = txtBarangay.Text.Trim(),
                        City          = txtCity.Text.Trim(),
                        Province      = txtProvince.Text.Trim(),
                        Country       = txtCountry.Text.Trim(),
                        DateHired     = dtpHired.Value,
                        PayType       = (PayType)(cmbPayType.SelectedItem ?? PayType.Monthly),
                        PayRate       = rate,
                        CreateAccount = chkCreateAccount.Checked,
                        Username      = chkCreateAccount.Checked ? txtUsername.Text.Trim() : null,
                        AccountEmail  = chkCreateAccount.Checked && !string.IsNullOrWhiteSpace(txtAccountEmail.Text) ? txtAccountEmail.Text.Trim() : null,
                        Password      = chkCreateAccount.Checked ? txtPassword.Text : null
                    };

                    var res = await ApiConfig.Http.PostAsJsonAsync("api/Employees/with-account", createPayload);
                    if (res.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Employee created successfully.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        var body = await res.Content.ReadAsStringAsync();
                        MessageBox.Show("Create failed: " + body, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }
    }
}