using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms.Admin
{
    public sealed class CompanyFinanceMonitorForm : Form
    {
        private readonly Label _summary = new();
        private readonly DataGridView _grid = new();

        public CompanyFinanceMonitorForm()
        {
            Text = "Company Finance Monitor";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            Build();
            _ = LoadDashboard();
        }

        private void Build()
        {
            var title = new Label { Text = "Company Finance", Left = 32, Top = 28, Width = 500, Height = 36, Font = Theme.TitleFont, ForeColor = Theme.DarkText };
            var subtitle = new Label { Text = "Admin monitoring: sales income, deductions, taxes, and balance", Left = 32, Top = 64, Width = 700, Height = 24, Font = Theme.SubtitleFont, ForeColor = Theme.MutedText };
            _summary.SetBounds(32, 105, 900, 50);
            _summary.Font = Theme.LabelFont;
            _summary.ForeColor = Theme.DarkText;
            var refresh = new Button { Text = "Refresh", Left = 950, Top = 105, Width = 100, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Theme.StyleOutlineButton(refresh);
            refresh.Click += async (_, _) => await LoadDashboard();
            _grid.SetBounds(32, 175, 1020, 450);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _grid.ReadOnly = true;
            _grid.AutoGenerateColumns = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Theme.StyleGrid(_grid);
            Controls.AddRange(new Control[] { title, subtitle, _summary, refresh, _grid });
        }

        private async Task LoadDashboard()
        {
            var response = await ApiConfig.Http.GetAsync("api/finance/dashboard");
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(await response.Content.ReadAsStringAsync(), "Unable to load company finance", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var dashboard = await response.Content.ReadFromJsonAsync<FinanceDashboard>(ApiConfig.JsonOptions);
            if (dashboard is null) return;
            _summary.Text = $"Income: {dashboard.TotalIncome:N2}    Deductions: {dashboard.TotalDeductions:N2}    Payroll taxes: {dashboard.PayrollTaxes:N2}    Balance: {dashboard.CompanyBalance:N2}";
            _grid.DataSource = dashboard.Transactions;
        }

        private sealed class FinanceDashboard
        {
            public decimal TotalIncome { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal PayrollTaxes { get; set; }
            public decimal CompanyBalance { get; set; }
            public List<FinanceTransaction> Transactions { get; set; } = new();
        }

        private sealed class FinanceTransaction
        {
            public string Type { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime TransactionDate { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
}
