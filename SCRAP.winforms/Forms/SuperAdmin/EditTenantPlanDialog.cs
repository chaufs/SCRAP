using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public class EditTenantPlanDialog : Form
    {
        private readonly TenantViewModel _tenant;
        private ComboBox cboPlan = null!;
        private DateTimePicker dtpExpires = null!;
        private readonly Dictionary<string, CheckBox> _moduleCheckboxes = new();
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private static readonly (string Key, string Label, string Description)[] AvailableModules = new[]
        {
            ("Inventory", "Inventory Management", "Stock levels, item catalog, and scrap intake"),
            ("Sales", "Sales & Invoicing", "Customer sales, quotes, and delivery receipts"),
            ("Technical", "Technical & Teardown", "Device inspection, teardown yield, and destruction certificates"),
            ("HR", "HR Management", "Staff directories, attendance, and employee assignments"),
            ("Branches", "Branch Management", "Multi-branch operations and inter-branch transfers"),
            ("Reports", "Analytics & Reports", "Financial metrics, scrap recovery, and compliance reports"),
            ("Finance", "Company Finance", "Revenue tracking, cost allocation, and expense monitoring")
        };

        public EditTenantPlanDialog(TenantViewModel tenant)
        {
            _tenant = tenant;

            Text = $"Customize Subscription — {tenant.CompanyName}";
            Width = 540;
            Height = 620;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildUi();
            LoadCurrentData();
        }

        private void BuildUi()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
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
                Text = "Plan & Module Access",
                Left = 24,
                Top = 14,
                Width = 400,
                Height = 24,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            var lblSub = new Label
            {
                Text = $"Subscriber: {_tenant.CompanyName} ({_tenant.CompanyCode})",
                Left = 24,
                Top = 38,
                Width = 450,
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
                Padding = new Padding(24, 16, 24, 16)
            };

            int y = 14;

            var lblPlan = new Label
            {
                Text = "Subscription Tier:",
                Left = 24,
                Top = y,
                Width = 140,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            cboPlan = new ComboBox
            {
                Left = 170,
                Top = y - 4,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboPlan.Items.AddRange(new object[] { "Basic", "Standard", "Enterprise", "Custom" });
            Theme.StyleComboBox(cboPlan);
            cboPlan.SelectedIndexChanged += CboPlan_SelectedIndexChanged;

            body.Controls.Add(lblPlan);
            body.Controls.Add(cboPlan);

            y += 38;

            var lblExp = new Label
            {
                Text = "Expires On:",
                Left = 24,
                Top = y,
                Width = 140,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            dtpExpires = new DateTimePicker
            {
                Left = 170,
                Top = y - 4,
                Width = 200,
                Format = DateTimePickerFormat.Short
            };

            body.Controls.Add(lblExp);
            body.Controls.Add(dtpExpires);

            y += 44;

            var lblSection = new Label
            {
                Text = "ENABLED MODULES FOR THIS SUBSCRIBER",
                Left = 24,
                Top = y,
                Width = 450,
                Height = 20,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };
            body.Controls.Add(lblSection);
            y += 24;

            var cardModules = new Panel
            {
                Left = 24,
                Top = y,
                Width = 475,
                Height = 280,
                BackColor = Theme.White
            };
            cardModules.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardModules.Width - 1, cardModules.Height - 1);
            };

            int modY = 12;
            foreach (var mod in AvailableModules)
            {
                var chk = new CheckBox
                {
                    Text = mod.Label,
                    Left = 16,
                    Top = modY,
                    Width = 200,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.DarkText,
                    Cursor = Cursors.Hand
                };

                var lblDesc = new Label
                {
                    Text = mod.Description,
                    Left = 216,
                    Top = modY + 2,
                    Width = 245,
                    Height = 18,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Theme.MutedText
                };

                cardModules.Controls.Add(chk);
                cardModules.Controls.Add(lblDesc);
                _moduleCheckboxes[mod.Key] = chk;

                modY += 36;
            }

            body.Controls.Add(cardModules);

            // Bottom action panel
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

            btnSave = new Button
            {
                Text = "Save Changes",
                Left = 270,
                Top = 12,
                Width = 130,
                Height = 36
            };
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 410,
                Top = 12,
                Width = 90,
                Height = 36
            };
            Theme.StyleOutlineButton(btnCancel);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            bottom.Controls.Add(btnSave);
            bottom.Controls.Add(btnCancel);

            Controls.Add(body);
            Controls.Add(bottom);
            Controls.Add(header);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadCurrentData()
        {
            // Plan
            string plan = _tenant.SubscriptionPlan ?? "Standard";
            int idx = cboPlan.FindStringExact(plan);
            if (idx >= 0) cboPlan.SelectedIndex = idx;
            else cboPlan.SelectedItem = "Custom";

            // Expiration
            if (_tenant.SubscriptionExpiresAt.HasValue)
            {
                dtpExpires.Value = _tenant.SubscriptionExpiresAt.Value;
            }
            else
            {
                dtpExpires.Value = DateTime.UtcNow.AddYears(1);
            }

            // Modules
            string mods = _tenant.EnabledModules ?? "";
            bool isAll = mods.Equals("All", StringComparison.OrdinalIgnoreCase);

            foreach (var kvp in _moduleCheckboxes)
            {
                kvp.Value.Checked = isAll || mods.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void CboPlan_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string selected = cboPlan.SelectedItem?.ToString() ?? "";
            if (selected == "Basic")
            {
                SetModules("Inventory", "Reports");
            }
            else if (selected == "Standard")
            {
                SetModules("Inventory", "Sales", "Technical", "HR", "Branches", "Reports");
            }
            else if (selected == "Enterprise")
            {
                SetModules("Inventory", "Sales", "Technical", "HR", "Branches", "Reports", "Finance");
            }
        }

        private void SetModules(params string[] allowed)
        {
            var set = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _moduleCheckboxes)
            {
                kvp.Value.Checked = set.Contains(kvp.Key);
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var selectedMods = _moduleCheckboxes
                .Where(kvp => kvp.Value.Checked)
                .Select(kvp => kvp.Key)
                .ToList();

            string enabledModulesString = string.Join(",", selectedMods);
            string chosenPlan = cboPlan.SelectedItem?.ToString() ?? "Standard";

            btnSave.Enabled = false;
            btnSave.Text = "Saving...";

            try
            {
                var req = new
                {
                    SubscriptionPlan = chosenPlan,
                    SubscriptionExpiresAt = dtpExpires.Value,
                    EnabledModules = enabledModulesString
                };

                var res = await ApiConfig.Http.PutAsJsonAsync($"api/superadmin/tenants/{_tenant.CompanyId}/plan", req);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Subscriber plan and module permissions updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    string err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Failed to update plan: " + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                btnSave.Text = "Save Changes";
            }
        }
    }
}
