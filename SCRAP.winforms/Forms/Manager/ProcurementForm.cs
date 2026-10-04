using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;
using SCRAP.winforms.Forms.Common;

namespace SCRAP.winforms.Forms
{
    public sealed class ProcurementForm : Form
    {
        // ── Navigation Tabs ──────────────────────────────────────────────
        private TabControl _tabs = null!;
        private Button? _btnTabReview;
        private Button _btnTabHistory = null!;
        private int _pendingCount;

        // ── Tab 1: Review Requests controls ──────────────────────────────
        private DataGridView? _dgvPending;
        private Label? _lblSelectedPending;
        private ComboBox? _cmbAssignTech;
        private Button? _btnAccept;
        private Button? _btnReject;
        private Button? _btnViewPending;
        private dynamic? _selectedPendingItem = null;

        // ── Tab 2: History grid ──────────────────────────────────────────
        private DataGridView _dgvHistory = null!;

        public ProcurementForm()
        {
            Text = "Procurement Management";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            Build();

            bool isAdmin = string.Equals(CurrentSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin)
            {
                _ = LoadTechStaff();
                _ = LoadPendingRequests();
            }
            else
            {
                _ = LoadHistory();
            }
        }

        private void Build()
        {
            bool isAdmin = string.Equals(CurrentSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // ── Modern Top Segmented Tab Strip ──
            var topTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Theme.Background,
                Padding = new Padding(24, 12, 24, 10)
            };

            var tabStrip = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            if (!isAdmin)
            {
                _btnTabReview = new Button
                {
                    Text = "📥  Review Requests",
                    Left = 0,
                    Top = 0,
                    Width = 230,
                    Height = 40,
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat
                };
                _btnTabReview.FlatAppearance.BorderSize = 0;
                _btnTabReview.Click += (s, e) => SwitchTab(0);
                tabStrip.Controls.Add(_btnTabReview);
            }

            _btnTabHistory = new Button
            {
                Text = "📋  Procurement History",
                Left = isAdmin ? 0 : 242,
                Top = 0,
                Width = 230,
                Height = 40,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            _btnTabHistory.FlatAppearance.BorderSize = 0;
            _btnTabHistory.Click += (s, e) => SwitchTab(isAdmin ? 0 : 1);

            tabStrip.Controls.Add(_btnTabHistory);
            topTabBar.Controls.Add(tabStrip);

            // Subtle divider at bottom of tab bar
            topTabBar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, topTabBar.Height - 1, topTabBar.Width, topTabBar.Height - 1);
            };

            // ── TabControl (Flat without legacy tabs) ──
            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed
            };

            if (!isAdmin)
            {
                _tabs.TabPages.Add(BuildReviewRequestsTab());
            }
            _tabs.TabPages.Add(BuildHistoryTab());

            // Order of controls: tabs first, then topTabBar on top
            Controls.Add(_tabs);
            Controls.Add(topTabBar);

            UpdateTabButtonStyles(isAdmin ? 1 : 0);
        }

        private void SwitchTab(int index)
        {
            bool isAdmin = string.Equals(CurrentSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            if (isAdmin)
            {
                _ = LoadHistory();
                return;
            }

            if (_tabs.SelectedIndex == index) return;
            _tabs.SelectedIndex = index;
            UpdateTabButtonStyles(index);
            if (index == 0) _ = LoadPendingRequests();
            else if (index == 1) _ = LoadHistory();
        }

        private void UpdateTabButtonStyles(int selectedIndex)
        {
            bool isAdmin = string.Equals(CurrentSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            if (isAdmin)
            {
                _btnTabHistory.BackColor = Theme.Blue;
                _btnTabHistory.ForeColor = Color.White;
                _btnTabHistory.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                _btnTabHistory.FlatAppearance.BorderSize = 0;
                return;
            }

            if (selectedIndex == 0 && _btnTabReview != null)
            {
                _btnTabReview.BackColor = Theme.Blue;
                _btnTabReview.ForeColor = Color.White;
                _btnTabReview.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                _btnTabReview.FlatAppearance.BorderSize = 0;

                _btnTabHistory.BackColor = Theme.White;
                _btnTabHistory.ForeColor = Theme.DarkText;
                _btnTabHistory.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
                _btnTabHistory.FlatAppearance.BorderSize = 1;
                _btnTabHistory.FlatAppearance.BorderColor = Theme.CardBorder;
            }
            else if (_btnTabReview != null)
            {
                _btnTabReview.BackColor = Theme.White;
                _btnTabReview.ForeColor = Theme.DarkText;
                _btnTabReview.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
                _btnTabReview.FlatAppearance.BorderSize = 1;
                _btnTabReview.FlatAppearance.BorderColor = Theme.CardBorder;

                _btnTabHistory.BackColor = Theme.Blue;
                _btnTabHistory.ForeColor = Color.White;
                _btnTabHistory.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                _btnTabHistory.FlatAppearance.BorderSize = 0;
            }
        }

        // ── Tab 1: Review Requests (Manager/Admin Accept/Reject & Assign Tech Staff) ──
        private TabPage BuildReviewRequestsTab()
        {
            var tab = new TabPage("Review Requests")
            {
                BackColor = Theme.Background,
                Padding = new Padding(24, 16, 24, 24)
            };

            var header = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.Transparent };
            var title = new Label
            {
                Text = "Procurement Review & Tech Assignment",
                Left = 0,
                Top = 8,
                Width = 600,
                Height = 32,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };
            var subtitle = new Label
            {
                Text = "Review incoming procurement requests submitted by Sales Staff. Accept and assign Tech Staff for device intake, or reject.",
                Left = 0,
                Top = 42,
                Width = 800,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };
            var btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 110,
                Height = 34,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Location = new Point(Math.Max(700, header.Width - btnRefresh.Width), 12);
            btnRefresh.Click += async (_, _) => await LoadPendingRequests();

            header.Resize += (s, e) =>
            {
                btnRefresh.Left = Math.Max(200, header.ClientSize.Width - btnRefresh.Width);
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(btnRefresh);
            tab.Controls.Add(header);

            // Action panel at bottom
            var actionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Theme.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            actionPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, actionPanel.Width - 1, actionPanel.Height - 1);
            };

            _lblSelectedPending = new Label
            {
                Text = "Select a request from the list above to review and assign.",
                Left = 16,
                Top = 12,
                Width = 700,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            var lblAssign = new Label
            {
                Text = "Assign Tech Staff:",
                Left = 16,
                Top = 46,
                Width = 135,
                Height = 26,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText
            };

            _cmbAssignTech = new ComboBox
            {
                Left = 155,
                Top = 42,
                Width = 240,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(_cmbAssignTech);
            _cmbAssignTech.DrawMode = DrawMode.Normal;
            _cmbAssignTech.BackColor = Color.White;
            _cmbAssignTech.ForeColor = Color.FromArgb(15, 23, 42);

            _btnAccept = new Button
            {
                Text = "✓ Accept & Assign",
                Left = 410,
                Top = 40,
                Width = 150,
                Height = 36,
                Enabled = false
            };
            Theme.StylePrimaryButton(_btnAccept);
            _btnAccept.Click += async (_, _) => await AcceptSelectedRequest();

            _btnReject = new Button
            {
                Text = "✗ Reject Request",
                Left = 570,
                Top = 40,
                Width = 140,
                Height = 36,
                Enabled = false
            };
            Theme.StyleOutlineButton(_btnReject);
            _btnReject.ForeColor = Color.FromArgb(180, 35, 24);
            _btnReject.Click += async (_, _) => await RejectSelectedRequest();

            _btnViewPending = new Button
            {
                Text = "🔍  View Details",
                Left = 720,
                Top = 40,
                Width = 135,
                Height = 36,
                Enabled = false
            };
            Theme.StyleOutlineButton(_btnViewPending);
            _btnViewPending.Click += (_, _) => OpenSelectedPendingDetails();

            actionPanel.Controls.Add(_lblSelectedPending);
            actionPanel.Controls.Add(lblAssign);
            actionPanel.Controls.Add(_cmbAssignTech);
            actionPanel.Controls.Add(_btnAccept);
            actionPanel.Controls.Add(_btnReject);
            actionPanel.Controls.Add(_btnViewPending);
            tab.Controls.Add(actionPanel);

            // Grid card
            var gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.White,
                Padding = new Padding(1)
            };
            gridPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, gridPanel.Width - 1, gridPanel.Height - 1);
            };

            _dgvPending = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(_dgvPending);
            AddPendingColumns();
            _dgvPending.SelectionChanged += (_, _) => OnPendingSelectionChanged();
            _dgvPending.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedPendingDetails();
            };
            gridPanel.Controls.Add(_dgvPending);

            tab.Controls.Add(gridPanel);
            gridPanel.BringToFront();

            return tab;
        }

        private void AddPendingColumns()
        {
            if (_dgvPending == null) return;
            _dgvPending.AutoGenerateColumns = false;
            _dgvPending.Columns.Clear();
            _dgvPending.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "#", Width = 55, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { DataPropertyName = "RequestedAt", HeaderText = "Date", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "MMM dd, yyyy" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "SupplierCompany", HeaderText = "Supplier / Company", Width = 190 },
                new DataGridViewTextBoxColumn { DataPropertyName = "DeviceName", HeaderText = "Device Item", Width = 190 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "Qty", Width = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { DataPropertyName = "TotalCostFormatted", HeaderText = "Total Cost", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { DataPropertyName = "RequestedByFullName", HeaderText = "Sales Requester", Width = 150 }
            });
        }

        // ── Tab 2: Procurement History ────────────────────────────────────
        private TabPage BuildHistoryTab()
        {
            var tab = new TabPage("Procurement History")
            {
                BackColor = Theme.Background,
                Padding = new Padding(24, 16, 24, 24)
            };

            var header = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.Transparent };
            var title = new Label
            {
                Text = "Procurement History",
                Left = 0,
                Top = 8,
                Width = 400,
                Height = 32,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };
            var subtitle = new Label
            {
                Text = "Complete audit history of all procurements showing Sales Requester, Management Review, and assigned Tech Staff.",
                Left = 0,
                Top = 42,
                Width = 800,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };
            var btnViewHistory = new Button
            {
                Text = "🔍  View Details",
                Width = 130,
                Height = 34,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnViewHistory);
            btnViewHistory.Click += (_, _) => OpenSelectedHistoryDetails();

            var btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 110,
                Height = 34,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (_, _) => await LoadHistory();

            header.Resize += (s, e) =>
            {
                btnRefresh.Left = Math.Max(200, header.ClientSize.Width - btnRefresh.Width);
                btnViewHistory.Left = btnRefresh.Left - btnViewHistory.Width - 10;
            };
            btnRefresh.Location = new Point(Math.Max(700, header.Width - btnRefresh.Width), 12);
            btnViewHistory.Location = new Point(btnRefresh.Left - btnViewHistory.Width - 10, 12);

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(btnViewHistory);
            header.Controls.Add(btnRefresh);
            tab.Controls.Add(header);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.White,
                Padding = new Padding(1)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            _dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(_dgvHistory);
            AddHistoryColumns();
            _dgvHistory.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedHistoryDetails();
            };
            _dgvHistory.CellContentClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && _dgvHistory.Columns[e.ColumnIndex].Name == "ColViewHistory")
                    OpenSelectedHistoryDetails();
            };
            card.Controls.Add(_dgvHistory);

            tab.Controls.Add(card);
            card.BringToFront();

            return tab;
        }

        private void AddHistoryColumns()
        {
            _dgvHistory.AutoGenerateColumns = false;
            _dgvHistory.Columns.Clear();
            _dgvHistory.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "#", Width = 55, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { DataPropertyName = "RequestedDate", HeaderText = "Date", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "MMM dd, yyyy" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "SupplierCompany", HeaderText = "Supplier / Company", Width = 190 },
                new DataGridViewTextBoxColumn { DataPropertyName = "DeviceName", HeaderText = "Device Item", Width = 190 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "Qty", Width = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { DataPropertyName = "TotalCostFormatted", HeaderText = "Total Cost", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewButtonColumn
                {
                    Name = "ColViewHistory",
                    HeaderText = "Action",
                    Text = "🔍 View",
                    UseColumnTextForButtonValue = true,
                    Width = 90,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 9f) }
                }
            });
        }

        // ── Data Loading & Actions ─────────────────────────────────────────

        private async Task LoadPendingRequests()
        {
            if (_dgvPending == null) return;
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/procurement/requests?status=PendingApproval");
                if (!res.IsSuccessStatusCode) return;

                var data = await res.Content.ReadFromJsonAsync<List<ProcurementRequest>>(ApiConfig.JsonOptions)
                           ?? new List<ProcurementRequest>();

                var displayList = data.Select(p => new
                {
                    p.Id,
                    RequestedAt = p.RequestedAtUtc.ToLocalTime(),
                    RequestedByFullName = !string.IsNullOrWhiteSpace(p.RequestedByFullName) ? p.RequestedByFullName : (p.RequestedByUserName ?? "—"),
                    SupplierCompany = p.SupplierCompany,
                    DeviceName = p.DeviceName,
                    CategoryName = p.DeviceCategory?.Name ?? "—",
                    Quantity = p.Quantity,
                    CostPerDevice = p.CostPerDevice,
                    TotalCost = p.TotalCost,
                    TotalCostFormatted = $"₱{p.TotalCost:N2}",
                    Notes = p.Notes ?? ""
                }).ToList();

                _pendingCount = displayList.Count;

                void Bind()
                {
                    if (_dgvPending == null) return;
                    _dgvPending.AutoGenerateColumns = false;
                    _dgvPending.DataSource = null;
                    _dgvPending.DataSource = displayList;
                    Theme.FillColumnsToWidth(_dgvPending);
                    OnPendingSelectionChanged();
                    if (_btnTabReview != null)
                        _btnTabReview.Text = _pendingCount > 0 ? $"📥  Review Requests  ({_pendingCount})" : "📥  Review Requests";
                }

                if (InvokeRequired) Invoke(Bind);
                else Bind();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading pending requests: " + ex.Message, "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadTechStaff()
        {
            if (_cmbAssignTech == null) return;
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/procurement/tech-staff");
                if (!res.IsSuccessStatusCode) return;

                var data = await res.Content.ReadFromJsonAsync<List<TechStaffItem>>(ApiConfig.JsonOptions)
                           ?? new List<TechStaffItem>();

                var distinctData = data
                    .GroupBy(t => string.IsNullOrWhiteSpace(t.FullName) ? t.Username.ToLowerInvariant() : t.FullName.ToLowerInvariant())
                    .Select(g => g.OrderByDescending(t => t.Username.Contains('.')).ThenBy(t => t.Id).First())
                    .ToList();

                void Apply()
                {
                    if (_cmbAssignTech == null) return;
                    _cmbAssignTech.BeginUpdate();
                    try
                    {
                        _cmbAssignTech.DataSource = null;
                        _cmbAssignTech.DisplayMember = "DisplayName";
                        _cmbAssignTech.ValueMember = "Id";
                        _cmbAssignTech.DataSource = distinctData;
                    }
                    finally
                    {
                        _cmbAssignTech.EndUpdate();
                    }
                }

                if (InvokeRequired) Invoke(Apply);
                else Apply();
            }
            catch { }
        }

        private void OnPendingSelectionChanged()
        {
            if (_dgvPending == null || _lblSelectedPending == null || _btnAccept == null || _btnReject == null || _btnViewPending == null) return;

            if (_dgvPending.CurrentRow?.DataBoundItem != null)
            {
                _selectedPendingItem = _dgvPending.CurrentRow.DataBoundItem;
                int id = _selectedPendingItem.Id;
                string device = _selectedPendingItem.DeviceName;
                int qty = _selectedPendingItem.Quantity;
                decimal cost = _selectedPendingItem.TotalCost;
                string comp = _selectedPendingItem.SupplierCompany;
                string requester = _selectedPendingItem.RequestedByFullName;

                _lblSelectedPending.Text = $"Selected: #{id} — {qty}x {device} from {comp} | Total: ₱{cost:N2} | By: {requester}";
                _btnAccept.Enabled = true;
                _btnReject.Enabled = true;
                _btnViewPending.Enabled = true;
            }
            else
            {
                _selectedPendingItem = null;
                _lblSelectedPending.Text = "Select a request from the list above to review and assign.";
                _btnAccept.Enabled = false;
                _btnReject.Enabled = false;
                _btnViewPending.Enabled = false;
            }
        }

        private void OpenSelectedPendingDetails()
        {
            if (_dgvPending?.CurrentRow?.DataBoundItem != null)
            {
                dynamic item = _dgvPending.CurrentRow.DataBoundItem;
                int id = item.Id;
                using var dlg = new ProcurementDetailsDialog(id);
                dlg.ShowDialog(this);
            }
        }

        private void OpenSelectedHistoryDetails()
        {
            if (_dgvHistory.CurrentRow?.DataBoundItem != null)
            {
                dynamic item = _dgvHistory.CurrentRow.DataBoundItem;
                int id = item.Id;
                using var dlg = new ProcurementDetailsDialog(id);
                dlg.ShowDialog(this);
            }
        }

        private async Task AcceptSelectedRequest()
        {
            if (_selectedPendingItem == null) return;
            int reqId = _selectedPendingItem.Id;

            if (_cmbAssignTech == null || _cmbAssignTech.SelectedItem is not TechStaffItem selectedTech || selectedTech.Id <= 0)
            {
                MessageBox.Show("Please select an active Tech Staff member to handle the arrival of the devices.", "Assign Tech Staff", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var response = await ApiConfig.Http.PostAsJsonAsync($"api/procurement/{reqId}/review", new
                {
                    Accept = true,
                    AssignedTechStaffUserId = selectedTech.Id,
                    AssignedTechStaffId = selectedTech.Id,
                    HasFinanceModule = CurrentSession.HasModuleAccess("Finance")
                });

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Unable to Accept Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"Procurement request #{reqId} approved and assigned to {selectedTech.DisplayName}.", "Request Accepted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadPendingRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error accepting request: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RejectSelectedRequest()
        {
            if (_selectedPendingItem == null) return;
            int reqId = _selectedPendingItem.Id;

            string reason = "";
            using (var prompt = new Form
            {
                Width = 420,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Reject Procurement Request",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.White
            })
            {
                var lbl = new Label { Text = $"Reason for rejecting request #{reqId}:", Left = 20, Top = 20, Width = 360, Height = 22, Font = Theme.LabelFont };
                var txt = new TextBox { Left = 20, Top = 50, Width = 360, Height = 60, Multiline = true };
                Theme.StyleTextBox(txt);
                var btnOk = new Button { Text = "Confirm Reject", Left = 160, Top = 125, Width = 110, Height = 34, DialogResult = DialogResult.OK };
                Theme.StylePrimaryButton(btnOk);
                btnOk.BackColor = Color.FromArgb(180, 35, 24);
                var btnCancel = new Button { Text = "Cancel", Left = 280, Top = 125, Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
                Theme.StyleOutlineButton(btnCancel);

                prompt.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                prompt.AcceptButton = btnOk;
                prompt.CancelButton = btnCancel;

                if (prompt.ShowDialog(this) != DialogResult.OK) return;
                reason = txt.Text.Trim();
            }

            try
            {
                var response = await ApiConfig.Http.PostAsJsonAsync($"api/procurement/{reqId}/review", new
                {
                    Accept = false,
                    RejectionReason = reason
                });

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Unable to Reject Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"Procurement request #{reqId} has been rejected.", "Request Rejected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadPendingRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error rejecting request: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadHistory()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/procurement/history");
                if (!res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Error loading procurement history: " + res.StatusCode, "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var data = await res.Content.ReadFromJsonAsync<List<ProcurementHistoryItem>>(ApiConfig.JsonOptions)
                           ?? new List<ProcurementHistoryItem>();

                var displayList = data.Select(i => new
                {
                    i.Id,
                    RequestedDate = i.RequestedDate.ToLocalTime(),
                    SupplierCompany = i.SupplierCompany,
                    DeviceName = i.DeviceName,
                    Quantity = i.Quantity,
                    TotalCostFormatted = $"₱{i.TotalCost:N2}",
                    Status = i.Status switch
                    {
                        "PendingApproval" => "⏳ Pending Approval",
                        "Approved" => "✓ Accepted / Processing",
                        "Rejected" => "✗ Rejected",
                        "Received" => "📦 Received at Facility",
                        _ => i.Status
                    }
                }).ToList();

                void Bind()
                {
                    _dgvHistory.AutoGenerateColumns = false;
                    _dgvHistory.DataSource = null;
                    _dgvHistory.DataSource = displayList;
                    Theme.FillColumnsToWidth(_dgvHistory);
                }

                if (InvokeRequired) Invoke(Bind);
                else Bind();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading history: " + ex.Message, "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class TechStaffItem
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? $"{FullName} ({Username})" : Username;
        }

        private sealed class ProcurementHistoryItem
        {
            public int Id { get; set; }
            public DateTime RequestedDate { get; set; }
            public string SupplierCompany { get; set; } = string.Empty;
            public string DeviceName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal TotalCost { get; set; }
            public decimal CostPerDevice { get; set; }
            public string Status { get; set; } = string.Empty;
            public string RequestedByFullName { get; set; } = string.Empty;
            public string ReviewedByFullName { get; set; } = string.Empty;
            public string AssignedTechStaffFullName { get; set; } = string.Empty;
            public string SerialNumber { get; set; } = string.Empty;
            public string? BatchCode { get; set; }
            public string Notes { get; set; } = string.Empty;
            public string? RejectionReason { get; set; }
            public DateTime? CompletedAtUtc { get; set; }
        }
    }
}