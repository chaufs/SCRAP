using System;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public class RenewSubscriptionDialog : Form
    {
        private readonly TenantViewModel _tenant;

        private ComboBox cboPlan = null!;
        private ComboBox cboDuration = null!;
        private DateTimePicker dtpCustomExpiry = null!;
        private Label lblCalculatedExpiry = null!;
        private Label lblCalculatedAmount = null!;
        private TextBox txtNotes = null!;
        private Button btnConfirm = null!;
        private Button btnCancel = null!;

        public RenewSubscriptionDialog(TenantViewModel tenant)
        {
            _tenant = tenant;

            Text = $"Renew Subscription — {tenant.CompanyName} ({tenant.CompanyCode})";
            Width = 580;
            Height = 620;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildUi();
            Recalculate();
        }

        private void BuildUi()
        {
            // ── Top Header ──────────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
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
                Text = "Renew & Extend Subscription",
                Left = 24,
                Top = 14,
                Width = 400,
                Height = 26,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            var lblSubtitle = new Label
            {
                Text = "Extend subscription duration, restore deactivated accounts, and record audit details.",
                Left = 24,
                Top = 42,
                Width = 520,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);
            Controls.Add(header);

            // ── Main Content Container ──────────────────────────────────────────────
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };
            Controls.Add(content);
            content.BringToFront();

            int y = 8;
            int w = 512;

            // ── Tenant Summary Card ─────────────────────────────────────────────────
            var cardTenant = new Panel
            {
                Left = 0,
                Top = y,
                Width = w,
                Height = 76,
                BackColor = Theme.White
            };
            cardTenant.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardTenant.Width - 1, cardTenant.Height - 1);
            };

            var badgeCode = new Label
            {
                Text = _tenant.CompanyCode ?? "CODE",
                Left = 14,
                Top = 12,
                Width = 72,
                Height = 24,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Blue,
                BackColor = Theme.SoftBlue,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblCompName = new Label
            {
                Text = _tenant.CompanyName ?? "Company",
                Left = 94,
                Top = 12,
                Width = 400,
                Height = 24,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            bool isExpired = _tenant.IsExpired;
            var lblStatusLine = new Label
            {
                Text = $"Current Plan: {_tenant.SubscriptionPlan ?? "Standard"}   •   Status: {(isExpired ? "EXPIRED" : (_tenant.IsActive ? "Active" : "Suspended"))}   •   Expires: {_tenant.ExpiryFormatted}",
                Left = 14,
                Top = 44,
                Width = 480,
                Height = 20,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = isExpired ? ColorTranslator.FromHtml("#DC2626") : Theme.MutedText
            };

            cardTenant.Controls.Add(badgeCode);
            cardTenant.Controls.Add(lblCompName);
            cardTenant.Controls.Add(lblStatusLine);
            content.Controls.Add(cardTenant);
            y += 88;

            // ── Renewal Options Card ────────────────────────────────────────────────
            var cardForm = new Panel
            {
                Left = 0,
                Top = y,
                Width = w,
                Height = 280,
                BackColor = Theme.White
            };
            cardForm.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardForm.Width - 1, cardForm.Height - 1);
            };

            // Plan Tier
            var lblPlan = new Label
            {
                Text = "Subscription Tier:",
                Left = 16,
                Top = 16,
                Width = 140,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };
            cboPlan = new ComboBox
            {
                Left = 160,
                Top = 12,
                Width = 336,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboPlan.Items.AddRange(new object[] { "Basic ($99/mo)", "Standard ($249/mo)", "Enterprise ($599/mo)" });
            Theme.StyleComboBox(cboPlan);
            int planIdx = _tenant.SubscriptionPlan?.ToLowerInvariant() switch
            {
                "basic" => 0,
                "enterprise" => 2,
                _ => 1
            };
            cboPlan.SelectedIndex = planIdx;
            cboPlan.SelectedIndexChanged += (s, e) => Recalculate();

            // Renewal Duration
            var lblDuration = new Label
            {
                Text = "Renewal Period:",
                Left = 16,
                Top = 56,
                Width = 140,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };
            cboDuration = new ComboBox
            {
                Left = 160,
                Top = 52,
                Width = 336,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboDuration.Items.AddRange(new object[]
            {
                "1 Month",
                "3 Months",
                "6 Months",
                "1 Year (12 Months) — Recommended",
                "Custom Expiration Date..."
            });
            cboDuration.SelectedIndex = 3; // 1 Year default
            Theme.StyleComboBox(cboDuration);
            cboDuration.SelectedIndexChanged += CboDuration_SelectedIndexChanged;

            // Custom Expiry DatePicker (hidden by default)
            var lblCustom = new Label
            {
                Text = "Custom Expiry Date:",
                Left = 16,
                Top = 96,
                Width = 140,
                Height = 20,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                Visible = false
            };
            dtpCustomExpiry = new DateTimePicker
            {
                Left = 160,
                Top = 92,
                Width = 336,
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today.AddDays(1),
                Value = DateTime.Today.AddYears(1),
                Visible = false
            };
            dtpCustomExpiry.ValueChanged += (s, e) => Recalculate();

            // Calculations Display Box
            var pnlCalc = new Panel
            {
                Left = 16,
                Top = 132,
                Width = 480,
                Height = 64,
                BackColor = Theme.SoftBlue
            };
            pnlCalc.Paint += (s, e) =>
            {
                using var p = new Pen(ColorTranslator.FromHtml("#BFDBFE"), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlCalc.Width - 1, pnlCalc.Height - 1);
            };

            lblCalculatedExpiry = new Label
            {
                Text = "New Expiration: Calculating...",
                Left = 14,
                Top = 10,
                Width = 450,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.Blue,
                BackColor = Color.Transparent
            };

            lblCalculatedAmount = new Label
            {
                Text = "Renewal Rate: Calculating...",
                Left = 14,
                Top = 34,
                Width = 450,
                Height = 20,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            pnlCalc.Controls.Add(lblCalculatedExpiry);
            pnlCalc.Controls.Add(lblCalculatedAmount);

            // Audit Memo
            var lblMemo = new Label
            {
                Text = "Audit / Notes:",
                Left = 16,
                Top = 208,
                Width = 140,
                Height = 20,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.MutedText
            };
            txtNotes = new TextBox
            {
                Left = 16,
                Top = 232,
                Width = 480,
                Height = 30
            };
            Theme.StyleTextBox(txtNotes);
            txtNotes.PlaceholderText = "Reason for renewal or invoice payment reference (optional)";

            cardForm.Controls.Add(lblPlan);
            cardForm.Controls.Add(cboPlan);
            cardForm.Controls.Add(lblDuration);
            cardForm.Controls.Add(cboDuration);
            cardForm.Controls.Add(lblCustom);
            cardForm.Controls.Add(dtpCustomExpiry);
            cardForm.Controls.Add(pnlCalc);
            cardForm.Controls.Add(lblMemo);
            cardForm.Controls.Add(txtNotes);

            content.Controls.Add(cardForm);
            y += 292;

            // ── Action Buttons ──────────────────────────────────────────────────────
            btnConfirm = new Button
            {
                Text = "✓ Confirm & Renew Subscription",
                Left = 0,
                Top = y,
                Width = 320,
                Height = 40
            };
            Theme.StylePrimaryButton(btnConfirm);
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 330,
                Top = y,
                Width = 100,
                Height = 40
            };
            Theme.StyleOutlineButton(btnCancel);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            content.Controls.Add(btnConfirm);
            content.Controls.Add(btnCancel);
        }

        private void CboDuration_SelectedIndexChanged(object? sender, EventArgs e)
        {
            bool isCustom = cboDuration.SelectedIndex == 4;
            dtpCustomExpiry.Visible = isCustom;
            if (cardFormHasCustomLabel(out var lblCustom))
            {
                lblCustom.Visible = isCustom;
            }
            Recalculate();
        }

        private bool cardFormHasCustomLabel(out Label lbl)
        {
            foreach (Control c in Controls)
            {
                if (c is Panel p)
                {
                    foreach (Control child in p.Controls)
                    {
                        if (child is Panel inner)
                        {
                            foreach (Control sub in inner.Controls)
                            {
                                if (sub is Label l && l.Text == "Custom Expiry Date:")
                                {
                                    lbl = l;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            lbl = null!;
            return false;
        }

        private void Recalculate()
        {
            int months = GetSelectedMonths();
            string planName = GetSelectedPlan();
            decimal monthlyRate = planName switch
            {
                "Basic" => 99.00m,
                "Enterprise" => 599.00m,
                _ => 249.00m
            };

            DateTime targetExpiry;
            var now = DateTime.UtcNow;

            if (cboDuration.SelectedIndex == 4) // Custom
            {
                targetExpiry = dtpCustomExpiry.Value;
                int estMonths = Math.Max(1, (int)Math.Round((targetExpiry - now).TotalDays / 30.0));
                decimal total = monthlyRate * estMonths;
                lblCalculatedExpiry.Text = $"New Expiration: {targetExpiry:MMMM dd, yyyy}";
                lblCalculatedAmount.Text = $"Est. Renewal Total: ${total:N2} (~{estMonths} month(s) @ ${monthlyRate:N2}/mo)";
            }
            else
            {
                // If active in the future, extend from current expiry date; if expired or null, extend from today
                if (_tenant.SubscriptionExpiresAt.HasValue && _tenant.SubscriptionExpiresAt.Value > now)
                {
                    targetExpiry = _tenant.SubscriptionExpiresAt.Value.AddMonths(months);
                }
                else
                {
                    targetExpiry = now.AddMonths(months);
                }

                decimal total = monthlyRate * months;
                lblCalculatedExpiry.Text = $"New Expiration: {targetExpiry:MMMM dd, yyyy}";
                lblCalculatedAmount.Text = $"Total Amount: ${total:N2} ({months} month(s) @ ${monthlyRate:N2}/mo)";
            }
        }

        private int GetSelectedMonths()
        {
            return cboDuration.SelectedIndex switch
            {
                0 => 1,
                1 => 3,
                2 => 6,
                3 => 12,
                _ => 1
            };
        }

        private string GetSelectedPlan()
        {
            return cboPlan.SelectedIndex switch
            {
                0 => "Basic",
                2 => "Enterprise",
                _ => "Standard"
            };
        }

        private async void BtnConfirm_Click(object? sender, EventArgs e)
        {
            btnConfirm.Enabled = false;
            btnConfirm.Text = "Renewing...";

            try
            {
                string planName = GetSelectedPlan();
                int months = GetSelectedMonths();
                DateTime? customDate = cboDuration.SelectedIndex == 4 ? dtpCustomExpiry.Value : null;

                var req = new
                {
                    Months = months,
                    NewExpiryDate = customDate,
                    PlanName = planName,
                    Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim()
                };

                var res = await ApiConfig.Http.PostAsJsonAsync($"api/superadmin/tenants/{_tenant.CompanyId}/renew", req);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show(this,
                        $"Subscription for '{_tenant.CompanyName}' has been successfully renewed!\r\n\r\nThe tenant is now ACTIVE and access has been restored.",
                        "Subscription Renewed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    _tenant.IsActive = true;
                    _tenant.SubscriptionPlan = planName;
                    _tenant.SubscriptionExpiresAt = customDate ?? (
                        (_tenant.SubscriptionExpiresAt.HasValue && _tenant.SubscriptionExpiresAt.Value > DateTime.UtcNow)
                            ? _tenant.SubscriptionExpiresAt.Value.AddMonths(months)
                            : DateTime.UtcNow.AddMonths(months)
                    );

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    string err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show(this, "Failed to renew subscription: " + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConfirm.Enabled = true;
                btnConfirm.Text = "✓ Confirm & Renew Subscription";
            }
        }
    }
}
