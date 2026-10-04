using System;
using System.Drawing;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    public partial class SuperAdminMainForm : Form
    {
        private Panel sidebar = null!;
        private Panel contentHost = null!;
        private Panel logoArea = null!;
        private Label lblBrand = null!;
        private Label lblBrandSub = null!;
        private Panel userArea = null!;

        private Button btnSubscribers = null!;
        private Button btnUsers = null!;
        private Button btnPlans = null!;
        private Button btnTerms = null!;
        private Button btnReports = null!;
        private Button? _activeNav;

        public SuperAdminMainForm()
        {
            InitializeComponent();

            Text = $"S.C.R.A.P — Super Admin Console — {CurrentSession.GetCompanyDisplay()}";
            Width = 1280;
            Height = 800;
            MinimumSize = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = Theme.Background;
            Font = Theme.LabelFont;

            BuildSidebar();
            BuildContentHost();

            ShowSubscribers();
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
                Text = "Super Admin Console",
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

            // Nav section
            int top = 126;

            var secTenants = CreateSectionLabel("PLATFORM MANAGEMENT", top);
            sidebar.Controls.Add(secTenants);
            top += 28;

            btnSubscribers = Theme.CreateNavButton("Subscribers & Tenancy", top);
            btnSubscribers.Click += (s, e) => ShowSubscribers();
            sidebar.Controls.Add(btnSubscribers);
            top += 44;

            btnUsers = Theme.CreateNavButton("Company Admins", top);
            btnUsers.Click += (s, e) => ShowUsers();
            sidebar.Controls.Add(btnUsers);
            top += 44;

            btnPlans = Theme.CreateNavButton("Plans & Module Gates", top);
            btnPlans.Click += (s, e) => ShowPlans();
            sidebar.Controls.Add(btnPlans);
            top += 44;

            var secReports = CreateSectionLabel("BUSINESS INTELLIGENCE", top);
            sidebar.Controls.Add(secReports);
            top += 28;

            btnReports = Theme.CreateNavButton("Business Intelligence", top);
            btnReports.Click += (s, e) => ShowReports();
            sidebar.Controls.Add(btnReports);
            top += 44;

            var secGovernance = CreateSectionLabel("LEGAL & GOVERNANCE", top);
            sidebar.Controls.Add(secGovernance);
            top += 28;

            btnTerms = Theme.CreateNavButton("Terms & Conditions", top);
            btnTerms.Click += (s, e) => ShowTerms();
            sidebar.Controls.Add(btnTerms);
            top += 44;

            // Bottom user area
            userArea = new Panel
            {
                Height = 92,
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

            var avatarLabel = new Label
            {
                Text = "SA",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            avatar.Controls.Add(avatarLabel);

            var lblUser = new Label
            {
                Text = !string.IsNullOrWhiteSpace(CurrentSession.FullName) ? CurrentSession.FullName : "Platform Super Admin",
                Left = 60,
                Top = 14,
                Width = 160,
                Height = 20,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            var lblRole = new Label
            {
                Text = "Platform Master • SuperAdmin",
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

            sidebar.Controls.Add(logoArea);
            Theme.AttachCloudSyncBadge(sidebar, userArea);

            Controls.Add(sidebar);
            SetActive(btnSubscribers);
        }

        private Label CreateSectionLabel(string text, int top)
        {
            return new Label
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
        }

        private void SetActive(Button btn)
        {
            foreach (var b in new[] { btnSubscribers, btnUsers, btnPlans, btnReports, btnTerms })
            {
                if (b != null)
                {
                    b.BackColor = Theme.SidebarBg;
                    b.ForeColor = Theme.SidebarText;
                    b.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
                }
            }

            if (btn != null)
            {
                btn.BackColor = Theme.SidebarActive;
                btn.ForeColor = Theme.SidebarTextActive;
                btn.FlatAppearance.MouseOverBackColor = Theme.SidebarActive;
            }
            _activeNav = btn;
        }

        private void BuildContentHost()
        {
            contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background
            };
            Controls.Add(contentHost);
            contentHost.BringToFront();
        }

        private void EmbedForm(Form child)
        {
            foreach (Control c in contentHost.Controls)
            {
                c.Dispose();
            }
            contentHost.Controls.Clear();
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            child.BackColor = Theme.Background;
            contentHost.Controls.Add(child);
            child.Show();
        }

        private void ShowSubscribers()
        {
            EmbedForm(new TenantManagementForm());
            SetActive(btnSubscribers);
        }

        private void ShowUsers()
        {
            EmbedForm(new SuperAdminUsersForm());
            SetActive(btnUsers);
        }

        private void ShowPlans()
        {
            EmbedForm(new SubscriptionPlansOverviewForm());
            SetActive(btnPlans);
        }

        private void ShowReports()
        {
            EmbedForm(new SuperAdminReportsForm());
            SetActive(btnReports);
        }

        private void ShowTerms()
        {
            EmbedForm(new PlatformTermsForm(true));
            SetActive(btnTerms);
        }
    }
}
