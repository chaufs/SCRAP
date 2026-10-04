using System;
using System.Drawing;
using System.Windows.Forms;
using SCRAP.winforms.Forms;
using SCRAP.winforms.Forms.Admin;
using SCRAP.winforms.Forms.Sales;
using SCRAP.winforms.Forms.Technical;

namespace SCRAP.winforms
{
    /// <summary>
    /// Admin: Dashboard, Device Categories, Inventory (view-only tabs), Reports.
    /// </summary>
    public class MainForm : Form
    {
        private Panel sidebar = null!;
        private Panel contentHost = null!;
        private Panel logoArea = null!;
        private Label lblBrand = null!;
        private Label lblBrandSub = null!;
        private Panel userArea = null!;

        private Button btnDashboard = null!;
        private Button? btnCategories;
        private Button? btnInventory;
        private Button? btnProcurement;
        private Button? btnHR;
        private Button? btnBranches;
        private Button? btnBranchOverview;
        private Button? btnReports;
        private Button? btnFinance;
        private Button? btnTerms;
        private Button? _activeNav;

        public MainForm()
        {
            Text = $"S.C.R.A.P — Admin — {CurrentSession.GetCompanyDisplay()}";
            Width = 1200;
            Height = 750;
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildSidebar();
            BuildContentHost();
            ShowDashboard();
        }

        private void BuildSidebar()
        {
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Theme.SidebarBg
            };

            logoArea = new Panel
            {
                Dock = DockStyle.Top,
                Height = 114,
                BackColor = Theme.SidebarBg
            };

            var accent = new Panel
            {
                Left = 0,
                Top = 0,
                Width = 4,
                Height = 114,
                BackColor = Theme.Green
            };

            lblBrand = new Label
            {
                Text = "S.C.R.A.P",
                Left = 20,
                Top = 14,
                Width = 200,
                Height = 26,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            lblBrandSub = new Label
            {
                Text = "Admin Console",
                Left = 20,
                Top = 40,
                Width = 200,
                Height = 20,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent
            };

            var companyBadge = Theme.CreateCompanyBadge(top: 68, left: 16, width: 208, height: 34);

            logoArea.Controls.Add(accent);
            logoArea.Controls.Add(lblBrand);
            logoArea.Controls.Add(lblBrandSub);
            logoArea.Controls.Add(companyBadge);

            var navPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SidebarBg,
                AutoScroll = true
            };

            int nextTop = 10;
            
            var secOps = CreateSectionLabel("OPERATIONS", nextTop);
            navPanel.Controls.Add(secOps);
            nextTop += 24;

            btnDashboard = CreateNavButton("Dashboard", nextTop);
            btnDashboard.Click += (s, e) => { SetActive(btnDashboard); ShowDashboard(); };
            navPanel.Controls.Add(btnDashboard);
            nextTop += 42;

            if (CurrentSession.HasModuleAccess("Inventory"))
            {
                btnCategories = CreateNavButton("Device Categories", nextTop);
                btnCategories.Click += (s, e) => { SetActive(btnCategories); ShowCategories(); };
                navPanel.Controls.Add(btnCategories);
                nextTop += 42;

                btnInventory = CreateNavButton("Inventory", nextTop);
                btnInventory.Click += (s, e) => { SetActive(btnInventory); ShowInventory(0); };
                navPanel.Controls.Add(btnInventory);
                nextTop += 42;
            }

            if (CurrentSession.HasModuleAccess("Procurement"))
            {
                btnProcurement = CreateNavButton("Procurement History", nextTop);
                btnProcurement.Click += (s, e) => { SetActive(btnProcurement); ShowProcurement(); };
                navPanel.Controls.Add(btnProcurement);
                nextTop += 42;
            }

            if (CurrentSession.HasModuleAccess("HR"))
            {
                var secPeople = CreateSectionLabel("STAFF & HR", nextTop);
                navPanel.Controls.Add(secPeople);
                nextTop += 24;

                btnHR = CreateNavButton("HR Management", nextTop);
                btnHR.Click += (s, e) => { SetActive(btnHR); ShowHR(); };
                navPanel.Controls.Add(btnHR);
                nextTop += 42;
            }

            if (CurrentSession.HasModuleAccess("Branches"))
            {
                var secBranches = CreateSectionLabel("BRANCHES", nextTop);
                navPanel.Controls.Add(secBranches);
                nextTop += 24;

                btnBranches = CreateNavButton("Branch Management", nextTop);
                btnBranches.Click += (s, e) => { SetActive(btnBranches); ShowBranches(); };
                navPanel.Controls.Add(btnBranches);
                nextTop += 42;

                btnBranchOverview = CreateNavButton("Branch Overview", nextTop);
                btnBranchOverview.Click += (s, e) => { SetActive(btnBranchOverview); ShowBranchOverview(); };
                navPanel.Controls.Add(btnBranchOverview);
                nextTop += 42;
            }

            if (CurrentSession.HasModuleAccess("Reports"))
            {
                var secAnalytics = CreateSectionLabel("ANALYTICS", nextTop);
                navPanel.Controls.Add(secAnalytics);
                nextTop += 24;

                btnReports = CreateNavButton("Reports", nextTop);
                btnReports.Click += (s, e) => { SetActive(btnReports); ShowReports(); };
                navPanel.Controls.Add(btnReports);
                nextTop += 42;
            }

            if (CurrentSession.HasModuleAccess("Finance"))
            {
                var secFinance = CreateSectionLabel("FINANCE", nextTop);
                navPanel.Controls.Add(secFinance);
                nextTop += 24;

                btnFinance = CreateNavButton("Company Finance", nextTop);
                btnFinance.Click += (s, e) => { SetActive(btnFinance); ShowFinance(); };
                navPanel.Controls.Add(btnFinance);
                nextTop += 42;
            }

            var secGovernance = CreateSectionLabel("LEGAL & GOVERNANCE", nextTop);
            navPanel.Controls.Add(secGovernance);
            nextTop += 24;

            btnTerms = CreateNavButton("Terms & Conditions", nextTop);
            btnTerms.Click += (s, e) => { SetActive(btnTerms); ShowTerms(); };
            navPanel.Controls.Add(btnTerms);
            nextTop += 48;

            userArea = new Panel
            {
                Height = 96,
                Dock = DockStyle.Bottom,
                BackColor = ColorTranslator.FromHtml("#0B1220")
            };
            var avatar = new Panel
            {
                Left = 16,
                Top = 14,
                Width = 36,
                Height = 36,
                BackColor = Theme.Blue
            };

            var lblUser = new Label
            {
                Text = CurrentSession.FullName,
                Left = 60,
                Top = 14,
                Width = 160,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var lblRole = new Label
            {
                Text = !string.IsNullOrWhiteSpace(CurrentSession.CompanyCode)
                    ? $"{CurrentSession.Role} • {CurrentSession.CompanyCode}"
                    : CurrentSession.Role,
                Left = 60,
                Top = 34,
                Width = 160,
                Height = 18,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            var btnLogout = new Button
            {
                Text = "Logout",
                Left = 16,
                Top = 58,
                Width = 100,
                Height = 26
            };
            Theme.StyleOutlineButton(btnLogout);
            btnLogout.Click += (s, e) =>
            {
                CurrentSession.LogoutRequested = true;
                FindForm()?.Close();
            };

            userArea.Controls.Add(avatar);
            userArea.Controls.Add(lblUser);
            userArea.Controls.Add(lblRole);
            userArea.Controls.Add(btnLogout);

            sidebar.Controls.Add(navPanel);
            sidebar.Controls.Add(logoArea);
            Theme.AttachCloudSyncBadge(sidebar, userArea);

            logoArea.SendToBack();
            navPanel.BringToFront();

            Controls.Add(sidebar);
            SetActive(btnDashboard);
        }

        private Button CreateNavButton(string text, int top)
        {
            var btn = Theme.CreateNavButton(text, top);
            btn.Left = 8;
            btn.Width = 208;
            return btn;
        }

        private Label CreateSectionLabel(string text, int top) => new Label
        {
            Text = text,
            Left = 16,
            Top = top,
            Width = 195,
            Height = 20,
            Font = Theme.NavSectionFont,
            ForeColor = Theme.SidebarSection,
            BackColor = Color.Transparent,
            UseMnemonic = false
        };

        private void SetActive(Button? btn)
        {
            foreach (var b in new[] { btnDashboard, btnCategories, btnInventory, btnProcurement, btnReports, btnHR, btnFinance, btnBranches, btnBranchOverview, btnTerms })
            {
                if (b != null)
                {
                    b.BackColor = Theme.SidebarBg;
                    b.ForeColor = Theme.SidebarText;
                }
            }
            if (btn != null)
            {
                btn.BackColor = Theme.SidebarActive;
                btn.ForeColor = Theme.SidebarTextActive;
            }
            _activeNav = btn;
        }


        private void BuildContentHost()
        {
            contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            Controls.Add(contentHost);
            contentHost.BringToFront();
        }

        private void EmbedForm(Form child)
        {
            contentHost.Controls.Clear();
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            child.BackColor = Theme.Background;
            contentHost.Controls.Add(child);
            child.Show();
        }

        private void ShowDashboard()
        {
            var dash = new DashboardForm { ShowInTaskbar = false };
            dash.NavigateRequested = target =>
            {
                switch (target)
                {
                    case DashboardNavTarget.Inventory:
                        if (btnInventory != null)
                        {
                            SetActive(btnInventory);
                            ShowInventory(0);
                        }
                        break;
                    case DashboardNavTarget.DeviceCategories:
                        if (btnCategories != null)
                        {
                            SetActive(btnCategories);
                            ShowCategories();
                        }
                        break;
                    case DashboardNavTarget.RecoveredCommodities:
                        if (btnInventory != null)
                        {
                            SetActive(btnInventory);
                            ShowInventory(1);
                        }
                        break;
                    case DashboardNavTarget.Reports:
                        if (btnReports != null)
                        {
                            SetActive(btnReports);
                            ShowReports();
                        }
                        break;
                }
            };
            EmbedForm(dash);
            SetActive(btnDashboard);
        }
        private void ShowHR()
        {
            EmbedForm(new HRManagementForm { ShowInTaskbar = false });
        }

        private void ShowCategories()
        {
            EmbedForm(new DeviceCategoriesForm { ShowInTaskbar = false });
            SetActive(btnCategories);
        }

        private void ShowInventory(int initialTab = 0)
        {
            EmbedForm(new AdminInventoryForm(initialTab) { ShowInTaskbar = false });
            SetActive(btnInventory);
        }

        private void ShowReports()
        {
            EmbedForm(new ReportGenerationForm { ShowInTaskbar = false });
            SetActive(btnReports);
        }

        private void ShowFinance()
        {
            EmbedForm(new CompanyFinanceMonitorForm { ShowInTaskbar = false });
            SetActive(btnFinance);
        }
        private void ShowBranches()
        {
            EmbedForm(new BranchManagementForm { ShowInTaskbar = false });
        }

        private void ShowBranchOverview()
        {
            EmbedForm(new BranchOverviewForm { ShowInTaskbar = false });
        }

        private void ShowProcurement()
        {
            EmbedForm(new ProcurementForm { ShowInTaskbar = false });
        }

        private void ShowTerms()
        {
            EmbedForm(new PlatformTermsForm { ShowInTaskbar = false });
            SetActive(btnTerms);
        }
    }
}