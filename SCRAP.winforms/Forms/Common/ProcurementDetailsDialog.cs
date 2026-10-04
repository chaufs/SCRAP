using System;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Common
{
    public class ProcurementDetailsDialog : Form
    {
        private readonly int _requestId;
        private ProcurementDetailsModel _model;

        private Label _lblHeaderTitle = null!;
        private Label _lblHeaderSubtitle = null!;
        private Panel _badgeStatus = null!;
        private Label _lblBadgeText = null!;

        // Content panels
        private Panel _bodyPanel = null!;

        public ProcurementDetailsDialog(int requestId)
        {
            _requestId = requestId;
            _model = new ProcurementDetailsModel { Id = requestId };

            InitializeUi();
            _ = LoadDetailsAsync();
        }

        public ProcurementDetailsDialog(ProcurementDetailsModel model)
        {
            _requestId = model.Id;
            _model = model;

            InitializeUi();
            PopulateUi();
        }

        private void InitializeUi()
        {
            Text = $"Procurement Details — Request #{_requestId}";
            Width = 740;
            Height = 670;
            MinimumSize = new Size(680, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            // Header Panel
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.White,
                Padding = new Padding(24, 16, 24, 12)
            };
            header.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            _lblHeaderTitle = new Label
            {
                Text = $"Procurement Request #{_requestId}",
                Left = 24,
                Top = 14,
                Width = 460,
                Height = 30,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };

            _lblHeaderSubtitle = new Label
            {
                Text = "Loading details...",
                Left = 24,
                Top = 48,
                Width = 460,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            // Status Pill (Top Right)
            _badgeStatus = new Panel
            {
                Top = 22,
                Width = 160,
                Height = 34,
                BackColor = ColorTranslator.FromHtml("#F1F5F9")
            };
            _badgeStatus.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(40, 0, 0, 0), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _badgeStatus.Width - 1, _badgeStatus.Height - 1);
            };

            _lblBadgeText = new Label
            {
                Text = "PENDING",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };
            _badgeStatus.Controls.Add(_lblBadgeText);

            header.Controls.Add(_lblHeaderTitle);
            header.Controls.Add(_lblHeaderSubtitle);
            header.Controls.Add(_badgeStatus);

            header.Resize += (s, e) =>
            {
                _badgeStatus.Left = header.ClientSize.Width - 24 - _badgeStatus.Width;
            };

            // Bottom Footer
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.White,
                Padding = new Padding(24, 12, 24, 12)
            };
            footer.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
            };

            var btnClose = new Button
            {
                Text = "Close",
                Width = 110,
                Height = 36,
                DialogResult = DialogResult.OK
            };
            Theme.StylePrimaryButton(btnClose);
            btnClose.Click += (_, _) => Close();

            footer.Controls.Add(btnClose);
            footer.Resize += (s, e) =>
            {
                btnClose.Left = footer.ClientSize.Width - 24 - btnClose.Width;
                btnClose.Top = (footer.ClientSize.Height - btnClose.Height) / 2;
            };

            // Scrollable Body
            _bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };

            Controls.Add(_bodyPanel);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private async Task LoadDetailsAsync()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync($"api/procurement/{_requestId}");
                if (res.IsSuccessStatusCode)
                {
                    var entity = await res.Content.ReadFromJsonAsync<ProcurementRequest>(ApiConfig.JsonOptions);
                    if (entity != null)
                    {
                        _model = ProcurementDetailsModel.FromEntity(entity);
                    }
                }
            }
            catch { }

            PopulateUi();
        }

        public void PopulateUi()
        {
            if (InvokeRequired)
            {
                Invoke(PopulateUi);
                return;
            }

            _lblHeaderTitle.Text = $"{_model.Quantity}x {_model.DeviceName}";
            _lblHeaderSubtitle.Text = $"Procurement #{_model.Id} • Requested: {(_model.RequestedDate.HasValue ? _model.RequestedDate.Value.ToString("MMM dd, yyyy h:mm tt") : "N/A")}";

            // Style Badge
            ApplyStatusBadge(_model.Status);

            _bodyPanel.Controls.Clear();

            int cardW = _bodyPanel.ClientSize.Width - 48;
            if (cardW < 600) cardW = 640;

            int currentY = 12;

            // 1. Device & Sourcing Card
            var cardDevice = CreateSectionCard("📦  DEVICE & SOURCING INFORMATION", 24, currentY, cardW, 140);
            AddPropertyRow(cardDevice, "Vendor / Supplier:", _model.SupplierCompany, "Device Model / Type:", _model.DeviceName, 36);
            AddPropertyRow(cardDevice, "Category:", _model.CategoryName, "Quantity:", $"{_model.Quantity} unit(s)", 64);
            string codeDisplay = !string.IsNullOrWhiteSpace(_model.BatchCode)
                ? $"Batch: {_model.BatchCode}"
                : (!string.IsNullOrWhiteSpace(_model.SerialNumber) ? $"Serial: {_model.SerialNumber}" : "—");
            AddPropertyRow(cardDevice, "Has Storage Device:", _model.HasStorageDevice ? "Yes (Degaussing / Sanitization required)" : "No", "Serial / Batch:", codeDisplay, 92);
            _bodyPanel.Controls.Add(cardDevice);
            currentY += cardDevice.Height + 14;

            // 2. Financial Breakdown Card
            var cardFinance = CreateSectionCard("💰  FINANCIAL & COST DETAILS", 24, currentY, cardW, 85);
            AddFinancialRow(cardFinance, "Cost Per Unit:", $"₱{_model.CostPerDevice:N2}", "TOTAL PROCUREMENT COST:", $"₱{_model.TotalCost:N2}", 36);
            _bodyPanel.Controls.Add(cardFinance);
            currentY += cardFinance.Height + 14;

            // 3. Workflow & Personnel Audit Card
            var cardPersonnel = CreateSectionCard("👥  WORKFLOW & PERSONNEL AUDIT", 24, currentY, cardW, 130);
            AddPersonnelRow(cardPersonnel, "Requested By (Sales):", _model.RequestedByFullName ?? "—", _model.RequestedDate, 36);
            AddPersonnelRow(cardPersonnel, "Reviewed By (Manager):", _model.ReviewedByFullName ?? "—", _model.ReviewedDate, 64);
            AddPersonnelRow(cardPersonnel, "Assigned Tech Staff:", _model.AssignedTechStaffFullName ?? "—", _model.AssignedDate, 92);
            _bodyPanel.Controls.Add(cardPersonnel);
            currentY += cardPersonnel.Height + 14;

            // 4. Notes & Rejection Remarks Card
            bool hasNotes = !string.IsNullOrWhiteSpace(_model.Notes);
            bool hasRejection = !string.IsNullOrWhiteSpace(_model.RejectionReason);

            if (hasNotes || hasRejection || true)
            {
                int noteCardH = 100;
                if (hasRejection) noteCardH += 50;

                var cardNotes = CreateSectionCard("📝  REMARKS, INTAKE NOTES & REJECTION REASONS", 24, currentY, cardW, noteCardH);

                int noteY = 36;
                if (hasRejection)
                {
                    var lblRejTitle = new Label
                    {
                        Text = "Rejection Reason:",
                        Left = 16,
                        Top = noteY,
                        Width = 140,
                        Height = 20,
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                        ForeColor = ColorTranslator.FromHtml("#DC2626")
                    };
                    var lblRejVal = new Label
                    {
                        Text = _model.RejectionReason,
                        Left = 160,
                        Top = noteY,
                        Width = cardW - 180,
                        Height = 38,
                        Font = Theme.LabelFont,
                        ForeColor = ColorTranslator.FromHtml("#991B1B")
                    };
                    cardNotes.Controls.Add(lblRejTitle);
                    cardNotes.Controls.Add(lblRejVal);
                    noteY += 44;
                }

                var lblNotesTitle = new Label
                {
                    Text = "General Notes:",
                    Left = 16,
                    Top = noteY,
                    Width = 140,
                    Height = 20,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Theme.MutedText
                };
                var lblNotesVal = new Label
                {
                    Text = !string.IsNullOrWhiteSpace(_model.Notes) ? _model.Notes : "(None provided)",
                    Left = 160,
                    Top = noteY,
                    Width = cardW - 180,
                    Height = 44,
                    Font = Theme.LabelFont,
                    ForeColor = Theme.DarkText
                };
                cardNotes.Controls.Add(lblNotesTitle);
                cardNotes.Controls.Add(lblNotesVal);

                _bodyPanel.Controls.Add(cardNotes);
                currentY += cardNotes.Height + 20;
            }
        }

        private Panel CreateSectionCard(string title, int left, int top, int width, int height)
        {
            var pnl = new Panel
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                BackColor = Theme.White
            };
            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            var lblT = new Label
            {
                Text = title,
                Left = 16,
                Top = 10,
                Width = width - 32,
                Height = 20,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };
            pnl.Controls.Add(lblT);
            return pnl;
        }

        private void AddPropertyRow(Panel parent, string l1, string v1, string l2, string v2, int top)
        {
            int col1 = 16;
            int val1 = 160;
            int col2 = parent.Width / 2 + 10;
            int val2 = col2 + 140;

            var lbl1 = new Label { Text = l1, Left = col1, Top = top, Width = 140, Height = 22, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.MutedText };
            var txt1 = new Label { Text = v1, Left = val1, Top = top, Width = (parent.Width / 2) - val1 - 10, Height = 22, Font = Theme.LabelFont, ForeColor = Theme.DarkText };

            var lbl2 = new Label { Text = l2, Left = col2, Top = top, Width = 135, Height = 22, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.MutedText };
            var txt2 = new Label { Text = v2, Left = val2, Top = top, Width = parent.Width - val2 - 16, Height = 22, Font = Theme.LabelFont, ForeColor = Theme.DarkText };

            parent.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2 });
        }

        private void AddFinancialRow(Panel parent, string l1, string v1, string l2, string v2, int top)
        {
            int col1 = 16;
            int val1 = 160;
            int col2 = parent.Width / 2 + 10;
            int val2 = col2 + 180;

            var lbl1 = new Label { Text = l1, Left = col1, Top = top + 2, Width = 140, Height = 22, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.MutedText };
            var txt1 = new Label { Text = v1, Left = val1, Top = top + 1, Width = (parent.Width / 2) - val1 - 10, Height = 24, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Theme.DarkText };

            var lbl2 = new Label { Text = l2, Left = col2, Top = top + 2, Width = 175, Height = 22, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.MutedText };
            var txt2 = new Label { Text = v2, Left = val2, Top = top - 2, Width = parent.Width - val2 - 16, Height = 28, Font = new Font("Segoe UI", 13.5f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#059669") };

            parent.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2 });
        }

        private void AddPersonnelRow(Panel parent, string roleLabel, string name, DateTime? date, int top)
        {
            var lblRole = new Label { Text = roleLabel, Left = 16, Top = top, Width = 180, Height = 22, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.MutedText };
            var lblName = new Label { Text = name, Left = 200, Top = top, Width = 260, Height = 22, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Theme.DarkText };

            string dateStr = date.HasValue ? $"Timestamp: {date.Value.ToLocalTime():yyyy-MM-dd h:mm tt}" : "—";
            var lblDate = new Label { Text = dateStr, Left = 470, Top = top, Width = parent.Width - 480, Height = 22, Font = new Font("Segoe UI", 8.5f), ForeColor = Theme.MutedText };

            parent.Controls.AddRange(new Control[] { lblRole, lblName, lblDate });
        }

        private void ApplyStatusBadge(string? status)
        {
            string s = status?.Trim() ?? "";

            if (s.Contains("Pending", StringComparison.OrdinalIgnoreCase))
            {
                _badgeStatus.BackColor = ColorTranslator.FromHtml("#FEF3C7");
                _lblBadgeText.ForeColor = ColorTranslator.FromHtml("#92400E");
                _lblBadgeText.Text = "⏳  PENDING REVIEW";
            }
            else if (s.Contains("Approved", StringComparison.OrdinalIgnoreCase) || s.Contains("Arrival", StringComparison.OrdinalIgnoreCase))
            {
                _badgeStatus.BackColor = ColorTranslator.FromHtml("#DBEAFE");
                _lblBadgeText.ForeColor = ColorTranslator.FromHtml("#1E40AF");
                _lblBadgeText.Text = "📦  AWAITING ARRIVAL";
            }
            else if (s.Contains("Completed", StringComparison.OrdinalIgnoreCase))
            {
                _badgeStatus.BackColor = ColorTranslator.FromHtml("#D1FAE5");
                _lblBadgeText.ForeColor = ColorTranslator.FromHtml("#065F46");
                _lblBadgeText.Text = "✓  COMPLETED";
            }
            else if (s.Contains("Reject", StringComparison.OrdinalIgnoreCase))
            {
                _badgeStatus.BackColor = ColorTranslator.FromHtml("#FEE2E2");
                _lblBadgeText.ForeColor = ColorTranslator.FromHtml("#991B1B");
                _lblBadgeText.Text = "✗  REJECTED";
            }
            else
            {
                _badgeStatus.BackColor = ColorTranslator.FromHtml("#F1F5F9");
                _lblBadgeText.ForeColor = Theme.DarkText;
                _lblBadgeText.Text = s.ToUpperInvariant();
            }
        }
    }

    public class ProcurementDetailsModel
    {
        public int Id { get; set; }
        public string SupplierCompany { get; set; } = "—";
        public string DeviceName { get; set; } = "—";
        public string CategoryName { get; set; } = "—";
        public int Quantity { get; set; } = 1;
        public decimal TotalCost { get; set; }
        public decimal CostPerDevice { get; set; }
        public bool HasStorageDevice { get; set; }
        public string? SerialNumber { get; set; }
        public string? BatchCode { get; set; }
        public string? Status { get; set; }
        public string? RequestedByFullName { get; set; }
        public DateTime? RequestedDate { get; set; }
        public string? ReviewedByFullName { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? AssignedTechStaffFullName { get; set; }
        public DateTime? AssignedDate { get; set; }
        public string? CompletedByUserName { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? BranchName { get; set; }
        public string? Notes { get; set; }
        public string? RejectionReason { get; set; }

        public static ProcurementDetailsModel FromEntity(ProcurementRequest p)
        {
            return new ProcurementDetailsModel
            {
                Id = p.Id,
                SupplierCompany = !string.IsNullOrWhiteSpace(p.SupplierCompany) ? p.SupplierCompany : "—",
                DeviceName = p.DeviceName,
                CategoryName = p.DeviceCategory?.Name ?? "—",
                Quantity = p.Quantity,
                TotalCost = p.TotalCost,
                CostPerDevice = p.CostPerDevice > 0 ? p.CostPerDevice : (p.Quantity > 0 ? p.TotalCost / p.Quantity : 0),
                HasStorageDevice = p.HasStorageDevice,
                SerialNumber = p.SerialNumber,
                BatchCode = p.BatchCode,
                Status = p.Status.ToString(),
                RequestedByFullName = p.RequestedByFullName ?? p.RequestedByUserName,
                RequestedDate = p.RequestedAtUtc.ToLocalTime(),
                ReviewedByFullName = p.ReviewedByFullName ?? p.ReviewedByUserName,
                ReviewedDate = p.ReviewedAtUtc.HasValue ? p.ReviewedAtUtc.Value.ToLocalTime() : null,
                AssignedTechStaffFullName = p.AssignedTechStaffFullName ?? p.AssignedTechStaffUserName,
                AssignedDate = p.AssignedAtUtc.HasValue ? p.AssignedAtUtc.Value.ToLocalTime() : null,
                CompletedByUserName = p.CompletedByUserName,
                CompletedDate = p.CompletedAtUtc.HasValue ? p.CompletedAtUtc.Value.ToLocalTime() : null,
                BranchName = p.Branch?.Name,
                Notes = p.Notes,
                RejectionReason = p.RejectionReason
            };
        }

        public static ProcurementDetailsModel FromDynamic(dynamic d)
        {
            var model = new ProcurementDetailsModel();
            try { model.Id = d.Id; } catch { }
            try { model.SupplierCompany = d.SupplierCompany ?? "—"; } catch { }
            try { model.DeviceName = d.DeviceName ?? "—"; } catch { }
            try { model.CategoryName = d.CategoryName ?? "—"; } catch { }
            try { model.Quantity = d.Quantity; } catch { }
            try { model.TotalCost = d.TotalCost; } catch { }
            try { model.CostPerDevice = d.CostPerDevice; } catch { }
            try { model.Status = d.Status?.ToString() ?? "Pending"; } catch { }
            try { model.RequestedByFullName = d.RequestedByFullName?.ToString(); } catch { }
            try { model.RequestedDate = d.RequestedDate != null ? (DateTime?)d.RequestedDate : (d.RequestedAt != null ? (DateTime?)d.RequestedAt : null); } catch { }
            try { model.ReviewedByFullName = d.ReviewedByFullName?.ToString(); } catch { }
            try { model.AssignedTechStaffFullName = d.AssignedTechStaffFullName?.ToString(); } catch { }
            try { model.SerialNumber = d.SerialNumber?.ToString(); } catch { }
            try { model.Notes = d.Notes?.ToString(); } catch { }
            try { model.RejectionReason = d.RejectionReason?.ToString(); } catch { }
            return model;
        }
    }
}
