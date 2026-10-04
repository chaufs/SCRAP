using System;
using System.Drawing;
using System.Net.Http.Json;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public class RegisterTenantForm : Form
    {
        private TextBox txtCompanyName = null!;
        private TextBox txtContactEmail = null!;
        private ComboBox cboSubscriptionPlan = null!;
        
        private TextBox txtAdminUsername = null!;
        private TextBox txtAdminPassword = null!;
        private TextBox txtAdminFirstName = null!;
        private TextBox txtAdminLastName = null!;

        private Button btnRegister = null!;
        private Button btnCancel = null!;

        public RegisterTenantForm()
        {
            Text = "Register New Tenant";
            Width = 520;
            Height = 620;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildUi();
        }

        private void BuildUi()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.White,
                Padding = new Padding(24, 16, 24, 0)
            };
            header.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "Register New Tenant",
                Left = 24,
                Top = 14,
                Width = 400,
                Height = 24,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            var lblSub = new Label
            {
                Text = "Creates Master records, auto-generates code, and provisions dedicated database.",
                Left = 24,
                Top = 38,
                Width = 460,
                Height = 20,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(24, 14, 24, 14)
            };

            int y = 10;
            int cardW = 455;

            // Section 1: Company
            var lblSec1 = new Label
            {
                Text = "COMPANY INFORMATION",
                Left = 24,
                Top = y,
                Width = 300,
                Height = 18,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };
            body.Controls.Add(lblSec1);
            y += 22;

            var card1 = new Panel
            {
                Left = 24,
                Top = y,
                Width = cardW,
                Height = 145,
                BackColor = Theme.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            card1.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, card1.Width - 1, card1.Height - 1);
            };

            AddRow(card1, "Company Name *", out txtCompanyName, 12, false);
            AddRow(card1, "Contact Email", out txtContactEmail, 52, false);
            AddComboRow(card1, "Subscription Plan", out cboSubscriptionPlan, 92, new[] { "Basic", "Standard", "Enterprise" });
            cboSubscriptionPlan.SelectedIndex = 1;

            body.Controls.Add(card1);
            y += 160;

            // Section 2: Admin User
            var lblSec2 = new Label
            {
                Text = "INITIAL ADMIN CREDENTIALS",
                Left = 24,
                Top = y,
                Width = 300,
                Height = 18,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };
            body.Controls.Add(lblSec2);
            y += 22;

            var card2 = new Panel
            {
                Left = 24,
                Top = y,
                Width = cardW,
                Height = 185,
                BackColor = Theme.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            card2.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, card2.Width - 1, card2.Height - 1);
            };

            AddRow(card2, "Admin Username *", out txtAdminUsername, 12, false);
            txtAdminUsername.Text = "admin";

            AddRow(card2, "Admin Password *", out txtAdminPassword, 52, true);
            txtAdminPassword.Text = "Admin@123";

            AddRow(card2, "First Name", out txtAdminFirstName, 92, false);
            AddRow(card2, "Last Name", out txtAdminLastName, 132, false);

            body.Controls.Add(card2);

            // Bottom Buttons
            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.White
            };
            bottom.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, 0, bottom.Width, 0);
            };

            btnRegister = new Button
            {
                Text = "Register & Provision DB",
                Left = 230,
                Top = 12,
                Width = 175,
                Height = 36
            };
            Theme.StylePrimaryButton(btnRegister);
            btnRegister.Click += BtnRegister_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 415,
                Top = 12,
                Width = 75,
                Height = 36
            };
            Theme.StyleOutlineButton(btnCancel);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            bottom.Controls.Add(btnRegister);
            bottom.Controls.Add(btnCancel);

            Controls.Add(body);
            Controls.Add(bottom);
            Controls.Add(header);

            AcceptButton = btnRegister;
            CancelButton = btnCancel;
        }

        private static void AddRow(Panel parent, string label, out TextBox txt, int top, bool isPassword)
        {
            var lbl = new Label
            {
                Text = label,
                Left = 16,
                Top = top + 4,
                Width = 140,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            txt = new TextBox
            {
                Left = 165,
                Top = top,
                Width = 260,
                Height = 28
            };
            if (isPassword) txt.PasswordChar = '•';
            Theme.StyleTextBox(txt);

            parent.Controls.Add(lbl);
            parent.Controls.Add(txt);
        }

        private static void AddComboRow(Panel parent, string label, out ComboBox cbo, int top, string[] items)
        {
            var lbl = new Label
            {
                Text = label,
                Left = 16,
                Top = top + 4,
                Width = 140,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            cbo = new ComboBox
            {
                Left = 165,
                Top = top,
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbo.Items.AddRange(items);
            Theme.StyleComboBox(cbo);

            parent.Controls.Add(lbl);
            parent.Controls.Add(cbo);
        }

        private async void BtnRegister_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCompanyName.Text) ||
                string.IsNullOrWhiteSpace(txtAdminUsername.Text) ||
                string.IsNullOrWhiteSpace(txtAdminPassword.Text))
            {
                MessageBox.Show("Please fill out all required (*) fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var req = new
            {
                CompanyCode = "", // Auto-generated on backend
                CompanyName = txtCompanyName.Text.Trim(),
                ContactEmail = txtContactEmail.Text.Trim(),
                SubscriptionPlan = cboSubscriptionPlan.SelectedItem?.ToString() ?? "Standard",
                AdminUsername = txtAdminUsername.Text.Trim(),
                AdminPassword = txtAdminPassword.Text,
                AdminFirstName = txtAdminFirstName.Text.Trim(),
                AdminLastName = txtAdminLastName.Text.Trim()
            };

            btnRegister.Enabled = false;
            btnRegister.Text = "Provisioning DB...";
            Cursor = Cursors.WaitCursor;

            try
            {
                var response = await ApiConfig.Http.PostAsJsonAsync("api/superadmin/tenants/register", req);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Tenant successfully registered and dedicated database provisioned!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to register tenant: {err}", "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRegister.Enabled = true;
                btnRegister.Text = "Register & Provision DB";
                Cursor = Cursors.Default;
            }
        }
    }
}
