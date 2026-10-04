using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public class PlatformTermsForm : Form
    {
        // ── State ───────────────────────────────────────────────────────────────────
        private bool _isSuperAdmin;
        private string _savedTermsText = string.Empty;   // persisted/remote content
        private DateTime? _lastUpdatedAt;
        private string? _lastUpdatedBy;

        // ── Controls we need to reference ──────────────────────────────────────────
        private Panel _headerPanel = null!;
        private Label _lblLastUpdated = null!;
        private Panel _pnlBadge = null!;

        // Read-only section cards container
        private Panel _scrollBody = null!;

        // Edit-mode panel (overlays the scroll body)
        private Panel _editPanel = null!;
        private RichTextBox _richEditor = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private Button? _btnEdit;

        // ── Default content (used when no DB record exists yet) ─────────────────────
        private static readonly string DefaultTermsText =
            "1. SOFTWARE LICENSE & PERMITTED USAGE\r\n" +
            "• License Grant: The Developer (Super Admin) grants each verified subscribing company (Tenant) a non-exclusive, non-transferable subscription right to utilize the S.C.R.A.P EcoExtract ERP system for internal recycling and processing operations.\r\n" +
            "• Modular Access: Access permissions to specific modules (Inventory, Teardown, HR, Branches, Finance) are strictly governed by the Tenant's selected Subscription Plan and can be customized in real-time by the Super Admin.\r\n" +
            "• Prohibitions: Reverse engineering, decompiling, sub-licensing, or attempting to breach tenant isolation barriers is strictly prohibited.\r\n\r\n" +
            "2. COMPLETE TENANCY ISOLATION & DATA OWNERSHIP\r\n" +
            "• Isolated Database Architecture: Every tenant operates on a dedicated SQL Server database (one-to-one tenancy). Operational records, transactions, and employee information are never co-mingled or shared across tenants.\r\n" +
            "• 100% Data Ownership: All company transactions, receipts, customer records, and scrap recovery data belong solely and exclusively to the Tenant.\r\n" +
            "• Privileged Access: The Super Admin may only inspect tenant operational databases upon explicit request from the subscriber for debugging, maintenance, or data migration.\r\n\r\n" +
            "3. SERVICE LEVEL AGREEMENT (SLA) & 99.9% UPTIME\r\n" +
            "• Uptime Commitment: The Developer guarantees 99.9% monthly platform uptime for API endpoints and tenant database connectivity.\r\n" +
            "• Maintenance Protocol: Core schema migrations and platform upgrades are scheduled outside standard operating hours (weekends/evenings). Tenants will receive 48-hour prior notification for any planned maintenance downtime.\r\n" +
            "• Emergency Hotfixes: Security patches and critical bug fixes will be deployed immediately with automatic rollback safeguards.\r\n\r\n" +
            "4. SUBSCRIPTION BILLING, RENEWAL & SUSPENSION POLICY\r\n" +
            "• Invoicing & Term: Subscription fees are billed on an ongoing monthly or annual cycle in accordance with the assigned subscription tier (Basic, Standard, Enterprise).\r\n" +
            "• Grace Period: A 15-day grace period is provided following subscription expiration before any access suspension is enforced.\r\n" +
            "• Suspension: Accounts in default may be temporarily set to 'Suspended' status via the Super Admin Console. Operational data remains intact during suspension and restores immediately upon renewal.\r\n\r\n" +
            "5. TERMINATION, DATA EXPORT & DECOMMISSIONING\r\n" +
            "• Cancellation: Tenants may terminate their subscription at any time by providing written notice prior to the next billing cycle.\r\n" +
            "• 30-Day Export Window: Upon termination, the Super Admin will deliver a complete, isolated SQL backup (.bak) or CSV data export within 30 calendar days.\r\n" +
            "• Secure Disposal: Following the 30-day export grace period, the dedicated database will be permanently and securely scrubbed.\r\n\r\n" +
            "6. GOVERNANCE & SUPPORT CONTACTS\r\n" +
            "• Inquiries & Amendments: For custom enterprise service level agreements, regulatory compliance certificates, or SLA inquiries, contact the Platform Administration Team.\r\n" +
            "• Official Contact: support@scraperp.local  •  Dedicated Tenant Support Desk  •  Escalation Response SLA: < 4 Hours.\r\n" +
            "• Governing Jurisdiction: Terms and operating agreements are executed in accordance with applicable national enterprise software licensing standards.";

        public PlatformTermsForm(bool? isSuperAdmin = null)
        {
            _isSuperAdmin = isSuperAdmin ?? (
                CurrentSession.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                CurrentSession.CompanyCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase) ||
                CurrentSession.Username.Equals("superadmin", StringComparison.OrdinalIgnoreCase)
            );

            Text = "Terms & Conditions";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;

            BuildUi();
            _ = LoadTermsAsync();
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  UI Build
        // ════════════════════════════════════════════════════════════════════════════

        private void BuildUi()
        {
            // ── Header ──────────────────────────────────────────────────────────────
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Theme.Background,
                Padding = new Padding(32, 20, 32, 0)
            };

            var lblTitle = new Label
            {
                Text = "Platform Terms & Service Agreements",
                UseMnemonic = false,
                Left = 32,
                Top = 18,
                Width = 580,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            _lblLastUpdated = new Label
            {
                Text = string.Empty,
                UseMnemonic = false,
                Left = 32,
                Top = 56,
                Width = 580,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            // Tenant badge (right side)
            _pnlBadge = new Panel
            {
                Height = 44,
                Width = 300,
                BackColor = Color.White
            };
            _pnlBadge.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, _pnlBadge.Width - 1, _pnlBadge.Height - 1);
                using var dotBrush = new SolidBrush(Theme.Green);
                e.Graphics.FillEllipse(dotBrush, 14, 17, 10, 10);
            };
            var lblBadgeCompany = new Label
            {
                Text = CurrentSession.GetCompanyDisplay(),
                Left = 32, Top = 6, Width = 260, Height = 16,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.DarkText, BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            var lblBadgeStatus = new Label
            {
                Text = _isSuperAdmin ? "Master Policy • Platform Administration" : "Subscriber Agreement • Active & Binding",
                Left = 32, Top = 23, Width = 260, Height = 16,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.Green, BackColor = Color.Transparent
            };
            _pnlBadge.Controls.AddRange(new Control[] { lblBadgeCompany, lblBadgeStatus });

            _headerPanel.Controls.AddRange(new Control[] { lblTitle, _lblLastUpdated, _pnlBadge });

            // ── Edit button (SuperAdmin only, placed in header to the right of the badge)
            if (_isSuperAdmin)
            {
                _btnEdit = new Button
                {
                    Text = "✏ Edit Terms",
                    Width = 130,
                    Height = 36,
                    BackColor = Theme.Blue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                _btnEdit.FlatAppearance.BorderSize = 0;
                _btnEdit.Click += (s, e) => EnterEditMode();
                _headerPanel.Controls.Add(_btnEdit);
            }

            _headerPanel.Resize += (s, e) => PositionHeaderControls();
            PositionHeaderControls();

            // ── Scroll body (read-only sections) ─────────────────────────────────────
            _scrollBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                AutoScroll = true,
                Padding = new Padding(32, 8, 32, 32)
            };
            _scrollBody.Resize += (s, e) =>
            {
                if (!_editPanel.Visible && _scrollBody.Controls.Count > 0)
                {
                    int cardW = Math.Max(700, _scrollBody.ClientSize.Width - 64);
                    foreach (Control c in _scrollBody.Controls)
                    {
                        if (c is Panel card) card.Width = cardW;
                    }
                }
            };

            // ── Edit panel (hidden until edit mode) ───────────────────────────────────
            _editPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Visible = false,
                Padding = new Padding(32, 8, 32, 24)
            };

            // Editor toolbar
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.Background
            };

            var lblEditHint = new Label
            {
                Text = "💡 Edit the Terms & Conditions below. Use numbers (e.g. 1. TITLE) for section cards, and • for bullet points.",
                Location = new Point(0, 8),
                Height = 30,
                Font = new Font("Segoe UI", 9.25f),
                ForeColor = Theme.MutedText,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _btnSave = new Button
            {
                Text = "💾 Save & Publish",
                Width = 150,
                Height = 34,
                BackColor = Theme.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += async (s, e) => await SaveTermsAsync();

            _btnCancel = new Button
            {
                Text = "✕ Cancel",
                Width = 90,
                Height = 34,
                BackColor = ColorTranslator.FromHtml("#EF4444"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => ExitEditMode(discard: true);

            void PositionToolbarControls()
            {
                int w = toolbar.ClientSize.Width;
                _btnCancel.Location = new Point(Math.Max(100, w - _btnCancel.Width), 5);
                _btnSave.Location = new Point(_btnCancel.Left - _btnSave.Width - 10, 5);
                lblEditHint.Width = Math.Max(100, _btnSave.Left - 10);
            }
            toolbar.Resize += (s, e) => PositionToolbarControls();

            toolbar.Controls.AddRange(new Control[] { lblEditHint, _btnSave, _btnCancel });
            PositionToolbarControls();

            // Rich editor wrapped inside an elegant card panel
            _richEditor = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Theme.DarkText,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                AcceptsTab = false
            };

            var editorCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            editorCard.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, editorCard.Width - 1, editorCard.Height - 1);
            };
            editorCard.Controls.Add(_richEditor);

            _editPanel.Controls.Add(editorCard);
            _editPanel.Controls.Add(toolbar);
            toolbar.BringToFront();

            // Wire up layout
            Controls.Add(_editPanel);
            Controls.Add(_scrollBody);
            Controls.Add(_headerPanel);
        }

        private void PositionHeaderControls()
        {
            if (_headerPanel == null) return;
            int clientWidth = _headerPanel.ClientSize.Width;

            if (_isSuperAdmin && _btnEdit != null && _btnEdit.Visible)
            {
                _btnEdit.Location = new Point(Math.Max(300, clientWidth - _btnEdit.Width - 32), 26);
                if (_pnlBadge != null)
                {
                    _pnlBadge.Location = new Point(_btnEdit.Left - _pnlBadge.Width - 14, 22);
                }
            }
            else if (_pnlBadge != null)
            {
                _pnlBadge.Location = new Point(Math.Max(300, clientWidth - _pnlBadge.Width - 32), 22);
            }
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  Data: Load & Save
        // ════════════════════════════════════════════════════════════════════════════

        private async Task LoadTermsAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/platform/settings/terms");
                request.Headers.Add("X-Company-Code", string.IsNullOrWhiteSpace(CurrentSession.CompanyCode) ? "MASTER" : CurrentSession.CompanyCode);
                if (!string.IsNullOrWhiteSpace(CurrentSession.Username))
                    request.Headers.Add("X-Api-User", CurrentSession.Username);

                var response = await ApiConfig.Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dto = JsonSerializer.Deserialize<TermsApiDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dto != null && !string.IsNullOrWhiteSpace(dto.Value))
                    {
                        _savedTermsText = dto.Value;
                        _lastUpdatedAt = dto.UpdatedAt;
                        _lastUpdatedBy = dto.UpdatedBy;
                    }
                    else
                    {
                        _savedTermsText = DefaultTermsText;
                    }
                }
                else
                {
                    _savedTermsText = DefaultTermsText;
                }
            }
            catch
            {
                _savedTermsText = DefaultTermsText;
            }

            if (InvokeRequired)
                Invoke(new Action(RenderReadOnlyView));
            else
                RenderReadOnlyView();
        }

        private async Task SaveTermsAsync()
        {
            var newText = _richEditor.Text.Trim();
            if (string.IsNullOrWhiteSpace(newText))
            {
                MessageBox.Show("Terms content cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving…";

            try
            {
                var payload = JsonSerializer.Serialize(new { value = newText });
                using var request = new HttpRequestMessage(HttpMethod.Put, "api/platform/settings/terms")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Company-Code", "MASTER");
                string username = !string.IsNullOrWhiteSpace(CurrentSession.Username) ? CurrentSession.Username : "superadmin";
                request.Headers.Add("X-Api-User", username);

                var response = await ApiConfig.Http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _savedTermsText = newText;
                    _lastUpdatedAt = DateTime.UtcNow;
                    _lastUpdatedBy = username;
                    ExitEditMode(discard: false);
                    MessageBox.Show("Terms & Conditions have been saved and published successfully.",
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to save terms:\n{err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Network error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "💾 Save & Publish";
            }
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  Mode Switching
        // ════════════════════════════════════════════════════════════════════════════

        private void EnterEditMode()
        {
            _richEditor.Text = _savedTermsText;
            _scrollBody.Visible = false;
            _editPanel.Visible = true;
            if (_btnEdit != null) _btnEdit.Visible = false;
            PositionHeaderControls();
            _richEditor.Focus();
        }

        private void ExitEditMode(bool discard)
        {
            _editPanel.Visible = false;
            _scrollBody.Visible = true;
            if (_btnEdit != null) _btnEdit.Visible = true;
            PositionHeaderControls();

            if (!discard)
                RenderReadOnlyView();
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  Read-Only View Rendering
        // ════════════════════════════════════════════════════════════════════════════

        private void RenderReadOnlyView()
        {
            // Update last-updated label
            if (_lastUpdatedAt.HasValue)
                _lblLastUpdated.Text = $"Last updated by {_lastUpdatedBy} on {_lastUpdatedAt.Value.ToLocalTime():dd MMM yyyy, hh:mm tt}";
            else
                _lblLastUpdated.Text = "Platform licensing terms, subscriber privacy guarantees, and SLA policies.";

            // Rebuild section cards from text
            _scrollBody.Controls.Clear();

            int topY = 12;
            int cardW = Math.Max(700, _scrollBody.ClientSize.Width - 64);

            // Colors for sections in order (cycles if more than 6)
            Color[] sectionColors =
            {
                Theme.Blue,
                Theme.Green,
                ColorTranslator.FromHtml("#4F46E5"),
                ColorTranslator.FromHtml("#D97706"),
                ColorTranslator.FromHtml("#DC2626"),
                ColorTranslator.FromHtml("#64748B")
            };

            // Parse sections: split on numbered "N." headings or blank-line-delimited blocks
            var sections = ParseSections(_savedTermsText);
            using var measureFont = new Font("Segoe UI", 9.5f);
            for (int i = 0; i < sections.Length; i++)
            {
                var (title, body) = sections[i];
                var color = sectionColors[i % sectionColors.Length];

                int textAvailableWidth = Math.Max(200, cardW - 44);
                var measured = TextRenderer.MeasureText(body, measureFont, new Size(textAvailableWidth, int.MaxValue), TextFormatFlags.WordBreak);
                int cardH = Math.Max(90, 52 + measured.Height + 16);

                var card = CreateSectionCard(title, color, body, topY, cardW, cardH);
                _scrollBody.Controls.Add(card);
                topY += cardH + 16;
            }
        }

        /// <summary>
        /// Splits the plain-text terms into (title, body) pairs.
        /// Sections are delimited by lines matching "N. TITLE" or blank lines.
        /// </summary>
        private static (string title, string body)[] ParseSections(string text)
        {
            var result = new System.Collections.Generic.List<(string, string)>();
            var lines = text.Replace("\r\n", "\n").Split('\n');

            string currentTitle = string.Empty;
            var bodyLines = new System.Collections.Generic.List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();

                // Detect section heading: starts with "1." ... "99." (optional space after digit)
                bool isHeading = false;
                for (int n = 1; n <= 50; n++)
                {
                    if (line.StartsWith($"{n}.", StringComparison.Ordinal) && line.Length > 3)
                    {
                        isHeading = true;
                        break;
                    }
                }

                if (isHeading)
                {
                    // Flush previous section
                    if (!string.IsNullOrEmpty(currentTitle))
                        result.Add((currentTitle, string.Join("\n", bodyLines).Trim()));

                    currentTitle = line;
                    bodyLines.Clear();
                }
                else
                {
                    bodyLines.Add(line);
                }
            }

            // Flush last section
            if (!string.IsNullOrEmpty(currentTitle))
                result.Add((currentTitle, string.Join("\n", bodyLines).Trim()));

            // If no headings found, treat the whole text as one section
            if (result.Count == 0 && !string.IsNullOrWhiteSpace(text))
                result.Add(("Terms & Conditions", text.Trim()));

            return result.ToArray();
        }

        private Panel CreateSectionCard(string title, Color accentColor, string bodyText, int top, int width, int height)
        {
            var pnl = new Panel
            {
                Left = 0,
                Top = top,
                Width = width,
                Height = height,
                BackColor = Theme.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            var accent = new Panel
            {
                Left = 0, Top = 0, Width = 5, Height = height,
                BackColor = accentColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            var lblTitle = new Label
            {
                Text = title,
                UseMnemonic = false,
                Left = 20, Top = 14,
                Width = width - 40, Height = 24,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblContent = new Label
            {
                Text = bodyText,
                UseMnemonic = false,
                Left = 20, Top = 44,
                Width = width - 40,
                Height = height - 52,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = ColorTranslator.FromHtml("#334155"),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            pnl.Controls.AddRange(new Control[] { accent, lblTitle, lblContent });
            return pnl;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  DTO (private, local deserialization only)
        // ════════════════════════════════════════════════════════════════════════════

        private class TermsApiDto
        {
            public string Value { get; set; } = string.Empty;
            public DateTime? UpdatedAt { get; set; }
            public string? UpdatedBy { get; set; }
        }
    }
}
