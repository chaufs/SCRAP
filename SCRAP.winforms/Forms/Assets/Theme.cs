using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SCRAP.winforms
{
    /// <summary>
    /// Modern admin-panel theme for S.C.R.A.P (EcoExtract branding).
    /// Dark sidebar + light content, clean data tables.
    /// </summary>
    public static class Theme
    {
        // Brand
        public static readonly Color Green = ColorTranslator.FromHtml("#00AD4C");
        public static readonly Color Blue = ColorTranslator.FromHtml("#0069E5");
        public static readonly Color BlueHover = ColorTranslator.FromHtml("#0057C2");

        // Shell
        public static readonly Color SidebarBg = ColorTranslator.FromHtml("#0F172A");
        public static readonly Color SidebarHover = ColorTranslator.FromHtml("#1E293B");
        public static readonly Color SidebarActive = ColorTranslator.FromHtml("#1E293B");
        public static readonly Color SidebarText = ColorTranslator.FromHtml("#94A3B8");
        public static readonly Color SidebarTextActive = Color.White;
        public static readonly Color SidebarSection = ColorTranslator.FromHtml("#64748B");

        // Content
        public static readonly Color Background = ColorTranslator.FromHtml("#F8FAFC");
        public static readonly Color White = Color.White;
        public static readonly Color DarkText = ColorTranslator.FromHtml("#0F172A");
        public static readonly Color MutedText = ColorTranslator.FromHtml("#64748B");
        public static readonly Color CardBorder = ColorTranslator.FromHtml("#E2E8F0");
        public static readonly Color SoftBlue = ColorTranslator.FromHtml("#EFF6FF");
        public static readonly Color SoftGreen = ColorTranslator.FromHtml("#E8F8EF");
        public static readonly Color GridHeaderBg = ColorTranslator.FromHtml("#F1F5F9");
        public static readonly Color GridLine = ColorTranslator.FromHtml("#F1F5F9");

        private static Image? _appLogo;
        public static Image? AppLogo
        {
            get
            {
                if (_appLogo != null) return _appLogo;
                try
                {
                    var asm = typeof(Theme).Assembly;
                    foreach (var name in asm.GetManifestResourceNames())
                    {
                        if (name.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase))
                        {
                            using var stream = asm.GetManifestResourceStream(name);
                            if (stream != null)
                            {
                                _appLogo = Image.FromStream(stream);
                                return _appLogo;
                            }
                        }
                    }

                    string localPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Forms", "Assets", "logo.png");
                    if (System.IO.File.Exists(localPath))
                    {
                        _appLogo = Image.FromFile(localPath);
                        return _appLogo;
                    }

                    string devPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Forms", "Assets", "logo.png");
                    if (System.IO.File.Exists(devPath))
                    {
                        _appLogo = Image.FromFile(devPath);
                        return _appLogo;
                    }
                }
                catch { }
                return null;
            }
        }

        // Typography
        public static readonly Font TitleFont = new Font("Segoe UI", 20, FontStyle.Bold);
        public static readonly Font SubtitleFont = new Font("Segoe UI", 10.5f);
        public static readonly Font LabelFont = new Font("Segoe UI", 10);
        public static readonly Font ButtonFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font NavFont = new Font("Segoe UI", 10f);
        public static readonly Font NavSectionFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        public static readonly Font StatValueFont = new Font("Segoe UI", 22, FontStyle.Bold);
        public static readonly Font StatLabelFont = new Font("Segoe UI", 9f);
        public static readonly Font SectionFont = new Font("Segoe UI", 11, FontStyle.Bold);
        public static readonly Font GridHeaderFont = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        public static readonly Font GridCellFont = new Font("Segoe UI", 9.5f);

        public static void StyleForm(Form form)
        {
            form.BackColor = Background;
            form.Font = LabelFont;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
            form.MinimumSize = new Size(1000, 600);
        }

        public static void StylePrimaryButton(Button btn)
        {
            btn.BackColor = Blue;
            btn.ForeColor = White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = BlueHover;
            btn.FlatAppearance.MouseDownBackColor = ColorTranslator.FromHtml("#004AAB");
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            btn.Height = 36;
            btn.Padding = new Padding(12, 0, 12, 0);
        }

        public static void StyleSecondaryButton(Button btn)
        {
            btn.BackColor = Green;
            btn.ForeColor = White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#009640");
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            btn.Height = 36;
            btn.Padding = new Padding(12, 0, 12, 0);
        }

        public static void StyleDangerButton(Button btn)
        {
            btn.BackColor = ColorTranslator.FromHtml("#DC2626");
            btn.ForeColor = White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#B91C1C");
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            btn.Height = 36;
        }

        public static void StyleOutlineButton(Button btn)
        {
            btn.BackColor = White;
            btn.ForeColor = DarkText;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = CardBorder;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = GridHeaderBg;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            btn.Height = 36;
        }

        public static void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;

            // Light header like modern admin tables
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = MutedText;
            dgv.ColumnHeadersDefaultCellStyle.Font = GridHeaderFont;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(14, 0, 8, 0);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBg;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = MutedText;
            dgv.ColumnHeadersHeight = 42;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgv.DefaultCellStyle.Font = GridCellFont;
            dgv.DefaultCellStyle.ForeColor = DarkText;
            dgv.DefaultCellStyle.BackColor = White;
            dgv.DefaultCellStyle.SelectionBackColor = SoftBlue;
            dgv.DefaultCellStyle.SelectionForeColor = DarkText;
            dgv.DefaultCellStyle.Padding = new Padding(14, 4, 8, 4);
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FAFBFC");
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = SoftBlue;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = DarkText;

            dgv.RowHeadersVisible = false;
            dgv.GridColor = GridLine;
            dgv.RowTemplate.Height = 40;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToOrderColumns = false;
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Keep columns stretched when the grid is resized
            dgv.SizeChanged -= GridSizeChangedFill;
            dgv.SizeChanged += GridSizeChangedFill;
        }

        private static void GridSizeChangedFill(object? sender, EventArgs e)
        {
            if (sender is DataGridView dgv && dgv.Columns.Count > 0)
            {
                // Fill mode already stretches; ensure weights stay positive
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible && col.FillWeight < 1)
                        col.FillWeight = 1;
                }
            }
        }

        public static void ConfigureColumns(
            DataGridView dgv,
            Dictionary<string, (string Header, int Width)> map,
            params string[] hideColumns)
        {
            if (dgv == null || dgv.IsDisposed || dgv.Columns.Count == 0) return;
            if (!dgv.IsHandleCreated) return;

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            map ??= new Dictionary<string, (string, int)>();

            var hide = new HashSet<string>(hideColumns ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                var n = col.Name;
                if (hide.Contains(n) ||
                    n.EndsWith("Navigation", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("DeviceCategory", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("Inventory", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("TeardownBatch", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("User", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("Branch", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("BranchId", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("Yields", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("ProcessedByUser", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("ArchetypeRecipes", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("RawInventories", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("Inventories", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("TeardownBatches", StringComparison.OrdinalIgnoreCase))
                {
                    col.Visible = false;
                }
            }

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!col.Visible) continue;

                (string Header, int Width)? entry = null;
                if (map.TryGetValue(col.Name, out var exact))
                    entry = exact;
                else
                {
                    foreach (var kv in map)
                    {
                        if (string.Equals(kv.Key, col.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            entry = kv.Value;
                            break;
                        }
                    }
                }

                if (entry.HasValue)
                {
                    col.HeaderText = entry.Value.Header;
                    col.Width = entry.Value.Width;
                    col.MinimumWidth = Math.Max(70, entry.Value.Width / 2);
                }
                else
                {
                    col.HeaderText = SplitPascalCase(col.Name);
                    col.Width = Math.Max(col.Width, 130);
                    col.MinimumWidth = 80;
                }

                // Prefer width that fits the header text so labels are not truncated
                try
                {
                    int headerPx = TextRenderer.MeasureText(col.HeaderText, GridHeaderFont).Width + 28;
                    if (col.Width < headerPx) col.Width = headerPx;
                    if (col.MinimumWidth < headerPx) col.MinimumWidth = headerPx;
                }
                catch { /* ignore measure failures */ }

                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.SortMode = DataGridViewColumnSortMode.Automatic;

                var valueType = col.ValueType;
                if (valueType != null && (valueType == typeof(DateTime) || valueType == typeof(DateTime?)))
                {
                    col.DefaultCellStyle.Format = "MMM dd, yyyy  h:mm tt";
                    if (col.Width < 170) col.Width = 180;
                }
                else if (valueType != null && (valueType == typeof(decimal) || valueType == typeof(decimal?) ||
                         valueType == typeof(double) || valueType == typeof(double?) ||
                         valueType == typeof(float) || valueType == typeof(float?)))
                {
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (valueType != null && (valueType == typeof(int) || valueType == typeof(int?) ||
                         valueType == typeof(long) || valueType == typeof(long?)))
                {
                    if (col.Name.IndexOf("Id", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        col.Name.IndexOf("Quantity", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }
            }

            // Stretch all visible columns proportionally to fill the grid width
            FillColumnsToWidth(dgv);
        }

        /// <summary>
        /// Distributes column widths so the table spans the full grid client width.
        /// </summary>
        public static void FillColumnsToWidth(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed || !dgv.IsHandleCreated || dgv.Columns == null || dgv.Columns.Count == 0) return;

            try
            {
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                var visible = new List<DataGridViewColumn>();
                int fixedTotal = 0;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col == null || !col.Visible) continue;
                    visible.Add(col);
                    fixedTotal += Math.Max(1, col.Width);
                }
                if (visible.Count == 0 || fixedTotal <= 0) return;

                int avail = dgv.ClientSize.Width - 4;
                if (avail < 100) avail = Math.Max(100, dgv.Width - 4);

                if (avail > fixedTotal)
                {
                    float scale = (float)avail / fixedTotal;
                    int used = 0;
                    for (int i = 0; i < visible.Count; i++)
                    {
                        var col = visible[i];
                        if (col == null) continue;
                        if (i == visible.Count - 1)
                        {
                            try
                            {
                                col.Width = Math.Max(col.MinimumWidth, avail - used);
                            }
                            catch { }
                        }
                        else
                        {
                            int w = Math.Max(col.MinimumWidth, (int)(col.Width * scale));
                            try
                            {
                                col.Width = w;
                            }
                            catch { }
                            used += w;
                        }
                    }
                }

                // Also set Fill mode + weights so when the form resizes, columns keep filling
                try
                {
                    dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    foreach (var col in visible)
                    {
                        if (col != null)
                        {
                            if (col is DataGridViewButtonColumn || col.Name.StartsWith("ColView", StringComparison.OrdinalIgnoreCase) || col.HeaderText.Equals("Action", StringComparison.OrdinalIgnoreCase))
                            {
                                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                                col.Width = 90;
                            }
                            else
                            {
                                col.FillWeight = Math.Max(1, col.Width);
                            }
                        }
                    }
                }
                catch { }
            }
            catch
            {
                // Ignore transient layout exceptions during window handle creation/resizing
            }
        }

        public static void ConfigureSimpleColumns(DataGridView dgv, params (string NameContains, string Header, int Width)[] rules)
        {
            if (dgv.Columns.Count == 0) return;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                bool matched = false;
                foreach (var rule in rules)
                {
                    if (col.Name.IndexOf(rule.NameContains, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        col.HeaderText.IndexOf(rule.NameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        col.HeaderText = rule.Header;
                        col.Width = rule.Width;
                        col.MinimumWidth = 80;
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    col.HeaderText = SplitPascalCase(col.Name);
                    col.Width = Math.Max(130, col.Width);
                }

                var vt = col.ValueType;
                if (vt == typeof(decimal) || vt == typeof(decimal?) ||
                    vt == typeof(double) || vt == typeof(double?))
                {
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            FillColumnsToWidth(dgv);
        }

        private static string SplitPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var chars = new List<char> { name[0] };
            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                    chars.Add(' ');
                chars.Add(name[i]);
            }
            return new string(chars.ToArray());
        }

        public static void StyleTextBox(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = LabelFont;
            txt.Height = 32;
            txt.BackColor = White;
            txt.ForeColor = DarkText;
        }

        public static void StyleComboBox(ComboBox cmb)
        {
            cmb.Font = LabelFont;
            cmb.FlatStyle = FlatStyle.Standard;
            cmb.BackColor = White;
            cmb.ForeColor = DarkText;
        }

        public static void StyleNumericUpDown(NumericUpDown num)
        {
            num.Font = LabelFont;
            num.BorderStyle = BorderStyle.FixedSingle;
            num.BackColor = White;
            num.ForeColor = DarkText;
        }

        public static Panel CreateCard()
        {
            return new Panel
            {
                BackColor = White,
                Padding = new Padding(0),
                // Border drawn via parent or BorderStyle
            };
        }

        public static Label CreateSectionTitle(string text, int left, int top, int width = 400)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Font = SectionFont,
                ForeColor = DarkText,
                AutoSize = false,
                Height = 28
            };
        }

        /// <summary>
        /// Builds a sidebar nav button (flat, left-aligned text).
        /// </summary>
        public static Button CreateNavButton(string text, int top, bool active = false)
        {
            var btn = new Button
            {
                Text = "   " + text,
                Left = 12,
                Top = top,
                Width = 216,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = NavFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                FlatAppearance = { BorderSize = 0 }
            };
            if (active)
            {
                btn.BackColor = SidebarActive;
                btn.ForeColor = SidebarTextActive;
            }
            else
            {
                btn.BackColor = SidebarBg;
                btn.ForeColor = SidebarText;
                btn.FlatAppearance.MouseOverBackColor = SidebarHover;
            }
            return btn;
        }

        /// <summary>
        /// Embeds a live Cloud Sync status indicator badge into a sidebar above the user profile area.
        /// </summary>
        public static Forms.CloudSyncBadgeControl AttachCloudSyncBadge(Panel sidebar, Panel userArea)
        {
            if (sidebar.Controls.Contains(userArea))
            {
                sidebar.Controls.Remove(userArea);
            }

            int userHeight = userArea.Height > 0 ? userArea.Height : 92;
            var bottomContainer = new Panel
            {
                Height = userHeight + 54,
                Dock = DockStyle.Bottom,
                BackColor = ColorTranslator.FromHtml("#0B1220")
            };

            var badge = new Forms.CloudSyncBadgeControl
            {
                Dock = DockStyle.Top,
                Height = 54
            };

            userArea.Dock = DockStyle.Fill;

            bottomContainer.Controls.Add(userArea);
            bottomContainer.Controls.Add(badge);
            sidebar.Controls.Add(bottomContainer);

            return badge;
        }

        /// <summary>
        /// Creates a sleek company badge panel to display the current tenant company in the sidebar.
        /// </summary>
        public static Panel CreateCompanyBadge(int top = 70, int left = 16, int width = 208, int height = 34)
        {
            var pnl = new Panel
            {
                Top = top,
                Left = left,
                Width = width,
                Height = height,
                BackColor = ColorTranslator.FromHtml("#1E293B"),
                Padding = new Padding(6, 0, 6, 0)
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(ColorTranslator.FromHtml("#334155"), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            var companyDisplay = Forms.CurrentSession.GetCompanyDisplay();
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "🏢 " + companyDisplay,
                ForeColor = ColorTranslator.FromHtml("#38BDF8"),
                Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };

            var tip = new ToolTip();
            string tipText = $"Active Organization: {companyDisplay}\nTenant Code: {Forms.CurrentSession.CompanyCode}";
            tip.SetToolTip(lbl, tipText);
            tip.SetToolTip(pnl, tipText);

            pnl.Controls.Add(lbl);
            return pnl;
        }
    }
}