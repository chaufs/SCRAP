using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Technical
{
    public sealed class TeardownYieldAdjustmentDialog : Form
    {
        private readonly string _categoryName;
        private readonly int _quantity;
        private readonly List<ArchetypeRecipe> _recipes;
        private readonly BindingList<YieldEditItem> _items = new();

        private DataGridView _dgv = null!;
        private Label _lblSummary = null!;
        private Button _btnReset = null!;
        private Button _btnConfirm = null!;
        private Button _btnCancel = null!;

        public TeardownYieldAdjustmentDialog(string categoryName, int quantity, List<ArchetypeRecipe> recipes)
        {
            _categoryName = categoryName;
            _quantity = quantity;
            _recipes = recipes;

            Text = "Review & Adjust Teardown Yields — S.C.R.A.P";
            Width = 780;
            Height = 580;
            MinimumSize = new Size(720, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            InitializeItems();
            BuildUi();
            UpdateSummary();
        }

        private void InitializeItems()
        {
            foreach (var r in _recipes)
            {
                var standard = Math.Round(r.WeightKgPerUnit * _quantity, 2, MidpointRounding.AwayFromZero);
                _items.Add(new YieldEditItem
                {
                    MaterialName = r.MaterialName,
                    StandardWeightKg = standard,
                    ActualWeightKg = standard
                });
            }
        }

        private void BuildUi()
        {
            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Theme.White,
                Padding = new Padding(24, 14, 24, 12)
            };
            var headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Theme.CardBorder
            };
            header.Controls.Add(headerBorder);

            var lblTitle = new Label
            {
                Text = $"Teardown Yields — {_quantity}x {_categoryName}",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                AutoSize = true,
                Location = new Point(24, 12)
            };

            var lblSubtitle = new Label
            {
                Text = "Standard expected yields are shown below. If the actual measured yield differs, edit the Actual Yield (kg) column directly.",
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                Location = new Point(24, 44),
                AutoSize = true,
                MaximumSize = new Size(720, 0)
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);

            // Footer
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                BackColor = Theme.White,
                Padding = new Padding(24, 14, 24, 14)
            };
            var footerBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.CardBorder
            };
            footer.Controls.Add(footerBorder);

            _btnReset = new Button
            {
                Text = "↺ Reset to Standard",
                Width = 175,
                Height = 38,
                Left = 24,
                Top = 15,
                UseMnemonic = false
            };
            Theme.StyleOutlineButton(_btnReset);
            _btnReset.Click += (_, _) => ResetToStandard();
            footer.Controls.Add(_btnReset);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = 38,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(footer.Width - 124, 15),
                DialogResult = DialogResult.Cancel,
                UseMnemonic = false
            };
            Theme.StyleOutlineButton(_btnCancel);
            footer.Controls.Add(_btnCancel);

            _btnConfirm = new Button
            {
                Text = "✓ Confirm & Record Teardown",
                Width = 240,
                Height = 38,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(footer.Width - 376, 15),
                DialogResult = DialogResult.OK,
                UseMnemonic = false
            };
            Theme.StylePrimaryButton(_btnConfirm);
            _btnConfirm.Click += (_, _) => OnConfirm();
            footer.Controls.Add(_btnConfirm);

            // Center Panel with Grid and Summary
            var centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            // Summary Bar above the footer
            var summaryPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                Padding = new Padding(4, 8, 4, 4)
            };

            _lblSummary = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                Text = "Calculating totals..."
            };
            summaryPanel.Controls.Add(_lblSummary);

            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.White
            };
            gridCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, gridCard.Width - 1, gridCard.Height - 1);
            };

            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false
            };
            Theme.StyleGrid(_dgv);

            // Overwrite Theme.StyleGrid defaults that disable editing
            _dgv.ReadOnly = false;
            _dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _dgv.EditMode = DataGridViewEditMode.EditOnEnter;

            SetupColumns();

            _dgv.CellClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == _dgv.Columns["ActualWeightKg"]?.Index)
                {
                    _dgv.BeginEdit(true);
                }
            };

            _dgv.CellEndEdit += (_, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _items.Count)
                {
                    var item = _items[e.RowIndex];
                    var cellVal = _dgv.Rows[e.RowIndex].Cells["ActualWeightKg"].Value;
                    if (cellVal != null && decimal.TryParse(cellVal.ToString(), out var val))
                    {
                        item.ActualWeightKg = Math.Max(0m, Math.Round(val, 2, MidpointRounding.AwayFromZero));
                        _dgv.Rows[e.RowIndex].Cells["ActualWeightKg"].Value = item.ActualWeightKg;
                    }
                    else
                    {
                        _dgv.Rows[e.RowIndex].Cells["ActualWeightKg"].Value = item.ActualWeightKg;
                    }
                    UpdateRowVariance(e.RowIndex);
                    UpdateSummary();
                }
            };

            _dgv.DataError += (_, e) =>
            {
                e.ThrowException = false;
                MessageBox.Show("Please enter a valid numeric weight in kg.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            gridCard.Controls.Add(_dgv);

            centerPanel.Controls.Add(gridCard);
            centerPanel.Controls.Add(summaryPanel);
            gridCard.BringToFront();

            Controls.Add(centerPanel);
            Controls.Add(footer);
            Controls.Add(header);
            centerPanel.BringToFront();

            BindData();
        }

        private void SetupColumns()
        {
            _dgv.Columns.Clear();

            _dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaterialName",
                HeaderText = "Material",
                DataPropertyName = "MaterialName",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 160
            });

            _dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StandardWeightKg",
                HeaderText = "Standard Yield (kg)",
                DataPropertyName = "StandardWeightKg",
                ReadOnly = true,
                Width = 150,
                MinimumWidth = 140,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N2",
                    ForeColor = Theme.MutedText
                }
            });

            var actualCol = new DataGridViewTextBoxColumn
            {
                Name = "ActualWeightKg",
                HeaderText = "Actual Yield (kg) ✎",
                DataPropertyName = "ActualWeightKg",
                ReadOnly = false,
                Width = 165,
                MinimumWidth = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N2",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(5, 96, 74),
                    BackColor = Color.FromArgb(240, 253, 244)
                }
            };
            _dgv.Columns.Add(actualCol);

            _dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Variance",
                HeaderText = "Difference (kg)",
                DataPropertyName = "VarianceText",
                ReadOnly = true,
                Width = 150,
                MinimumWidth = 130,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9f)
                }
            });
        }

        private void BindData()
        {
            _dgv.DataSource = null;
            _dgv.DataSource = _items;

            for (int i = 0; i < _items.Count; i++)
            {
                UpdateRowVariance(i);
            }
        }

        private void UpdateRowVariance(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _items.Count) return;

            var item = _items[rowIndex];
            decimal diff = item.ActualWeightKg - item.StandardWeightKg;

            if (diff == 0)
                item.VarianceText = "Standard (0.00)";
            else if (diff > 0)
                item.VarianceText = $"+{diff:F2} kg";
            else
                item.VarianceText = $"{diff:F2} kg";

            if (_dgv.Rows.Count > rowIndex)
            {
                var cell = _dgv.Rows[rowIndex].Cells["Variance"];
                if (cell != null)
                {
                    cell.Value = item.VarianceText;
                    if (diff > 0)
                        cell.Style.ForeColor = Color.FromArgb(16, 120, 60); // Green
                    else if (diff < 0)
                        cell.Style.ForeColor = Color.FromArgb(180, 80, 20); // Amber/Red
                    else
                        cell.Style.ForeColor = Theme.MutedText;
                }
            }
        }

        private void UpdateSummary()
        {
            decimal totalStd = _items.Sum(x => x.StandardWeightKg);
            decimal totalAct = _items.Sum(x => x.ActualWeightKg);
            decimal totalDiff = totalAct - totalStd;

            string diffStr = totalDiff == 0
                ? "Exact Match"
                : (totalDiff > 0 ? $"+{totalDiff:F2} kg" : $"{totalDiff:F2} kg");

            _lblSummary.Text = $"Total Standard: {totalStd:F2} kg   |   Total Actual: {totalAct:F2} kg   |   Difference: {diffStr}";
        }

        private void ResetToStandard()
        {
            _dgv.EndEdit();
            foreach (var item in _items)
            {
                item.ActualWeightKg = item.StandardWeightKg;
            }
            _items.ResetBindings();
            for (int i = 0; i < _items.Count; i++)
            {
                UpdateRowVariance(i);
            }
            UpdateSummary();
        }

        private void OnConfirm()
        {
            _dgv.EndEdit();

            for (int i = 0; i < _dgv.Rows.Count && i < _items.Count; i++)
            {
                var cellVal = _dgv.Rows[i].Cells["ActualWeightKg"].Value;
                if (cellVal != null && decimal.TryParse(cellVal.ToString(), out var val))
                {
                    _items[i].ActualWeightKg = Math.Max(0m, Math.Round(val, 2, MidpointRounding.AwayFromZero));
                }
            }

            // Validate all numbers are >= 0
            foreach (var item in _items)
            {
                if (item.ActualWeightKg < 0)
                {
                    MessageBox.Show($"Weight for '{item.MaterialName}' cannot be negative.", "Invalid Weight", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Returns the dictionary of overrides for any materials where actual weight was specified.
        /// </summary>
        public Dictionary<string, decimal> GetYieldOverrides()
        {
            var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in _items)
            {
                dict[item.MaterialName] = item.ActualWeightKg;
            }
            return dict;
        }

        public class YieldEditItem
        {
            public string MaterialName { get; set; } = string.Empty;
            public decimal StandardWeightKg { get; set; }
            public decimal ActualWeightKg { get; set; }
            public string VarianceText { get; set; } = "Standard (0.00)";
        }
    }
}
