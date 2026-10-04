using System;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms.Manager
{
    [DesignerCategory("Code")]
    public sealed class FinanceEntryDialog : Form
    {
        private ComboBox cmbType = null!;
        private ComboBox cmbCategory = null!;
        private NumericUpDown numAmount = null!;
        private DateTimePicker dtpDate = null!;
        private TextBox txtDescription = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public FinanceEntryDialog()
        {
            Text = "Add Finance Entry";
            Width = 520;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Theme.White
            };

            var lblTitle = new Label
            {
                Text = "Record Financial Transaction",
                Left = 24,
                Top = 20,
                Width = 450,
                Height = 30,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };

            string branchInfo = !string.IsNullOrWhiteSpace(CurrentSession.BranchName)
                ? $" for {CurrentSession.BranchName}"
                : "";

            var lblSub = new Label
            {
                Text = $"Record money spent, revenue, or investments{branchInfo}",
                Left = 24,
                Top = 52,
                Width = 450,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            var line = new Panel
            {
                Left = 24,
                Top = 82,
                Width = 450,
                Height = 1,
                BackColor = Theme.CardBorder
            };

            int y = 98;
            int fieldW = 450;

            // 1. Transaction Classification
            var lblType = MakeFieldLabel("Transaction Type", 24, y);
            cmbType = new ComboBox
            {
                Left = 24,
                Top = y + 22,
                Width = fieldW,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cmbType);
            cmbType.Items.AddRange(new object[]
            {
                "Expense / Deduction (Money Spent)",
                "Operational Revenue (Money Received)",
                "Investment / Donation (Capital & Grants Received)"
            });
            cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += (s, e) => UpdateCategoryOptions();

            y += 66;

            // 2. Category
            var lblCategory = MakeFieldLabel("Category", 24, y);
            cmbCategory = new ComboBox
            {
                Left = 24,
                Top = y + 22,
                Width = fieldW,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            Theme.StyleComboBox(cmbCategory);

            y += 66;

            // 3. Amount & Date in 2 columns
            int colW = 215;
            var lblAmount = MakeFieldLabel("Amount (₱)", 24, y);
            numAmount = new NumericUpDown
            {
                Left = 24,
                Top = y + 22,
                Width = colW,
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Minimum = 0.01m,
                Maximum = 1000000000m,
                Value = 1000m
            };
            Theme.StyleNumericUpDown(numAmount);

            var lblDate = MakeFieldLabel("Transaction Date", 259, y);
            dtpDate = new DateTimePicker
            {
                Left = 259,
                Top = y + 22,
                Width = colW,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            y += 66;

            // 4. Description / Note
            var lblDesc = MakeFieldLabel("Description / Source Note", 24, y);
            txtDescription = new TextBox
            {
                Left = 24,
                Top = y + 22,
                Width = fieldW,
                Height = 65,
                Multiline = true,
                PlaceholderText = "e.g., Donated by EcoFund Foundation for green recycling initiatives..."
            };
            Theme.StyleTextBox(txtDescription);

            y += 100;

            // Action Buttons
            btnSave = new Button
            {
                Text = "Record Entry",
                Left = 205,
                Top = y,
                Width = 145,
                Height = 36
            };
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += async (s, e) => await SaveEntry();

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 360,
                Top = y,
                Width = 114,
                Height = 36
            };
            Theme.StyleOutlineButton(btnCancel);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnl.Controls.AddRange(new Control[]
            {
                lblTitle, lblSub, line,
                lblType, cmbType,
                lblCategory, cmbCategory,
                lblAmount, numAmount,
                lblDate, dtpDate,
                lblDesc, txtDescription,
                btnSave, btnCancel
            });

            Controls.Add(pnl);
            UpdateCategoryOptions();
        }

        private void UpdateCategoryOptions()
        {
            cmbCategory.Items.Clear();
            if (cmbType.SelectedIndex == 0) // Expense / Deduction
            {
                cmbCategory.Items.AddRange(new object[]
                {
                    "Operations & Maintenance",
                    "Device Procurement",
                    "Logistics & Shipping",
                    "Facility Utilities & Rent",
                    "Office & Operating Supplies",
                    "Taxes & Government Licenses",
                    "Miscellaneous Expense"
                });
                btnSave.Text = "Record Expense";
            }
            else if (cmbType.SelectedIndex == 1) // Operational Revenue
            {
                cmbCategory.Items.AddRange(new object[]
                {
                    "Sales Revenue",
                    "Recycling / Dismantling Service Fee",
                    "Scrap Material Processing",
                    "Compliance Certification Fee",
                    "Other Operating Income"
                });
                btnSave.Text = "Record Revenue";
            }
            else // Investment / Donation
            {
                cmbCategory.Items.AddRange(new object[]
                {
                    "Capital Investment",
                    "Corporate Donation",
                    "Philanthropic Grant",
                    "Partner Sponsorship",
                    "Owner Capital Injection"
                });
                btnSave.Text = "Record Investment";
            }

            if (cmbCategory.Items.Count > 0)
                cmbCategory.SelectedIndex = 0;
        }

        private async Task SaveEntry()
        {
            string category = cmbCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Please enter or select a category.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return;
            }

            decimal amount = numAmount.Value;
            if (amount <= 0)
            {
                MessageBox.Show("Please enter an amount greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numAmount.Focus();
                return;
            }

            string desc = txtDescription.Text.Trim();
            if (string.IsNullOrWhiteSpace(desc))
                desc = category;

            bool isDeduction = cmbType.SelectedIndex == 0;
            string endpoint = isDeduction ? "api/finance/deductions" : "api/finance/income";

            var req = new
            {
                Category = category,
                Amount = amount,
                TransactionDate = dtpDate.Value.Date,
                Description = desc,
                BranchId = CurrentSession.BranchId
            };

            btnSave.Enabled = false;
            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync(endpoint, req);
                if (res.IsSuccessStatusCode)
                {
                    string actionType = isDeduction ? "Expense" : (cmbType.SelectedIndex == 2 ? "Investment/Donation" : "Income");
                    MessageBox.Show(
                        $"{actionType} of ₱{amount:N2} under category '{category}' recorded successfully!",
                        "Entry Recorded",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    var msg = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Failed to record entry: " + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving transaction: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        private static Label MakeFieldLabel(string text, int left, int top)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                AutoSize = true,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.DarkText
            };
        }
    }
}
