using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public class TenantManagementForm : Form
    {
        private DataGridView dgvTenants = null!;
        private Button btnRefresh = null!;
        private Button btnManage = null!;
        private Button btnManageUsers = null!;
        private Button btnRenew = null!;
        private Button btnAddTenant = null!;
        private Button btnToggleStatus = null!;
        private TextBox txtSearch = null!;

        // Stat cards
        private Label lblTotalCount = null!;
        private Label lblActiveCount = null!;
        private Label lblEnterpriseCount = null!;

        private List<TenantViewModel> _tenantsList = new();

        public TenantManagementForm()
        {
            Text = "Tenant Management";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;

            BuildUi();
            _ = LoadData();
        }

        private void BuildUi()
        {
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Theme.Background,
                Padding = new Padding(32, 24, 32, 0)
            };

            var lblTitle = new Label
            {
                Text = "Subscribers & Tenancy",
                Left = 32,
                Top = 20,
                Width = 500,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            var lblSubtitle = new Label
            {
                Text = "Provision dedicated tenant databases, configure subscriptions, and customize module permissions.",
                Left = 32,
                Top = 58,
                Width = 750,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);

            // Stats row
            int cardW = 180;
            int cardH = 54;
            int cardY = 88;

            var card1 = CreateMetricCard("TOTAL SUBSCRIBERS", out lblTotalCount, 32, cardY, cardW, cardH);
            var card2 = CreateMetricCard("ACTIVE TENANTS", out lblActiveCount, 32 + cardW + 16, cardY, cardW, cardH);
            var card3 = CreateMetricCard("ENTERPRISE PLANS", out lblEnterpriseCount, 32 + (cardW + 16) * 2, cardY, cardW, cardH);

            headerPanel.Controls.Add(card1);
            headerPanel.Controls.Add(card2);
            headerPanel.Controls.Add(card3);

            // Toolbar Panel
            var toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.Background,
                Padding = new Padding(32, 10, 32, 10)
            };

            btnAddTenant = new Button
            {
                Text = "+ Register New Tenant",
                Left = 32,
                Top = 8,
                Width = 180,
                Height = 36
            };
            Theme.StylePrimaryButton(btnAddTenant);
            btnAddTenant.Click += BtnAddTenant_Click;

            btnManage = new Button
            {
                Text = "Manage Tenant",
                Left = 220,
                Top = 8,
                Width = 125,
                Height = 36
            };
            Theme.StyleSecondaryButton(btnManage);
            btnManage.Click += BtnManage_Click;

            btnManageUsers = new Button
            {
                Text = "👥 Users",
                Left = 353,
                Top = 8,
                Width = 90,
                Height = 36
            };
            Theme.StyleOutlineButton(btnManageUsers);
            btnManageUsers.Click += BtnManageUsers_Click;

            btnRenew = new Button
            {
                Text = "🔄 Renew Plan",
                Left = 451,
                Top = 8,
                Width = 125,
                Height = 36
            };
            Theme.StylePrimaryButton(btnRenew);
            btnRenew.Click += BtnRenew_Click;

            btnToggleStatus = new Button
            {
                Text = "Activate/Deactivate",
                Left = 584,
                Top = 8,
                Width = 145,
                Height = 36
            };
            Theme.StyleOutlineButton(btnToggleStatus);
            btnToggleStatus.Click += BtnToggleStatus_Click;

            btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Left = 737,
                Top = 8,
                Width = 90,
                Height = 36
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadData();

            var lblFilter = new Label
            {
                Text = "Filter:",
                Top = 16,
                Width = 45,
                Height = 24,
                Font = Theme.LabelFont,
                ForeColor = Theme.MutedText,
                TextAlign = ContentAlignment.MiddleRight
            };

            txtSearch = new TextBox
            {
                Top = 10,
                Width = 200,
                Height = 32
            };
            Theme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            toolbarPanel.Controls.Add(btnAddTenant);
            toolbarPanel.Controls.Add(btnManage);
            toolbarPanel.Controls.Add(btnManageUsers);
            toolbarPanel.Controls.Add(btnRenew);
            toolbarPanel.Controls.Add(btnToggleStatus);
            toolbarPanel.Controls.Add(btnRefresh);
            toolbarPanel.Controls.Add(lblFilter);
            toolbarPanel.Controls.Add(txtSearch);

            toolbarPanel.Resize += (s, e) =>
            {
                txtSearch.Left = toolbarPanel.ClientSize.Width - 32 - txtSearch.Width;
                lblFilter.Left = txtSearch.Left - lblFilter.Width - 6;
            };

            // Grid container (Card panel)
            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.White,
                Padding = new Padding(1)
            };

            dgvTenants = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvTenants);

            SetupColumns();

            dgvTenants.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) BtnManage_Click(s, e);
            };

            gridCard.Controls.Add(dgvTenants);

            var contentWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(32, 4, 32, 32)
            };
            contentWrapper.Controls.Add(gridCard);

            Controls.Add(contentWrapper);
            Controls.Add(toolbarPanel);
            Controls.Add(headerPanel);
        }

        private Panel CreateMetricCard(string label, out Label valueLabel, int left, int top, int width, int height)
        {
            var pnl = new Panel
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                BackColor = Theme.White
            };
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = label,
                Left = 12,
                Top = 8,
                Width = width - 24,
                Height = 14,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            valueLabel = new Label
            {
                Text = "0",
                Left = 12,
                Top = 24,
                Width = width - 24,
                Height = 24,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(valueLabel);
            return pnl;
        }

        private void SetupColumns()
        {
            dgvTenants.Columns.Clear();
            dgvTenants.AutoGenerateColumns = false;

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CompanyId",
                HeaderText = "ID",
                Width = 50,
                FillWeight = 6,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CompanyCode",
                HeaderText = "Code",
                Width = 85,
                FillWeight = 10,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CompanyName",
                HeaderText = "Company Name",
                FillWeight = 25
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ContactEmail",
                HeaderText = "Contact Email",
                FillWeight = 20
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SubscriptionPlan",
                HeaderText = "Plan",
                Width = 95,
                FillWeight = 11,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ExpiryFormatted",
                HeaderText = "Expires On",
                Width = 110,
                FillWeight = 13,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "EnabledModules",
                HeaderText = "Active Modules",
                FillWeight = 28
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "StatusText",
                HeaderText = "Status",
                Width = 90,
                FillWeight = 10,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreatedAtFormatted",
                HeaderText = "Created Date",
                Width = 110,
                FillWeight = 13
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LocalDbDisplay",
                HeaderText = "Local DB",
                FillWeight = 15
            });

            dgvTenants.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CloudDbDisplay",
                HeaderText = "Cloud DB",
                FillWeight = 18
            });

            dgvTenants.CellFormatting += (s, e) =>
            {
                if (dgvTenants.Columns[e.ColumnIndex].DataPropertyName == "StatusText" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? "";
                    if (status == "Active")
                    {
                        e.CellStyle.ForeColor = Theme.Green;
                        e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    }
                    else if (status == "Expired")
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#DC2626"); // Bright Red
                        e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    }
                    else
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#D97706"); // Amber for suspended
                        e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    }
                }
                else if (dgvTenants.Columns[e.ColumnIndex].DataPropertyName == "ExpiryFormatted" && e.RowIndex >= 0)
                {
                    if (dgvTenants.Rows[e.RowIndex].DataBoundItem is TenantViewModel item)
                    {
                        if (item.IsExpired)
                        {
                            e.CellStyle.ForeColor = ColorTranslator.FromHtml("#DC2626"); // Red
                            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                        else if (item.IsExpiringSoon)
                        {
                            e.CellStyle.ForeColor = ColorTranslator.FromHtml("#D97706"); // Amber
                            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        }
                    }
                }
                else if (dgvTenants.Columns[e.ColumnIndex].DataPropertyName == "CloudDbDisplay" && e.Value != null)
                {
                    string val = e.Value.ToString() ?? "";
                    if (val == "—")
                    {
                        e.CellStyle.ForeColor = Theme.MutedText;
                        e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
                    }
                    else
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#2563EB"); // Blue for cloud
                        e.CellStyle.Font = new Font("Segoe UI", 8.5f);
                    }
                }
            };
        }

        private async Task LoadData()
        {
            try
            {
                var data = await ApiConfig.Http.GetFromJsonAsync<TenantViewModel[]>("api/superadmin/tenants");
                _tenantsList = data?.ToList() ?? new List<TenantViewModel>();
                ApplyFilter();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading tenants: " + ex.Message, "S.C.R.A.P Super Admin", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStats()
        {
            lblTotalCount.Text = _tenantsList.Count.ToString();
            lblActiveCount.Text = _tenantsList.Count(t => t.IsActive && !t.IsExpired).ToString();
            lblEnterpriseCount.Text = _tenantsList.Count(t => string.Equals(t.SubscriptionPlan, "Enterprise", StringComparison.OrdinalIgnoreCase)).ToString();
        }

        private void ApplyFilter()
        {
            string q = txtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
            var filtered = string.IsNullOrEmpty(q)
                ? _tenantsList
                : _tenantsList.Where(t =>
                    (t.CompanyCode?.ToLowerInvariant().Contains(q) ?? false) ||
                    (t.CompanyName?.ToLowerInvariant().Contains(q) ?? false) ||
                    (t.ContactEmail?.ToLowerInvariant().Contains(q) ?? false) ||
                    (t.SubscriptionPlan?.ToLowerInvariant().Contains(q) ?? false) ||
                    (t.EnabledModules?.ToLowerInvariant().Contains(q) ?? false)).ToList();

            dgvTenants.DataSource = filtered;
        }

        private async void BtnAddTenant_Click(object? sender, EventArgs e)
        {
            using var f = new RegisterTenantForm();
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                await LoadData();
            }
        }

        private async void BtnRenew_Click(object? sender, EventArgs e)
        {
            if (dgvTenants.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a tenant company to renew.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tenant = dgvTenants.SelectedRows[0].DataBoundItem as TenantViewModel;
            if (tenant == null) return;

            using var dlg = new RenewSubscriptionDialog(tenant);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadData();
            }
        }

        private async void BtnToggleStatus_Click(object? sender, EventArgs e)
        {
            if (dgvTenants.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a tenant from the list.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tenant = dgvTenants.SelectedRows[0].DataBoundItem as TenantViewModel;
            if (tenant == null) return;

            bool targetActive = !tenant.IsActive;
            string actionName = targetActive ? "activate" : "suspend";

            var confirm = MessageBox.Show(
                $"Are you sure you want to {actionName} tenant '{tenant.CompanyName}' ({tenant.CompanyCode})?",
                "Confirm Status Change",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync($"api/superadmin/tenants/{tenant.CompanyId}/status", new { IsActive = targetActive });
                if (res.IsSuccessStatusCode)
                {
                    await LoadData();
                }
                else
                {
                    MessageBox.Show("Failed to update tenant status.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnManage_Click(object? sender, EventArgs e)
        {
            if (dgvTenants.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please click on a company to manage.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tenant = dgvTenants.SelectedRows[0].DataBoundItem as TenantViewModel;
            if (tenant == null) return;

            using var dlg = new ManageCompanyDialog(tenant, initialTab: 0);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _ = LoadData();
            }
        }

        private void BtnManageUsers_Click(object? sender, EventArgs e)
        {
            if (dgvTenants.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please click on a company to manage its users.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tenant = dgvTenants.SelectedRows[0].DataBoundItem as TenantViewModel;
            if (tenant == null) return;

            using var dlg = new ManageCompanyDialog(tenant, initialTab: 2);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _ = LoadData();
            }
        }
    }

    public class TenantViewModel
    {
        public int CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? SubscriptionPlan { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public string? EnabledModules { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<TenantDatabaseViewModel>? Databases { get; set; }

        public bool IsExpired => SubscriptionExpiresAt.HasValue && SubscriptionExpiresAt.Value < DateTime.UtcNow;
        public bool IsExpiringSoon => SubscriptionExpiresAt.HasValue && !IsExpired && (SubscriptionExpiresAt.Value - DateTime.UtcNow).TotalDays <= 14;
        public string ExpiryFormatted => SubscriptionExpiresAt.HasValue ? SubscriptionExpiresAt.Value.ToString("MMM dd, yyyy") : "No Expiry";
        public string StatusText => IsExpired ? "Expired" : (IsActive ? "Active" : "Suspended");
        public string CreatedAtFormatted => CreatedAt.ToString("MMM dd, yyyy");

        public string LocalDbDisplay
        {
            get
            {
                var localDb = Databases?.FirstOrDefault(d => d.DatabaseType == "Local" || string.IsNullOrEmpty(d.DatabaseType));
                return localDb != null ? $"{localDb.DatabaseName}" : "—";
            }
        }

        public string CloudDbDisplay
        {
            get
            {
                var cloudDb = Databases?.FirstOrDefault(d => d.DatabaseType == "Cloud");
                return cloudDb != null ? $"{cloudDb.DatabaseName} @ {cloudDb.ServerName}" : "—";
            }
        }
    }

    public class TenantDatabaseViewModel
    {
        public int CompanyDatabaseId { get; set; }
        public string? DatabaseType { get; set; }
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public string? CredentialKey { get; set; }
        public string? ConnectionString { get; set; }
        public bool IsActive { get; set; }
    }
}
