using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    public class CustomizePlanDialog : Form
    {
        private readonly SubscriptionPlanItem _plan;
        private readonly bool _isNew;

        private TextBox txtName = null!;
        private TextBox txtDisplayName = null!;
        private NumericUpDown numPrice = null!;
        private ComboBox cboBillingCycle = null!;
        private TextBox txtDescription = null!;
        private ComboBox cboColor = null!;
        private CheckBox chkIsRecommended = null!;
        private readonly Dictionary<string, CheckBox> _moduleCheckboxes = new();
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public SubscriptionPlanItem PlanResult { get; private set; } = null!;

        public static readonly (string Key, string Label, string Description)[] AllModules = new[]
        {
            ("Inventory", "Inventory Management", "Stock levels, item catalog, and scrap intake"),
            ("Procurement", "Procurement & Intake", "Supplier purchases, receiving, and intake batches"),
            ("Sales", "Sales & Invoicing", "Customer sales, quotes, and delivery receipts"),
            ("Technical", "Technical & Teardown", "Device inspection, teardown yield, and destruction certificates"),
            ("HR", "HR Management", "Staff directories, attendance, and employee assignments"),
            ("Branches", "Branch Management", "Multi-branch operations and inter-branch transfers"),
            ("Reports", "Analytics & Reports", "Financial metrics, scrap recovery, and compliance reports"),
            ("Finance", "Company Finance", "Revenue tracking, cost allocation, and expense monitoring")
        };

        private static readonly (string Name, string Hex, Color Color)[] ColorPresets = new[]
        {
            ("Emerald Green (#16A34A)", "#16A34A", ColorTranslator.FromHtml("#16A34A")),
            ("Royal Blue (#2563EB)", "#2563EB", ColorTranslator.FromHtml("#2563EB")),
            ("Indigo (#4F46E5)", "#4F46E5", ColorTranslator.FromHtml("#4F46E5")),
            ("Purple (#9333EA)", "#9333EA", ColorTranslator.FromHtml("#9333EA")),
            ("Amber / Gold (#D97706)", "#D97706", ColorTranslator.FromHtml("#D97706")),
            ("Rose Red (#E11D48)", "#E11D48", ColorTranslator.FromHtml("#E11D48")),
            ("Slate (#475569)", "#475569", ColorTranslator.FromHtml("#475569"))
        };

        public CustomizePlanDialog(SubscriptionPlanItem plan, bool isNew = false)
        {
            _plan = plan;
            _isNew = isNew;

            Text = isNew ? "Add Subscription Plan" : $"Customize Plan — {plan.DisplayName}";
            Width = 580;
            Height = 710;
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
                Text = _isNew ? "Create New Subscription Plan" : $"Customize {_plan.DisplayName}",
                Left = 24,
                Top = 12,
                Width = 500,
                Height = 24,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            var lblSub = new Label
            {
                Text = "Configure pricing, descriptive details, and module feature gates for this subscription tier.",
                Left = 24,
                Top = 38,
                Width = 520,
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
                Padding = new Padding(24, 14, 24, 14),
                AutoScroll = true
            };

            int y = 10;
            int colW = 240;

            // Plan Code / Name & Display Name
            var lblName = new Label { Text = "Plan Identifier / Code *", Left = 24, Top = y, Width = colW, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtName = new TextBox { Left = 24, Top = y + 20, Width = colW, Height = 30 };
            Theme.StyleTextBox(txtName);
            if (!_isNew) txtName.ReadOnly = true;

            var lblDisp = new Label { Text = "Display Title *", Left = 280, Top = y, Width = colW, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtDisplayName = new TextBox { Left = 280, Top = y + 20, Width = colW, Height = 30 };
            Theme.StyleTextBox(txtDisplayName);

            body.Controls.AddRange(new Control[] { lblName, txtName, lblDisp, txtDisplayName });
            y += 60;

            // Price & Billing Cycle
            var lblPrice = new Label { Text = "Price (₱) *", Left = 24, Top = y, Width = colW, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            numPrice = new NumericUpDown
            {
                Left = 24,
                Top = y + 20,
                Width = colW,
                Height = 30,
                DecimalPlaces = 2,
                Maximum = 999999,
                Minimum = 0,
                ThousandsSeparator = true,
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblCycle = new Label { Text = "Billing Cycle *", Left = 280, Top = y, Width = colW, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            cboBillingCycle = new ComboBox
            {
                Left = 280,
                Top = y + 20,
                Width = colW,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.LabelFont
            };
            cboBillingCycle.Items.AddRange(new object[] { "month", "year", "one-time" });
            cboBillingCycle.SelectedIndex = 0;

            body.Controls.AddRange(new Control[] { lblPrice, numPrice, lblCycle, cboBillingCycle });
            y += 60;

            // Description
            var lblDesc = new Label { Text = "Plan Description", Left = 24, Top = y, Width = 496, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            txtDescription = new TextBox { Left = 24, Top = y + 20, Width = 496, Height = 50, Multiline = true };
            Theme.StyleTextBox(txtDescription);

            body.Controls.AddRange(new Control[] { lblDesc, txtDescription });
            y += 80;

            // Accent Color & Recommended
            var lblColor = new Label { Text = "Card Accent Color", Left = 24, Top = y, Width = colW, Height = 18, ForeColor = Theme.DarkText, Font = Theme.LabelFont };
            cboColor = new ComboBox
            {
                Left = 24,
                Top = y + 20,
                Width = colW,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.LabelFont
            };
            foreach (var cp in ColorPresets)
            {
                cboColor.Items.Add(cp.Name);
            }
            cboColor.SelectedIndex = 0;

            chkIsRecommended = new CheckBox
            {
                Text = "Highlight as Recommended Tier",
                Left = 280,
                Top = y + 20,
                Width = colW,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                Cursor = Cursors.Hand
            };

            body.Controls.AddRange(new Control[] { lblColor, cboColor, chkIsRecommended });
            y += 64;

            // Module Gates Checklist
            var lblModulesHeader = new Label
            {
                Text = "DEFAULT ENABLED MODULES FOR THIS PLAN",
                Left = 24,
                Top = y,
                Width = 496,
                Height = 18,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };
            body.Controls.Add(lblModulesHeader);
            y += 22;

            var cardModules = new Panel
            {
                Left = 24,
                Top = y,
                Width = 496,
                Height = 210,
                BackColor = Theme.White,
                AutoScroll = true
            };
            cardModules.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardModules.Width - 1, cardModules.Height - 1);
            };

            int modY = 10;
            foreach (var mod in AllModules)
            {
                var chk = new CheckBox
                {
                    Text = mod.Label,
                    Left = 14,
                    Top = modY,
                    Width = 195,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Theme.DarkText,
                    Cursor = Cursors.Hand
                };

                var lblModDesc = new Label
                {
                    Text = mod.Description,
                    Left = 212,
                    Top = modY + 2,
                    Width = 265,
                    Height = 18,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Theme.MutedText
                };

                cardModules.Controls.Add(chk);
                cardModules.Controls.Add(lblModDesc);
                _moduleCheckboxes[mod.Key] = chk;

                modY += 28;
            }

            body.Controls.Add(cardModules);

            // Footer
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
                Text = "💾 Save Plan",
                Width = 130,
                Height = 36,
                Left = footer.Width - 130 - 24,
                Top = 12,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += (s, e) => SavePlan();

            btnCancel = new Button
            {
                Text = "Cancel",
                Width = 90,
                Height = 36,
                Left = btnSave.Left - 100,
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
            txtName.Text = _plan.Name;
            txtDisplayName.Text = _plan.DisplayName;
            numPrice.Value = _plan.Price >= 0 ? _plan.Price : 0;
            txtDescription.Text = _plan.Description;
            chkIsRecommended.Checked = _plan.IsRecommended;

            int cycleIdx = cboBillingCycle.FindStringExact(_plan.BillingCycle ?? "month");
            cboBillingCycle.SelectedIndex = cycleIdx >= 0 ? cycleIdx : 0;

            // Match color preset
            for (int i = 0; i < ColorPresets.Length; i++)
            {
                if (string.Equals(ColorPresets[i].Hex, _plan.ColorHex, StringComparison.OrdinalIgnoreCase))
                {
                    cboColor.SelectedIndex = i;
                    break;
                }
            }

            // Enabled modules
            var enabledSet = new HashSet<string>(
                (_plan.EnabledModules ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()),
                StringComparer.OrdinalIgnoreCase);

            bool isAll = (_plan.EnabledModules ?? "").Equals("All", StringComparison.OrdinalIgnoreCase);

            foreach (var kvp in _moduleCheckboxes)
            {
                kvp.Value.Checked = isAll || enabledSet.Contains(kvp.Key);
            }
        }

        private void SavePlan()
        {
            string name = txtName.Text.Trim();
            string displayName = txtDisplayName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Plan Identifier/Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                MessageBox.Show("Display Title is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDisplayName.Focus();
                return;
            }

            var selectedMods = _moduleCheckboxes
                .Where(kvp => kvp.Value.Checked)
                .Select(kvp => kvp.Key)
                .ToList();

            string selectedHex = "#2563EB";
            if (cboColor.SelectedIndex >= 0 && cboColor.SelectedIndex < ColorPresets.Length)
            {
                selectedHex = ColorPresets[cboColor.SelectedIndex].Hex;
            }

            PlanResult = new SubscriptionPlanItem
            {
                Name = name,
                DisplayName = displayName,
                Price = numPrice.Value,
                BillingCycle = cboBillingCycle.SelectedItem?.ToString() ?? "month",
                Description = txtDescription.Text.Trim(),
                EnabledModules = string.Join(",", selectedMods),
                ColorHex = selectedHex,
                IsRecommended = chkIsRecommended.Checked
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
