using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    [DesignerCategory("Code")]
    public class DeviceCategoriesForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnAddCategory = null!;
        private Button btnDeleteCategory = null!;
        private Button btnAddYield = null!;
        private Button btnDeleteYield = null!;

        private DataGridView dgvCategories = null!;
        private DataGridView dgvYields = null!;
        private Panel cardCategories = null!;
        private Panel cardYields = null!;
        private TextBox txtName = null!;
        private TextBox txtDescription = null!;
        private TextBox txtMaterialName = null!;
        private TextBox txtWeight = null!;
        private Label lblSelectedCategory = null!;
        private Label lblLeft = null!;
        private Label lblRight = null!;

        private int? _selectedCategoryId;

        public DeviceCategoriesForm()
        {
            Text = "Device Categories";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadCategories();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Device Categories",
                Left = 32,
                Top = 28,
                Width = 400,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Define archetypes and expected material yields per device type",
                Left = 32,
                Top = 64,
                Width = 560,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            lblLeft = new Label
            {
                Text = "Categories",
                Left = 32,
                Top = 100,
                Width = 200,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText,
                Height = 24
            };

            lblRight = new Label
            {
                Text = "Standard Yields",
                Left = 520,
                Top = 100,
                Width = 200,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText,
                Height = 24
            };

            lblSelectedCategory = new Label
            {
                Text = "Select a category to manage yields",
                Left = 520,
                Top = 124,
                Width = 400,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                Height = 18
            };

            cardCategories = MakeCard();
            dgvCategories = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvCategories);
            dgvCategories.SelectionChanged += async (s, e) => await OnCategorySelected();
            cardCategories.Controls.Add(dgvCategories);

            cardYields = MakeCard();
            dgvYields = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvYields);
            cardYields.Controls.Add(dgvYields);

            txtName = new TextBox { Width = 180, PlaceholderText = "Category name", Height = 32 };
            txtDescription = new TextBox { Width = 200, PlaceholderText = "Description", Height = 32 };
            txtMaterialName = new TextBox { Width = 180, PlaceholderText = "Material name", Height = 32 };
            txtWeight = new TextBox { Width = 100, PlaceholderText = "kg / unit", Height = 32 };
            Theme.StyleTextBox(txtName);
            Theme.StyleTextBox(txtDescription);
            Theme.StyleTextBox(txtMaterialName);
            Theme.StyleTextBox(txtWeight);

            btnAddCategory = new Button { Text = "Add Category", Width = 130, Height = 36 };
            btnDeleteCategory = new Button { Text = "Delete", Width = 100, Height = 36 };
            btnAddYield = new Button { Text = "Add Yield", Width = 120, Height = 36 };
            btnDeleteYield = new Button { Text = "Delete", Width = 100, Height = 36 };
            Theme.StyleSecondaryButton(btnAddCategory);
            Theme.StyleDangerButton(btnDeleteCategory);
            Theme.StyleSecondaryButton(btnAddYield);
            Theme.StyleDangerButton(btnDeleteYield);

            btnAddCategory.Click += async (s, e) => await AddCategory();
            btnDeleteCategory.Click += async (s, e) => await DeleteCategory();
            btnAddYield.Click += async (s, e) => await AddYield();
            btnDeleteYield.Click += async (s, e) => await DeleteYield();

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(lblLeft);
            Controls.Add(lblRight);
            Controls.Add(lblSelectedCategory);
            Controls.Add(cardCategories);
            Controls.Add(cardYields);
            Controls.Add(txtName);
            Controls.Add(txtDescription);
            Controls.Add(txtMaterialName);
            Controls.Add(txtWeight);
            Controls.Add(btnAddCategory);
            Controls.Add(btnDeleteCategory);
            Controls.Add(btnAddYield);
            Controls.Add(btnDeleteYield);

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
            int gap = 24;
            int mid = ClientSize.Width / 2;
            int leftW = mid - 32 - gap / 2;
            int rightW = ClientSize.Width - mid - 32 - gap / 2;
            int gridTop = 150;
            // Leave room for inputs + buttons at bottom (~100px)
            int gridH = Math.Max(220, ClientSize.Height - gridTop - 110);

            lblRight.Left = mid + gap / 2;
            lblSelectedCategory.Left = mid + gap / 2;

            cardCategories.Left = 32;
            cardCategories.Top = gridTop;
            cardCategories.Width = Math.Max(280, leftW);
            cardCategories.Height = gridH;

            cardYields.Left = mid + gap / 2;
            cardYields.Top = gridTop;
            cardYields.Width = Math.Max(280, rightW);
            cardYields.Height = gridH;

            int by = gridTop + gridH + 12;
            txtName.Left = 32; txtName.Top = by;
            txtDescription.Left = 32 + txtName.Width + 10; txtDescription.Top = by;
            btnAddCategory.Left = 32; btnAddCategory.Top = by + 40;
            btnDeleteCategory.Left = 32 + btnAddCategory.Width + 10; btnDeleteCategory.Top = by + 40;

            txtMaterialName.Left = mid + gap / 2; txtMaterialName.Top = by;
            txtWeight.Left = mid + gap / 2 + txtMaterialName.Width + 10; txtWeight.Top = by;
            btnAddYield.Left = mid + gap / 2; btnAddYield.Top = by + 40;
            btnDeleteYield.Left = mid + gap / 2 + btnAddYield.Width + 10; btnDeleteYield.Top = by + 40;

            // Re-stretch grid columns after card resize
            if (dgvCategories.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvCategories);
            if (dgvYields.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvYields);
        }

        private void ConfigureCategoryColumns()
        {
            Theme.ConfigureColumns(
                dgvCategories,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Name"] = ("Name", 220),
                    ["Description"] = ("Description", 280)
                },
                "Id", "ArchetypeRecipes", "Inventories", "TeardownBatches");
        }

        private void ConfigureYieldColumns()
        {
            Theme.ConfigureColumns(
                dgvYields,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["MaterialName"] = ("Material", 240),
                    ["WeightKgPerUnit"] = ("kg / unit", 140)
                },
                "Id", "DeviceCategoryId", "DeviceCategory");
        }

        private async Task LoadCategories()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/DeviceCategories");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<DeviceCategory>>();
                    dgvCategories.DataSource = data;
                    ConfigureCategoryColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading categories: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnCategorySelected()
        {
            if (dgvCategories.CurrentRow?.DataBoundItem is DeviceCategory category)
            {
                _selectedCategoryId = category.Id;
                lblSelectedCategory.Text = $"Yields for: {category.Name}";
                lblSelectedCategory.ForeColor = Theme.DarkText;
                await LoadYields(category.Id);
            }
        }

        private async Task LoadYields(int categoryId)
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync($"api/DeviceCategories/{categoryId}/yields");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<ArchetypeRecipe>>();
                    dgvYields.DataSource = data;
                    ConfigureYieldColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading yields: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AddCategory()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Category name is required.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var category = new DeviceCategory { Name = txtName.Text, Description = txtDescription.Text };
            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync("api/DeviceCategories", category);
                if (res.IsSuccessStatusCode)
                {
                    txtName.Clear();
                    txtDescription.Clear();
                    await LoadCategories();
                }
                else
                    MessageBox.Show("Add failed: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding category: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteCategory()
        {
            if (_selectedCategoryId is null)
            {
                MessageBox.Show("Select a category first.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Delete this category and its yields?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                var res = await ApiConfig.Http.DeleteAsync($"api/DeviceCategories/{_selectedCategoryId}");
                if (res.IsSuccessStatusCode)
                {
                    _selectedCategoryId = null;
                    dgvYields.DataSource = null;
                    lblSelectedCategory.Text = "Select a category to manage yields";
                    lblSelectedCategory.ForeColor = Theme.MutedText;
                    await LoadCategories();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting category: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AddYield()
        {
            if (_selectedCategoryId is null)
            {
                MessageBox.Show("Select a category first.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtMaterialName.Text) || !decimal.TryParse(txtWeight.Text, out var weight))
            {
                MessageBox.Show("Enter a valid material name and weight (kg).", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var recipe = new ArchetypeRecipe { MaterialName = txtMaterialName.Text, WeightKgPerUnit = weight };
            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync($"api/DeviceCategories/{_selectedCategoryId}/yields", recipe);
                if (res.IsSuccessStatusCode)
                {
                    txtMaterialName.Clear();
                    txtWeight.Clear();
                    await LoadYields(_selectedCategoryId.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding yield: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteYield()
        {
            if (dgvYields.CurrentRow?.DataBoundItem is ArchetypeRecipe recipe)
            {
                if (MessageBox.Show("Delete this yield entry?", "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                try
                {
                    var res = await ApiConfig.Http.DeleteAsync($"api/DeviceCategories/yields/{recipe.Id}");
                    if (res.IsSuccessStatusCode && _selectedCategoryId != null)
                        await LoadYields(_selectedCategoryId.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting yield: " + ex.Message, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}