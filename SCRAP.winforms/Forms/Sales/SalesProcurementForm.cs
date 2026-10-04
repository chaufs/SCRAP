using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;
using SCRAP.winforms.Forms.Common;

namespace SCRAP.winforms.Forms.Sales
{
    public sealed class SalesProcurementForm : Form
    {
        // ── Request entry controls ───────────────────────────────────────
        private readonly TextBox _supplierCompany = new();
        private readonly TextBox _deviceName = new();
        private readonly ComboBox _category = new();
        private readonly DateTimePicker _purchaseDate = new();
        private readonly TextBox _totalCost = new();
        private readonly NumericUpDown _quantity = new();
        private readonly CheckBox _hasStorage = new();
        private readonly TextBox _serial = new();
        private readonly TextBox _batch = new();
        private readonly TextBox _notes = new();
        private Label _lblSerial = null!;
        private Label _lblBatch = null!;
        private Button _btnRegenSerial = null!;
        private Button _btnRegenBatch = null!;
        private Label _lblCodeHint = null!;
        private static readonly Random _rnd = new();

        // ── History grid ─────────────────────────────────────────────────
        private DataGridView _dgvHistory = null!;

        private readonly int _preselectedCategoryId;
        private readonly string? _preselectedDeviceName;

        public SalesProcurementForm(int preselectedCategoryId = 0, string? preselectedDeviceName = null)
        {
            _preselectedCategoryId = preselectedCategoryId;
            _preselectedDeviceName = preselectedDeviceName;
            Text = "Procurement Requests";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            Build();
            if (!string.IsNullOrWhiteSpace(_preselectedDeviceName))
            {
                _deviceName.Text = _preselectedDeviceName;
            }
            _ = LoadCategories();
        }

        public SalesProcurementForm() : this(0, null) { }

        private void Build()
        {
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.LabelFont
            };

            tabs.TabPages.Add(BuildRequestTab());
            tabs.TabPages.Add(BuildHistoryTab());

            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedIndex == 1)
                    _ = LoadHistory();
            };

            Controls.Add(tabs);
        }

        // ── Tab 1: Request Procurement ────────────────────────────────────
        private TabPage BuildRequestTab()
        {
            var tab = new TabPage("New Procurement Request") { BackColor = Theme.Background, Padding = new Padding(12) };
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            var title = new Label
            {
                Text = "Request Device Procurement",
                UseMnemonic = false,
                Left = 32,
                Top = 24,
                Width = 700,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };
            var subtitle = new Label
            {
                Text = "Submit procurement details to the Manager for review, financial approval, and Tech Staff assignment.",
                UseMnemonic = false,
                Left = 32,
                Top = 60,
                Width = 900,
                Height = 24,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            // Row 1
            AddLabelTo(panel, "Where Company (Supplier / Vendor)", 32, 105);
            SetupText(panel, _supplierCompany, 32, 130, 230, "e.g. Acme Tech Solutions, Store Corp");

            AddLabelTo(panel, "What Devices (Device type / Model)", 280, 105);
            SetupText(panel, _deviceName, 280, 130, 230, "e.g. Dell Latitude 5400, ThinkPad");

            AddLabelTo(panel, "Category", 528, 105);
            _category.SetBounds(528, 130, 210, 30);
            _category.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(_category);
            _category.DrawMode = DrawMode.Normal;
            _category.BackColor = Color.White;
            _category.ForeColor = Color.FromArgb(15, 23, 42);
            _category.DisplayMember = "Name";
            _category.ValueMember = "Id";
            panel.Controls.Add(_category);

            AddLabelTo(panel, "Estimated Purchase Date", 756, 105);
            _purchaseDate.SetBounds(756, 130, 180, 30);
            _purchaseDate.Format = DateTimePickerFormat.Short;
            _purchaseDate.Value = DateTime.Today;
            panel.Controls.Add(_purchaseDate);

            // Row 2
            AddLabelTo(panel, "How many devices (Quantity)", 32, 175);
            _quantity.SetBounds(32, 200, 140, 30);
            _quantity.Minimum = 1;
            _quantity.Maximum = 10000;
            _quantity.Value = 1;
            panel.Controls.Add(_quantity);

            AddLabelTo(panel, "Total Procurement Cost (₱)", 195, 175);
            SetupText(panel, _totalCost, 195, 200, 175, "0.00");

            _lblSerial = AddLabelTo(panel, "Serial Number (Auto-generated)", 395, 175);
            SetupText(panel, _serial, 395, 200, 190, "Single device serial");
            _btnRegenSerial = new Button
            {
                Text = "↻ Auto",
                Left = 590,
                Top = 199,
                Width = 65,
                Height = 31,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold)
            };
            Theme.StyleOutlineButton(_btnRegenSerial);
            _btnRegenSerial.Click += (_, _) =>
            {
                _serial.Text = GenerateSerialCode();
            };
            panel.Controls.Add(_btnRegenSerial);

            _lblBatch = AddLabelTo(panel, "Batch Code (N/A for 1 unit)", 675, 175);
            SetupText(panel, _batch, 675, 200, 190, "Shared batch code");
            _btnRegenBatch = new Button
            {
                Text = "↻ Auto",
                Left = 870,
                Top = 199,
                Width = 65,
                Height = 31,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold)
            };
            Theme.StyleOutlineButton(_btnRegenBatch);
            _btnRegenBatch.Click += (_, _) =>
            {
                _batch.Text = GenerateBatchCode();
                int q = (int)_quantity.Value;
                _serial.Text = $"{_batch.Text}-001..{q:D3} (auto on intake)";
            };
            panel.Controls.Add(_btnRegenBatch);

            _lblCodeHint = new Label
            {
                Left = 395,
                Top = 236,
                Width = 540,
                Height = 18,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(_lblCodeHint);

            _quantity.ValueChanged += (_, _) => UpdateCodeFields();
            _batch.TextChanged += (_, _) =>
            {
                if ((int)_quantity.Value > 1 && !_batch.ReadOnly)
                {
                    var b = string.IsNullOrWhiteSpace(_batch.Text) ? "BAT-XXXX" : _batch.Text.Trim();
                    _serial.Text = $"{b}-001..{(int)_quantity.Value:D3} (auto on intake)";
                }
            };

            // Initialize automated code values
            UpdateCodeFields();

            // Row 3
            _hasStorage.Text = "Has storage device (HDD/SSD)";
            _hasStorage.SetBounds(32, 265, 260, 26);
            _hasStorage.AutoSize = true;
            _hasStorage.ForeColor = Theme.DarkText;
            panel.Controls.Add(_hasStorage);

            // Row 4
            AddLabelTo(panel, "Procurement Notes / Justification", 32, 300);
            SetupText(panel, _notes, 32, 325, 904, "Purpose, source contact, or condition notes");

            // Row 5
            var btnSubmit = new Button
            {
                Text = "Submit to Manager",
                Left = 32,
                Top = 380,
                Width = 220,
                Height = 40
            };
            Theme.StylePrimaryButton(btnSubmit);
            btnSubmit.Click += async (_, _) => await SubmitRequest();
            panel.Controls.Add(btnSubmit);

            panel.Controls.Add(title);
            panel.Controls.Add(subtitle);
            tab.Controls.Add(panel);

            return tab;
        }

        // ── Tab 2: Procurement Requests History ────────────────────────────
        private TabPage BuildHistoryTab()
        {
            var tab = new TabPage("My Procurement Requests") { BackColor = Theme.Background, Padding = new Padding(12) };

            var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };
            var title = new Label
            {
                Text = "Procurement Requests Status",
                Left = 16,
                Top = 12,
                Width = 450,
                Height = 32,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };
            var subtitle = new Label
            {
                Text = "Track review status, Manager approvals, and assigned Tech Staff for your procurement requests.",
                Left = 16,
                Top = 44,
                Width = 750,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };
            var btnView = new Button { Text = "🔍  View Details", Left = 650, Top = 16, Width = 120, Height = 34, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Theme.StyleOutlineButton(btnView);
            btnView.Click += (_, _) => OpenSelectedHistoryDetails();

            var btnRefresh = new Button { Text = "↻  Refresh", Left = 780, Top = 16, Width = 110, Height = 34, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (_, _) => await LoadHistory();

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(btnView);
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

            _dgvHistory = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };
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

        private async Task LoadCategories()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/DeviceCategories");
                if (!res.IsSuccessStatusCode) return;

                var data = await res.Content.ReadFromJsonAsync<List<DeviceCategory>>(ApiConfig.JsonOptions);
                if (data == null) return;

                void Apply()
                {
                    _category.BeginUpdate();
                    try
                    {
                        _category.DataSource = null;
                        _category.DisplayMember = "Name";
                        _category.ValueMember = "Id";
                        _category.DataSource = data;
                        if (_preselectedCategoryId > 0)
                        {
                            _category.SelectedValue = _preselectedCategoryId;
                        }
                    }
                    finally
                    {
                        _category.EndUpdate();
                    }
                }

                if (IsHandleCreated && InvokeRequired) Invoke(Apply);
                else Apply();
            }
            catch { }
        }

        private async Task SubmitRequest()
        {
            int categoryId = 0;
            if (_category.SelectedItem is DeviceCategory selected)
                categoryId = selected.Id;
            else if (_category.SelectedValue is int id)
                categoryId = id;

            if (string.IsNullOrWhiteSpace(_supplierCompany.Text))
            {
                MessageBox.Show("Please enter the supplier or company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _supplierCompany.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_deviceName.Text))
            {
                MessageBox.Show("Please enter the device name or type.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _deviceName.Focus();
                return;
            }

            if (categoryId <= 0)
            {
                MessageBox.Show("Please select a device category.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(_totalCost.Text, out var totalCost) || totalCost <= 0)
            {
                MessageBox.Show("Please enter a valid total procurement cost greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _totalCost.Focus();
                return;
            }

            var quantity = (int)_quantity.Value;
            string? serialToSend = null;
            string? batchToSend = null;

            if (quantity <= 1)
            {
                serialToSend = !string.IsNullOrWhiteSpace(_serial.Text)
                    ? _serial.Text.Trim()
                    : GenerateSerialCode();
            }
            else
            {
                batchToSend = !string.IsNullOrWhiteSpace(_batch.Text)
                    ? _batch.Text.Trim()
                    : GenerateBatchCode();
            }

            var response = await ApiConfig.Http.PostAsJsonAsync("api/procurement/request", new
            {
                SupplierCompany = _supplierCompany.Text.Trim(),
                DeviceName = _deviceName.Text.Trim(),
                DeviceCategoryId = categoryId,
                Quantity = quantity,
                TotalCost = totalCost,
                HasStorageDevice = _hasStorage.Checked,
                SerialNumber = serialToSend,
                BatchCode = batchToSend,
                Notes = _notes.Text.Trim()
            });

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                MessageBox.Show(err, "Unable to Submit Request", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(
                $"Procurement request for {quantity}x {_deviceName.Text.Trim()} submitted successfully!\n\nIt has been forwarded to the Manager for review and Tech Staff assignment.",
                "Request Submitted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _supplierCompany.Clear();
            _deviceName.Clear();
            _totalCost.Clear();
            _notes.Clear();
            _quantity.Value = 1;
            _hasStorage.Checked = false;
            UpdateCodeFields();
        }

        private static string GenerateSerialCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var suffix = new string(Enumerable.Repeat(chars, 4).Select(s => s[_rnd.Next(s.Length)]).ToArray());
            return $"SN-{DateTime.Now:yyyyMMdd}-{suffix}";
        }

        private static string GenerateBatchCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var suffix = new string(Enumerable.Repeat(chars, 4).Select(s => s[_rnd.Next(s.Length)]).ToArray());
            return $"BAT-{DateTime.Now:yyyyMMdd}-{suffix}";
        }

        private void UpdateCodeFields()
        {
            int qty = (int)_quantity.Value;
            if (qty <= 1)
            {
                _lblSerial.Text = "Serial Number (Auto-generated)";
                _lblBatch.Text = "Batch Code (N/A for 1 unit)";
                _serial.ReadOnly = false;
                _serial.BackColor = Color.White;
                _serial.ForeColor = Theme.DarkText;
                _btnRegenSerial.Enabled = true;

                if (string.IsNullOrWhiteSpace(_serial.Text) || _serial.Text.StartsWith("BAT-") || _serial.Text.Contains("auto on intake"))
                {
                    _serial.Text = GenerateSerialCode();
                }

                _batch.Text = string.Empty;
                _batch.PlaceholderText = "(Not applicable for 1 unit)";
                _batch.ReadOnly = true;
                _batch.BackColor = ColorTranslator.FromHtml("#F1F5F9");
                _btnRegenBatch.Enabled = false;

                _lblCodeHint.Text = "💡 Automated: Single unit receives a unique Serial Number.";
            }
            else
            {
                _lblSerial.Text = "Serial Numbers (Auto on arrival)";
                _lblBatch.Text = "Batch Code (Auto-generated)";

                _batch.ReadOnly = false;
                _batch.BackColor = Color.White;
                _batch.ForeColor = Theme.DarkText;
                _btnRegenBatch.Enabled = true;

                if (string.IsNullOrWhiteSpace(_batch.Text) || _batch.Text.StartsWith("SN-"))
                {
                    _batch.Text = GenerateBatchCode();
                }

                _serial.ReadOnly = true;
                _serial.BackColor = ColorTranslator.FromHtml("#F1F5F9");
                _serial.ForeColor = Theme.MutedText;
                _serial.Text = $"{_batch.Text}-001..{qty:D3} (auto on intake)";
                _btnRegenSerial.Enabled = false;

                _lblCodeHint.Text = $"💡 Automated: Batch of {qty} will auto-assign sequential serial numbers ({_batch.Text}-001 to {_batch.Text}-{qty:D3}) upon arrival.";
            }
        }

        private async Task LoadHistory()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/procurement/history");
                if (!res.IsSuccessStatusCode) return;

                var data = await res.Content.ReadFromJsonAsync<List<ProcurementItemDto>>(ApiConfig.JsonOptions)
                           ?? new List<ProcurementItemDto>();

                var displayList = data.Select(i => new
                {
                    Id = i.Id,
                    RequestedDate = i.RequestedDate.ToLocalTime(),
                    SupplierCompany = !string.IsNullOrWhiteSpace(i.SupplierCompany) ? i.SupplierCompany : "—",
                    DeviceName = i.DeviceName,
                    CategoryName = !string.IsNullOrWhiteSpace(i.CategoryName) ? i.CategoryName : "—",
                    Quantity = i.Quantity,
                    TotalCost = i.TotalCost,
                    TotalCostFormatted = $"₱{i.TotalCost:N2}",
                    Status = FormatStatus(i.Status),
                    ReviewedByFullName = !string.IsNullOrWhiteSpace(i.ReviewedByFullName) ? i.ReviewedByFullName : "—",
                    AssignedTechStaffFullName = !string.IsNullOrWhiteSpace(i.AssignedTechStaffFullName) ? i.AssignedTechStaffFullName : "—",
                    Notes = !string.IsNullOrWhiteSpace(i.RejectionReason) ? $"[Rejected] {i.RejectionReason}" : (i.Notes ?? "")
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

        private static string FormatStatus(string s) => s switch
        {
            "PendingApproval" => "Pending Review",
            "Approved" => "Accepted (Awaiting Arrival)",
            "Completed" => "Completed (In Stock)",
            "Rejected" => "Rejected",
            _ => s
        };

        private Label AddLabelTo(Control parent, string text, int left, int top)
        {
            var lbl = new Label
            {
                Text = text,
                Left = left,
                Top = top,
                AutoSize = true,
                UseMnemonic = false,
                Font = Theme.LabelFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private void SetupText(Control parent, TextBox box, int left, int top, int width, string placeholder)
        {
            box.SetBounds(left, top, width, 30);
            box.PlaceholderText = placeholder;
            Theme.StyleTextBox(box);
            parent.Controls.Add(box);
        }

        private sealed class ProcurementItemDto
        {
            public int Id { get; set; }
            public DateTime RequestedDate { get; set; }
            public string SupplierCompany { get; set; } = string.Empty;
            public string DeviceName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal TotalCost { get; set; }
            public string Status { get; set; } = string.Empty;
            public string ReviewedByFullName { get; set; } = string.Empty;
            public string AssignedTechStaffFullName { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public string? RejectionReason { get; set; }
        }
    }
}
