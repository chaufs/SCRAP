using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Technical
{
    [DesignerCategory("Code")]
    public class TeardownForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblCategory = null!;
        private ComboBox cmbCategory = null!;
        private Label lblQty = null!;
        private NumericUpDown numQuantity = null!;
        private Button btnPreview = null!;
        private Button btnConfirm = null!;
        private DataGridView dgvPreview = null!;
        private DataGridView dgvHistory = null!;
        private Panel cardPreview = null!;
        private Panel cardHistory = null!;
        private Label lblPreview = null!;
        private Label lblHistory = null!;

        private List<ArchetypeRecipe> _recipes = new();

        public TeardownForm()
        {
            Text = "Teardown";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadCategories();
            _ = LoadHistory();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Teardown",
                Left = 32,
                Top = 28,
                Width = 300,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Preview expected yields and record dismantling batches",
                Left = 32,
                Top = 64,
                Width = 500,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            lblCategory = new Label
            {
                Text = "Device Category",
                Left = 32,
                Top = 104,
                Width = 140,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                Height = 18
            };

            cmbCategory = new ComboBox
            {
                Left = 32,
                Top = 124,
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Name",
                ValueMember = "Id"
            };
            Theme.StyleComboBox(cmbCategory);
            cmbCategory.SelectedIndexChanged += (_, _) => { _recipes.Clear(); dgvPreview.DataSource = null; };

            lblQty = new Label
            {
                Text = "Quantity",
                Left = 308,
                Top = 104,
                Width = 80,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                Height = 18
            };

            numQuantity = new NumericUpDown
            {
                Left = 308,
                Top = 124,
                Width = 90,
                Minimum = 1,
                Maximum = 10000,
                Value = 1
            };
            Theme.StyleNumericUpDown(numQuantity);
            numQuantity.ValueChanged += (_, _) => { _recipes.Clear(); dgvPreview.DataSource = null; };

            btnPreview = new Button
            {
                Text = "Preview Expected Yield",
                Left = 416,
                Top = 122,
                Width = 190,
                Height = 36
            };
            Theme.StylePrimaryButton(btnPreview);
            btnPreview.Click += async (s, e) => await PreviewYield();

            btnConfirm = new Button
            {
                Text = "Confirm Teardown",
                Width = 170,
                Height = 36
            };
            Theme.StyleSecondaryButton(btnConfirm);
            btnConfirm.Click += async (s, e) => await ConfirmTeardown();

            lblPreview = Theme.CreateSectionTitle("Expected Yield Preview", 32, 178);

            cardPreview = MakeCard();
            dgvPreview = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvPreview);
            SetupPreviewColumns();
            cardPreview.Controls.Add(dgvPreview);

            lblHistory = Theme.CreateSectionTitle("Teardown History", 32, 0);

            cardHistory = MakeCard();
            dgvHistory = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvHistory);
            SetupHistoryColumns();
            cardHistory.Controls.Add(dgvHistory);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(lblCategory);
            Controls.Add(cmbCategory);
            Controls.Add(lblQty);
            Controls.Add(numQuantity);
            Controls.Add(btnPreview);
            Controls.Add(btnConfirm);
            Controls.Add(lblPreview);
            Controls.Add(cardPreview);
            Controls.Add(lblHistory);
            Controls.Add(cardHistory);

            Resize += (s, e) => LayoutPage();
            LayoutPage();
        }

        private Panel MakeCard()
        {
            var p = new Panel { BackColor = Theme.White };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private void LayoutPage()
        {
            int contentW = Math.Max(400, ClientSize.Width - 64);

            cardPreview.Left = 32;
            cardPreview.Top = 210;
            cardPreview.Width = contentW;
            cardPreview.Height = 160;

            btnConfirm.Left = 32;
            btnConfirm.Top = 384;

            lblHistory.Top = 434;
            lblHistory.Left = 32;

            cardHistory.Left = 32;
            cardHistory.Top = 466;
            cardHistory.Width = contentW;
            cardHistory.Height = Math.Max(180, ClientSize.Height - 490);

            if (dgvPreview.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvPreview);
            if (dgvHistory.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvHistory);
        }

        private class StockCheckResult
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public int AvailableStock { get; set; }
            public int RequestedQuantity { get; set; }
            public bool HasSufficientStock { get; set; }
        }

        private void SetupPreviewColumns()
        {
            dgvPreview.AutoGenerateColumns = false;
            dgvPreview.Columns.Clear();

            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Material",
                HeaderText = "Output Material",
                DataPropertyName = "Material",
                FillWeight = 50,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Weight",
                HeaderText = "Expected Weight (kg)",
                DataPropertyName = "ExpectedWeight",
                Width = 220,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
        }

        private void SetupHistoryColumns()
        {
            dgvHistory.AutoGenerateColumns = false;
            dgvHistory.Columns.Clear();

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BatchCode",
                HeaderText = "Batch #",
                DataPropertyName = "BatchCode",
                Width = 110
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Date",
                HeaderText = "Date Processed",
                DataPropertyName = "Date",
                Width = 160
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Device Category",
                DataPropertyName = "Category",
                FillWeight = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Quantity",
                HeaderText = "Qty Dismantled",
                DataPropertyName = "Quantity",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Yields",
                HeaderText = "Yield Types",
                DataPropertyName = "Yields",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Branch",
                HeaderText = "Branch",
                DataPropertyName = "Branch",
                Width = 180
            });
        }

        private async Task LoadCategories()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/DeviceCategories");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<DeviceCategory>>();
                    cmbCategory.DataSource = data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading categories: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PreviewYield()
        {
            if (cmbCategory.SelectedValue is not int categoryId)
            {
                MessageBox.Show("Please select a device category first.", "Select Category",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int qty = (int)numQuantity.Value;

            try
            {
                // Validate branch inventory stock BEFORE previewing yield
                var stockRes = await ApiConfig.Http.GetAsync($"api/Teardown/stock-check?categoryId={categoryId}&quantity={qty}");
                if (stockRes.IsSuccessStatusCode)
                {
                    var stock = await stockRes.Content.ReadFromJsonAsync<StockCheckResult>();
                    if (stock != null)
                    {
                        if (stock.AvailableStock <= 0)
                        {
                            dgvPreview.DataSource = null;
                            _recipes.Clear();
                            MessageBox.Show(
                                $"No available stock: There are 0 {stock.CategoryName} units in stock for your branch.\n\nCannot preview or perform teardown without available inventory.",
                                "No Stock Available",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        if (!stock.HasSufficientStock)
                        {
                            dgvPreview.DataSource = null;
                            _recipes.Clear();
                            MessageBox.Show(
                                $"Insufficient stock: Only {stock.AvailableStock} {stock.CategoryName} unit(s) available in your branch inventory, but {qty} requested.\n\nPlease adjust the quantity or wait for incoming inventory before proceeding.",
                                "Insufficient Stock",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                else
                {
                    var errorBody = await stockRes.Content.ReadAsStringAsync();
                    string msg = "Unable to check inventory stock.";
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(errorBody);
                        if (doc.RootElement.TryGetProperty("message", out var m))
                            msg = m.GetString() ?? msg;
                    }
                    catch { }
                    MessageBox.Show(msg, "Stock Check Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = await ApiConfig.Http.GetAsync($"api/DeviceCategories/{categoryId}/yields");
                if (res.IsSuccessStatusCode)
                {
                    _recipes = await res.Content.ReadFromJsonAsync<List<ArchetypeRecipe>>() ?? new();
                    if (_recipes.Count == 0)
                    {
                        dgvPreview.DataSource = null;
                        MessageBox.Show("No archetype recipe defined for this device category.", "No Recipe Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var preview = _recipes.ConvertAll(r => new
                    {
                        Material = r.MaterialName,
                        ExpectedWeight = $"{r.WeightKgPerUnit * qty:F2} kg"
                    });
                    dgvPreview.DataSource = preview;
                }
                else
                {
                    MessageBox.Show("Failed to load recipes for category.", "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error previewing yield: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ConfirmTeardown()
        {
            if (cmbCategory.SelectedValue is not int categoryId)
            {
                MessageBox.Show("Please select a device category first.", "Select Category",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int qty = (int)numQuantity.Value;

            // If recipes not yet loaded, load and check stock now
            if (_recipes.Count == 0)
            {
                await PreviewYield();
                if (_recipes.Count == 0) return;
            }

            // Pop-up dialog allowing the tech staff to see the expected yields and manipulate them or not
            using var dlg = new TeardownYieldAdjustmentDialog(cmbCategory.Text, qty, _recipes);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var overrides = dlg.GetYieldOverrides();

            var request = new
            {
                ProcessedByUserId = CurrentSession.UserId > 0 ? CurrentSession.UserId : 1,
                DeviceCategoryId = categoryId,
                Quantity = qty,
                Overrides = overrides
            };

            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync("api/Teardown", request);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        $"Teardown for {qty}x {cmbCategory.Text} recorded successfully!\n\nRaw material yields have been updated in inventory.",
                        "Teardown Recorded",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    dgvPreview.DataSource = null;
                    _recipes.Clear();
                    await LoadHistory();
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    string errorMsg = "Teardown operation failed.";
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("message", out var m) && !string.IsNullOrWhiteSpace(m.GetString()))
                            errorMsg = m.GetString()!;
                        else if (doc.RootElement.TryGetProperty("detail", out var d) && !string.IsNullOrWhiteSpace(d.GetString()))
                            errorMsg = d.GetString()!;
                        else if (doc.RootElement.TryGetProperty("title", out var t) && !string.IsNullOrWhiteSpace(t.GetString()))
                            errorMsg = t.GetString()!;
                    }
                    catch
                    {
                        if (!string.IsNullOrWhiteSpace(body))
                            errorMsg = body;
                    }

                    MessageBox.Show(errorMsg, "Teardown Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error confirming teardown: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadHistory()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Teardown/history");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<TeardownBatch>>();
                    if (data != null)
                    {
                        var rows = data.ConvertAll(b => new
                        {
                            BatchCode = $"BAT-{b.Id:D4}",
                            Date = b.DateProcessed.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                            Category = b.DeviceCategory?.Name ?? $"Category #{b.DeviceCategoryId}",
                            Quantity = b.QuantityDismantled,
                            Yields = $"{b.Yields?.Count ?? 0} material(s)",
                            Branch = b.Branch?.Name ?? $"Branch #{b.BranchId}"
                        });
                        dgvHistory.DataSource = rows;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}