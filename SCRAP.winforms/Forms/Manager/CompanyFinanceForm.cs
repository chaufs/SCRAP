using System;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public sealed class CompanyFinanceForm : Form
    {
        private readonly TextBox _category = new();
        private readonly TextBox _amount = new();
        private readonly TextBox _description = new();
        private readonly ComboBox _entryType = new();
        private readonly Label _categoryLabel = new();
        private readonly Button _save = new();

        public CompanyFinanceForm()
        {
            Text = "Company Finance";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            Build();
        }

        private void Build()
        {
            var title = new Label { Text = "Company Finance", Left = 32, Top = 28, Width = 500, Height = 36, Font = Theme.TitleFont, ForeColor = Theme.DarkText };
            var subtitle = new Label { Text = "Manager controls for company income and deductions", Left = 32, Top = 64, Width = 650, Height = 24, Font = Theme.SubtitleFont, ForeColor = Theme.MutedText };
            var typeLabel = new Label { Text = "Entry type", Left = 32, Top = 105, Width = 180 };
            _entryType.SetBounds(32, 130, 180, 30);
            _entryType.DropDownStyle = ComboBoxStyle.DropDownList;
            _entryType.Items.AddRange(new object[] { "Add Income", "Record Deduction" });
            _entryType.SelectedIndex = 0;
            _entryType.SelectedIndexChanged += (_, _) => UpdateEntryLabels();
            Theme.StyleComboBox(_entryType);
            _categoryLabel.Text = "Income category";
            _categoryLabel.SetBounds(235, 105, 180, 20);
            _category.SetBounds(235, 130, 260, 30);
            _category.PlaceholderText = "Sales, investment, service...";
            Theme.StyleTextBox(_category);
            var amountLabel = new Label { Text = "Amount", Left = 520, Top = 105, Width = 120 };
            _amount.SetBounds(520, 130, 160, 30);
            _amount.PlaceholderText = "0.00";
            Theme.StyleTextBox(_amount);
            var descriptionLabel = new Label { Text = "Description", Left = 705, Top = 105, Width = 160 };
            _description.SetBounds(705, 130, 360, 30);
            Theme.StyleTextBox(_description);
            _save.Text = "Add Income";
            _save.SetBounds(32, 190, 160, 34);
            Theme.StylePrimaryButton(_save);
            _save.Click += async (_, _) => await RecordEntry();
            Controls.AddRange(new Control[] { title, subtitle, typeLabel, _entryType, _categoryLabel, _category, amountLabel, _amount, descriptionLabel, _description, _save });
        }

        private void UpdateEntryLabels()
        {
            var isIncome = _entryType.SelectedIndex == 0;
            _categoryLabel.Text = isIncome ? "Income category" : "Deduction category";
            _category.PlaceholderText = isIncome ? "Sales, investment, service..." : "Tax, utilities, supplies...";
            _save.Text = isIncome ? "Add Income" : "Record Deduction";
        }

        private async Task RecordEntry()
        {
            if (!decimal.TryParse(_amount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Enter a valid amount.", "Company Finance", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(_category.Text))
            {
                MessageBox.Show("Enter a category.", "Company Finance", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var isIncome = _entryType.SelectedIndex == 0;
            var response = await ApiConfig.Http.PostAsJsonAsync(isIncome ? "api/finance/income" : "api/finance/deductions", new
            {
                Category = _category.Text.Trim(),
                Amount = amount,
                Description = _description.Text.Trim()
            });
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(await response.Content.ReadAsStringAsync(), isIncome ? "Unable to add income" : "Unable to record deduction", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show(isIncome ? "Income added." : "Deduction recorded.", "Company Finance", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _amount.Clear();
            _description.Clear();
        }
    }
}
