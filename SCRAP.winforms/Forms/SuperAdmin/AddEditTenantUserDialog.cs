using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    public class AddEditTenantUserDialog : Form
    {
        private readonly int _companyId;
        private readonly string _companyCode;
        private readonly string _companyName;
        private readonly TenantUserViewModel? _existingUser;
        private readonly List<TenantBranchItem> _branches;

        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnTogglePass = null!;
        private TextBox txtFirstName = null!;
        private TextBox txtMiddleName = null!;
        private TextBox txtLastName = null!;
        private TextBox txtEmail = null!;
        private ComboBox cboRole = null!;
        private ComboBox cboBranch = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public bool UserSaved { get; private set; }

        public AddEditTenantUserDialog(
            int companyId,
            string companyCode,
            string companyName,
            List<TenantBranchItem> branches,
            TenantUserViewModel? existingUser = null)
        {
            _companyId = companyId;
            _companyCode = companyCode;
            _companyName = companyName;
            _branches = branches ?? new List<TenantBranchItem>();
            _existingUser = existingUser;

            Text = _existingUser == null
                ? $"Add Administrator — {_companyName} ({_companyCode})"
                : $"Edit Administrator: {_existingUser.Username} — {_companyName} ({_companyCode})";

            Width = 560;
            Height = 650;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildUi();
            PopulateData();
        }

        private void BuildUi()
        {
            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.White,
                Padding = new Padding(24, 14, 24, 0)
            };
            header.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = _existingUser == null ? "Create Company Administrator" : $"Edit Administrator: {_existingUser.Username}",
                Left = 24,
                Top = 12,
                Width = 460,
                Height = 24,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            var lblSub = new Label
            {
                Text = $"Company: {_companyName} ({_companyCode})",
                Left = 24,
                Top = 38,
                Width = 460,
                Height = 20,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);

            // Body
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };

            int y = 14;

            // Username
            var lblUsername = new Label { Text = "Username *", Left = 24, Top = y, Width = 230, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtUsername = new TextBox { Left = 24, Top = y + 20, Width = 230, Height = 30 };
            Theme.StyleTextBox(txtUsername);

            // Password
            var lblPass = new Label
            {
                Text = _existingUser == null ? "Password *" : "Reset Password (leave blank to keep)",
                Left = 270,
                Top = y,
                Width = 240,
                Height = 18,
                ForeColor = Theme.DarkText,
                Font = Theme.LabelFont
            };
            txtPassword = new TextBox { Left = 270, Top = y + 20, Width = 200, Height = 30, UseSystemPasswordChar = true };
            Theme.StyleTextBox(txtPassword);

            btnTogglePass = new Button
            {
                Text = "👁",
                Left = 475,
                Top = y + 20,
                Width = 35,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.White,
                Cursor = Cursors.Hand
            };
            btnTogglePass.FlatAppearance.BorderColor = Theme.CardBorder;
            btnTogglePass.Click += (s, e) => txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;

            body.Controls.AddRange(new Control[] { lblUsername, txtUsername, lblPass, txtPassword, btnTogglePass });
            y += 62;

            // First Name & Last Name
            var lblFirst = new Label { Text = "First Name *", Left = 24, Top = y, Width = 230, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtFirstName = new TextBox { Left = 24, Top = y + 20, Width = 230, Height = 30 };
            Theme.StyleTextBox(txtFirstName);

            var lblLast = new Label { Text = "Last Name *", Left = 270, Top = y, Width = 240, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtLastName = new TextBox { Left = 270, Top = y + 20, Width = 240, Height = 30 };
            Theme.StyleTextBox(txtLastName);

            body.Controls.AddRange(new Control[] { lblFirst, txtFirstName, lblLast, txtLastName });
            y += 62;

            // Middle Name & Email
            var lblMiddle = new Label { Text = "Middle Name (Optional)", Left = 24, Top = y, Width = 230, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtMiddleName = new TextBox { Left = 24, Top = y + 20, Width = 230, Height = 30 };
            Theme.StyleTextBox(txtMiddleName);

            var lblEmail = new Label { Text = "Email Address (Optional)", Left = 270, Top = y, Width = 240, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtEmail = new TextBox { Left = 270, Top = y + 20, Width = 240, Height = 30 };
            Theme.StyleTextBox(txtEmail);

            body.Controls.AddRange(new Control[] { lblMiddle, txtMiddleName, lblEmail, txtEmail });
            y += 62;

            // Role & Branch
            var lblRole = new Label { Text = "User Role (Administrator Only)", Left = 24, Top = y, Width = 230, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            cboRole = new ComboBox
            {
                Left = 24,
                Top = y + 20,
                Width = 230,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.LabelFont,
                Enabled = false
            };
            cboRole.Items.Add("Admin");
            cboRole.SelectedIndex = 0;

            var lblBranch = new Label { Text = "Assigned Branch", Left = 270, Top = y, Width = 240, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            cboBranch = new ComboBox
            {
                Left = 270,
                Top = y + 20,
                Width = 240,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.LabelFont
            };

            // Populate branches
            cboBranch.Items.Add(new BranchComboItem { Id = null, Display = "All Branches / Head Office (None)" });
            foreach (var b in _branches)
            {
                cboBranch.Items.Add(new BranchComboItem { Id = b.Id, Display = b.ToString() });
            }
            cboBranch.SelectedIndex = 0;

            body.Controls.AddRange(new Control[] { lblRole, cboRole, lblBranch, cboBranch });
            y += 66;

            // Active Checkbox
            chkIsActive = new CheckBox
            {
                Text = "Account is Active (check to allow login, uncheck to deactivate access)",
                Left = 24,
                Top = y,
                Width = 480,
                Height = 24,
                Checked = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };
            body.Controls.Add(chkIsActive);
            y += 40;

            // Info note
            var lblHint = new Label
            {
                Text = "Note: SuperAdmins provision and manage Company Administrator accounts. Internal company roles (Managers, Tech, Sales) are managed directly by the tenant Administrator.",
                Left = 24,
                Top = y,
                Width = 486,
                Height = 44,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.MutedText
            };
            body.Controls.Add(lblHint);

            // Bottom Buttons
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.White,
                Padding = new Padding(24, 12, 24, 12)
            };
            footer.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
            };

            btnSave = new Button
            {
                Text = _existingUser == null ? "💾 Create Admin" : "💾 Update Admin",
                Width = 140,
                Height = 36,
                Left = footer.Width - 140 - 24,
                Top = 12,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += async (s, e) => await SaveUserAsync();

            btnCancel = new Button
            {
                Text = "✕ Cancel",
                Width = 100,
                Height = 36,
                Left = btnSave.Left - 110,
                Top = 12,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnCancel);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            footer.Controls.AddRange(new Control[] { btnSave, btnCancel });

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private void PopulateData()
        {
            if (_existingUser == null) return;

            txtUsername.Text = _existingUser.Username;
            txtUsername.Enabled = false; // keep username immutable or read-only on edit
            txtFirstName.Text = _existingUser.FirstName;
            txtMiddleName.Text = _existingUser.MiddleName ?? "";
            txtLastName.Text = _existingUser.LastName;
            txtEmail.Text = _existingUser.Email;
            chkIsActive.Checked = _existingUser.IsActive;

            // Role is fixed to Admin
            cboRole.SelectedIndex = 0;

            // Set branch
            if (_existingUser.BranchId.HasValue)
            {
                for (int i = 0; i < cboBranch.Items.Count; i++)
                {
                    if (cboBranch.Items[i] is BranchComboItem item && item.Id == _existingUser.BranchId.Value)
                    {
                        cboBranch.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private async Task SaveUserAsync()
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text.Trim();
            var firstName = txtFirstName.Text.Trim();
            var lastName = txtLastName.Text.Trim();
            var middleName = txtMiddleName.Text.Trim();
            var email = string.IsNullOrWhiteSpace(txtEmail.Text)
                ? $"{username}@{_companyCode.ToLower()}.local"
                : txtEmail.Text.Trim();
            var roleEnum = UserRole.Admin;
            var branchItem = cboBranch.SelectedItem as BranchComboItem;
            int? branchId = branchItem?.Id;
            bool isActive = chkIsActive.Checked;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            if (_existingUser == null && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required for a new administrator account.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("First name and last name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFirstName.Focus();
                return;
            }

            btnSave.Enabled = false;
            btnSave.Text = "Saving…";

            try
            {
                if (_existingUser == null)
                {
                    // CREATE
                    var payload = new
                    {
                        username = username,
                        password = password,
                        firstName = firstName,
                        middleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
                        lastName = lastName,
                        email = email,
                        role = (int)roleEnum,
                        branchId = branchId,
                        isActive = isActive
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Post, $"api/superadmin/tenants/{_companyId}/users")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                    };
                    if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Company-Code"))
                        request.Headers.TryAddWithoutValidation("X-Company-Code", "MASTER");
                    if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Api-User"))
                        request.Headers.TryAddWithoutValidation("X-Api-User", CurrentSession.Username);

                    var response = await ApiConfig.Http.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Administrator '{username}' created successfully for {_companyName}.",
                            "Administrator Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UserSaved = true;
                        DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to create administrator:\n{err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // UPDATE
                    var payload = new
                    {
                        firstName = firstName,
                        middleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
                        lastName = lastName,
                        email = email,
                        role = (int)roleEnum,
                        branchId = branchId,
                        isActive = isActive,
                        newPassword = string.IsNullOrWhiteSpace(password) ? null : password
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Put, $"api/superadmin/tenants/{_companyId}/users/{_existingUser.Id}")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                    };
                    if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Company-Code"))
                        request.Headers.TryAddWithoutValidation("X-Company-Code", "MASTER");
                    if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Api-User"))
                        request.Headers.TryAddWithoutValidation("X-Api-User", CurrentSession.Username);

                    var response = await ApiConfig.Http.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Administrator '{_existingUser.Username}' updated successfully.",
                            "Administrator Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UserSaved = true;
                        DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to update administrator:\n{err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Network or server error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                btnSave.Text = _existingUser == null ? "💾 Create Admin" : "💾 Update Admin";
            }
        }

        private class BranchComboItem
        {
            public int? Id { get; set; }
            public string Display { get; set; } = string.Empty;
            public override string ToString() => Display;
        }
    }
}
