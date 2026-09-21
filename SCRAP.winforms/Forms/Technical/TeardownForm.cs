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
            cardPreview.Controls.Add(dgvPreview);

            lblHistory = Theme.CreateSectionTitle("Teardown History", 32, 0);

            cardHistory = MakeCard();
            dgvHistory = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvHistory);
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

        private void ConfigurePreviewColumns()
        {
            Theme.ConfigureSimpleColumns(dgvPreview,
                ("Material", "Material", 320),
                ("Weight", "Expected Weight (kg)", 200),
                ("Expected", "Expected Weight (kg)", 200));
        }

        private void ConfigureHistoryColumns()
        {
            Theme.ConfigureColumns(
                dgvHistory,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 70),
                    ["ProcessedByUserId"] = ("Processed By", 120),
                    ["ProcessedBy"] = ("Processed By", 120),
                    ["DeviceCategoryId"] = ("Category ID", 110),
                    ["CategoryId"] = ("Category ID", 110),
                    ["Quantity"] = ("Quantity", 100),
                    ["QuantityDismantled"] = ("Quantity", 100),
                    ["DateProcessed"] = ("Date Processed", 180),
                    ["ProcessedAt"] = ("Date Processed", 180),
                    ["TotalWeightKg"] = ("Total Weight (kg)", 140),
                    ["Notes"] = ("Notes", 180)
                },
                "DeviceCategory", "User", "RawInventories", "ProcessedByUser",
                "DeviceCategoryNavigation", "ProcessedByUserNavigation");
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
                MessageBox.Show("Select a category first.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var res = await ApiConfig.Http.GetAsync($"api/DeviceCategories/{categoryId}/yields");
                if (res.IsSuccessStatusCode)
                {
                    _recipes = await res.Content.ReadFromJsonAsync<List<ArchetypeRecipe>>() ?? new();
                    var qty = numQuantity.Value;
                    var preview = _recipes.ConvertAll(r => new
                    {
                        Material = r.MaterialName,
                        ExpectedWeightKg = r.WeightKgPerUnit * qty
                    });
                    dgvPreview.DataSource = preview;
                    ConfigurePreviewColumns();
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
                MessageBox.Show("Select a category first.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_recipes.Count == 0)
            {
                MessageBox.Show("Preview the yield first.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var request = new
            {
                ProcessedByUserId = 1,
                DeviceCategoryId = categoryId,
                Quantity = (int)numQuantity.Value,
                Overrides = (Dictionary<string, decimal>?)null
            };

            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync("api/Teardown", request);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Teardown recorded successfully.", "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dgvPreview.DataSource = null;
                    _recipes.Clear();
                    await LoadHistory();
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Teardown failed: " + res.StatusCode + "\n" + body, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    dgvHistory.DataSource = data;
                    ConfigureHistoryColumns();
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