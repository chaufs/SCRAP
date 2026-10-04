
using System;
using System.Drawing;
using System.Windows.Forms;
using SCRAP.winforms.Forms;
using SCRAP.winforms.Forms.Destruction;

namespace SCRAP.winforms;

/// <summary>
/// Manager shell: Dashboard + Certificate of Destruction.
/// </summary>
public class ManagerMainForm : Form
{
    private Panel sidebar = null!;
    private Panel contentHost = null!;
    private Panel logoArea = null!;
    private Label lblBrand = null!;
    private Label lblBrandSub = null!;
    private Panel userArea = null!;

    private Button btnDashboard = null!;
    private Button? btnCertificates;
    private Button? btnInventory;
    private Button? btnAttendancePayroll;
    private Button? btnFinance;
    private Button? btnProcurement;
    private Button btnMyPortal = null!;
    private Button? _activeNav;

    public ManagerMainForm()
    {
        Text = $"S.C.R.A.P — Manager — {CurrentSession.GetCompanyDisplay()}";
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
            Text = "Manager Console",
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
        var secMgmt = CreateSectionLabel("MANAGEMENT", nextTop);
        sidebar.Controls.Add(secMgmt);
        nextTop += 28;

        btnDashboard = Theme.CreateNavButton("Dashboard", nextTop);
        btnDashboard.Click += (s, e) => { SetActive(btnDashboard); ShowDashboard(); };
        sidebar.Controls.Add(btnDashboard);
        nextTop += 44;

        // Destruction of storage devices & certificate generation is available to Manager in all subscription plans
        btnCertificates = Theme.CreateNavButton("Certificates", nextTop);
        btnCertificates.Click += (s, e) => { SetActive(btnCertificates); ShowCertificates(); };
        sidebar.Controls.Add(btnCertificates);
        nextTop += 44;

        if (CurrentSession.HasModuleAccess("Inventory"))
        {
            btnInventory = Theme.CreateNavButton("Inventory", nextTop);
            btnInventory.Click += (s, e) => { SetActive(btnInventory); ShowInventory(); };
            sidebar.Controls.Add(btnInventory);
            nextTop += 44;
        }


        if (CurrentSession.HasModuleAccess("Procurement"))
        {
            btnProcurement = Theme.CreateNavButton("Procurement", nextTop);
            btnProcurement.Click += (s, e) => { SetActive(btnProcurement); ShowProcurement(); };
            sidebar.Controls.Add(btnProcurement);
            nextTop += 44;
        }

        if (CurrentSession.HasModuleAccess("HR"))
        {
            btnAttendancePayroll = Theme.CreateNavButton("Staff & Payroll", nextTop);
            btnAttendancePayroll.Click += (s, e) => { SetActive(btnAttendancePayroll); ShowAttendancePayroll(); };
            sidebar.Controls.Add(btnAttendancePayroll);
            nextTop += 44;
        }

        if (CurrentSession.HasModuleAccess("Finance"))
        {
            btnFinance = Theme.CreateNavButton("Company Finance", nextTop);
            btnFinance.Click += (s, e) => { SetActive(btnFinance); ShowFinance(); };
            sidebar.Controls.Add(btnFinance);
            nextTop += 44;
        }

        if (CurrentSession.HasModuleAccess("HR") && !string.Equals(CurrentSession.CompanyCode, "GREEN", StringComparison.OrdinalIgnoreCase))
        {
            var secSelf = CreateSectionLabel("MY ACCOUNT", nextTop);
            sidebar.Controls.Add(secSelf);
            nextTop += 28;

            btnMyPortal = Theme.CreateNavButton("My Portal", nextTop);
            btnMyPortal.Click += (s, e) => { SetActive(btnMyPortal); ShowMyPortal(); };
            sidebar.Controls.Add(btnMyPortal);
            nextTop += 44;
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
            Text = !string.IsNullOrWhiteSpace(CurrentSession.FullName) ? CurrentSession.FullName : "Manager",
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
                ? $"{CurrentSession.BranchName} • Manager ({CurrentSession.CompanyCode})" 
                : $"Manager ({CurrentSession.CompanyCode})",
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
            BackColor = Color.Transparent,
            UseMnemonic = false
        };
    }

    private void SetActive(Button? btn)
    {
        foreach (var b in new[] { btnDashboard, btnCertificates, btnInventory, btnAttendancePayroll, btnFinance, btnProcurement, btnMyPortal })
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
        contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(0)
        };
        Controls.Add(contentHost);
        contentHost.BringToFront();
    }

    private void ClearContent()
    {
        contentHost.Controls.Clear();
    }

    private void EmbedForm(Form child)
    {
        ClearContent();
        child.TopLevel = false;
        child.FormBorderStyle = FormBorderStyle.None;
        child.Dock = DockStyle.Fill;
        child.BackColor = Theme.Background;
        contentHost.Controls.Add(child);
        child.Show();
    }

    private void ShowDashboard()
    {
        var dash = new ManagerDashboardForm { ShowInTaskbar = false };
        dash.NavigateRequested = target =>
        {
            if (target == "Certificates")
            {
                ShowCertificates();
            }
            else if (target == "Inventory")
            {
                if (btnInventory != null)
                {
                    SetActive(btnInventory);
                    ShowInventory();
                }
            }
        };
        EmbedForm(dash);
    }

    public void ShowCertificates()
    {
        SetActive(btnCertificates);
        EmbedForm(new CertificateOfDestructionForm { ShowInTaskbar = false });
    }

    private void ShowInventory()
    {
        // Manager: view-only inventory overview
        EmbedForm(new ManagerInventoryForm() { ShowInTaskbar = false });
    }

    private void ShowAttendancePayroll()
    {
        EmbedForm(new AttendancePayrollForm { ShowInTaskbar = false });
    }

    private void ShowFinance()
    {
        EmbedForm(new CompanyFinanceForm { ShowInTaskbar = false });
    }

    private void ShowProcurement()
    {
        EmbedForm(new ProcurementForm { ShowInTaskbar = false });
    }

    private void ShowMyPortal()
    {
        EmbedForm(new EmployeePortalForm { ShowInTaskbar = false });
    }
}