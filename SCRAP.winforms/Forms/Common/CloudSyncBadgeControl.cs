using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    public class CloudSyncBadgeControl : Panel
    {
        private readonly System.Windows.Forms.Timer _timer;
        private SyncState _currentState = SyncState.PendingSync;
        private string _statusText = "Checking Sync...";
        private string _subText = "Hybrid Database • Click to view";
        private bool _isHovered = false;
        private bool _isUpdating = false;
        private bool _isSyncing = false;

        /// <summary>
        /// True when the current session is a tenant (non-MASTER) user.
        /// Tenants click the badge to trigger sync directly instead of opening the dialog.
        /// </summary>
        private static bool IsTenantUser()
        {
            return !string.IsNullOrWhiteSpace(ApiConfig.CompanyCode)
                && !ApiConfig.CompanyCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase);
        }

        public CloudSyncBadgeControl()
        {
            Height = 54;
            Dock = DockStyle.Bottom;
            BackColor = ColorTranslator.FromHtml("#111C30");
            Cursor = Cursors.Hand;
            DoubleBuffered = true;

            _timer = new System.Windows.Forms.Timer
            {
                Interval = 20000 // 20 seconds
            };
            _timer.Tick += async (_, __) => await RefreshStatusAsync();

            MouseEnter += (_, __) => { _isHovered = true; Invalidate(); };
            MouseLeave += (_, __) => { _isHovered = false; Invalidate(); };
            Click += async (_, __) => await OnBadgeClicked();

            // Initial refresh after handle creation
            HandleCreated += (_, __) =>
            {
                _timer.Start();
                _ = RefreshStatusAsync();
            };
        }

        /// <summary>
        /// Tenant: triggers sync directly on click. Admin: opens the full sync dialog.
        /// </summary>
        private async Task OnBadgeClicked()
        {
            if (IsTenantUser())
            {
                await TriggerTenantSyncAsync();
            }
            else
            {
                OpenSyncDialog();
            }
        }

        public void OpenSyncDialog()
        {
            using var dlg = new CloudSyncDialog();
            dlg.ShowDialog(FindForm());
            _ = RefreshStatusAsync();
        }

        /// <summary>
        /// Triggers a sync directly from the badge for tenant users.
        /// The badge shows syncing state, waits for completion, then refreshes.
        /// </summary>
        private async Task TriggerTenantSyncAsync()
        {
            if (_isSyncing) return;
            _isSyncing = true;

            // Show syncing state immediately on the badge
            _currentState = SyncState.Syncing;
            _statusText = "Syncing...";
            _subText = "Replicating your data to cloud...";
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke(new Action(Invalidate));

            try
            {
                var response = await ApiConfig.Http.PostAsync("api/sync/trigger", null);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CloudSyncResult>(ApiConfig.JsonOptions);
                    if (result != null && result.Success)
                    {
                        _currentState = SyncState.Synced;
                        _statusText = "Cloud Synced";
                        _subText = "Synced just now";
                    }
                    else
                    {
                        _currentState = SyncState.Error;
                        _statusText = "Sync Issue";
                        _subText = "Sync completed with issues";
                    }
                }
                else
                {
                    _currentState = SyncState.Error;
                    _statusText = "Sync Failed";
                    _subText = "Could not sync — try again later";
                }
            }
            catch
            {
                _currentState = SyncState.Offline;
                _statusText = "Offline Mode";
                _subText = "No connection — will retry";
            }
            finally
            {
                _isSyncing = false;
                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke(new Action(Invalidate));

                // Refresh from server to get accurate last sync time
                _ = RefreshStatusAsync();
            }
        }

        public async Task RefreshStatusAsync()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                var status = await ApiConfig.Http.GetFromJsonAsync<CloudSyncStatus>("api/sync/status", ApiConfig.JsonOptions);
                if (status != null)
                {
                    _currentState = status.State;
                    _statusText = status.StatusText;
                    if (status.State == SyncState.Synced && status.LastSyncUtc.HasValue)
                    {
                        var diff = DateTime.UtcNow - status.LastSyncUtc.Value;
                        if (diff.TotalMinutes < 1)
                            _subText = "Synced just now";
                        else if (diff.TotalMinutes < 60)
                            _subText = $"Synced {(int)diff.TotalMinutes}m ago";
                        else
                            _subText = $"Synced at {status.LastSyncUtc.Value.ToLocalTime():h:mm tt}";
                    }
                    else if (status.State == SyncState.Offline)
                    {
                        _subText = "Operating locally";
                    }
                    else if (status.State == SyncState.Syncing)
                    {
                        _subText = "Replicating changes...";
                    }
                    else
                    {
                        _subText = IsTenantUser()
                            ? "Click to sync"
                            : "Click to inspect / sync";
                    }
                }
            }
            catch
            {
                _currentState = SyncState.Offline;
                _statusText = "Offline Mode";
                _subText = "Local server active";
            }
            finally
            {
                _isUpdating = false;
                if (!IsDisposed && IsHandleCreated)
                {
                    BeginInvoke(new Action(Invalidate));
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background hover color
            var bgColor = _isHovered
                ? ColorTranslator.FromHtml("#1E293B")
                : ColorTranslator.FromHtml("#111C30");

            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Top subtle separator
            using (var borderPen = new Pen(ColorTranslator.FromHtml("#1E293B"), 1))
            {
                g.DrawLine(borderPen, 0, 0, Width, 0);
            }

            // Status color dot
            Color dotColor = _currentState switch
            {
                SyncState.Synced => ColorTranslator.FromHtml("#10B981"),     // Green
                SyncState.Syncing => ColorTranslator.FromHtml("#3B82F6"),    // Blue
                SyncState.PendingSync => ColorTranslator.FromHtml("#F59E0B"),// Amber
                SyncState.Offline => ColorTranslator.FromHtml("#64748B"),    // Slate/Gray
                SyncState.Error => ColorTranslator.FromHtml("#EF4444"),      // Red
                _ => ColorTranslator.FromHtml("#94A3B8")
            };

            int dotSize = 10;
            int dotX = 16;
            int dotY = 14;

            // Soft glow around the dot
            using (var glowBrush = new SolidBrush(Color.FromArgb(50, dotColor)))
            {
                g.FillEllipse(glowBrush, dotX - 2, dotY - 2, dotSize + 4, dotSize + 4);
            }

            // Solid dot
            using (var dotBrush = new SolidBrush(dotColor))
            {
                g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
            }

            // Primary text: Status
            using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(_statusText, font, textBrush, dotX + dotSize + 8, dotY - 2);
            }

            // Secondary text: Subtext
            using (var subFont = new Font("Segoe UI", 7.5f))
            using (var subBrush = new SolidBrush(ColorTranslator.FromHtml("#94A3B8")))
            {
                g.DrawString(_subText, subFont, subBrush, dotX + dotSize + 8, dotY + 16);
            }

            // Right-side indicator: arrow for admin (opens dialog), sync icon for tenant
            using (var arrowBrush = new SolidBrush(ColorTranslator.FromHtml("#64748B")))
            using (var arrowFont = new Font("Segoe UI", 8.5f))
            {
                var indicator = IsTenantUser() ? "⟳" : "›";
                g.DrawString(indicator, arrowFont, arrowBrush, Width - 20, 16);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
