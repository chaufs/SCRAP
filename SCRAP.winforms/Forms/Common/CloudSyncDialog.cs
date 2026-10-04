using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    public class CloudSyncDialog : Form
    {
        private Panel cardStatus = null!;
        private Panel pillStatus = null!;
        private Label lblPillText = null!;
        private Label lblStatusHeadline = null!;
        private Label lblStatusMessage = null!;
        private Label lblLastSync = null!;
        private Label lblRecordCount = null!;

        private Panel cardTopology = null!;
        private Label lblLocalHost = null!;
        private Label lblCloudMasterHost = null!;
        private Label lblActiveTenantHost = null!;

        private Button btnSyncNow = null!;
        private Button btnRefresh = null!;
        private Button btnClose = null!;
        private ProgressBar prgSync = null!;
        private Label lblSyncProgress = null!;

        private RichTextBox rtbLogs = null!;
        private CloudSyncStatus? _currentStatus;

        public CloudSyncDialog()
        {
            Text = "Database Synchronization — Local & Cloud";
            Width = 840;
            Height = 680;
            MinimumSize = new Size(760, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildUi();
            _ = RefreshStatusAsync();
        }

        private void BuildUi()
        {
            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Theme.White,
                Padding = new Padding(24, 16, 24, 12)
            };

            var headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Theme.CardBorder
            };
            header.Controls.Add(headerBorder);

            var lblTitle = new Label
            {
                Text = "Hybrid Cloud Database Sync Center",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                AutoSize = true,
                Location = new Point(24, 14)
            };

            var lblSubtitle = new Label
            {
                Text = "Offline-first local SQL Server with automatic replication to masterasp.net cloud hosting",
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                AutoSize = true,
                Location = new Point(24, 44)
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);
            Controls.Add(header);

            // Footer
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Theme.White,
                Padding = new Padding(24, 14, 24, 14)
            };

            var footerBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.CardBorder
            };
            footer.Controls.Add(footerBorder);

            btnClose = new Button
            {
                Text = "Close",
                Width = 100,
                Height = 36,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(footer.Width - 124, 14)
            };
            Theme.StyleOutlineButton(btnClose);
            btnClose.Click += (_, __) => Close();
            footer.Controls.Add(btnClose);

            btnRefresh = new Button
            {
                Text = "Refresh Status",
                Width = 130,
                Height = 36,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(footer.Width - 264, 14)
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (_, __) => await RefreshStatusAsync();
            footer.Controls.Add(btnRefresh);

            btnSyncNow = new Button
            {
                Text = "⚡ Sync to Cloud Now",
                Width = 180,
                Height = 36,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(footer.Width - 454, 14)
            };
            Theme.StyleSecondaryButton(btnSyncNow);
            btnSyncNow.Click += async (_, __) => await TriggerSyncAsync();
            footer.Controls.Add(btnSyncNow);

            Controls.Add(footer);

            // Body
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };
            Controls.Add(body);

            int currentTop = 16;

            // 1. Status Card
            cardStatus = CreateCard(body, ref currentTop, 140);
            cardStatus.Padding = new Padding(20, 16, 20, 16);

            pillStatus = new Panel
            {
                Location = new Point(20, 16),
                Size = new Size(160, 30),
                BackColor = ColorTranslator.FromHtml("#E2E8F0")
            };
            pillStatus.Paint += (s, e) =>
            {
                using var path = GetRoundedPath(pillStatus.ClientRectangle, 12);
                pillStatus.Region = new Region(path);
            };

            lblPillText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                Text = "Checking..."
            };
            pillStatus.Controls.Add(lblPillText);
            cardStatus.Controls.Add(pillStatus);

            lblStatusHeadline = new Label
            {
                Text = "Evaluating database connection...",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                Location = new Point(190, 20),
                AutoSize = true
            };
            cardStatus.Controls.Add(lblStatusHeadline);

            lblStatusMessage = new Label
            {
                Text = "Local SQL Server stores all operational data. Cloud sync runs when an internet connection is available.",
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                Location = new Point(20, 56),
                Size = new Size(740, 36)
            };
            cardStatus.Controls.Add(lblStatusMessage);

            lblLastSync = new Label
            {
                Text = "Last Sync: Pending verification",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.DarkText,
                Location = new Point(20, 100),
                AutoSize = true
            };
            cardStatus.Controls.Add(lblLastSync);

            lblRecordCount = new Label
            {
                Text = "Records in Cloud: --",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.MutedText,
                Location = new Point(320, 100),
                AutoSize = true
            };
            cardStatus.Controls.Add(lblRecordCount);

            // Progress bar (hidden by default)
            prgSync = new ProgressBar
            {
                Location = new Point(20, 122),
                Size = new Size(500, 10),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            cardStatus.Controls.Add(prgSync);

            lblSyncProgress = new Label
            {
                Location = new Point(530, 118),
                Size = new Size(200, 18),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.Blue,
                Text = "Replicating records...",
                Visible = false
            };
            cardStatus.Controls.Add(lblSyncProgress);

            currentTop += 16;

            // 2. Topology Card
            cardTopology = CreateCard(body, ref currentTop, 150);
            cardTopology.Padding = new Padding(20, 16, 20, 16);

            var lblTopTitle = new Label
            {
                Text = "DATABASE TOPOLOGY & CONNECTION MAPPING",
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection,
                Location = new Point(20, 14),
                AutoSize = true
            };
            cardTopology.Controls.Add(lblTopTitle);

            lblLocalHost = new Label
            {
                Text = "🖥 Local Database: DESKTOPPOL\\geofferpserver (Primary Active)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                Location = new Point(20, 42),
                AutoSize = true
            };
            cardTopology.Controls.Add(lblLocalHost);

            lblCloudMasterHost = new Label
            {
                Text = "☁ Cloud Master DB: db66898.public.databaseasp.net (db66898)",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.DarkText,
                Location = new Point(20, 72),
                AutoSize = true
            };
            cardTopology.Controls.Add(lblCloudMasterHost);

            lblActiveTenantHost = new Label
            {
                Text = $"🏢 Active Tenant: {ApiConfig.CompanyCode} ↔ Cloud Tenant DB",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.DarkText,
                Location = new Point(20, 102),
                AutoSize = true
            };
            cardTopology.Controls.Add(lblActiveTenantHost);

            currentTop += 16;

            // 3. Activity / Log Card
            var cardLogs = CreateCard(body, ref currentTop, 180);
            cardLogs.Padding = new Padding(20, 14, 20, 14);

            var lblLogTitle = new Label
            {
                Text = "SYNCHRONIZATION ACTIVITY & AUDIT LOG",
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection,
                Location = new Point(20, 14),
                AutoSize = true
            };
            cardLogs.Controls.Add(lblLogTitle);

            rtbLogs = new RichTextBox
            {
                Location = new Point(20, 38),
                Size = new Size(cardLogs.Width - 40, 126),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ReadOnly = true,
                BackColor = ColorTranslator.FromHtml("#F8FAFC"),
                ForeColor = Theme.DarkText,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9f)
            };
            cardLogs.Controls.Add(rtbLogs);
        }

        private Panel CreateCard(Panel parent, ref int top, int height)
        {
            var card = new Panel
            {
                Left = 24,
                Top = top,
                Width = parent.ClientSize.Width - 48,
                Height = height,
                BackColor = Theme.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            parent.Controls.Add(card);
            top += height;
            return card;
        }

        public async Task RefreshStatusAsync()
        {
            if (IsDisposed || Disposing) return;
            try
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "Checking...";

                var status = await ApiConfig.Http.GetFromJsonAsync<CloudSyncStatus>("api/sync/status", ApiConfig.JsonOptions);
                if (IsDisposed || Disposing) return;

                _currentStatus = status;
                UpdateUiWithStatus(status);
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                lblStatusHeadline.Text = "Could not reach local API service";
                lblStatusMessage.Text = ex.Message;
                pillStatus.BackColor = ColorTranslator.FromHtml("#FEE2E2");
                lblPillText.ForeColor = ColorTranslator.FromHtml("#991B1B");
                lblPillText.Text = "● Disconnected";
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    btnRefresh.Enabled = true;
                    btnRefresh.Text = "Refresh Status";
                }
            }
        }

        private void UpdateUiWithStatus(CloudSyncStatus? status)
        {
            if (status == null || IsDisposed || Disposing) return;

            if (status.State == SyncState.Synced)
            {
                pillStatus.BackColor = ColorTranslator.FromHtml("#DCFCE7");
                lblPillText.ForeColor = ColorTranslator.FromHtml("#166534");
                lblPillText.Text = "● Cloud Synced";
                lblStatusHeadline.Text = "Local and Cloud Databases are Synchronized";
            }
            else if (status.State == SyncState.Syncing)
            {
                pillStatus.BackColor = ColorTranslator.FromHtml("#DBEAFE");
                lblPillText.ForeColor = ColorTranslator.FromHtml("#1E40AF");
                lblPillText.Text = "● Syncing...";
                lblStatusHeadline.Text = "Replicating data to cloud database...";
            }
            else if (status.State == SyncState.PendingSync)
            {
                pillStatus.BackColor = ColorTranslator.FromHtml("#FEF3C7");
                lblPillText.ForeColor = ColorTranslator.FromHtml("#92400E");
                lblPillText.Text = "● Sync Pending";
                lblStatusHeadline.Text = "Cloud is connected; pending scheduled replication";
            }
            else if (status.State == SyncState.Offline)
            {
                pillStatus.BackColor = ColorTranslator.FromHtml("#F1F5F9");
                lblPillText.ForeColor = ColorTranslator.FromHtml("#475569");
                lblPillText.Text = "● Offline (Local)";
                lblStatusHeadline.Text = "Operating Offline in Local Mode";
            }
            else
            {
                pillStatus.BackColor = ColorTranslator.FromHtml("#FEE2E2");
                lblPillText.ForeColor = ColorTranslator.FromHtml("#991B1B");
                lblPillText.Text = "● Sync Error";
                lblStatusHeadline.Text = "Cloud Synchronization Issue Detected";
            }

            lblStatusMessage.Text = !string.IsNullOrWhiteSpace(status.Message)
                ? status.Message
                : "Local SQL Server stores all operational data. Online cloud database receives synced copies.";

            if (status.LastSyncUtc.HasValue)
            {
                lblLastSync.Text = $"Last Sync: {status.LastSyncUtc.Value.ToLocalTime():yyyy-MM-dd h:mm:ss tt}";
            }
            else
            {
                lblLastSync.Text = "Last Sync: Not yet synced in this session";
            }

            lblRecordCount.Text = $"Synced Entities: {status.SyncedEntitiesCount} records";

            if (!string.IsNullOrWhiteSpace(status.CloudMasterHost))
            {
                lblCloudMasterHost.Text = $"☁ Cloud Master DB: {status.CloudMasterHost} (db66898)";
            }

            if (string.IsNullOrWhiteSpace(ApiConfig.CompanyCode) || ApiConfig.CompanyCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase))
            {
                lblActiveTenantHost.Text = "🏢 Platform Admin (Replicating Master ERP & All Local Tenant Databases)";
            }
            else
            {
                lblActiveTenantHost.Text = $"🏢 Active Tenant: {ApiConfig.CompanyCode} ↔ Tenant Cloud Database";
            }

            // Update log box safely
            if (!rtbLogs.IsDisposed)
            {
                rtbLogs.Clear();
                rtbLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] Status: {status.StatusText}\n");
                rtbLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] Internet/Cloud Host Reachable: {(status.IsOnline ? "YES" : "NO")}\n");

                if (status.TenantStatuses != null && status.TenantStatuses.Count > 0)
                {
                    rtbLogs.AppendText("Tenant Cloud Mappings:\n");
                    foreach (var kvp in status.TenantStatuses)
                    {
                        rtbLogs.AppendText($"  • Tenant [{kvp.Key}]: {kvp.Value}\n");
                    }
                }
            }
        }

        private void SafeAppendLog(string text)
        {
            if (IsDisposed || Disposing) return;
            try
            {
                if (!rtbLogs.IsDisposed)
                {
                    rtbLogs.AppendText(text);
                    rtbLogs.ScrollToCaret();
                }
            }
            catch { }
        }

        private async Task TriggerSyncAsync()
        {
            if (IsDisposed || Disposing) return;
            try
            {
                btnSyncNow.Enabled = false;
                btnRefresh.Enabled = false;
                prgSync.Visible = true;
                lblSyncProgress.Visible = true;
                lblSyncProgress.Text = "Replicating data to cloud...";

                SafeAppendLog($"[{DateTime.Now:HH:mm:ss}] Starting manual cloud replication...\n");

                var response = await ApiConfig.Http.PostAsync("api/sync/trigger", null);
                if (IsDisposed || Disposing) return;

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CloudSyncResult>(ApiConfig.JsonOptions);
                    if (IsDisposed || Disposing) return;
                    if (result != null)
                    {
                        SafeAppendLog($"[{DateTime.Now:HH:mm:ss}] Replication completed. Success: {result.Success}. Records pushed: {result.RecordsPushed}\n");
                        foreach (var line in result.Details)
                        {
                            SafeAppendLog($"  {line}\n");
                        }
                    }
                }
                else
                {
                    SafeAppendLog($"[{DateTime.Now:HH:mm:ss}] Sync request returned HTTP {(int)response.StatusCode}: {response.ReasonPhrase}\n");
                }

                await RefreshStatusAsync();
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                SafeAppendLog($"[{DateTime.Now:HH:mm:ss}] ERROR during sync: {ex.Message}\n");
                try
                {
                    MessageBox.Show($"Failed to complete synchronization:\n\n{ex.Message}", "Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch { }
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    prgSync.Visible = false;
                    lblSyncProgress.Visible = false;
                    btnSyncNow.Enabled = true;
                    btnRefresh.Enabled = true;
                }
            }
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
