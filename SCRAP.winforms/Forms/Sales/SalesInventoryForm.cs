using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    /// <summary>
    /// SalesInventoryForm — View device stock levels, low-stock alerts, detailed device items, and recovered commodities.
    /// Allows Sales Staff to immediately request procurement when device inventory is low.
    /// </summary>
    [DesignerCategory("Code")]
    public class SalesInventoryForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;
        private TabControl tabs = null!;

        // ── Low Stock Alert Banner ───────────────────────────────────────
        private Panel pnlAlertBanner = null!;
        private Label lblAlertText = null!;
        private Button btnAlertRequest = null!;

        // ── Tab 1: Device Stock & Alerts ─────────────────────────────────
        private DataGridView dgvStockLevels = null!;
        private Panel cardStockLevels = null!;
        private Label lblSelectedCategory = null!;
        private Button btnRequestForSelected = null!;
        private dynamic? _selectedCategoryItem = null;

        // KPI stats
        private Label valTotalInStock = null!;
        private Label valTotalCategories = null!;
        private Label valLowStockCount = null!;
        private Panel cardInStock = null!;
        private Panel cardCategories = null!;
        private Panel cardLowStock = null!;
        private Label lblFilterBadge = null!;

        private enum StockFilterMode
        {
            All,
            InStockOnly,
            LowStockOnly
        }

        private StockFilterMode _currentFilter = StockFilterMode.All;
        private List<StockLevelDto> _stockDataList = new();

        // ── Tab 2: Detailed Devices List ─────────────────────────────────
        private DataGridView dgvActive = null!;
        private Panel cardActive = null!;

        // ── Tab 3: Recovered Commodities ─────────────────────────────────
        private DataGridView dgvCommodities = null!;
        private Panel cardCommodities = null!;

        private readonly int _initialTab;

        public SalesInventoryForm(int initialTab = 0)
        {
            _initialTab = initialTab;
            Text = "Device Inventory & Stock";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadStockLevels();
            _ = LoadActive();
            if (_initialTab > 0 && tabs.TabPages.Count > _initialTab)
            {
                tabs.SelectedIndex = _initialTab;
                if (_initialTab == 2) _ = LoadCommodities();
            }
        }

        public SalesInventoryForm() : this(0) { }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Device Inventory & Stock",
                UseMnemonic = false,
                Left = 32,
                Top = 24,
                Width = 520,
                Height = 34,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };
            lblSubtitle = new Label
            {
                Text = "Monitor device inventory levels, track low stock alerts, and request procurement for replenishment.",
                Left = 32,
                Top = 58,
                Width = 750,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };
            btnRefresh = new Button
            {
                Text = "↻  Refresh All",
                Width = 120,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) =>
            {
                await LoadStockLevels();
                if (tabs.SelectedIndex == 1) await LoadActive();
                else if (tabs.SelectedIndex == 2) await LoadCommodities();
            };

            // ── Alert Banner ─────────────────────────────────────────────
            pnlAlertBanner = new Panel
            {
                Left = 32,
                Top = 86,
                Height = 44,
                BackColor = ColorTranslator.FromHtml("#FEF2F2"),
                Visible = false
            };
            pnlAlertBanner.Paint += (s, e) =>
            {
                using var pen = new Pen(ColorTranslator.FromHtml("#F87171"), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlAlertBanner.Width - 1, pnlAlertBanner.Height - 1);
            };

            lblAlertText = new Label
            {
                Left = 14,
                Top = 11,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#991B1B"),
                Text = "⚠️ LOW STOCK ALERT: Some device categories are running low or out of stock!"
            };

            btnAlertRequest = new Button
            {
                Text = "Request Procurement →",
                Width = 175,
                Height = 28,
                Top = 8,
                BackColor = ColorTranslator.FromHtml("#DC2626"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnAlertRequest.FlatAppearance.BorderSize = 0;
            btnAlertRequest.Click += (_, _) => OpenProcurementRequest();

            pnlAlertBanner.Controls.Add(lblAlertText);
            pnlAlertBanner.Controls.Add(btnAlertRequest);

            // ── Tabs ─────────────────────────────────────────────────────
            tabs = new TabControl
            {
                Left = 32,
                Top = 138,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var tabStock = new TabPage("Device Stock & Alerts") { BackColor = Theme.Background, Padding = new Padding(12) };
            var tabActive = new TabPage("Device Serial List") { BackColor = Theme.Background, Padding = new Padding(0) };
            var tabComm = new TabPage("Recovered Commodities") { BackColor = Theme.Background, Padding = new Padding(0) };

            BuildStockTab(tabStock);

            cardActive = MakeCard();
            dgvActive = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvActive);
            cardActive.Controls.Add(dgvActive);
            tabActive.Controls.Add(cardActive);

            cardCommodities = MakeCard();
            dgvCommodities = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvCommodities);
            cardCommodities.Controls.Add(dgvCommodities);
            tabComm.Controls.Add(cardCommodities);

            tabs.TabPages.Add(tabStock);
            tabs.TabPages.Add(tabActive);
            tabs.TabPages.Add(tabComm);

            tabs.SelectedIndexChanged += async (s, e) =>
            {
                if (tabs.SelectedIndex == 0) await LoadStockLevels();
                else if (tabs.SelectedIndex == 1) await LoadActive();
                else if (tabs.SelectedIndex == 2) await LoadCommodities();
            };

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(pnlAlertBanner);
            Controls.Add(tabs);

            Resize += (s, e) => LayoutPage();
            LayoutPage();
        }

        private void BuildStockTab(TabPage tab)
        {
            // Stat Cards Bar
            var pnlStats = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.Transparent };

            cardInStock = MakeMiniStatCard(
                0, 
                "TOTAL IN-STOCK DEVICES", 
                "—", 
                ColorTranslator.FromHtml("#2563EB"), 
                ColorTranslator.FromHtml("#EFF6FF"),
                () => _currentFilter == StockFilterMode.InStockOnly,
                out valTotalInStock,
                () =>
                {
                    if (_currentFilter == StockFilterMode.InStockOnly)
                    {
                        tabs.SelectedIndex = 1;
                    }
                    else
                    {
                        _currentFilter = StockFilterMode.InStockOnly;
                        ApplyStockFilter();
                    }
                },
                () => tabs.SelectedIndex = 1,
                "Click to filter categories with in-stock devices. Click again or double-click to view Device Serial List."
            );

            cardCategories = MakeMiniStatCard(
                220, 
                "CATEGORIES MONITORED", 
                "—", 
                ColorTranslator.FromHtml("#475569"), 
                ColorTranslator.FromHtml("#F1F5F9"),
                () => _currentFilter == StockFilterMode.All,
                out valTotalCategories,
                () =>
                {
                    _currentFilter = StockFilterMode.All;
                    ApplyStockFilter();
                },
                null,
                "Click to show all monitored device categories."
            );

            cardLowStock = MakeMiniStatCard(
                440, 
                "LOW STOCK ALERTS", 
                "—", 
                ColorTranslator.FromHtml("#DC2626"), 
                ColorTranslator.FromHtml("#FEF2F2"),
                () => _currentFilter == StockFilterMode.LowStockOnly,
                out valLowStockCount,
                () =>
                {
                    if (_currentFilter == StockFilterMode.LowStockOnly)
                        _currentFilter = StockFilterMode.All;
                    else
                        _currentFilter = StockFilterMode.LowStockOnly;
                    ApplyStockFilter();
                },
                null,
                "Click to filter categories with low stock or out-of-stock alerts."
            );

            lblFilterBadge = new Label
            {
                Left = 660,
                Top = 22,
                Height = 24,
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.MutedText,
                Text = "✓ Showing all categories",
                Cursor = Cursors.Default
            };
            lblFilterBadge.Click += (_, _) =>
            {
                if (_currentFilter != StockFilterMode.All)
                {
                    _currentFilter = StockFilterMode.All;
                    ApplyStockFilter();
                }
            };

            pnlStats.Controls.AddRange(new Control[] { cardInStock, cardCategories, cardLowStock, lblFilterBadge });
            tab.Controls.Add(pnlStats);

            // Action Panel at bottom
            var actionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Theme.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            actionPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, actionPanel.Width - 1, actionPanel.Height - 1);
            };

            btnRequestForSelected = new Button
            {
                Text = "➕ Request Procurement",
                Width = 280,
                Height = 36,
                Top = 14,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Enabled = false
            };
            Theme.StylePrimaryButton(btnRequestForSelected);
            btnRequestForSelected.Click += (_, _) =>
            {
                if (_selectedCategoryItem != null)
                {
                    try
                    {
                        int catId = _selectedCategoryItem.CategoryId;
                        string catName = _selectedCategoryItem.CategoryName;
                        OpenProcurementRequest(catId, catName);
                    }
                    catch
                    {
                        OpenProcurementRequest();
                    }
                }
                else
                {
                    OpenProcurementRequest();
                }
            };

            lblSelectedCategory = new Label
            {
                Text = "Select a device category above to check stock or request replenishment.",
                Left = 16,
                Top = 20,
                Width = Math.Max(200, actionPanel.ClientSize.Width - btnRequestForSelected.Width - 40),
                Height = 24,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            actionPanel.Resize += (s, e) =>
            {
                btnRequestForSelected.Left = Math.Max(200, actionPanel.ClientSize.Width - btnRequestForSelected.Width - 16);
                lblSelectedCategory.Width = Math.Max(180, btnRequestForSelected.Left - 24);
            };

            actionPanel.Controls.Add(lblSelectedCategory);
            actionPanel.Controls.Add(btnRequestForSelected);
            tab.Controls.Add(actionPanel);

            // Grid card in center
            cardStockLevels = MakeCard();
            dgvStockLevels = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvStockLevels);
            SetupStockLevelColumns();
            dgvStockLevels.SelectionChanged += (_, _) => OnStockLevelSelectionChanged();
            dgvStockLevels.CellFormatting += DgvStockLevels_CellFormatting;
            dgvStockLevels.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    tabs.SelectedIndex = 1;
                }
            };

            cardStockLevels.Controls.Add(dgvStockLevels);
            tab.Controls.Add(cardStockLevels);
            cardStockLevels.BringToFront();
        }

        private Panel MakeMiniStatCard(
            int left, 
            string labelText, 
            string valueText, 
            Color accentColor, 
            Color activeBgColor,
            Func<bool> isActive,
            out Label outVal, 
            Action onClick, 
            Action? onDoubleClick = null, 
            string tooltipText = "")
        {
            var p = new Panel
            {
                Left = left,
                Top = 0,
                Width = 200,
                Height = 62,
                BackColor = Theme.White,
                Cursor = Cursors.Hand
            };

            bool isHovered = false;

            p.Paint += (s, e) =>
            {
                bool active = isActive();
                Color bg = active ? activeBgColor : (isHovered ? ColorTranslator.FromHtml("#F8FAFC") : Theme.White);
                p.BackColor = bg;

                Color borderColor = active ? accentColor : (isHovered ? ColorTranslator.FromHtml("#CBD5E1") : Theme.CardBorder);
                int borderWidth = active ? 2 : 1;

                using var pen = new Pen(borderColor, borderWidth);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);

                // Left accent bar
                int barWidth = active ? 6 : 4;
                using var brush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(brush, 0, 0, barWidth, p.Height);
            };

            var lbl = new Label
            {
                Text = labelText,
                Left = 14,
                Top = 8,
                Width = 180,
                Height = 16,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            var val = new Label
            {
                Text = valueText,
                Left = 14,
                Top = 26,
                Width = 180,
                Height = 28,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            outVal = val;

            void HandleClick(object? sender, EventArgs e) => onClick();
            p.Click += HandleClick;
            lbl.Click += HandleClick;
            val.Click += HandleClick;

            if (onDoubleClick != null)
            {
                void HandleDoubleClick(object? sender, EventArgs e) => onDoubleClick();
                p.DoubleClick += HandleDoubleClick;
                lbl.DoubleClick += HandleDoubleClick;
                val.DoubleClick += HandleDoubleClick;
            }

            void OnEnter(object? sender, EventArgs e)
            {
                isHovered = true;
                p.Invalidate();
            }

            void OnLeave(object? sender, EventArgs e)
            {
                var pt = p.PointToClient(Cursor.Position);
                if (!p.ClientRectangle.Contains(pt))
                {
                    isHovered = false;
                    p.Invalidate();
                }
            }

            p.MouseEnter += OnEnter;
            lbl.MouseEnter += OnEnter;
            val.MouseEnter += OnEnter;

            p.MouseLeave += OnLeave;
            lbl.MouseLeave += OnLeave;
            val.MouseLeave += OnLeave;

            if (!string.IsNullOrEmpty(tooltipText))
            {
                var tip = new ToolTip();
                tip.SetToolTip(p, tooltipText);
                tip.SetToolTip(lbl, tooltipText);
                tip.SetToolTip(val, tooltipText);
            }

            p.Controls.Add(lbl);
            p.Controls.Add(val);
            return p;
        }

        private void SetupStockLevelColumns()
        {
            dgvStockLevels.AutoGenerateColumns = false;
            dgvStockLevels.Columns.Clear();
            dgvStockLevels.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "CategoryName", DataPropertyName = "CategoryName", HeaderText = "Device Category", Width = 180 },
                new DataGridViewTextBoxColumn
                {
                    Name = "InStockCount",
                    DataPropertyName = "InStockCount",
                    HeaderText = "In-Stock (Units)",
                    Width = 120,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                    }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "AlertStatusText",
                    DataPropertyName = "AlertStatusText",
                    HeaderText = "Stock Status",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                    }
                },
                new DataGridViewTextBoxColumn { Name = "InTeardownCount", DataPropertyName = "InTeardownCount", HeaderText = "In Teardown", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "DisposedCount", DataPropertyName = "DisposedCount", HeaderText = "Processed / Disposed", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "TotalReceived", DataPropertyName = "TotalReceived", HeaderText = "Total Received", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "Description", DataPropertyName = "Description", HeaderText = "Description", Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });
        }

        private static Panel MakeCard()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.White, Padding = new Padding(1) };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private void LayoutPage()
        {
            btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
            btnRefresh.Top = 26;

            int contentW = Math.Max(400, ClientSize.Width - 64);
            pnlAlertBanner.Width = contentW;
            lblAlertText.Width = contentW - 200;
            btnAlertRequest.Left = contentW - 190;

            if (pnlAlertBanner.Visible)
            {
                tabs.Top = 140;
                tabs.Height = Math.Max(260, ClientSize.Height - 156);
            }
            else
            {
                tabs.Top = 96;
                tabs.Height = Math.Max(300, ClientSize.Height - 112);
            }

            tabs.Left = 32;
            tabs.Width = contentW;

            if (dgvStockLevels != null && dgvStockLevels.Columns.Count > 0) Theme.FillColumnsToWidth(dgvStockLevels);
            if (dgvActive != null && dgvActive.Columns.Count > 0) Theme.FillColumnsToWidth(dgvActive);
            if (dgvCommodities != null && dgvCommodities.Columns.Count > 0) Theme.FillColumnsToWidth(dgvCommodities);
        }

        private void DgvStockLevels_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvStockLevels.Rows.Count) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= dgvStockLevels.Columns.Count) return;

            var row = dgvStockLevels.Rows[e.RowIndex];
            if (row.DataBoundItem is { } item)
            {
                string alert = "";
                try { alert = ((dynamic)item).AlertLevel?.ToString() ?? ""; } catch { }

                var colName = dgvStockLevels.Columns[e.ColumnIndex].Name;
                if (colName == "AlertStatusText" || colName == "InStockCount")
                {
                    if (e.CellStyle == null) return;
                    if (alert == "OutOfStock")
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#DC2626");
                        e.CellStyle.SelectionForeColor = ColorTranslator.FromHtml("#FCA5A5");
                    }
                    else if (alert == "LowStock")
                    {
                        e.CellStyle.ForeColor = ColorTranslator.FromHtml("#D97706");
                        e.CellStyle.SelectionForeColor = ColorTranslator.FromHtml("#FDE68A");
                    }
                    else
                    {
                        if (colName == "AlertStatusText")
                            e.CellStyle.ForeColor = ColorTranslator.FromHtml("#16A34A");
                    }
                }
            }
        }

        private void OnStockLevelSelectionChanged()
        {
            if (dgvStockLevels.CurrentRow?.DataBoundItem != null)
            {
                _selectedCategoryItem = dgvStockLevels.CurrentRow.DataBoundItem;
                try
                {
                    string catName = _selectedCategoryItem.CategoryName;
                    int inStock = _selectedCategoryItem.InStockCount;
                    string alert = _selectedCategoryItem.AlertLevel;

                    string badge = alert == "OutOfStock" ? "🔴 OUT OF STOCK" : (alert == "LowStock" ? "🟡 LOW STOCK" : "🟢 OK");
                    lblSelectedCategory.Text = $"Selected: {catName} — {inStock} units in stock ({badge})";
                    btnRequestForSelected.Text = inStock <= 5 ? $"⚠️ Request Procurement for {catName}" : $"➕ Request Procurement ({catName})";
                    btnRequestForSelected.Enabled = true;
                }
                catch
                {
                    btnRequestForSelected.Enabled = false;
                }
            }
            else
            {
                _selectedCategoryItem = null;
                lblSelectedCategory.Text = "Select a device category above to check stock or request replenishment.";
                btnRequestForSelected.Text = "➕ Request Procurement";
                btnRequestForSelected.Enabled = false;
            }
        }

        private void OpenProcurementRequest(int categoryId = 0, string? deviceName = null)
        {
            // If parent form is SalesMainForm, navigate directly to procurement tab
            if (TopLevelControl is SCRAP.winforms.SalesMainForm main)
            {
                main.NavigateToProcurement(categoryId, deviceName);
            }
            else
            {
                using var dlg = new Form
                {
                    Text = "Request Device Procurement",
                    Width = 900,
                    Height = 650,
                    StartPosition = FormStartPosition.CenterParent
                };
                var child = new Sales.SalesProcurementForm(categoryId, deviceName)
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                dlg.Controls.Add(child);
                child.Show();
                dlg.ShowDialog(this);
                _ = LoadStockLevels();
            }
        }

        private async Task LoadStockLevels()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Inventory/stock-levels");
                if (!res.IsSuccessStatusCode) return;

                var data = await res.Content.ReadFromJsonAsync<List<StockLevelDto>>(ApiConfig.JsonOptions) ?? new();
                _stockDataList = data;

                int totalInStock = data.Sum(x => x.InStockCount);
                int totalCategories = data.Count;
                var lowStockCategories = data.Where(x => x.IsLowStock).ToList();
                int lowStockCount = lowStockCategories.Count;

                valTotalInStock.Text = totalInStock.ToString("N0");
                valTotalCategories.Text = totalCategories.ToString();
                valLowStockCount.Text = lowStockCount.ToString();

                if (lowStockCount > 0)
                {
                    var names = string.Join(", ", lowStockCategories.Take(3).Select(x => $"{x.CategoryName} ({x.InStockCount})"));
                    lblAlertText.Text = $"⚠️ LOW STOCK ALERT: {lowStockCount} category(s) low or out of stock: {names}!";
                    pnlAlertBanner.Visible = true;
                }
                else
                {
                    pnlAlertBanner.Visible = false;
                }
                LayoutPage();

                ApplyStockFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading stock levels: " + ex.Message, "Inventory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyStockFilter()
        {
            if (_stockDataList == null) return;

            IEnumerable<StockLevelDto> filtered = _stockDataList;
            if (_currentFilter == StockFilterMode.InStockOnly)
            {
                filtered = _stockDataList.Where(x => x.InStockCount > 0);
            }
            else if (_currentFilter == StockFilterMode.LowStockOnly)
            {
                filtered = _stockDataList.Where(x => x.IsLowStock || x.AlertLevel == "LowStock" || x.AlertLevel == "OutOfStock");
            }

            var displayList = filtered.Select(x => new
            {
                x.CategoryName,
                x.InStockCount,
                AlertStatusText = x.AlertLevel switch
                {
                    "OutOfStock" => "🔴 Out of Stock (0)",
                    "LowStock" => $"🟡 Low Stock ({x.InStockCount})",
                    _ => $"🟢 Normal ({x.InStockCount})"
                },
                x.InTeardownCount,
                x.DisposedCount,
                x.TotalReceived,
                x.Description,
                x.CategoryId,
                AlertLevel = x.AlertLevel
            }).ToList();

            void Bind()
            {
                dgvStockLevels.AutoGenerateColumns = false;
                dgvStockLevels.DataSource = null;
                dgvStockLevels.AutoGenerateColumns = false;
                dgvStockLevels.DataSource = displayList;
                Theme.FillColumnsToWidth(dgvStockLevels);
                OnStockLevelSelectionChanged();
                UpdateFilterBadge();
                cardInStock?.Invalidate();
                cardCategories?.Invalidate();
                cardLowStock?.Invalidate();
            }

            if (InvokeRequired) Invoke(Bind);
            else Bind();
        }

        private void UpdateFilterBadge()
        {
            if (lblFilterBadge == null) return;

            int total = _stockDataList.Count;
            int count = dgvStockLevels.Rows.Count;

            switch (_currentFilter)
            {
                case StockFilterMode.InStockOnly:
                    lblFilterBadge.Text = $"🔍 Showing {count} in-stock category(s) — (Click card again for Serial List)";
                    lblFilterBadge.ForeColor = ColorTranslator.FromHtml("#2563EB");
                    lblFilterBadge.Cursor = Cursors.Hand;
                    break;
                case StockFilterMode.LowStockOnly:
                    lblFilterBadge.Text = $"⚠️ Showing {count} of {total} low-stock category(s)  [Click to Show All]";
                    lblFilterBadge.ForeColor = ColorTranslator.FromHtml("#DC2626");
                    lblFilterBadge.Cursor = Cursors.Hand;
                    break;
                default:
                    lblFilterBadge.Text = $"✓ Showing all {total} monitored categories";
                    lblFilterBadge.ForeColor = Theme.MutedText;
                    lblFilterBadge.Cursor = Cursors.Default;
                    break;
            }
        }

        private void ConfigureActiveColumns()
        {
            Theme.ConfigureColumns(dgvActive,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("Product ID", 90),
                    ["DeviceName"] = ("Device Name", 180),
                    ["Category"] = ("Category", 130),
                    ["SerialNumber"] = ("Serial Number", 150),
                    ["Status"] = ("Status", 100),
                    ["DateReceived"] = ("Date Received", 120),
                    ["HasStorage"] = ("Storage Device", 110),
                    ["Notes"] = ("Notes", 140)
                },
                "DeviceCategoryId", "DeviceCategory", "DeviceCategoryNavigation", "TeardownBatches", "Branch", "BranchId");
        }

        private void ConfigureCommodityColumns()
        {
            Theme.ConfigureColumns(dgvCommodities,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 70),
                    ["MaterialName"] = ("Material", 200),
                    ["CurrentTotalWeightKg"] = ("Current Total Weight (kg)", 200),
                    ["WeightKg"] = ("Weight (kg)", 120),
                    ["Quantity"] = ("Quantity", 100),
                    ["SourceBatchId"] = ("Batch ID", 100),
                    ["DateRecovered"] = ("Date Recovered", 180),
                    ["Notes"] = ("Notes", 180)
                },
                "TeardownBatch", "TeardownBatchNavigation");
        }

        private async Task LoadActive()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Inventory");
                if (res.IsSuccessStatusCode)
                {
                    var items = await res.Content.ReadFromJsonAsync<List<Inventory>>(ApiConfig.JsonOptions) ?? new();
                    var displayList = items.Select(x => new
                    {
                        x.Id,
                        DeviceName = x.DeviceName,
                        Category = x.DeviceCategory?.Name ?? $"Category #{x.DeviceCategoryId}",
                        SerialNumber = x.SerialNumber ?? "",
                        Status = x.Status.ToString(),
                        DateReceived = x.DateReceived.ToString("yyyy-MM-dd"),
                        HasStorage = x.HasStorageDevice ? "Yes" : "No",
                        Notes = x.Notes ?? ""
                    }).ToList();

                    dgvActive.DataSource = displayList;
                    ConfigureActiveColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading detailed inventory: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadCommodities()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/RawInventory");
                if (res.IsSuccessStatusCode)
                {
                    dgvCommodities.DataSource = await res.Content.ReadFromJsonAsync<List<RawInventory>>();
                    ConfigureCommodityColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading commodities: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class StockLevelDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int InStockCount { get; set; }
            public int InTeardownCount { get; set; }
            public int DisposedCount { get; set; }
            public int TotalReceived { get; set; }
            public string AlertLevel { get; set; } = "Normal";
            public bool IsLowStock { get; set; }
        }
    }
}