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
        private TextBox txtFullName = null!;
        private TextBox txtPosition = null!;
        private ComboBox cmbDepartment = null!;
        private TextBox txtContact = null!;
        private TextBox txtEmail = null!;
        private TextBox txtAddress = null!;
        private DateTimePicker dtpHired = null!;
        private ComboBox cmbPayType = null!;
        private TextBox txtPayRate = null!;

        private CheckBox chkCreateAccount = null!;
        private Panel accountPanel = null!;
        private TextBox txtUsername = null!;
        private TextBox txtAccountEmail = null!;
        private TextBox txtPassword = null!;
        private ComboBox cmbRole = null!;

        private Button btnSave = null!;
        private Button btnCancel = null!;

        public EmployeeEditDialog()
        {
            Text = "New Employee";
            Width = 480;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            int top = 20;

            var lblFull = MakeLabel("Full Name", top);
            txtFullName = MakeText(top + 20);
            top += 56;

            var lblPos = MakeLabel("Position", top);
            txtPosition = MakeText(top + 20);
            top += 56;

            var lblDept = MakeLabel("Department", top);
            cmbDepartment = new ComboBox { Left = 20, Top = top + 20, Width = 420, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbDepartment);
            cmbDepartment.Items.AddRange(new object[] { "Tech", "Manager", "Sales" });
            top += 56;

            var lblContact = MakeLabel("Contact Number", top);
            txtContact = MakeText(top + 20);
            top += 56;

            var lblEmail = MakeLabel("Email Address", top);
            txtEmail = MakeText(top + 20);
            top += 56;

            var lblAddress = MakeLabel("Address", top);
            txtAddress = MakeText(top + 20);
            top += 56;

            var lblHired = MakeLabel("Date Hired", top);
            dtpHired = new DateTimePicker { Left = 20, Top = top + 20, Width = 200, Format = DateTimePickerFormat.Short };
            top += 56;

            var lblPayType = MakeLabel("Pay Type", top);
            cmbPayType = new ComboBox { Left = 20, Top = top + 20, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbPayType);
            cmbPayType.DataSource = Enum.GetValues(typeof(PayType));

            var lblRate = MakeLabel("Pay Rate", top, 240);
            txtPayRate = new TextBox { Left = 240, Top = top + 20, Width = 200, PlaceholderText = "0.00" };
            Theme.StyleTextBox(txtPayRate);
            top += 56;

            chkCreateAccount = new CheckBox { Text = "Create a login account for this employee", Left = 20, Top = top, Width = 400 };
            chkCreateAccount.CheckedChanged += (s, e) => accountPanel.Visible = chkCreateAccount.Checked;
            top += 30;

            accountPanel = new Panel { Left = 20, Top = top, Width = 420, Height = 170, Visible = false };

            var lblUser = new Label { Text = "Username", Left = 0, Top = 0, Width = 200, ForeColor = Theme.MutedText };
            txtUsername = new TextBox { Left = 0, Top = 20, Width = 200 };
            Theme.StyleTextBox(txtUsername);

            var lblAcctEmail = new Label { Text = "Account Email (optional)", Left = 220, Top = 0, Width = 200, ForeColor = Theme.MutedText };
            txtAccountEmail = new TextBox { Left = 220, Top = 20, Width = 200, PlaceholderText = "Uses employee email if blank" };
            Theme.StyleTextBox(txtAccountEmail);

            var lblPass = new Label { Text = "Password", Left = 0, Top = 56, Width = 200, ForeColor = Theme.MutedText };
            txtPassword = new TextBox { Left = 0, Top = 76, Width = 200, PasswordChar = '•' };
            Theme.StyleTextBox(txtPassword);

            var lblRole = new Label { Text = "Role", Left = 220, Top = 56, Width = 200, ForeColor = Theme.MutedText };
            cmbRole = new ComboBox { Left = 220, Top = 76, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbRole);
            cmbRole.Items.AddRange(new object[] { UserRole.Manager, UserRole.Tech, UserRole.Sales });

            accountPanel.Controls.AddRange(new Control[] { lblUser, txtUsername, lblAcctEmail, txtAccountEmail, lblPass, txtPassword, lblRole, cmbRole });
            top += 180;

            btnSave = new Button { Text = "Save", Left = 240, Top = top, Width = 90, Height = 34 };
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += async (s, e) => await Save();

            btnCancel = new Button { Text = "Cancel", Left = 340, Top = top, Width = 90, Height = 34 };
            Theme.StyleOutlineButton(btnCancel);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                lblFull, txtFullName, lblPos, txtPosition, lblDept, cmbDepartment,
                lblContact, txtContact, lblEmail, txtEmail, lblAddress, txtAddress,
                lblHired, dtpHired, lblPayType, cmbPayType, lblRate, txtPayRate,
                chkCreateAccount, accountPanel, btnSave, btnCancel
            });

            Height = top + 90;
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
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtPosition.Text))
            {
                MessageBox.Show("Full name and position are required.", "S.C.R.A.P",
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
            if (chkCreateAccount.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Username and password are required to create an account.", "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (cmbRole.SelectedItem is null)
                {
                    MessageBox.Show("Select a role for the account.", "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            var payload = new
            {
                FullName = txtFullName.Text.Trim(),
                ContactNumber = string.IsNullOrWhiteSpace(txtContact.Text) ? null : txtContact.Text.Trim(),
                EmailAddress = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                Address = string.IsNullOrWhiteSpace(txtAddress.Text) ? null : txtAddress.Text.Trim(),
                Position = txtPosition.Text.Trim(),
                Department = cmbDepartment.SelectedItem!.ToString(),
                DateHired = dtpHired.Value,
                PayType = (PayType)cmbPayType.SelectedItem!,
                PayRate = rate,
                CreateAccount = chkCreateAccount.Checked,
                Username = chkCreateAccount.Checked ? txtUsername.Text.Trim() : null,
                AccountEmail = chkCreateAccount.Checked && !string.IsNullOrWhiteSpace(txtAccountEmail.Text) ? txtAccountEmail.Text.Trim() : null,
                Password = chkCreateAccount.Checked ? txtPassword.Text : null,
                Role = chkCreateAccount.Checked ? (UserRole?)cmbRole.SelectedItem! : null
            };

            btnSave.Enabled = false;
            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync("api/Employees/with-account", payload);
                if (res.IsSuccessStatusCode)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Save failed: " + res.StatusCode + "\n" + body, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving employee: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }
    }
}