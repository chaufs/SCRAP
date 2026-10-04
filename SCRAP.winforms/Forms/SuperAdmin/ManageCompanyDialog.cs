using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    public class ManageCompanyDialog : Form
    {
        private readonly TenantViewModel _tenant;

        // Navigation buttons
        private Button btnTabPlan = null!;
        private Button btnTabHistory = null!;
        private Button btnTabUsers = null!;
        private Panel tabContainer = null!;
        private Panel panelPlan = null!;
        private Panel panelHistory = null!;
        private Panel panelUsers = null!;
        private Label lblDetails = null!;

        // Plan Tab Controls
        private Panel cardRenewalStatus = null!;
        private Label lblRenewalStatusMsg = null!;
        private ComboBox cboPlan = null!;
        private DateTimePicker dtpExpires = null!;
        private TextBox txtNotes = null!;
        private readonly Dictionary<string, CheckBox> _moduleCheckboxes = new();
        private Button btnSavePlan = null!;
        private List<SubscriptionPlanItem> _loadedPlans = new();

        // History Tab Controls
        private DataGridView dgvHistory = null!;
        private Button btnRefreshHistory = null!;
        private Label lblHistoryCount = null!;

        // Users Tab Controls
        private DataGridView dgvUsers = null!;
        private Button btnRefreshUsers = null!;
        private Label lblUsersCount = null!;
        private TextBox txtUserSearch = null!;
        private List<TenantUserViewModel> _usersList = new();
        private List<TenantBranchItem> _tenantBranches = new();

        private static readonly (string Key, string Label, string Description)[] AvailableModules = new[]
        {
            ("Inventory", "Inventory Management", "Stock levels, item catalog, and material balances"),
            ("Procurement", "Procurement & Intake", "Raw scrap purchasing, intake weighing, and supplier bills"),
            ("Sales", "Sales & Invoicing", "Customer sales, quotes, and delivery receipts"),
            ("Technical", "Technical & Teardown", "Device inspection, teardown yield, and destruction certificates"),
            ("HR", "HR Management", "Staff directories, attendance, and employee assignments"),
            ("Branches", "Branch Management", "Multi-branch operations and inter-branch transfers"),
            ("Reports", "Analytics & Reports", "Financial metrics, scrap recovery, and compliance reports"),
            ("Finance", "Company Finance", "Revenue tracking, cost allocation, and expense monitoring")
        };

        public ManageCompanyDialog(TenantViewModel tenant, int initialTab = 0)
        {
            _tenant = tenant;

            if (SubscriptionPlansOverviewForm.CachedPlans != null && SubscriptionPlansOverviewForm.CachedPlans.Count > 0)
            {
                _loadedPlans = new List<SubscriptionPlanItem>(SubscriptionPlansOverviewForm.CachedPlans);
            }

            Text = $"Manage Company — {tenant.CompanyName} ({tenant.CompanyCode})";
            Width = 1040;
            Height = 740;
            MinimumSize = new Size(880, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildUi(initialTab);
            PopulatePlanComboBox();
            LoadCurrentPlanData();
            _ = LoadAvailablePlansAsync();
            _ = LoadHistoryData();
            _ = LoadUsersData();
        }

        private void BuildUi(int initialTab = 0)
        {
            // Top Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Theme.White,
                Padding = new Padding(28, 16, 28, 0)
            };
            header.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var badgeCode = new Label
            {
                Text = _tenant.CompanyCode ?? "CODE",
                Left = 28,
                Top = 16,
                Width = 80,
                Height = 24,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Blue,
                BackColor = Theme.SoftBlue,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblCompanyName = new Label
            {
                Text = _tenant.CompanyName ?? "Company Details",
                Left = 118,
                Top = 14,
                Width = 480,
                Height = 28,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            lblDetails = new Label
            {
                Text = $"Email: {_tenant.ContactEmail ?? "N/A"}   •   Current Plan: {_tenant.SubscriptionPlan ?? "Standard"}   •   Status: {_tenant.StatusText}",
                Left = 28,
                Top = 48,
                Width = 680,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            header.Controls.Add(badgeCode);
            header.Controls.Add(lblCompanyName);
            header.Controls.Add(lblDetails);

            // Tab bar
            var tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.White,
                Padding = new Padding(28, 0, 28, 0)
            };
            tabBar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, tabBar.Height - 1, tabBar.Width, tabBar.Height - 1);
            };

            btnTabPlan = new Button
            {
                Text = "Plan & Module Access",
                UseMnemonic = false,
                Left = 28,
                Top = 4,
                Width = 200,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.ButtonFont,
                BackColor = Theme.SoftBlue,
                ForeColor = Theme.Blue,
                Cursor = Cursors.Hand
            };
            btnTabPlan.FlatAppearance.BorderSize = 0;
            btnTabPlan.Click += (s, e) => SwitchTab(0);

            btnTabHistory = new Button
            {
                Text = "Subscription History",
                UseMnemonic = false,
                Left = 236,
                Top = 4,
                Width = 200,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.ButtonFont,
                BackColor = Theme.White,
                ForeColor = Theme.MutedText,
                Cursor = Cursors.Hand
            };
            btnTabHistory.FlatAppearance.BorderSize = 0;
            btnTabHistory.Click += (s, e) => SwitchTab(1);

            btnTabUsers = new Button
            {
                Text = "Company Admins",
                UseMnemonic = false,
                Left = 444,
                Top = 4,
                Width = 210,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.ButtonFont,
                BackColor = Theme.White,
                ForeColor = Theme.MutedText,
                Cursor = Cursors.Hand
            };
            btnTabUsers.FlatAppearance.BorderSize = 0;
            btnTabUsers.Click += (s, e) => SwitchTab(2);

            tabBar.Controls.AddRange(new Control[] { btnTabPlan, btnTabHistory, btnTabUsers });

            // Tab container
            tabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(28, 16, 28, 16)
            };

            BuildPlanPanel();
            BuildHistoryPanel();
            BuildUsersPanel();

            tabContainer.Controls.Add(panelPlan);
            tabContainer.Controls.Add(panelHistory);
            tabContainer.Controls.Add(panelUsers);

            Controls.Add(tabContainer);
            Controls.Add(tabBar);
            Controls.Add(header);

            SwitchTab(initialTab);
        }

        private void SwitchTab(int tabIndex)
        {
            btnTabPlan.BackColor = tabIndex == 0 ? Theme.SoftBlue : Theme.White;
            btnTabPlan.ForeColor = tabIndex == 0 ? Theme.Blue : Theme.MutedText;

            btnTabHistory.BackColor = tabIndex == 1 ? Theme.SoftBlue : Theme.White;
            btnTabHistory.ForeColor = tabIndex == 1 ? Theme.Blue : Theme.MutedText;

            btnTabUsers.BackColor = tabIndex == 2 ? Theme.SoftBlue : Theme.White;
            btnTabUsers.ForeColor = tabIndex == 2 ? Theme.Blue : Theme.MutedText;

            panelPlan.Visible = tabIndex == 0;
            panelHistory.Visible = tabIndex == 1;
            panelUsers.Visible = tabIndex == 2;

            if (tabIndex == 0) panelPlan.BringToFront();
            else if (tabIndex == 1) { panelHistory.BringToFront(); _ = LoadHistoryData(); }
            else if (tabIndex == 2) { panelUsers.BringToFront(); _ = LoadUsersData(); }
        }

        private void BuildPlanPanel()
        {
            panelPlan = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                AutoScroll = true
            };

            int y = 8;
            int cardW = 950;

            // ── Subscription Status & Quick Renewal Banner ──────────────────────────
            cardRenewalStatus = new Panel
            {
                Left = 0,
                Top = y,
                Width = cardW,
                Height = 60,
                BackColor = Theme.White
            };
            cardRenewalStatus.Paint += (s, e) =>
            {
                Color border = _tenant.IsExpired ? ColorTranslator.FromHtml("#FCA5A5")
                    : (_tenant.IsExpiringSoon ? ColorTranslator.FromHtml("#FCD34D") : ColorTranslator.FromHtml("#86EFAC"));
                using var p = new Pen(border, 1.5f);
                e.Graphics.DrawRectangle(p, 0, 0, cardRenewalStatus.Width - 1, cardRenewalStatus.Height - 1);
            };

            lblRenewalStatusMsg = new Label
            {
                Left = 18,
                Top = 18,
                Width = 490,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            var btnOpenRenew = new Button
            {
                Text = "⚡ Renew Subscription...",
                Left = 520,
                Top = 12,
                Width = 190,
                Height = 36
            };
            Theme.StylePrimaryButton(btnOpenRenew);
            btnOpenRenew.Click += async (s, e) =>
            {
                using var dlg = new RenewSubscriptionDialog(_tenant);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    UpdateRenewalStatusCard();
                    LoadCurrentPlanData();
                    await LoadHistoryData();
                }
            };

            var btnQuick1Yr = new Button
            {
                Text = "+1 Year",
                Left = 720,
                Top = 12,
                Width = 90,
                Height = 36
            };
            Theme.StyleOutlineButton(btnQuick1Yr);
            btnQuick1Yr.Click += async (s, e) => await QuickRenewAsync(12);

            var btnQuick1Mo = new Button
            {
                Text = "+1 Month",
                Left = 820,
                Top = 12,
                Width = 95,
                Height = 36
            };
            Theme.StyleOutlineButton(btnQuick1Mo);
            btnQuick1Mo.Click += async (s, e) => await QuickRenewAsync(1);

            cardRenewalStatus.Controls.Add(lblRenewalStatusMsg);
            cardRenewalStatus.Controls.Add(btnOpenRenew);
            cardRenewalStatus.Controls.Add(btnQuick1Yr);
            cardRenewalStatus.Controls.Add(btnQuick1Mo);

            panelPlan.Controls.Add(cardRenewalStatus);
            y += 72;

            // Tier & Expiration card
            var cardMeta = new Panel
            {
                Left = 0,
                Top = y,
                Width = cardW,
                Height = 85,
                BackColor = Theme.White
            };
            cardMeta.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardMeta.Width - 1, cardMeta.Height - 1);
            };

            var lblPlan = new Label
            {
                Text = "Subscription Tier:",
                Left = 20,
                Top = 16,
                Width = 140,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            cboPlan = new ComboBox
            {
                Left = 165,
                Top = 12,
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cboPlan);
            cboPlan.SelectedIndexChanged += CboPlan_SelectedIndexChanged;

            var lblExp = new Label
            {
                Text = "Expires On:",
                Left = 380,
                Top = 16,
                Width = 100,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            dtpExpires = new DateTimePicker
            {
                Left = 485,
                Top = 12,
                Width = 160,
                Format = DateTimePickerFormat.Short
            };

            var lblMemo = new Label
            {
                Text = "Audit / Update Memo:",
                Left = 20,
                Top = 50,
                Width = 140,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.MutedText
            };

            txtNotes = new TextBox
            {
                Left = 165,
                Top = 48,
                Width = 750,
                Height = 26
            };
            Theme.StyleTextBox(txtNotes);
            txtNotes.PlaceholderText = "Reason for modifying subscription plan or permissions (optional)";

            cardMeta.Controls.Add(lblPlan);
            cardMeta.Controls.Add(cboPlan);
            cardMeta.Controls.Add(lblExp);
            cardMeta.Controls.Add(dtpExpires);
            cardMeta.Controls.Add(lblMemo);
            cardMeta.Controls.Add(txtNotes);

            panelPlan.Controls.Add(cardMeta);
            y += 98;

            // Module checklist card
            var lblSec = new Label
            {
                Text = "INCLUDED MODULES FOR THIS PLAN (PRECONFIGURED BY TIER)",
                Left = 0,
                Top = y,
                Width = 600,
                Height = 18,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };
            panelPlan.Controls.Add(lblSec);
            y += 24;

            var cardModules = new Panel
            {
                Left = 0,
                Top = y,
                Width = cardW,
                Height = 310,
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
                    UseMnemonic = false,
                    AutoCheck = false,
                    Left = 20,
                    Top = modY,
                    Width = 220,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Theme.DarkText,
                    Cursor = Cursors.Default
                };

                var lblDesc = new Label
                {
                    Text = mod.Description,
                    UseMnemonic = false,
                    Left = 250,
                    Top = modY + 2,
                    Width = 660,
                    Height = 20,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Theme.MutedText
                };

                cardModules.Controls.Add(chk);
                cardModules.Controls.Add(lblDesc);
                _moduleCheckboxes[mod.Key] = chk;

                modY += 36;
            }

            panelPlan.Controls.Add(cardModules);
            y += 325;

            // Action row
            btnSavePlan = new Button
            {
                Text = "Save Plan & Permissions",
                Left = 0,
                Top = y,
                Width = 230,
                Height = 38
            };
            Theme.StylePrimaryButton(btnSavePlan);
            btnSavePlan.Click += BtnSavePlan_Click;

            var btnClose = new Button
            {
                Text = "Close",
                Left = 215,
                Top = y,
                Width = 90,
                Height = 38
            };
            Theme.StyleOutlineButton(btnClose);
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            panelPlan.Controls.Add(btnSavePlan);
            panelPlan.Controls.Add(btnClose);
        }

        private void BuildHistoryPanel()
        {
            panelHistory = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Visible = false
            };

            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.Background
            };

            lblHistoryCount = new Label
            {
                Text = "Subscription audit records for this company:",
                Left = 0,
                Top = 10,
                Width = 400,
                Height = 24,
                Font = Theme.LabelFont,
                ForeColor = Theme.MutedText
            };

            btnRefreshHistory = new Button
            {
                Text = "↻ Refresh History",
                Top = 4,
                Width = 150,
                Height = 32
            };
            Theme.StyleOutlineButton(btnRefreshHistory);
            btnRefreshHistory.Click += async (s, e) => await LoadHistoryData();

            topBar.Controls.Add(lblHistoryCount);
            topBar.Controls.Add(btnRefreshHistory);

            topBar.Resize += (s, e) =>
            {
                btnRefreshHistory.Left = Math.Max(400, topBar.ClientSize.Width - btnRefreshHistory.Width);
            };

            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.White
            };
            gridCard.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, gridCard.Width - 1, gridCard.Height - 1);
            };

            dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvHistory);

            SetupHistoryColumns();
            gridCard.Controls.Add(dgvHistory);

            panelHistory.Controls.Add(gridCard);
            panelHistory.Controls.Add(topBar);
        }

        private void SetupHistoryColumns()
        {
            dgvHistory.Columns.Clear();
            dgvHistory.AutoGenerateColumns = false;

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreatedAtFormatted",
                HeaderText = "Date & Time",
                Width = 185,
                MinimumWidth = 175,
                FillWeight = 20
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PlanName",
                HeaderText = "Plan Tier",
                Width = 130,
                MinimumWidth = 120,
                FillWeight = 14,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AmountFormatted",
                HeaderText = "Rate / mo",
                Width = 110,
                MinimumWidth = 100,
                FillWeight = 12,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "EndDateFormatted",
                HeaderText = "Expires",
                Width = 130,
                MinimumWidth = 120,
                FillWeight = 14
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 95,
                MinimumWidth = 90,
                FillWeight = 10,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Notes",
                HeaderText = "Memo / Details",
                Width = 320,
                MinimumWidth = 240,
                FillWeight = 30
            });

            dgvHistory.CellFormatting += (s, e) =>
            {
                if (dgvHistory.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? "";
                    if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Theme.Green;
                        e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    }
                }
            };
        }

        private void PopulatePlanComboBox()
        {
            string currentSelected = cboPlan.SelectedItem?.ToString() ?? _tenant.SubscriptionPlan ?? "Standard";

            cboPlan.SelectedIndexChanged -= CboPlan_SelectedIndexChanged;
            cboPlan.Items.Clear();

            if (_loadedPlans.Count > 0)
            {
                foreach (var p in _loadedPlans)
                {
                    if (!string.IsNullOrWhiteSpace(p.Name) && !cboPlan.Items.Contains(p.Name))
                        cboPlan.Items.Add(p.Name);
                }
            }
            else
            {
                cboPlan.Items.AddRange(new object[] { "Basic", "Standard", "Enterprise" });
            }

            if (!string.IsNullOrWhiteSpace(currentSelected) && !cboPlan.Items.Contains(currentSelected))
            {
                cboPlan.Items.Add(currentSelected);
            }

            int idx = cboPlan.FindStringExact(currentSelected);
            cboPlan.SelectedIndex = idx >= 0 ? idx : 0;
            cboPlan.SelectedIndexChanged += CboPlan_SelectedIndexChanged;

            SyncModulesToPlan(cboPlan.SelectedItem?.ToString() ?? "Standard");
        }

        private async Task LoadAvailablePlansAsync()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/platform/settings/plans");
                if (res.IsSuccessStatusCode)
                {
                    var plans = await res.Content.ReadFromJsonAsync<List<SubscriptionPlanItem>>();
                    if (plans != null && plans.Count > 0)
                    {
                        _loadedPlans = plans;
                        SubscriptionPlansOverviewForm.CachedPlans = new List<SubscriptionPlanItem>(plans);

                        if (!IsDisposed)
                        {
                            if (InvokeRequired)
                            {
                                BeginInvoke(new Action(() => PopulatePlanComboBox()));
                            }
                            else
                            {
                                PopulatePlanComboBox();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Keep cached/fallback plans
            }
        }

        private void LoadCurrentPlanData()
        {
            string plan = _tenant.SubscriptionPlan ?? "Standard";
            int idx = cboPlan.FindStringExact(plan);
            if (idx >= 0) cboPlan.SelectedIndex = idx;
            else if (cboPlan.Items.Count > 0) cboPlan.SelectedIndex = 0;

            if (_tenant.SubscriptionExpiresAt.HasValue)
            {
                dtpExpires.Value = _tenant.SubscriptionExpiresAt.Value;
            }
            else
            {
                dtpExpires.Value = DateTime.UtcNow.AddYears(1);
            }

            SyncModulesToPlan(cboPlan.SelectedItem?.ToString() ?? "Standard");
            UpdateRenewalStatusCard();
        }

        private void UpdateRenewalStatusCard()
        {
            if (lblDetails != null)
            {
                lblDetails.Text = $"Email: {_tenant.ContactEmail ?? "N/A"}   •   Current Plan: {_tenant.SubscriptionPlan ?? "Standard"}   •   Status: {_tenant.StatusText}";
            }

            if (cardRenewalStatus == null || lblRenewalStatusMsg == null) return;

            if (_tenant.IsExpired)
            {
                cardRenewalStatus.BackColor = ColorTranslator.FromHtml("#FEF2F2"); // Light red
                lblRenewalStatusMsg.ForeColor = ColorTranslator.FromHtml("#DC2626"); // Red
                lblRenewalStatusMsg.Text = $"⚠️ SUBSCRIPTION EXPIRED ({_tenant.ExpiryFormatted}) — Access deactivated.";
            }
            else if (_tenant.IsExpiringSoon)
            {
                cardRenewalStatus.BackColor = ColorTranslator.FromHtml("#FFFBEB"); // Light yellow
                lblRenewalStatusMsg.ForeColor = ColorTranslator.FromHtml("#D97706"); // Amber
                lblRenewalStatusMsg.Text = $"⏳ EXPIRING SOON — Valid until {_tenant.ExpiryFormatted}.";
            }
            else
            {
                cardRenewalStatus.BackColor = ColorTranslator.FromHtml("#F0FDF4"); // Light green
                lblRenewalStatusMsg.ForeColor = ColorTranslator.FromHtml("#16A34A"); // Green
                lblRenewalStatusMsg.Text = $"✅ ACTIVE SUBSCRIPTION — Valid through {_tenant.ExpiryFormatted}.";
            }
            cardRenewalStatus.Invalidate();
        }

        private async Task QuickRenewAsync(int months)
        {
            var confirm = MessageBox.Show(this,
                $"Are you sure you want to renew '{_tenant.CompanyName}' for {months} month(s) under its current '{_tenant.SubscriptionPlan}' tier?",
                "Confirm Quick Renewal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var req = new
                {
                    Months = months,
                    PlanName = _tenant.SubscriptionPlan ?? "Standard",
                    Notes = $"Quick renewal (+{months} month(s)) performed from Company Management console."
                };

                var res = await ApiConfig.Http.PostAsJsonAsync($"api/superadmin/tenants/{_tenant.CompanyId}/renew", req);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show(this, $"Subscription renewed successfully for {months} month(s)!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _tenant.IsActive = true;
                    var now = DateTime.UtcNow;
                    if (_tenant.SubscriptionExpiresAt.HasValue && _tenant.SubscriptionExpiresAt.Value > now)
                        _tenant.SubscriptionExpiresAt = _tenant.SubscriptionExpiresAt.Value.AddMonths(months);
                    else
                        _tenant.SubscriptionExpiresAt = now.AddMonths(months);

                    UpdateRenewalStatusCard();
                    LoadCurrentPlanData();
                    await LoadHistoryData();
                }
                else
                {
                    string err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show(this, "Failed to renew: " + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CboPlan_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string selected = cboPlan.SelectedItem?.ToString() ?? "Standard";
            SyncModulesToPlan(selected);
        }

        private void SyncModulesToPlan(string plan)
        {
            var planItem = _loadedPlans.FirstOrDefault(p => string.Equals(p.Name, plan, StringComparison.OrdinalIgnoreCase));
            if (planItem != null && !string.IsNullOrWhiteSpace(planItem.EnabledModules))
            {
                var enabledSet = new HashSet<string>(
                    planItem.EnabledModules.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                bool isAll = planItem.EnabledModules.Equals("All", StringComparison.OrdinalIgnoreCase);

                foreach (var kvp in _moduleCheckboxes)
                {
                    kvp.Value.Checked = isAll || enabledSet.Contains(kvp.Key);
                }
                return;
            }

            // Fallback presets if plans have not yet loaded
            if (string.Equals(plan, "Basic", StringComparison.OrdinalIgnoreCase))
            {
                SetModules("Inventory", "Procurement", "Sales", "Technical", "Reports");
            }
            else if (string.Equals(plan, "Standard", StringComparison.OrdinalIgnoreCase))
            {
                SetModules("Inventory", "Procurement", "Sales", "Technical", "Reports");
            }
            else // Enterprise
            {
                SetModules("Inventory", "Procurement", "Sales", "Technical", "HR", "Branches", "Reports", "Finance");
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

        private async Task LoadHistoryData()
        {
            try
            {
                var history = await ApiConfig.Http.GetFromJsonAsync<SubscriptionHistoryViewModel[]>($"api/superadmin/tenants/{_tenant.CompanyId}/history");
                var list = history?.ToList() ?? new List<SubscriptionHistoryViewModel>();
                dgvHistory.DataSource = list;
                lblHistoryCount.Text = $"Showing {list.Count} subscription history records for this company:";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading subscription history: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSavePlan_Click(object? sender, EventArgs e)
        {
            var selectedMods = _moduleCheckboxes
                .Where(kvp => kvp.Value.Checked)
                .Select(kvp => kvp.Key)
                .ToList();

            string enabledModulesString = string.Join(",", selectedMods);
            string chosenPlan = cboPlan.SelectedItem?.ToString() ?? "Standard";

            btnSavePlan.Enabled = false;
            btnSavePlan.Text = "Saving...";

            try
            {
                var req = new
                {
                    SubscriptionPlan = chosenPlan,
                    SubscriptionExpiresAt = dtpExpires.Value,
                    EnabledModules = enabledModulesString,
                    Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim()
                };

                var res = await ApiConfig.Http.PutAsJsonAsync($"api/superadmin/tenants/{_tenant.CompanyId}/plan", req);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Subscriber plan and module permissions updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _tenant.SubscriptionPlan = chosenPlan;
                    _tenant.SubscriptionExpiresAt = dtpExpires.Value;
                    _tenant.EnabledModules = enabledModulesString;
                    await LoadHistoryData();
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
                btnSavePlan.Enabled = true;
                btnSavePlan.Text = "Save Plan & Permissions";
            }
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  TAB 3: USERS & ACCOUNTS
        // ════════════════════════════════════════════════════════════════════════════

        private void BuildUsersPanel()
        {
            panelUsers = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background
            };

            // Top action bar
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Background
            };

            var lblUsersTitle = new Label
            {
                Text = "Company Administrators",
                Left = 0,
                Top = 6,
                Width = 300,
                Height = 22,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            lblUsersCount = new Label
            {
                Text = "Loading administrators…",
                Left = 0,
                Top = 32,
                Width = 320,
                Height = 20,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            var btnAddUser = new Button
            {
                Text = "+ Add Admin",
                Left = 310,
                Top = 12,
                Width = 115,
                Height = 36
            };
            Theme.StylePrimaryButton(btnAddUser);
            btnAddUser.Click += BtnAddUser_Click;

            var btnToggleUser = new Button
            {
                Text = "⏻ Deactivate / Activate",
                Left = 435,
                Top = 12,
                Width = 175,
                Height = 36
            };
            Theme.StyleOutlineButton(btnToggleUser);
            btnToggleUser.Click += BtnToggleUserStatus_Click;

            var btnEditUser = new Button
            {
                Text = "✏ Edit Admin",
                Left = 620,
                Top = 12,
                Width = 110,
                Height = 36
            };
            Theme.StyleSecondaryButton(btnEditUser);
            btnEditUser.Click += BtnEditUser_Click;

            btnRefreshUsers = new Button
            {
                Text = "↻ Refresh",
                Left = 740,
                Top = 12,
                Width = 90,
                Height = 36
            };
            Theme.StyleOutlineButton(btnRefreshUsers);
            btnRefreshUsers.Click += async (s, e) => await LoadUsersData();

            txtUserSearch = new TextBox
            {
                Width = 150,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleTextBox(txtUserSearch);
            txtUserSearch.TextChanged += (s, e) => ApplyUsersFilter();

            var lblSearch = new Label
            {
                Text = "Search:",
                Width = 55,
                Height = 24,
                Font = Theme.LabelFont,
                ForeColor = Theme.MutedText,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            void PositionSearch()
            {
                int r = bar.ClientSize.Width;
                txtUserSearch.Location = new Point(r - txtUserSearch.Width, 14);
                lblSearch.Location = new Point(txtUserSearch.Left - lblSearch.Width - 6, 18);
            }
            bar.Resize += (s, e) => PositionSearch();

            bar.Controls.AddRange(new Control[]
            {
                lblUsersTitle, lblUsersCount, btnAddUser, btnToggleUser, btnEditUser, btnRefreshUsers,
                lblSearch, txtUserSearch
            });
            PositionSearch();

            // Grid card
            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.White
            };
            gridCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, gridCard.Width - 1, gridCard.Height - 1);
            };

            dgvUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvUsers);
            SetupUsersColumns();

            dgvUsers.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) BtnEditUser_Click(s, e);
            };

            gridCard.Controls.Add(dgvUsers);

            panelUsers.Controls.Add(gridCard);
            panelUsers.Controls.Add(bar);
        }

        private void SetupUsersColumns()
        {
            dgvUsers.AutoGenerateColumns = false;
            dgvUsers.Columns.Clear();

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                HeaderText = "ID",
                Width = 50,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Username",
                HeaderText = "Username",
                Width = 130,
                DefaultCellStyle = { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.DarkText }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FullName",
                HeaderText = "Full Name",
                MinimumWidth = 170,
                FillWeight = 50,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Role",
                HeaderText = "Role",
                Width = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "BranchName",
                HeaderText = "Assigned Branch",
                Width = 180
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "StatusText",
                HeaderText = "Status",
                Width = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            var colAction = new DataGridViewButtonColumn
            {
                Name = "Action",
                HeaderText = "Action",
                Width = 120,
                Text = "Toggle Status",
                UseColumnTextForButtonValue = false,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgvUsers.Columns.Add(colAction);

            dgvUsers.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvUsers.Rows.Count) return;
                var rowItem = dgvUsers.Rows[e.RowIndex].DataBoundItem as TenantUserViewModel;
                if (rowItem == null) return;

                if (dgvUsers.Columns[e.ColumnIndex].DataPropertyName == "StatusText")
                {
                    e.CellStyle.ForeColor = rowItem.IsActive ? Theme.Green : ColorTranslator.FromHtml("#DC2626");
                }
                else if (dgvUsers.Columns[e.ColumnIndex].DataPropertyName == "Role")
                {
                    e.CellStyle.ForeColor = rowItem.Role switch
                    {
                        "Admin" => Theme.Blue,
                        "Manager" => ColorTranslator.FromHtml("#4F46E5"),
                        "TechStaff" => ColorTranslator.FromHtml("#D97706"),
                        _ => Theme.Green
                    };
                }
                else if (dgvUsers.Columns[e.ColumnIndex].Name == "Action")
                {
                    e.Value = rowItem.IsActive ? "Deactivate" : "Activate";
                }
            };

            dgvUsers.CellContentClick += async (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvUsers.Columns[e.ColumnIndex].Name == "Action")
                {
                    if (dgvUsers.Rows[e.RowIndex].DataBoundItem is TenantUserViewModel user)
                    {
                        await ToggleUserStatusAsync(user);
                    }
                }
            };
        }

        private async Task LoadUsersData()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/superadmin/tenants/{_tenant.CompanyId}/users?role=Admin");
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Company-Code"))
                    request.Headers.TryAddWithoutValidation("X-Company-Code", "MASTER");
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Api-User"))
                    request.Headers.TryAddWithoutValidation("X-Api-User", CurrentSession.Username);

                var response = await ApiConfig.Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = System.Text.Json.JsonSerializer.Deserialize<TenantUsersResponse>(json, ApiConfig.JsonOptions);
                    if (result != null)
                    {
                        var rawUsers = result.Users ?? new List<TenantUserViewModel>();
                        _usersList = rawUsers
                            .Where(u => string.Equals(u.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        _tenantBranches = result.Branches ?? new List<TenantBranchItem>();
                        ApplyUsersFilter();
                        UpdateUsersStats();
                    }
                }
                else
                {
                    lblUsersCount.Text = "Failed to load administrators.";
                }
            }
            catch (Exception ex)
            {
                lblUsersCount.Text = "Error: " + ex.Message;
            }
        }

        private void UpdateUsersStats()
        {
            int total = _usersList.Count;
            int active = _usersList.Count(u => u.IsActive);
            int deact = _usersList.Count(u => !u.IsActive);
            lblUsersCount.Text = $"{total} Total Admin{(total != 1 ? "s" : "")} • {active} Active • {deact} Deactivated";
        }

        private void ApplyUsersFilter()
        {
            string q = txtUserSearch?.Text?.Trim().ToLowerInvariant() ?? "";
            var filtered = string.IsNullOrEmpty(q)
                ? _usersList
                : _usersList.Where(u =>
                    (u.Username?.ToLowerInvariant().Contains(q) ?? false) ||
                    (u.FullName?.ToLowerInvariant().Contains(q) ?? false) ||
                    (u.BranchName?.ToLowerInvariant().Contains(q) ?? false)).ToList();

            dgvUsers.DataSource = null;
            dgvUsers.DataSource = filtered;
            dgvUsers.Refresh();
        }

        private async void BtnAddUser_Click(object? sender, EventArgs e)
        {
            using var dlg = new AddEditTenantUserDialog(_tenant.CompanyId, _tenant.CompanyCode ?? "", _tenant.CompanyName ?? "", _tenantBranches);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadUsersData();
            }
        }

        private async void BtnEditUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var user = dgvUsers.SelectedRows[0].DataBoundItem as TenantUserViewModel;
            if (user == null) return;

            using var dlg = new AddEditTenantUserDialog(_tenant.CompanyId, _tenant.CompanyCode ?? "", _tenant.CompanyName ?? "", _tenantBranches, user);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadUsersData();
            }
        }

        private async void BtnToggleUserStatus_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to toggle status.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var user = dgvUsers.SelectedRows[0].DataBoundItem as TenantUserViewModel;
            if (user != null)
            {
                await ToggleUserStatusAsync(user);
            }
        }

        private async Task ToggleUserStatusAsync(TenantUserViewModel user)
        {
            bool targetActive = !user.IsActive;
            string actionText = targetActive ? "activate" : "deactivate";
            string msg = targetActive
                ? $"Reactivate login access for user '{user.Username}' in {_tenant.CompanyName}?"
                : $"Are you sure you want to deactivate login access for user '{user.Username}' in {_tenant.CompanyName}?\nThey will immediately be blocked from logging into the platform.";

            var confirm = MessageBox.Show(msg, "Confirm User Status Change", MessageBoxButtons.YesNo,
                targetActive ? MessageBoxIcon.Question : MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var payload = new { IsActive = targetActive };
                using var request = new HttpRequestMessage(HttpMethod.Post, $"api/superadmin/tenants/{_tenant.CompanyId}/users/{user.Id}/status")
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
                };
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Company-Code"))
                    request.Headers.TryAddWithoutValidation("X-Company-Code", "MASTER");
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Api-User"))
                    request.Headers.TryAddWithoutValidation("X-Api-User", CurrentSession.Username);

                var response = await ApiConfig.Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    await LoadUsersData();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to update status:\n{err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating user status: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class SubscriptionHistoryViewModel
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public string AmountFormatted => $"₱{Amount:N2}";
        public string CreatedAtFormatted => CreatedAt.ToString("MMM dd, yyyy  h:mm tt");
        public string EndDateFormatted => EndDate.ToString("MMM dd, yyyy");
    }
}
