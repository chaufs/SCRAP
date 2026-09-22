using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;
using System.Drawing;
namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class ReportGenerationForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;
        private TabControl tabs = null!;
        private Button btnDownloadOverall = null!;
        private DataGridView dgvInventory = null!;
        private DataGridView dgvTeardowns = null!;
        private DataGridView dgvSales = null!;

        private Button btnExportInventory = null!;
        private Button btnExportTeardowns = null!;
        private Button btnExportSales = null!;

        public ReportGenerationForm()
        {
            Text = "Report Generation";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadAll();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Report Generation",
                Left = 32,
                Top = 28,
                Width = 420,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "View and export full history for inventory, teardowns, and sales",
                Left = 32,
                Top = 64,
                Width = 520,
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
            btnRefresh.Click += async (s, e) => await LoadAll();

            btnDownloadOverall = new Button
            {
                Text = "Download Overall Report",
                Width = 180,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StylePrimaryButton(btnDownloadOverall);
            btnDownloadOverall.Click += async (s, e) =>
                await DownloadPdf("api/Reports/overall/pdf", "OverallReport");

            Controls.Add(btnDownloadOverall);



            tabs = new TabControl
            {
                Left = 32,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // ---------------- Inventory tab ----------------
            var tabInventory = new TabPage("Inventory History") { BackColor = Theme.Background, Padding = new Padding(8) };

            var cardInventory = MakeCard();
            cardInventory.Dock = DockStyle.Fill;
            dgvInventory = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            Theme.StyleGrid(dgvInventory);
            cardInventory.Controls.Add(dgvInventory);

            var inventoryFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Background };
            btnExportInventory = new Button { Text = "Download PDF", Width = 140, Height = 32, Top = 8, Left = 0 };
            Theme.StylePrimaryButton(btnExportInventory);
            btnExportInventory.Click += async (s, e) =>
                await DownloadPdf("api/Reports/inventory-history/pdf", "InventoryHistory");
            inventoryFooter.Controls.Add(btnExportInventory);

            tabInventory.Controls.Add(cardInventory);
            tabInventory.Controls.Add(inventoryFooter);

            // ---------------- Teardown tab ----------------
            var tabTeardowns = new TabPage("Teardown History") { BackColor = Theme.Background, Padding = new Padding(8) };

            var cardTeardowns = MakeCard();
            cardTeardowns.Dock = DockStyle.Fill;
            dgvTeardowns = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            Theme.StyleGrid(dgvTeardowns);
            cardTeardowns.Controls.Add(dgvTeardowns);

            var teardownFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Background };
            btnExportTeardowns = new Button { Text = "Download PDF", Width = 140, Height = 32, Top = 8, Left = 0 };
            Theme.StylePrimaryButton(btnExportTeardowns);
            btnExportTeardowns.Click += async (s, e) =>
                await DownloadPdf("api/Reports/teardown-history/pdf", "TeardownHistory");
            teardownFooter.Controls.Add(btnExportTeardowns);

            tabTeardowns.Controls.Add(cardTeardowns);
            tabTeardowns.Controls.Add(teardownFooter);

            // ---------------- Sales tab ----------------
            var tabSales = new TabPage("Sales History") { BackColor = Theme.Background, Padding = new Padding(8) };

            var cardSales = MakeCard();
            cardSales.Dock = DockStyle.Fill;
            dgvSales = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
            Theme.StyleGrid(dgvSales);
            cardSales.Controls.Add(dgvSales);

            var salesFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Background };
            btnExportSales = new Button { Text = "Download PDF", Width = 140, Height = 32, Top = 8, Left = 0 };
            Theme.StylePrimaryButton(btnExportSales);
            btnExportSales.Click += async (s, e) =>
                await DownloadPdf("api/Reports/commodity-sales-history/pdf", "SalesHistory");
            salesFooter.Controls.Add(btnExportSales);

            tabSales.Controls.Add(cardSales);
            tabSales.Controls.Add(salesFooter);

            tabs.TabPages.Add(tabInventory);
            tabs.TabPages.Add(tabTeardowns);
            tabs.TabPages.Add(tabSales);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(tabs);

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
            btnDownloadOverall.Left = ClientSize.Width - btnDownloadOverall.Width - 32;
            btnDownloadOverall.Top = 36;

            btnRefresh.Left = btnDownloadOverall.Left - btnRefresh.Width - 10;
            btnRefresh.Top = 36;

            tabs.Left = 32;
            tabs.Top = 100;
            tabs.Width = Math.Max(400, ClientSize.Width - 64);
            tabs.Height = Math.Max(300, ClientSize.Height - 120);
        }

        private async Task LoadAll()
        {
            await LoadInventory();
            await LoadTeardowns();
            await LoadSales();
        }

        private async Task LoadInventory()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Reports/inventory-history");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<Inventory>>(ApiConfig.JsonOptions) ?? new List<Inventory>();
                    dgvInventory.DataSource = null;
                    dgvInventory.DataSource = data;
                }
                else
                {
                    MessageBox.Show("Error loading inventory history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading inventory history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadTeardowns()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Reports/teardown-history");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<TeardownBatch>>() ?? new List<TeardownBatch>();
                    dgvTeardowns.DataSource = null;
                    dgvTeardowns.DataSource = data;
                }
                else
                {
                    MessageBox.Show("Error loading teardown history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading teardown history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadSales()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Reports/commodity-sales-history");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CommoditySale>>() ?? new List<CommoditySale>();
                    dgvSales.DataSource = null;
                    dgvSales.DataSource = data;
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

        private async Task DownloadPdf(string endpoint, string filePrefix)
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync(endpoint);
                if (res.IsSuccessStatusCode)
                {
                    var bytes = await res.Content.ReadAsByteArrayAsync();
                    var path = Path.Combine(Path.GetTempPath(), $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    await File.WriteAllBytesAsync(path, bytes);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Failed to generate PDF: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating PDF: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}