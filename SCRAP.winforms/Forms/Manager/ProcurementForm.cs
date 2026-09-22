using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    public sealed class ProcurementForm : Form
    {
        private readonly TextBox _deviceName = new();
        private readonly ComboBox _category = new();
        private readonly DateTimePicker _purchaseDate = new();
        private readonly TextBox _purchasedFrom = new();
        private readonly TextBox _totalCost = new();
        private readonly NumericUpDown _quantity = new();
        private readonly CheckBox _hasStorage = new();
        private readonly TextBox _serial = new();
        private readonly TextBox _batch = new();
        private readonly TextBox _notes = new();

        public ProcurementForm()
        {
            Text = "Device Procurement";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            Build();
            _ = LoadCategories();
        }

        private void Build()
        {
            var title = new Label { Text = "Device Procurement", Left = 32, Top = 28, Width = 600, Height = 36, Font = Theme.TitleFont, ForeColor = Theme.DarkText };
            var subtitle = new Label { Text = "Manager-only purchasing and inventory intake", Left = 32, Top = 64, Width = 650, Height = 24, Font = Theme.SubtitleFont, ForeColor = Theme.MutedText };

            AddLabel("Device type", 32, 105);
            SetupText(_deviceName, 32, 130, 220, "Laptop, desktop, monitor...");

            AddLabel("Category", 272, 105);
            _category.SetBounds(272, 130, 220, 30);
            _category.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(_category);
            _category.DrawMode = DrawMode.Normal;
            _category.BackColor = Color.White;
            _category.ForeColor = Color.FromArgb(15, 23, 42);
            _category.DisplayMember = "Name";
            _category.ValueMember = "Id";

            AddLabel("Purchase date", 512, 105);
            _purchaseDate.SetBounds(512, 130, 150, 30);
            _purchaseDate.Format = DateTimePickerFormat.Short;
            _purchaseDate.Value = DateTime.Today;

            AddLabel("Purchased from", 682, 105);
            SetupText(_purchasedFrom, 682, 130, 230, "Supplier or store");

            AddLabel("Total procurement cost", 32, 180);
            SetupText(_totalCost, 32, 205, 160, "0.00");

            AddLabel("Number of devices", 212, 180);
            _quantity.SetBounds(212, 205, 130, 30);
            _quantity.Minimum = 1;
            _quantity.Maximum = 10000;
            _quantity.Value = 1;

            AddLabel("Serial code (one device)", 362, 180);
            SetupText(_serial, 362, 205, 220, "Serial code");

            AddLabel("Batch code (multiple devices)", 602, 180);
            SetupText(_batch, 602, 205, 250, "Shared batch code");

            _hasStorage.Text = "Has storage device";
            _hasStorage.SetBounds(32, 260, 180, 30);
            _hasStorage.AutoSize = true;
            _hasStorage.ForeColor = Theme.DarkText;

            AddLabel("Notes", 242, 255);
            SetupText(_notes, 242, 280, 610, "Optional notes");

            var save = new Button { Text = "Record Procurement", Left = 32, Top = 335, Width = 180, Height = 36 };
            Theme.StylePrimaryButton(save);
            save.Click += async (_, _) => await Submit();

            Controls.AddRange(new Control[] { title, subtitle, _category, _quantity, _purchaseDate, _hasStorage, save });
        }

        private void AddLabel(string text, int left, int top) =>
            Controls.Add(new Label { Text = text, Left = left, Top = top, Width = 220, Height = 22, ForeColor = Theme.DarkText });

        private void SetupText(TextBox box, int left, int top, int width, string placeholder)
        {
            box.SetBounds(left, top, width, 30);
            box.PlaceholderText = placeholder;
            Theme.StyleTextBox(box);
            Controls.Add(box);
        }

        private async Task LoadCategories()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/DeviceCategories");
                if (!res.IsSuccessStatusCode) return;

                // Same deserialize as DeviceCategoriesForm — do NOT pass JsonOptions here.
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
                    }
                    finally
                    {
                        _category.EndUpdate();
                    }
                }

                if (IsHandleCreated && InvokeRequired) Invoke(Apply);
                else Apply();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading categories: " + ex.Message, "Procurement",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Submit()
        {
            int categoryId = 0;
            if (_category.SelectedItem is DeviceCategory selected)
                categoryId = selected.Id;
            else if (_category.SelectedValue is int id)
                categoryId = id;

            if (string.IsNullOrWhiteSpace(_deviceName.Text) || categoryId <= 0 || string.IsNullOrWhiteSpace(_purchasedFrom.Text))
            {
                MessageBox.Show("Device type, category, and purchased-from are required.", "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!decimal.TryParse(_totalCost.Text, out var totalCost) || totalCost < 0)
            {
                MessageBox.Show("Enter a valid total procurement cost (0 or more).", "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var quantity = (int)_quantity.Value;
            if (quantity > 1 && string.IsNullOrWhiteSpace(_batch.Text))
            {
                MessageBox.Show("A batch code is required for multiple devices.", "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (quantity == 1 && string.IsNullOrWhiteSpace(_serial.Text) && string.IsNullOrWhiteSpace(_batch.Text))
            {
                MessageBox.Show("Enter a serial code or batch code.", "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var response = await ApiConfig.Http.PostAsJsonAsync("api/procurement", new
            {
                DeviceName = _deviceName.Text.Trim(),
                DeviceCategoryId = categoryId,
                PurchaseDate = _purchaseDate.Value.Date,
                PurchasedFrom = _purchasedFrom.Text.Trim(),
                TotalCost = totalCost,
                Quantity = quantity,
                HasStorageDevice = _hasStorage.Checked,
                SerialNumber = _serial.Text.Trim(),
                BatchCode = _batch.Text.Trim(),
                Notes = _notes.Text.Trim()
            });
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(await response.Content.ReadAsStringAsync(), "Unable to record procurement", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show("Procurement recorded and inventory updated.", "Procurement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _deviceName.Clear();
            _purchasedFrom.Clear();
            _totalCost.Clear();
            _serial.Clear();
            _batch.Clear();
            _notes.Clear();
            _quantity.Value = 1;
            _hasStorage.Checked = false;
        }
    }
}