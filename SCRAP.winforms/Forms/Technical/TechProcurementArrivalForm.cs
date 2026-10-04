using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;
using SCRAP.winforms.Forms.Common;

namespace SCRAP.winforms.Forms.Technical
{
    public sealed class TechProcurementArrivalForm : Form
    {
        private DataGridView _dgvArrivals = null!;
        private Label _lblSelected = null!;
        private Button _btnCompleteTask = null!;
        private Button _btnViewDetails = null!;
        private dynamic? _selectedItem = null;

        public TechProcurementArrivalForm()
        {
            Text = "Procurement Arrival & Intake";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            Build();
            _ = LoadArrivals();
        }

        private void Build()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };
            var title = new Label
            {
                Text = "Procurement Device Arrival",
                Left = 24,
                Top = 14,
                Width = 500,
                Height = 32,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };
            var subtitle = new Label
            {
                Text = "Verify arriving procurement shipments assigned to you and click Task Completed to intake devices into inventory.",
                Left = 24,
                Top = 46,
                Width = 750,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };
            var btnRefresh = new Button { Text = "↻  Refresh", Width = 110, Height = 34, Top = 18 };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (_, _) => await LoadArrivals();

            void LayoutHeader()
            {
                btnRefresh.Left = Math.Max(200, header.ClientSize.Width - btnRefresh.Width - 24);
                subtitle.Width = Math.Max(200, btnRefresh.Left - subtitle.Left - 16);
            }
            header.Resize += (_, _) => LayoutHeader();

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(btnRefresh);
            Controls.Add(header);

            // Action panel at bottom
            var actionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 84,
                BackColor = Theme.White,
                Padding = new Padding(24, 14, 24, 14)
            };
            actionPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, actionPanel.Width - 1, actionPanel.Height - 1);
            };

            _lblSelected = new Label
            {
                Text = "Select an arriving procurement from the list above.",
                Left = 24,
                Top = 16,
                Width = 600,
                Height = 44,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            _btnViewDetails = new Button
            {
                Text = "🔍  View Details",
                Top = 20,
                Width = 150,
                Height = 44,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Enabled = false
            };
            Theme.StyleOutlineButton(_btnViewDetails);
            _btnViewDetails.Click += (_, _) => OpenSelectedDetails();

            _btnCompleteTask = new Button
            {
                Text = "✓ Task Completed",
                Top = 20,
                Width = 190,
                Height = 44,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Theme.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnCompleteTask.FlatAppearance.BorderSize = 0;
            _btnCompleteTask.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#15803D");
            _btnCompleteTask.Click += async (_, _) => await CompleteArrival();

            void LayoutActionButtons()
            {
                int pad = 24;
                _btnCompleteTask.Left = Math.Max(200, actionPanel.ClientSize.Width - _btnCompleteTask.Width - pad);
                _btnViewDetails.Left = _btnCompleteTask.Left - _btnViewDetails.Width - 12;
                _lblSelected.Width = Math.Max(180, _btnViewDetails.Left - _lblSelected.Left - 16);
            }
            actionPanel.Resize += (_, _) => LayoutActionButtons();

            actionPanel.Controls.Add(_lblSelected);
            actionPanel.Controls.Add(_btnViewDetails);
            actionPanel.Controls.Add(_btnCompleteTask);
            Controls.Add(actionPanel);

            // Card grid in center
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

            _dgvArrivals = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(_dgvArrivals);
            AddArrivalColumns();
            _dgvArrivals.SelectionChanged += (_, _) => OnSelectionChanged();
            _dgvArrivals.CellClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    _dgvArrivals.Rows[e.RowIndex].Selected = true;
                    OnSelectionChanged();
                }
            };
            _dgvArrivals.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedDetails();
            };
            _dgvArrivals.CellContentClick += async (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var colName = _dgvArrivals.Columns[e.ColumnIndex].Name;
                if (colName == "ColViewArrival")
                {
                    OpenSelectedDetails();
                }
            };
            card.Controls.Add(_dgvArrivals);

            Controls.Add(card);
            card.BringToFront();

            Resize += (_, _) =>
            {
                LayoutHeader();
                LayoutActionButtons();
            };
            LayoutHeader();
            LayoutActionButtons();
        }

        private void AddArrivalColumns()
        {
            _dgvArrivals.AutoGenerateColumns = false;
            _dgvArrivals.Columns.Clear();
            _dgvArrivals.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "#", Width = 55, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold) } },
                new DataGridViewTextBoxColumn { DataPropertyName = "RequestedDate", HeaderText = "Arrival / Date", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "MMM dd, yyyy" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "SupplierCompany", HeaderText = "Source Company", Width = 190 },
                new DataGridViewTextBoxColumn { DataPropertyName = "DeviceName", HeaderText = "Device Details", Width = 200 },
                new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "Qty", Width = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { DataPropertyName = "StatusText", HeaderText = "Status", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewButtonColumn
                {
                    Name = "ColViewArrival",
                    HeaderText = "Details",
                    Text = "🔍 View",
                    UseColumnTextForButtonValue = true,
                    Width = 90,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 9f) }
                }
            });
        }

        private void OpenSelectedDetails()
        {
            if (_dgvArrivals.CurrentRow?.DataBoundItem != null)
            {
                dynamic item = _dgvArrivals.CurrentRow.DataBoundItem;
                int id = item.Id;
                using var dlg = new ProcurementDetailsDialog(id);
                dlg.ShowDialog(this);
            }
        }

        private async Task LoadArrivals()
        {
            try
            {
                // Only loads requests assigned to tech staff awaiting arrival
                var res = await ApiConfig.Http.GetAsync("api/procurement/requests?status=Approved&myAssignedOnly=true");
                if (!res.IsSuccessStatusCode) return;

                var data = await res.Content.ReadFromJsonAsync<List<ProcurementRequest>>(ApiConfig.JsonOptions)
                           ?? new List<ProcurementRequest>();

                var displayList = data.Select(p => new
                {
                    p.Id,
                    RequestedDate = p.RequestedAtUtc.ToLocalTime(),
                    p.SupplierCompany,
                    p.DeviceName,
                    CategoryName = p.DeviceCategory?.Name ?? "—",
                    p.Quantity,
                    SerialNumber = !string.IsNullOrWhiteSpace(p.SerialNumber) ? p.SerialNumber : (p.BatchCode ?? "—"),
                    StatusText = "Awaiting Arrival",
                    Notes = p.Notes ?? ""
                }).ToList();

                void Bind()
                {
                    _dgvArrivals.AutoGenerateColumns = false;
                    _dgvArrivals.DataSource = null;
                    _dgvArrivals.DataSource = displayList;
                    Theme.FillColumnsToWidth(_dgvArrivals);
                    if (_dgvArrivals.Rows.Count > 0)
                    {
                        _dgvArrivals.Rows[0].Selected = true;
                    }
                    OnSelectionChanged();
                }

                if (InvokeRequired) Invoke(Bind);
                else Bind();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading assigned arrivals: " + ex.Message, "Procurement Arrival", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSelectionChanged()
        {
            if (_dgvArrivals.CurrentRow?.DataBoundItem != null)
            {
                _selectedItem = _dgvArrivals.CurrentRow.DataBoundItem;
                int id = _selectedItem.Id;
                string device = _selectedItem.DeviceName;
                int qty = _selectedItem.Quantity;
                string comp = _selectedItem.SupplierCompany;

                _lblSelected.Text = $"Selected: Request #{id} — {qty}x {device} from {comp}\nClick 'Task Completed' when devices arrive to put them in inventory.";
                _btnCompleteTask.Enabled = true;
                _btnViewDetails.Enabled = true;
            }
            else
            {
                _selectedItem = null;
                _lblSelected.Text = "Select an arriving procurement from the list above.";
                _btnCompleteTask.Enabled = false;
                _btnViewDetails.Enabled = false;
            }
        }

        private async Task CompleteArrival()
        {
            if (_selectedItem == null) return;
            int reqId = _selectedItem.Id;
            int qty = _selectedItem.Quantity;
            string device = _selectedItem.DeviceName;
            string comp = _selectedItem.SupplierCompany;

            var confirm = MessageBox.Show(
                $"Have the devices arrived?\n\nRequest #{reqId}: {qty}x {device} from {comp}\n\n" +
                $"Clicking Yes will mark the task completed and immediately put {qty} devices into the inventory.",
                "Confirm Task Completed",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var response = await ApiConfig.Http.PostAsync($"api/procurement/{reqId}/complete-arrival", null);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Unable to Complete Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show(
                    $"Task completed!\n\n{qty}x {device} have been received and successfully added to the inventory.",
                    "Arrival Completed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await LoadArrivals();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error completing task: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
