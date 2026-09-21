using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Sales
{
    [DesignerCategory("Code")]
    public class SalesForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;

        private Panel formCard = null!;
        private ComboBox cmbMaterial = null!;
        private TextBox txtBuyer = null!;
        private TextBox txtQuantity = null!;
        private TextBox txtPricePerKg = null!;
        private TextBox txtInvoiceNumber = null!;
        private DateTimePicker dtpSaleDate = null!;
        private TextBox txtNotes = null!;
        private Label lblTotal = null!;
        private Button btnLogSale = null!;

        private Label lblHist = null!;
        private Panel cardHistory = null!;
        private DataGridView dgvSalesHistory = null!;

        public SalesForm()
        {
            Text = "Sales";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Sales",
                Left = 32,
                Top = 24,
                Width = 300,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Log commodity sales and view history",
                Left = 32,
                Top = 60,
                Width = 480,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 110,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) =>
            {
                await LoadMaterials();
                await LoadSalesHistory();
            };

            // ---- Form card ----
            formCard = new Panel
            {
                Left = 32,
                Top = 96,
                Height = 210,
                BackColor = Theme.White
            };
            formCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, formCard.Width - 1, formCard.Height - 1);
            };

            const int pad = 24;
            const int colGap = 20;
            const int colW = 200;
            const int row1Y = 20;
            const int row2Y = 100;

            // Row 1
            AddField(formCard, "Material", pad, row1Y, out var lbl1, out Control _);
            cmbMaterial = new ComboBox
            {
                Left = pad,
                Top = row1Y + 22,
                Width = colW,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.LabelFont,
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.White,
                ForeColor = Theme.DarkText
            };
            Theme.StyleComboBox(cmbMaterial);
            formCard.Controls.Add(cmbMaterial);

            AddField(formCard, "Buyer", pad + colW + colGap, row1Y, out _, out _);
            txtBuyer = new TextBox
            {
                Left = pad + colW + colGap,
                Top = row1Y + 22,
                Width = colW,
                Height = 30,
                PlaceholderText = "Buyer name"
            };
            Theme.StyleTextBox(txtBuyer);
            formCard.Controls.Add(txtBuyer);

            AddField(formCard, "Invoice #", pad + (colW + colGap) * 2, row1Y, out _, out _);
            txtInvoiceNumber = new TextBox
            {
                Left = pad + (colW + colGap) * 2,
                Top = row1Y + 22,
                Width = colW,
                Height = 30,
                PlaceholderText = "Invoice number"
            };
            Theme.StyleTextBox(txtInvoiceNumber);
            formCard.Controls.Add(txtInvoiceNumber);

            AddField(formCard, "Sale date", pad + (colW + colGap) * 3, row1Y, out _, out _);
            dtpSaleDate = new DateTimePicker
            {
                Left = pad + (colW + colGap) * 3,
                Top = row1Y + 22,
                Width = colW + 20,
                Font = Theme.LabelFont,
                Format = DateTimePickerFormat.Short
            };
            formCard.Controls.Add(dtpSaleDate);

            // Row 2 — evenly spaced columns so labels never overlap
            int qLeft = pad;
            int pLeft = pad + 180;
            int tLeft = pad + 360;
            int nLeft = pad + 540;
            int bLeft = pad + 760;

            AddField(formCard, "Quantity (kg)", qLeft, row2Y, out _, out _);
            txtQuantity = new TextBox
            {
                Left = qLeft,
                Top = row2Y + 22,
                Width = 150,
                Height = 30,
                PlaceholderText = "0.00"
            };
            Theme.StyleTextBox(txtQuantity);
            formCard.Controls.Add(txtQuantity);

            AddField(formCard, "Price per kg", pLeft, row2Y, out _, out _);
            txtPricePerKg = new TextBox
            {
                Left = pLeft,
                Top = row2Y + 22,
                Width = 150,
                Height = 30,
                PlaceholderText = "0.00"
            };
            Theme.StyleTextBox(txtPricePerKg);
            formCard.Controls.Add(txtPricePerKg);

            // Total display box
            var totalBox = new Panel
            {
                Left = tLeft,
                Top = row2Y + 6,
                Width = 150,
                Height = 50,
                BackColor = Theme.SoftBlue
            };
            lblTotal = new Label
            {
                Text = "Total: 0.00",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            totalBox.Controls.Add(lblTotal);
            formCard.Controls.Add(totalBox);

            AddField(formCard, "Notes", nLeft, row2Y, out _, out _);
            txtNotes = new TextBox
            {
                Left = nLeft,
                Top = row2Y + 22,
                Width = 190,
                Height = 30,
                PlaceholderText = "Optional notes"
            };
            Theme.StyleTextBox(txtNotes);
            formCard.Controls.Add(txtNotes);

            btnLogSale = new Button
            {
                Text = "Log Sale",
                Left = bLeft,
                Top = row2Y + 18,
                Width = 130,
                Height = 38
            };
            Theme.StylePrimaryButton(btnLogSale);
            btnLogSale.Click += async (s, e) => await LogSale();
            formCard.Controls.Add(btnLogSale);

            txtQuantity.TextChanged += (s, e) => UpdateTotalPreview();
            txtPricePerKg.TextChanged += (s, e) => UpdateTotalPreview();

            // ---- Sales history ----
            lblHist = Theme.CreateSectionTitle("Sales History", 32, 328);

            cardHistory = new Panel { BackColor = Theme.White };
            cardHistory.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, cardHistory.Width - 1, cardHistory.Height - 1);
            };
            dgvSalesHistory = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvSalesHistory);
            cardHistory.Controls.Add(dgvSalesHistory);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(formCard);
            Controls.Add(lblHist);
            Controls.Add(cardHistory);

            Resize += (s, e) => LayoutPage();
            LayoutPage();

            HandleCreated += async (s, e) =>
            {
                await LoadMaterials();
                await LoadSalesHistory();
            };
        }

        private void AddField(Panel parent, string text, int left, int top, out Label lbl, out Control _)
        {
            lbl = new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 180,
                Height = 18,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lbl);
            _ = lbl;
        }

        private void LayoutPage()
        {
            if (btnRefresh != null)
            {
                btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
                btnRefresh.Top = 28;
            }

            if (formCard != null)
            {
                formCard.Left = 32;
                formCard.Top = 96;
                formCard.Width = Math.Max(880, ClientSize.Width - 64);
            }

            if (lblHist != null)
            {
                lblHist.Left = 32;
                lblHist.Top = 328;
            }

            if (cardHistory != null)
            {
                cardHistory.Left = 32;
                cardHistory.Top = 360;
                cardHistory.Width = Math.Max(400, ClientSize.Width - 64);
                cardHistory.Height = Math.Max(160, ClientSize.Height - 388);
            }

            if (dgvSalesHistory != null && !dgvSalesHistory.IsDisposed && dgvSalesHistory.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvSalesHistory);
        }

        private void UpdateTotalPreview()
        {
            if (decimal.TryParse(txtQuantity.Text, out var qty) && decimal.TryParse(txtPricePerKg.Text, out var price))
                lblTotal.Text = $"Total: {qty * price:N2}";
            else
                lblTotal.Text = "Total: 0.00";
        }

        private void ConfigureSalesColumns()
        {
            Theme.ConfigureColumns(
                dgvSalesHistory,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 60),
                    ["MaterialName"] = ("Material", 150),
                    ["BuyerName"] = ("Buyer", 150),
                    ["QuantityKg"] = ("Qty (kg)", 100),
                    ["PricePerKg"] = ("Price / kg", 110),
                    ["TotalAmount"] = ("Total", 110),
                    ["InvoiceNumber"] = ("Invoice #", 130),
                    ["SaleDate"] = ("Sale Date", 160),
                    ["Notes"] = ("Notes", 140)
                });
        }

        private async Task LoadMaterials()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/RawInventory");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<RawInventory>>()
                               ?? new List<RawInventory>();
                    var materials = data
                        .Select(x => x.MaterialName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n)
                        .ToList();
                    cmbMaterial.DataSource = null;
                    cmbMaterial.DataSource = materials;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading materials: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LogSale()
        {
            if (cmbMaterial.SelectedItem is not string material)
            {
                MessageBox.Show("Select a material.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtBuyer.Text) || string.IsNullOrWhiteSpace(txtInvoiceNumber.Text))
            {
                MessageBox.Show("Buyer name and invoice number are required.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!decimal.TryParse(txtQuantity.Text, out var qty) || qty <= 0)
            {
                MessageBox.Show("Enter a valid quantity.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!decimal.TryParse(txtPricePerKg.Text, out var price) || price <= 0)
            {
                MessageBox.Show("Enter a valid price per kilo.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sale = new CommoditySale
            {
                MaterialName = material,
                BuyerName = txtBuyer.Text.Trim(),
                QuantityKg = qty,
                PricePerKg = price,
                InvoiceNumber = txtInvoiceNumber.Text.Trim(),
                SaleDate = dtpSaleDate.Value,
                Notes = txtNotes.Text
            };

            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync("api/CommoditySales", sale);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("Sale logged.", "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtBuyer.Clear();
                    txtQuantity.Clear();
                    txtPricePerKg.Clear();
                    txtInvoiceNumber.Clear();
                    txtNotes.Clear();
                    lblTotal.Text = "Total: 0.00";
                    await LoadMaterials();
                    await LoadSalesHistory();
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Failed to log sale: " + res.StatusCode + "\n" + body, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error logging sale: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadSalesHistory()
        {
            try
            {
                if (dgvSalesHistory == null || dgvSalesHistory.IsDisposed)
                    return;

                var res = await ApiConfig.Http.GetAsync("api/CommoditySales");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CommoditySale>>()
                               ?? new List<CommoditySale>();
                    dgvSalesHistory.DataSource = null;
                    dgvSalesHistory.DataSource = data;
                    if (dgvSalesHistory.Columns.Count > 0)
                        ConfigureSalesColumns();
                }
                else
                {
                    MessageBox.Show("Error loading sales history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sales history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}