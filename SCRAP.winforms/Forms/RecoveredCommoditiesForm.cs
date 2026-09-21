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
    /// <summary>
    /// Recovered commodities / raw inventory stock.
    /// </summary>
    [DesignerCategory("Code")]
    public class RecoveredCommoditiesForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;
        private Panel cardGrid = null!;
        private DataGridView dgv = null!;

        public RecoveredCommoditiesForm()
        {
            Text = "Recovered Commodities";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadData();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Recovered Commodities",
                Left = 32,
                Top = 28,
                Width = 480,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Materials recovered from teardown batches",
                Left = 32,
                Top = 64,
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
            btnRefresh.Click += async (s, e) => await LoadData();

            cardGrid = new Panel { BackColor = Theme.White };
            cardGrid.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, cardGrid.Width - 1, cardGrid.Height - 1);
            };
            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            cardGrid.Controls.Add(dgv);

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(cardGrid);

            Resize += (s, e) => LayoutPage();
            LayoutPage();
        }

        private void LayoutPage()
        {
            btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
            btnRefresh.Top = 36;

            cardGrid.Left = 32;
            cardGrid.Top = 100;
            cardGrid.Width = Math.Max(400, ClientSize.Width - 64);
            cardGrid.Height = Math.Max(200, ClientSize.Height - 124);

            if (dgv.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgv);
        }

        private void ConfigureColumns()
        {
            Theme.ConfigureColumns(
                dgv,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 70),
                    ["MaterialName"] = ("Material", 220),
                    ["CurrentTotalWeightKg"] = ("Current Total Weight (kg)", 200),
                    ["WeightKg"] = ("Weight (kg)", 140),
                    ["Quantity"] = ("Quantity", 100),
                    ["SourceBatchId"] = ("Batch ID", 100),
                    ["DateRecovered"] = ("Date Recovered", 180),
                    ["Notes"] = ("Notes", 180)
                },
                "TeardownBatch", "TeardownBatchNavigation");
        }

        private async Task LoadData()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/RawInventory");
                if (res.IsSuccessStatusCode)
                {
                    dgv.DataSource = await res.Content.ReadFromJsonAsync<List<RawInventory>>()
                                     ?? new List<RawInventory>();
                    ConfigureColumns();
                }
                else
                {
                    MessageBox.Show("Error loading commodities: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading commodities: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}