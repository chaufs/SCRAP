using System;
using System.Drawing;
using System.Windows.Forms;
using SCRAP.winforms.Forms;
using SCRAP.winforms.Forms.Technical;

namespace SCRAP.winforms
{
    /// <summary>
    /// Technical staff: Dashboard, Inventory (full + commodities tabs), Teardown.
    /// </summary>
    public class TechnicalMainForm : Form
    {
        private Panel sidebar = null!;
        private Panel contentHost = null!;
        private Panel logoArea = null!;
        private Label lblBrand = null!;
        private Label lblBrandSub = null!;
        private Panel userArea = null!;

        private Button btnDashboard = null!;
        private Button? btnInventory;
        private Button? btnTeardown;
        private Button? btnProcurement;
        private Button? btnMyPortal;
        private Button? _activeNav;

        public TechnicalMainForm()
        {
            Text = $"S.C.R.A.P — Technical Staff — {CurrentSession.GetCompanyDisplay()}";
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
                Text = "Technical Console",
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

            sidebar.Controls.Add(logoArea);

            int nextTop = 126;

            var secFloor = CreateSectionLabel("TECHNICAL OPERATIONS", nextTop);
            sidebar.Controls.Add(secFloor);
            nextTop += 28;

            btnDashboard = Theme.CreateNavButton("Dashboard", nextTop);
            btnDashboard.Click += (s, e) => { SetActive(btnDashboard); ShowDashboard(); };
            sidebar.Controls.Add(btnDashboard);
            nextTop += 44;

            if (CurrentSession.HasModuleAccess("Inventory"))
            {
                btnInventory = Theme.CreateNavButton("Inventory", nextTop);
                btnInventory.Click += (s, e) => { SetActive(btnInventory); ShowInventory(); };
                sidebar.Controls.Add(btnInventory);
                nextTop += 44;
            }

            // Teardown is accessible across all subscription plans
            btnTeardown = Theme.CreateNavButton("Teardown", nextTop);
            btnTeardown.Click += (s, e) => { SetActive(btnTeardown); ShowTeardown(); };
            sidebar.Controls.Add(btnTeardown);
            nextTop += 44;

            if (CurrentSession.HasModuleAccess("Procurement"))
            {
                btnProcurement = Theme.CreateNavButton("Procurement Arrival", nextTop);
                btnProcurement.Click += (s, e) => { SetActive(btnProcurement); ShowProcurementArrival(); };
                sidebar.Controls.Add(btnProcurement);
                nextTop += 44;
            }

            if (CurrentSession.HasModuleAccess("HR") && !string.Equals(CurrentSession.CompanyCode, "GREEN", StringComparison.OrdinalIgnoreCase))
            {
                nextTop += 10;
                var secSelf = CreateSectionLabel("MY ACCOUNT", nextTop);
                sidebar.Controls.Add(secSelf);
                nextTop += 28;

                btnMyPortal = Theme.CreateNavButton("My Portal", nextTop);
                btnMyPortal.Click += (s, e) => { SetActive(btnMyPortal); ShowMyPortal(); };
                sidebar.Controls.Add(btnMyPortal);
            }

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
                BackColor = Theme.Green
            };

            var lblUser = new Label
            {
                Text = !string.IsNullOrWhiteSpace(CurrentSession.FullName) ? CurrentSession.FullName : "Technical Staff",
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
                Text = !string.IsNullOrWhiteSpace(CurrentSession.BranchName)
                    ? $"{CurrentSession.BranchName} • Tech ({CurrentSession.CompanyCode})"
                    : $"Technical Staff ({CurrentSession.CompanyCode})",
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
            Theme.AttachCloudSyncBadge(sidebar, userArea);

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

        private void SetActive(Button? btn)
        {
            foreach (var b in new[] { btnDashboard, btnInventory, btnTeardown, btnProcurement, btnMyPortal })
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
                _activeNav = btn;
            }
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

        private void ShowDashboard() =>
            EmbedForm(new TechnicalDashboardForm { ShowInTaskbar = false });

        private void ShowInventory() =>
            EmbedForm(new TechnicalInventoryForm { ShowInTaskbar = false });

        private void ShowTeardown() =>
            EmbedForm(new TeardownForm { ShowInTaskbar = false });

        private void ShowProcurementArrival() =>
            EmbedForm(new TechProcurementArrivalForm { ShowInTaskbar = false });

        private void ShowMyPortal() =>
            EmbedForm(new EmployeePortalForm { ShowInTaskbar = false });
    }
}