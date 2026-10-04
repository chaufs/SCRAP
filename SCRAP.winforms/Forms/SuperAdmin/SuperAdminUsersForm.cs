using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public class SuperAdminUsersForm : Form
    {
        private ComboBox cboCompany = null!;
        private Label lblCompanyInfo = null!;
        private Label lblTotalUsers = null!;
        private Label lblActiveUsers = null!;
        private Label lblDeactivatedUsers = null!;

        private Button btnAddUser = null!;
        private Button btnToggleStatus = null!;
        private Button btnEditUser = null!;
        private Button btnManageCompany = null!;
        private Button btnRefresh = null!;
        private TextBox txtSearch = null!;
        private DataGridView dgvUsers = null!;

        private List<TenantViewModel> _companies = new();
        private TenantViewModel? _selectedCompany;
        private List<TenantUserViewModel> _usersList = new();
        private List<TenantBranchItem> _tenantBranches = new();
        private bool _isLoadingCompanies;

        public SuperAdminUsersForm(int? initialCompanyId = null)
        {
            Text = "Company Administrator Management";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;

            BuildUi();
            _ = LoadCompaniesAsync(initialCompanyId);
        }

        private void BuildUi()
        {
            // ── Header Panel ────────────────────────────────────────────────────────
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Theme.Background,
                Padding = new Padding(32, 20, 32, 0)
            };

            var lblTitle = new Label
            {
                Text = "Company Administrator Management & Access Control",
                Left = 32,
                Top = 18,
                Width = 700,
                Height = 34,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            var lblSubtitle = new Label
            {
                Text = "Provision and manage company Administrator accounts and their active/deactivated login access for each tenant.",
                Left = 32,
                Top = 54,
                Width = 850,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            // Stat cards
            int cardW = 180;
            int cardH = 54;
            int cardY = 88;

            var card1 = CreateMetricCard("TOTAL ADMINS", out lblTotalUsers, 32, cardY, cardW, cardH);
            var card2 = CreateMetricCard("ACTIVE ADMINS", out lblActiveUsers, 32 + cardW + 16, cardY, cardW, cardH);
            var card3 = CreateMetricCard("DEACTIVATED", out lblDeactivatedUsers, 32 + (cardW + 16) * 2, cardY, cardW, cardH);

            headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, card1, card2, card3 });

            // ── Company Selector Bar ────────────────────────────────────────────────
            var companyBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.White,
                Padding = new Padding(32, 10, 32, 10)
            };
            companyBar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, companyBar.Height - 1, companyBar.Width, companyBar.Height - 1);
            };

            var lblSelectCompany = new Label
            {
                Text = "🏢 Select Tenant Company:",
                Left = 32,
                Top = 15,
                Width = 190,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            cboCompany = new ComboBox
            {
                Left = 225,
                Top = 11,
                Width = 320,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            cboCompany.SelectedIndexChanged += async (s, e) =>
            {
                if (_isLoadingCompanies) return;
                if (cboCompany.SelectedItem is CompanyComboItem item)
                {
                    _selectedCompany = item.Company;
                    UpdateCompanyInfoBadge();
                    _usersList.Clear();
                    ApplyFilter();
                    await LoadUsersForSelectedCompanyAsync();
                }
            };

            lblCompanyInfo = new Label
            {
                Text = string.Empty,
                Left = 560,
                Top = 14,
                Width = 500,
                Height = 24,
                Font = Theme.LabelFont,
                ForeColor = Theme.MutedText
            };

            companyBar.Controls.AddRange(new Control[] { lblSelectCompany, cboCompany, lblCompanyInfo });

            // ── Action Toolbar ──────────────────────────────────────────────────────
            var toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Theme.Background,
                Padding = new Padding(32, 10, 32, 8)
            };

            btnAddUser = new Button
            {
                Text = "+ Add Admin",
                Left = 32,
                Top = 8,
                Width = 130,
                Height = 36
            };
            Theme.StylePrimaryButton(btnAddUser);
            btnAddUser.Click += BtnAddUser_Click;

            btnToggleStatus = new Button
            {
                Text = "⏻ Deactivate / Activate",
                Left = 172,
                Top = 8,
                Width = 180,
                Height = 36
            };
            Theme.StyleOutlineButton(btnToggleStatus);
            btnToggleStatus.Click += BtnToggleStatus_Click;

            btnEditUser = new Button
            {
                Text = "✏ Edit Admin",
                Left = 362,
                Top = 8,
                Width = 115,
                Height = 36
            };
            Theme.StyleSecondaryButton(btnEditUser);
            btnEditUser.Click += BtnEditUser_Click;

            btnManageCompany = new Button
            {
                Text = "Manage Tenant",
                Left = 487,
                Top = 8,
                Width = 135,
                Height = 36
            };
            Theme.StyleOutlineButton(btnManageCompany);
            btnManageCompany.Click += BtnManageCompany_Click;

            btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Left = 637,
                Top = 8,
                Width = 95,
                Height = 36
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadUsersForSelectedCompanyAsync();

            var lblSearch = new Label
            {
                Text = "Search:",
                Top = 16,
                Width = 55,
                Height = 24,
                Font = Theme.LabelFont,
                ForeColor = Theme.MutedText,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            txtSearch = new TextBox
            {
                Top = 11,
                Width = 220,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            void PositionSearch()
            {
                int right = toolbarPanel.ClientSize.Width - 32;
                txtSearch.Location = new Point(right - txtSearch.Width, 10);
                lblSearch.Location = new Point(txtSearch.Left - lblSearch.Width - 6, 15);
            }
            toolbarPanel.Resize += (s, e) => PositionSearch();

            toolbarPanel.Controls.AddRange(new Control[]
            {
                btnAddUser, btnToggleStatus, btnEditUser, btnManageCompany, btnRefresh,
                lblSearch, txtSearch
            });
            PositionSearch();

            // ── Grid Container ──────────────────────────────────────────────────────
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
            SetupColumns();

            dgvUsers.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) BtnEditUser_Click(s, e);
            };

            gridCard.Controls.Add(dgvUsers);

            var contentWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(32, 4, 32, 32)
            };
            contentWrapper.Controls.Add(gridCard);

            // Wire hierarchy
            Controls.Add(contentWrapper);
            Controls.Add(toolbarPanel);
            Controls.Add(companyBar);
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
                Top = 22,
                Width = width - 24,
                Height = 26,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            pnl.Controls.AddRange(new Control[] { lblTitle, valueLabel });
            return pnl;
        }

        private void SetupColumns()
        {
            dgvUsers.AutoGenerateColumns = false;
            dgvUsers.Columns.Clear();

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                HeaderText = "ID",
                Width = 55,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Username",
                HeaderText = "Username",
                Width = 140,
                DefaultCellStyle = { Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.DarkText }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FullName",
                HeaderText = "Full Name",
                MinimumWidth = 180,
                FillWeight = 50,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Role",
                HeaderText = "Role",
                Width = 120,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "BranchName",
                HeaderText = "Assigned Branch",
                Width = 220
            });

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "StatusText",
                HeaderText = "Status",
                Width = 120,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }
            });

            // Action button column (Deactivate / Activate)
            var colAction = new DataGridViewButtonColumn
            {
                Name = "Action",
                HeaderText = "Action",
                Width = 130,
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

                // Status column
                if (dgvUsers.Columns[e.ColumnIndex].DataPropertyName == "StatusText")
                {
                    if (rowItem.IsActive)
                    {
                        e.CellStyle.ForeColor = Theme.Green;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#DC2626");
                    }
                }
                // Role column color accents
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
                // Action column text
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

        private async Task LoadCompaniesAsync(int? preferredCompanyId = null)
        {
            _isLoadingCompanies = true;
            try
            {
                var data = await ApiConfig.Http.GetFromJsonAsync<TenantViewModel[]>("api/superadmin/tenants");
                _companies = data?.OrderBy(c => c.CompanyId).ToList() ?? new List<TenantViewModel>();

                cboCompany.Items.Clear();
                int selectIdx = 0;
                for (int i = 0; i < _companies.Count; i++)
                {
                    var c = _companies[i];
                    cboCompany.Items.Add(new CompanyComboItem { Company = c });
                    if (preferredCompanyId.HasValue && c.CompanyId == preferredCompanyId.Value)
                    {
                        selectIdx = i;
                    }
                }

                if (cboCompany.Items.Count > 0)
                {
                    _isLoadingCompanies = false;
                    cboCompany.SelectedIndex = selectIdx;
                    if (cboCompany.SelectedItem is CompanyComboItem item)
                    {
                        _selectedCompany = item.Company;
                        UpdateCompanyInfoBadge();
                        _usersList.Clear();
                        ApplyFilter();
                        await LoadUsersForSelectedCompanyAsync();
                    }
                }
                else
                {
                    _isLoadingCompanies = false;
                    lblCompanyInfo.Text = "No subscriber companies found.";
                }
            }
            catch (Exception ex)
            {
                _isLoadingCompanies = false;
                MessageBox.Show("Failed to load subscriber companies: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCompanyInfoBadge()
        {
            if (_selectedCompany == null)
            {
                lblCompanyInfo.Text = string.Empty;
                return;
            }

            lblCompanyInfo.Text = $"Code: {_selectedCompany.CompanyCode}   •   Plan: {_selectedCompany.SubscriptionPlan}   •   Status: {_selectedCompany.StatusText}";
            lblCompanyInfo.ForeColor = _selectedCompany.IsActive ? Theme.Green : ColorTranslator.FromHtml("#DC2626");
        }

        private async Task LoadUsersForSelectedCompanyAsync()
        {
            if (_selectedCompany == null) return;
            int targetCompanyId = _selectedCompany.CompanyId;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/superadmin/tenants/{targetCompanyId}/users?role=Admin");
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Company-Code"))
                    request.Headers.TryAddWithoutValidation("X-Company-Code", "MASTER");
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Api-User"))
                    request.Headers.TryAddWithoutValidation("X-Api-User", CurrentSession.Username);

                var response = await ApiConfig.Http.SendAsync(request);
                if (_selectedCompany?.CompanyId != targetCompanyId)
                {
                    // Discard response if user already switched to another tenant
                    return;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TenantUsersResponse>(json, ApiConfig.JsonOptions);
                    if (result != null)
                    {
                        var rawUsers = result.Users ?? new List<TenantUserViewModel>();
                        _usersList = rawUsers
                            .Where(u => string.Equals(u.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        _tenantBranches = result.Branches ?? new List<TenantBranchItem>();
                        ApplyFilter();
                        UpdateStats();
                    }
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to load administrators for {_selectedCompany.CompanyName}:\n{err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                if (_selectedCompany?.CompanyId == targetCompanyId)
                {
                    MessageBox.Show("Error loading tenant administrators: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateStats()
        {
            lblTotalUsers.Text = _usersList.Count.ToString();
            lblActiveUsers.Text = _usersList.Count(u => u.IsActive).ToString();
            lblDeactivatedUsers.Text = _usersList.Count(u => !u.IsActive).ToString();
        }

        private void ApplyFilter()
        {
            string q = txtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
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
            if (_selectedCompany == null)
            {
                MessageBox.Show("Please select a company first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new AddEditTenantUserDialog(_selectedCompany.CompanyId, _selectedCompany.CompanyCode ?? "", _selectedCompany.CompanyName ?? "", _tenantBranches);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadUsersForSelectedCompanyAsync();
            }
        }

        private async void BtnEditUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_selectedCompany == null) return;
            var user = dgvUsers.SelectedRows[0].DataBoundItem as TenantUserViewModel;
            if (user == null) return;

            using var dlg = new AddEditTenantUserDialog(_selectedCompany.CompanyId, _selectedCompany.CompanyCode ?? "", _selectedCompany.CompanyName ?? "", _tenantBranches, user);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadUsersForSelectedCompanyAsync();
            }
        }

        private async void BtnToggleStatus_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to toggle active/deactivated status.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (_selectedCompany == null) return;

            bool targetActive = !user.IsActive;
            string actionText = targetActive ? "activate" : "deactivate";
            string msg = targetActive
                ? $"Reactivate login access for user '{user.Username}' in {_selectedCompany.CompanyName}?"
                : $"Are you sure you want to deactivate login access for user '{user.Username}' in {_selectedCompany.CompanyName}?\nThey will immediately be blocked from logging into the platform.";

            var confirm = MessageBox.Show(msg, "Confirm User Status Change", MessageBoxButtons.YesNo,
                targetActive ? MessageBoxIcon.Question : MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var payload = new { IsActive = targetActive };
                using var request = new HttpRequestMessage(HttpMethod.Post, $"api/superadmin/tenants/{_selectedCompany.CompanyId}/users/{user.Id}/status")
                {
                    Content = new System.Net.Http.StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
                };
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Company-Code"))
                    request.Headers.TryAddWithoutValidation("X-Company-Code", "MASTER");
                if (!ApiConfig.Http.DefaultRequestHeaders.Contains("X-Api-User"))
                    request.Headers.TryAddWithoutValidation("X-Api-User", CurrentSession.Username);

                var response = await ApiConfig.Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    await LoadUsersForSelectedCompanyAsync();
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

        private void BtnManageCompany_Click(object? sender, EventArgs e)
        {
            if (_selectedCompany == null) return;

            using var dlg = new ManageCompanyDialog(_selectedCompany, initialTab: 2);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _ = LoadCompaniesAsync(_selectedCompany.CompanyId);
            }
        }

        private class CompanyComboItem
        {
            public TenantViewModel Company { get; set; } = null!;
            public override string ToString() => $"{Company.CompanyName} ({Company.CompanyCode})";
        }
    }
}
