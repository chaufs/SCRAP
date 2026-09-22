using SCRAP.domain.entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms.Destruction
{
    /// <summary>
    /// Manager-facing screen: pick pending storage-destruction records for drives that
    /// have been wiped/shredded, fill in the destruction details, and generate a
    /// Certificate of Destruction (POST api/CertificateOfDestruction).
    /// </summary>
    [DesignerCategory("Code")]
    public class CertificateOfDestructionForm : Form
    {
        private const string OrgName = "EcoExtract CO.";
        private const string OrgAddress = "Bangkal";

        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;
        private TabControl tabs = null!;

        // --- Generate tab ---
        private Panel formPanel = null!;
        private TextBox txtProviderName = null!;
        private TextBox txtProviderAddress = null!;
        private ComboBox cmbMethod = null!;
        private TextBox txtSecurityStandard = null!;
        private TextBox txtVerifiedBy = null!;
        private TextBox txtProcuredFrom = null!;
        private Label lblSelectedCount = null!;
        private Button btnGenerate = null!;
        private Panel cardPending = null!;
        private DataGridView dgvPending = null!;
        private BindingList<PendingItemRow> pendingRows = new();

        // --- History tab ---
        private Panel historyToolbar = null!;
        private Button btnDownloadPdf = null!;
        private Panel cardHistory = null!;
        private DataGridView dgvHistory = null!;

        public CertificateOfDestructionForm()
        {
            Text = "Certificate of Destruction";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Certificate of Destruction",
                Left = 32,
                Top = 24,
                Width = 520,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                AutoEllipsis = false
            };

            lblSubtitle = new Label
            {
                Text = "Select destroyed storage drives and generate a signed certificate",
                Left = 32,
                Top = 60,
                Width = 560,
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
                await LoadPending();
                await LoadHistory();
            };

            tabs = new TabControl
            {
                Left = 32,
                Font = Theme.LabelFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var tabGenerate = new TabPage("Generate Certificate") { BackColor = Theme.Background, Padding = new Padding(12) };
            var tabHistory = new TabPage("Certificate History") { BackColor = Theme.Background, Padding = new Padding(12) };

            // ========== GENERATE TAB ==========
            // Form fields panel (top)
            formPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220,
                BackColor = Theme.Background
            };

            // Row 1
            var lblProviderName = MakeFieldLabel("Provider Name", 0, 4);
            txtProviderName = new TextBox
            {
                Left = 0,
                Top = 24,
                Width = 200,
                Height = 32,
                PlaceholderText = "",
                Text = ""
            };
            Theme.StyleTextBox(txtProviderName);

            var lblProviderAddr = MakeFieldLabel("Provider Address", 220, 4);
            txtProviderAddress = new TextBox
            {
                Left = 220,
                Top = 24,
                Width = 220,
                Height = 32,
                PlaceholderText = ""
            };
            Theme.StyleTextBox(txtProviderAddress);

            var lblMethod = MakeFieldLabel("Destruction Method", 460, 4);
            cmbMethod = new ComboBox
            {
                Left = 460,
                Top = 24,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Theme.StyleComboBox(cmbMethod);
            cmbMethod.DataSource = Enum.GetValues(typeof(DestructionMethod));

            // Row 2
            var lblSecStd = MakeFieldLabel("Security Standard", 0, 68);
            txtSecurityStandard = new TextBox
            {
                Left = 0,
                Top = 88,
                Width = 200,
                Height = 32,
                PlaceholderText = "e.g. NIST 800-88"
            };
            Theme.StyleTextBox(txtSecurityStandard);

            var lblVerifiedBy = MakeFieldLabel("Verified By", 220, 68);
            txtVerifiedBy = new TextBox
            {
                Left = 220,
                Top = 88,
                Width = 220,
                Height = 32,
                PlaceholderText = ""
            };
            Theme.StyleTextBox(txtVerifiedBy);

            btnGenerate = new Button
            {
                Text = "Generate Certificate",
                Left = 460,
                Top = 86,
                Width = 200,
                Height = 36
            };
            Theme.StylePrimaryButton(btnGenerate);
            btnGenerate.Click += async (s, e) => await GenerateCertificate();

            // Row 3 — inherited from procurement (read-only)
            var lblProcuredFrom = MakeFieldLabel("Procured / Sourced From (auto-filled)", 0, 132);
            txtProcuredFrom = new TextBox
            {
                Left = 0,
                Top = 152,
                Width = 440,
                Height = 32,
                PlaceholderText = "Select drive(s) above to auto-fill",
                ReadOnly = true,
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240)
            };
            Theme.StyleTextBox(txtProcuredFrom);
            txtProcuredFrom.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);

            lblSelectedCount = new Label
            {
                Text = "0 drives selected",
                Left = 460,
                Top = 158,
                Width = 280,
                Height = 22,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            formPanel.Controls.Add(lblProviderName);
            formPanel.Controls.Add(txtProviderName);
            formPanel.Controls.Add(lblProviderAddr);
            formPanel.Controls.Add(txtProviderAddress);
            formPanel.Controls.Add(lblMethod);
            formPanel.Controls.Add(cmbMethod);
            formPanel.Controls.Add(lblSecStd);
            formPanel.Controls.Add(txtSecurityStandard);
            formPanel.Controls.Add(lblVerifiedBy);
            formPanel.Controls.Add(txtVerifiedBy);
            formPanel.Controls.Add(btnGenerate);
            formPanel.Controls.Add(lblProcuredFrom);
            formPanel.Controls.Add(txtProcuredFrom);
            formPanel.Controls.Add(lblSelectedCount);

            // Pending drives section
            var pendingHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Theme.Background
            };
            var lblPending = new Label
            {
                Text = "Drives Pending Destruction",
                Left = 0,
                Top = 8,
                Width = 400,
                Height = 24,
                Font = Theme.SectionFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };
            pendingHeader.Controls.Add(lblPending);

            cardPending = MakeCard();
            cardPending.Dock = DockStyle.Fill;
            dgvPending = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true
            };
            Theme.StyleGrid(dgvPending);
            BuildPendingColumns();
            cardPending.Controls.Add(dgvPending);

            // Dock order: Fill first, then Top panels (last Top is nearest top edge)
            tabGenerate.Controls.Add(cardPending);
            tabGenerate.Controls.Add(pendingHeader);
            tabGenerate.Controls.Add(formPanel);

            // ========== HISTORY TAB ==========
            historyToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Background
            };

            btnDownloadPdf = new Button
            {
                Text = "⬇  Download PDF",
                Left = 0,
                Top = 6,
                Width = 160,
                Height = 36
            };
            Theme.StylePrimaryButton(btnDownloadPdf);
            btnDownloadPdf.Click += async (s, e) => await DownloadSelectedCertificatePdf();

            var lblHistoryHint = new Label
            {
                Text = "Select a certificate row, then download its PDF",
                Left = 176,
                Top = 14,
                Width = 360,
                Height = 22,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            historyToolbar.Controls.Add(btnDownloadPdf);
            historyToolbar.Controls.Add(lblHistoryHint);

            cardHistory = MakeCard();
            cardHistory.Dock = DockStyle.Fill;
            dgvHistory = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvHistory);
            cardHistory.Controls.Add(dgvHistory);

            tabHistory.Controls.Add(cardHistory);
            tabHistory.Controls.Add(historyToolbar);

            tabs.TabPages.Add(tabGenerate);
            tabs.TabPages.Add(tabHistory);
            tabs.SelectedIndexChanged += async (s, e) =>
            {
                if (tabs.SelectedTab == tabHistory)
                    await LoadHistory();
            };

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(tabs);

            Resize += (s, e) => LayoutPage();
            LayoutPage();

            HandleCreated += async (s, e) =>
            {
                await LoadPending();
                await LoadHistory();
            };
        }

        private Label MakeFieldLabel(string text, int left, int top)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 200,
                Height = 18,
                Font = Theme.StatLabelFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };
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
            if (btnRefresh != null)
            {
                btnRefresh.Left = Math.Max(0, ClientSize.Width - btnRefresh.Width - 32);
                btnRefresh.Top = 28;
            }

            if (tabs != null)
            {
                tabs.Left = 32;
                tabs.Top = 96;
                tabs.Width = Math.Max(400, ClientSize.Width - 64);
                tabs.Height = Math.Max(300, ClientSize.Height - 116);
            }

            if (dgvPending != null && !dgvPending.IsDisposed && dgvPending.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvPending);
            if (dgvHistory != null && !dgvHistory.IsDisposed && dgvHistory.Columns.Count > 0)
                Theme.FillColumnsToWidth(dgvHistory);
        }

        private void BuildPendingColumns()
        {
            dgvPending.Columns.Clear();
            dgvPending.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPending.MultiSelect = true;
            dgvPending.ReadOnly = true;

            dgvPending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PendingItemRow.Id),
                HeaderText = "ID",
                Width = 70,
                ReadOnly = true
            });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PendingItemRow.SerialNumber),
                HeaderText = "Serial Number",
                Width = 180,
                ReadOnly = true
            });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PendingItemRow.DeviceType),
                HeaderText = "Device Type",
                Width = 160,
                ReadOnly = true
            });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PendingItemRow.Model),
                HeaderText = "Model",
                Width = 180,
                ReadOnly = true
            });
            dgvPending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PendingItemRow.CreatedAt),
                HeaderText = "Flagged On",
                Width = 170,
                ReadOnly = true
            });

            dgvPending.SelectionChanged += (s, e) => UpdateSelectedCount();
        }

        private void ConfigureHistoryColumns()
        {
            Theme.ConfigureColumns(
                dgvHistory,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = ("ID", 60),
                    ["CertificateNumber"] = ("Certificate #", 200),
                    ["DestructionDateTime"] = ("Destroyed On", 170),
                    ["OrganizationName"] = ("Organization", 180),
                    ["ProviderName"] = ("Provider", 160),
                    ["Method"] = ("Method", 140),
                    ["SecurityStandard"] = ("Standard", 140),
                    ["VerifiedByName"] = ("Verified By", 150)
                },
                "Items", "OrganizationAddress", "ProviderAddress", "ManagerUserId", "VerifiedDate", "Notes");
        }

        private void UpdateSelectedCount()
        {
            var count = dgvPending.SelectedRows.Count;
            lblSelectedCount.Text = $"{count} drive{(count == 1 ? "" : "s")} selected";

            // Auto-inherit the procurement source from the selected devices
            var sources = dgvPending.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => (r.DataBoundItem as PendingItemRow)?.PurchasedFrom ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            txtProcuredFrom.Text = sources.Count > 0 ? string.Join(", ", sources) : "";
        }

        private async Task LoadPending()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/StorageDestruction/pending");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<StorageDestructionRecord>>(ApiConfig.JsonOptions)
                               ?? new List<StorageDestructionRecord>();

                    pendingRows = new BindingList<PendingItemRow>(data.Select(r => new PendingItemRow
                    {
                        Id = r.Id,
                        SerialNumber = r.Inventory?.SerialNumber ?? "",
                        DeviceType = r.Inventory?.DeviceCategory?.Name ?? "",
                        Model = r.Inventory?.DeviceName ?? "",
                        PurchasedFrom = r.Inventory?.PurchasedFrom ?? "",
                        CreatedAt = r.CreatedAt
                    }).ToList());

                    dgvPending.DataSource = pendingRows;
                    UpdateSelectedCount();
                    Theme.FillColumnsToWidth(dgvPending);
                }
                else
                {
                    MessageBox.Show("Error loading pending drives: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading pending drives: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadHistory()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/CertificateOfDestruction");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<CertificateOfDestruction>>(ApiConfig.JsonOptions)
                               ?? new List<CertificateOfDestruction>();
                    dgvHistory.DataSource = null;
                    dgvHistory.DataSource = data;
                    if (dgvHistory.Columns.Count > 0)
                        ConfigureHistoryColumns();
                }
                else
                {
                    MessageBox.Show("Error loading certificate history: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading certificate history: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DownloadSelectedCertificatePdf()
        {
            if (dgvHistory.CurrentRow?.DataBoundItem is not CertificateOfDestruction cert)
            {
                MessageBox.Show("Select a certificate first.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var res = await ApiConfig.Http.GetAsync($"api/CertificateOfDestruction/{cert.Id}/pdf");
                if (res.IsSuccessStatusCode)
                {
                    var bytes = await res.Content.ReadAsByteArrayAsync();
                    var safeName = string.IsNullOrWhiteSpace(cert.CertificateNumber)
                        ? $"certificate-{cert.Id}"
                        : cert.CertificateNumber;
                    foreach (var c in Path.GetInvalidFileNameChars())
                        safeName = safeName.Replace(c, '_');

                    var path = Path.Combine(Path.GetTempPath(), $"{safeName}.pdf");
                    await File.WriteAllBytesAsync(path, bytes);
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Failed to download PDF: " + res.StatusCode + "\n" + body, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error downloading PDF: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task GenerateCertificate()
        {
            var selectedIds = dgvPending.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => (r.DataBoundItem as PendingItemRow)?.Id ?? 0)
                .Where(id => id != 0)
                .ToList();

            if (selectedIds.Count == 0)
            {
                MessageBox.Show("Select at least one drive pending destruction.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtProviderName.Text))
            {
                MessageBox.Show("Provider name is required.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtVerifiedBy.Text))
            {
                MessageBox.Show("Enter the name of the person verifying destruction.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (cmbMethod.SelectedItem is not DestructionMethod method)
            {
                MessageBox.Show("Select a destruction method.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Generate a certificate covering {selectedIds.Count} drive(s)? This will mark them as destroyed.",
                "Confirm Certificate", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var request = new CreateCertificateRequestDto
            {
                StorageDestructionRecordIds = selectedIds,
                OrganizationName = OrgName,
                OrganizationAddress = OrgAddress,
                ProviderName = txtProviderName.Text.Trim(),
                ProviderAddress = txtProviderAddress.Text.Trim(),
                Method = method,
                SecurityStandard = txtSecurityStandard.Text.Trim(),
                ManagerUserId = CurrentSession.UserId,
                VerifiedByName = txtVerifiedBy.Text.Trim()
            };

            btnGenerate.Enabled = false;
            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync("api/CertificateOfDestruction", request);
                if (res.IsSuccessStatusCode)
                {
                    var created = await res.Content.ReadFromJsonAsync<CertificateOfDestruction>(ApiConfig.JsonOptions);
                    MessageBox.Show(
                        $"Certificate generated: {created?.CertificateNumber}",
                        "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearForm();
                    await LoadPending();
                    await LoadHistory();
                    tabs.SelectedIndex = 1;
                }
                else
                {
                    var detail = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Error generating certificate: " + detail, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating certificate: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
            }
        }

        private void ClearForm()
        {
            txtProviderAddress.Clear();
            txtSecurityStandard.Clear();
            txtVerifiedBy.Clear();
            txtProcuredFrom.Clear();
            // keep provider name default
            if (string.IsNullOrWhiteSpace(txtProviderName.Text))
                txtProviderName.Text = "EcoExtract Co.";
        }

        private class PendingItemRow
        {
            public int Id { get; set; }
            public string SerialNumber { get; set; } = "";
            public string DeviceType { get; set; } = "";
            public string Model { get; set; } = "";
            public string PurchasedFrom { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        private class CreateCertificateRequestDto
        {
            public List<int> StorageDestructionRecordIds { get; set; } = new();
            public string OrganizationName { get; set; } = "";
            public string OrganizationAddress { get; set; } = "";
            public string ProviderName { get; set; } = "";
            public string ProviderAddress { get; set; } = "";
            public DestructionMethod Method { get; set; }
            public string SecurityStandard { get; set; } = "";
            public int ManagerUserId { get; set; }
            public string VerifiedByName { get; set; } = "";
        }
    }


}