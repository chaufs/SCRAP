using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms
{
    public class SubscriptionPlansOverviewForm : Form
    {
        private FlowLayoutPanel flowPlans = null!;
        private Button btnAddPlan = null!;
        private Button btnRefresh = null!;
        private Button btnResetDefaults = null!;
        private Label lblStatus = null!;
        private List<SubscriptionPlanItem> _plans = new();
        public static List<SubscriptionPlanItem> CachedPlans { get; set; } = new();

        public SubscriptionPlansOverviewForm()
        {
            Text = "Plans & Module Gates";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;

            BuildUi();
            _ = LoadPlansAsync();
        }

        private void BuildUi()
        {
            // ── Top Header Panel ────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Theme.Background,
                Padding = new Padding(32, 20, 32, 0)
            };

            var lblTitle = new Label
            {
                Text = "Subscription Tiers & Module Packages",
                Left = 32,
                Top = 16,
                Width = 600,
                Height = 32,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText
            };

            var lblSubtitle = new Label
            {
                Text = "Configure and customize subscription tiers, default pricing, and enabled module feature gates across the platform.",
                Left = 32,
                Top = 50,
                Width = 850,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);

            // ── Action Toolbar ──────────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.White,
                Padding = new Padding(32, 8, 32, 8)
            };
            toolbar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.CardBorder, 1);
                e.Graphics.DrawLine(p, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            btnAddPlan = new Button
            {
                Text = "+ Add New Tier",
                Left = 32,
                Top = 8,
                Width = 145,
                Height = 36
            };
            Theme.StylePrimaryButton(btnAddPlan);
            btnAddPlan.Click += BtnAddPlan_Click;

            btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Left = 187,
                Top = 8,
                Width = 100,
                Height = 36
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadPlansAsync();

            btnResetDefaults = new Button
            {
                Text = "↺ Reset to Defaults",
                Left = 297,
                Top = 8,
                Width = 150,
                Height = 36
            };
            Theme.StyleOutlineButton(btnResetDefaults);
            btnResetDefaults.Click += BtnResetDefaults_Click;

            lblStatus = new Label
            {
                Text = "Loading plans…",
                Left = 460,
                Top = 16,
                Width = 400,
                Height = 22,
                Font = Theme.LabelFont,
                ForeColor = Theme.MutedText
            };

            toolbar.Controls.AddRange(new Control[] { btnAddPlan, btnRefresh, btnResetDefaults, lblStatus });

            // ── Scrollable Body with FlowLayoutPanel ────────────────────────────────
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(32, 20, 32, 32),
                AutoScroll = true
            };

            flowPlans = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Background,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            body.Controls.Add(flowPlans);

            Controls.Add(body);
            Controls.Add(toolbar);
            Controls.Add(header);
        }

        private async Task LoadPlansAsync()
        {
            lblStatus.Text = "Loading subscription plans…";
            lblStatus.ForeColor = Theme.MutedText;

            try
            {
                var response = await ApiConfig.Http.GetAsync("api/platform/settings/plans");
                if (response.IsSuccessStatusCode)
                {
                    var plans = await response.Content.ReadFromJsonAsync<List<SubscriptionPlanItem>>();
                    if (plans != null && plans.Count > 0)
                    {
                        _plans = plans;
                    }
                    else
                    {
                        _plans = GetFallbackPlans();
                    }
                }
                else
                {
                    _plans = GetFallbackPlans();
                }

                CachedPlans = new List<SubscriptionPlanItem>(_plans);
                lblStatus.Text = $"{_plans.Count} subscription tier{(_plans.Count != 1 ? "s" : "")} active.";
                lblStatus.ForeColor = Theme.Green;
                RenderPlanCards();
            }
            catch
            {
                _plans = GetFallbackPlans();
                lblStatus.Text = "Using local plan definitions (offline).";
                lblStatus.ForeColor = Theme.MutedText;
                RenderPlanCards();
            }
        }

        private void RenderPlanCards()
        {
            flowPlans.SuspendLayout();
            flowPlans.Controls.Clear();

            int cardW = 330;
            int cardH = 500;

            foreach (var plan in _plans)
            {
                var card = BuildPlanCard(plan, cardW, cardH);
                flowPlans.Controls.Add(card);
            }

            flowPlans.ResumeLayout(true);
        }

        private Panel BuildPlanCard(SubscriptionPlanItem plan, int width, int height)
        {
            Color accentColor = Theme.Blue;
            try
            {
                if (!string.IsNullOrWhiteSpace(plan.ColorHex))
                {
                    accentColor = ColorTranslator.FromHtml(plan.ColorHex);
                }
            }
            catch { }

            var pnl = new Panel
            {
                Width = width,
                Height = height,
                BackColor = Theme.White,
                Margin = new Padding(0, 0, 24, 24)
            };

            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(plan.IsRecommended ? accentColor : Theme.CardBorder, plan.IsRecommended ? 2 : 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            // Top accent color bar
            var topBar = new Panel
            {
                Left = 0,
                Top = 0,
                Width = width,
                Height = 6,
                BackColor = accentColor
            };
            pnl.Controls.Add(topBar);

            // Title
            var lblT = new Label
            {
                Text = plan.DisplayName,
                Left = 20,
                Top = 18,
                Width = width - 40,
                Height = 24,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Theme.DarkText
            };
            pnl.Controls.Add(lblT);

            // Recommended Tag if applicable
            int contentY = 44;
            if (plan.IsRecommended)
            {
                var lblBadge = new Label
                {
                    Text = "★ RECOMMENDED",
                    Left = 20,
                    Top = contentY,
                    Width = 130,
                    Height = 20,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = accentColor,
                    BackColor = Color.FromArgb(25, accentColor.R, accentColor.G, accentColor.B),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnl.Controls.Add(lblBadge);
                contentY += 26;
            }

            // Price
            var lblPrice = new Label
            {
                Text = $"₱{plan.Price:N0} / {plan.BillingCycle ?? "month"}",
                Left = 20,
                Top = contentY,
                Width = width - 40,
                Height = 32,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = accentColor
            };
            pnl.Controls.Add(lblPrice);
            contentY += 36;

            // Description
            var lblDesc = new Label
            {
                Text = string.IsNullOrWhiteSpace(plan.Description) ? "Standard subscription package." : plan.Description,
                Left = 20,
                Top = contentY,
                Width = width - 40,
                Height = 38,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.MutedText
            };
            pnl.Controls.Add(lblDesc);
            contentY += 44;

            // Divider
            var divider = new Panel
            {
                Left = 20,
                Top = contentY,
                Width = width - 40,
                Height = 1,
                BackColor = Theme.CardBorder
            };
            pnl.Controls.Add(divider);
            contentY += 10;

            // Module Features Header
            var lblFeatHeader = new Label
            {
                Text = "MODULE FEATURE GATES",
                Left = 20,
                Top = contentY,
                Width = width - 40,
                Height = 18,
                Font = Theme.NavSectionFont,
                ForeColor = Theme.SidebarSection
            };
            pnl.Controls.Add(lblFeatHeader);
            contentY += 22;

            // Modules Checklist view
            var enabledSet = new HashSet<string>(
                (plan.EnabledModules ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()),
                StringComparer.OrdinalIgnoreCase);

            bool isAll = (plan.EnabledModules ?? "").Equals("All", StringComparison.OrdinalIgnoreCase);

            int fY = contentY;
            foreach (var mod in CustomizePlanDialog.AllModules)
            {
                bool isIncluded = isAll || enabledSet.Contains(mod.Key);
                var lblF = new Label
                {
                    Text = (isIncluded ? "✓  " : "✗  ") + mod.Label,
                    Left = 20,
                    Top = fY,
                    Width = width - 40,
                    Height = 20,
                    Font = new Font("Segoe UI", 9f, isIncluded ? FontStyle.Regular : FontStyle.Regular),
                    ForeColor = isIncluded ? Theme.DarkText : ColorTranslator.FromHtml("#94A3B8")
                };
                pnl.Controls.Add(lblF);
                fY += 22;
            }

            // Bottom Action Button: "⚙ Customize Plan"
            var btnCustomize = new Button
            {
                Text = "⚙ Customize Plan",
                Left = 20,
                Top = height - 52,
                Width = width - 40,
                Height = 34,
                Cursor = Cursors.Hand
            };
            Theme.StylePrimaryButton(btnCustomize);
            btnCustomize.BackColor = accentColor;
            btnCustomize.Click += async (s, e) => await EditPlanAsync(plan);
            pnl.Controls.Add(btnCustomize);

            return pnl;
        }

        private async void BtnAddPlan_Click(object? sender, EventArgs e)
        {
            var newPlan = new SubscriptionPlanItem
            {
                Name = $"CustomTier{_plans.Count + 1}",
                DisplayName = "New Custom Tier",
                Price = 349.00m,
                BillingCycle = "month",
                Description = "Customized feature package for specialized operations.",
                EnabledModules = "Inventory,Procurement,Sales,Reports",
                ColorHex = "#9333EA",
                IsRecommended = false
            };

            using var dlg = new CustomizePlanDialog(newPlan, isNew: true);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.PlanResult != null)
            {
                _plans.Add(dlg.PlanResult);
                await SavePlansToServerAsync();
            }
        }

        private async Task EditPlanAsync(SubscriptionPlanItem plan)
        {
            using var dlg = new CustomizePlanDialog(plan, isNew: false);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.PlanResult != null)
            {
                int idx = _plans.IndexOf(plan);
                if (idx >= 0)
                {
                    _plans[idx] = dlg.PlanResult;
                }
                else
                {
                    var existing = _plans.FirstOrDefault(p => string.Equals(p.Name, plan.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        int pIdx = _plans.IndexOf(existing);
                        _plans[pIdx] = dlg.PlanResult;
                    }
                }

                await SavePlansToServerAsync();
            }
        }

        private async void BtnResetDefaults_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to reset all subscription plans to their default values (Basic, Standard, Enterprise)?\nAny custom pricing or tier modifications will be restored to factory defaults.",
                "Confirm Reset Plans", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _plans = GetFallbackPlans();
                await SavePlansToServerAsync();
            }
        }

        private async Task SavePlansToServerAsync()
        {
            lblStatus.Text = "Saving updated plans…";
            lblStatus.ForeColor = Theme.MutedText;

            try
            {
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Put, "api/platform/settings/plans")
                {
                    Content = System.Net.Http.Json.JsonContent.Create(_plans)
                };
                request.Headers.Add("X-Company-Code", "MASTER");
                request.Headers.Add("X-Api-User", CurrentSession.Username);

                var response = await ApiConfig.Http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    CachedPlans = new List<SubscriptionPlanItem>(_plans);
                    lblStatus.Text = "All plan definitions saved successfully.";
                    lblStatus.ForeColor = Theme.Green;
                    RenderPlanCards();
                    MessageBox.Show("Subscription plan definitions and module gates updated successfully!", "Plans Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    lblStatus.Text = "Failed to save plans to server.";
                    lblStatus.ForeColor = ColorTranslator.FromHtml("#DC2626");
                    MessageBox.Show($"Failed to save plans:\n{err}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Network error saving plans.";
                lblStatus.ForeColor = ColorTranslator.FromHtml("#DC2626");
                MessageBox.Show("Error saving plans: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<SubscriptionPlanItem> GetFallbackPlans()
        {
            return new List<SubscriptionPlanItem>
            {
                new SubscriptionPlanItem
                {
                    Name = "Basic",
                    DisplayName = "Basic Tier",
                    Price = 99.00m,
                    BillingCycle = "month",
                    Description = "Core operations for scrap intake, sales, and inventory.",
                    EnabledModules = "Inventory,Procurement,Sales,Reports",
                    ColorHex = "#2563EB",
                    IsRecommended = false
                },
                new SubscriptionPlanItem
                {
                    Name = "Standard",
                    DisplayName = "Standard Tier (Recommended)",
                    Price = 249.00m,
                    BillingCycle = "month",
                    Description = "Best for growing multi-depot e-waste operations.",
                    EnabledModules = "Inventory,Procurement,Sales,Technical,HR,Branches,Reports",
                    ColorHex = "#16A34A",
                    IsRecommended = true
                },
                new SubscriptionPlanItem
                {
                    Name = "Enterprise",
                    DisplayName = "Enterprise Tier",
                    Price = 599.00m,
                    BillingCycle = "month",
                    Description = "Complete solution for full-scale industrial operations.",
                    EnabledModules = "Inventory,Procurement,Sales,Technical,HR,Branches,Reports,Finance",
                    ColorHex = "#4F46E5",
                    IsRecommended = false
                }
            };
        }
    }
}
