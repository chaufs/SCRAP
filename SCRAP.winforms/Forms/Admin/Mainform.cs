using System;
using System.Drawing;
using System.Windows.Forms;
using SCRAP.winforms.Forms;
using SCRAP.winforms.Forms.Admin;

namespace SCRAP.winforms
{
    /// <summary>
    /// Admin: Dashboard, Device Categories, Inventory (view-only tabs), Reports.
    /// </summary>
    public class MainForm : Form
    {

        private Button btnHR = null!;
        private Panel sidebar = null!;
        private Panel contentHost = null!;
        private Panel logoArea = null!;
        private Label lblBrand = null!;
        private Label lblBrandSub = null!;
        private Panel userArea = null!;

        private Button btnDashboard = null!;
        private Button btnCategories = null!;
        private Button btnInventory = null!;
        private Button btnReports = null!;
        private Button btnFinance = null!;
        private Button? _activeNav;

        public MainForm()
        {
            Text = "S.C.R.A.P — Admin";
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
                Left = 0,
                Top = 0,
                Width = 240,
                Height = 80,
                BackColor = Theme.SidebarBg
            };

            var accent = new Panel
            {
                Left = 0,
                Top = 0,
                Width = 4,
                Height = 80,
                BackColor = Theme.Green
            };

            lblBrand = new Label
            {
                Text = "S.C.R.A.P",
                Left = 20,
                Top = 18,
                Width = 200,
                Height = 28,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };



            lblBrandSub = new Label
            {
                Text = "Admin Console",
                Left = 20,
                Top = 48,
                Width = 200,
                Height = 20,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent
            };

            logoArea.Controls.Add(accent);
            logoArea.Controls.Add(lblBrand);
            logoArea.Controls.Add(lblBrandSub);

            var secOps = CreateSectionLabel("OPERATIONS", 100);


            btnDashboard = Theme.CreateNavButton("Dashboard", 128);
            btnDashboard.Click += (s, e) => { SetActive(btnDashboard); ShowDashboard(); };

            btnCategories = Theme.CreateNavButton("Device Categories", 172);
            btnCategories.Click += (s, e) => { SetActive(btnCategories); ShowCategories(); };

            btnInventory = Theme.CreateNavButton("Inventory", 216);
            btnInventory.Click += (s, e) => { SetActive(btnInventory); ShowInventory(0); };


            btnHR = Theme.CreateNavButton("HR Management", 304);
            btnHR.Click += (s, e) => { SetActive(btnHR); ShowHR(); };

            var secAnalytics = CreateSectionLabel("ANALYTICS", 276);

            btnReports = Theme.CreateNavButton("Reports", 304);
            btnReports.Click += (s, e) => { SetActive(btnReports); ShowReports(); };

            btnFinance = Theme.CreateNavButton("Company Finance", 348);
            btnFinance.Click += (s, e) => { SetActive(btnFinance); ShowFinance(); };

            userArea = new Panel
            {
                Height = 96,   // increased from 64
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
                Text = "Administrator",
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
                Text = "Admin",
                Left = 60,
                Top = 34,
                Width = 160,
                Height = 18,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent
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
            sidebar.Controls.Add(logoArea);
            sidebar.Controls.Add(secOps);
            sidebar.Controls.Add(btnDashboard);
            sidebar.Controls.Add(btnCategories);
            sidebar.Controls.Add(btnInventory);
            sidebar.Controls.Add(btnHR);

            sidebar.Controls.Add(secAnalytics);
            sidebar.Controls.Add(btnReports);
            sidebar.Controls.Add(btnFinance);
            sidebar.Controls.Add(userArea);

            Controls.Add(sidebar);
            SetActive(btnDashboard);
        }

        private Label CreateSectionLabel(string text, int top) => new Label
        {
            Text = text,
            Left = 20,
            Top = top,
            Width = 200,
            Height = 20,
            Font = Theme.NavSectionFont,
            ForeColor = Theme.SidebarSection,
            BackColor = Color.Transparent
        };

        private void SetActive(Button btn)
        {
            foreach (var b in new[] { btnDashboard, btnCategories, btnInventory, btnReports, btnHR, btnFinance })
            {
                b.BackColor = Theme.SidebarBg;
                b.ForeColor = Theme.SidebarText;
            }
            btn.BackColor = Theme.SidebarActive;
            btn.ForeColor = Theme.SidebarTextActive;
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
                        SetActive(btnInventory);
                        ShowInventory(0);
                        break;
                    case DashboardNavTarget.DeviceCategories:
                        SetActive(btnCategories);
                        ShowCategories();
                        break;
                    case DashboardNavTarget.RecoveredCommodities:
                        SetActive(btnInventory);
                        ShowInventory(1);
                        break;
                    case DashboardNavTarget.Reports:
                        SetActive(btnReports);
                        ShowReports();
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
    }
}