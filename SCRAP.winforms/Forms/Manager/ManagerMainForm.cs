
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
    private Button btnCertificates = null!;
    private Button btnInventory = null!;
    private Button? _activeNav;

    public ManagerMainForm()
    {
        Text = "S.C.R.A.P — Manager";
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
            Text = "Manager Console",
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

        var secMgmt = CreateSectionLabel("MANAGEMENT", 100);

        btnDashboard = Theme.CreateNavButton("Dashboard", 128);
        btnDashboard.Click += (s, e) => { SetActive(btnDashboard); ShowDashboard(); };

        btnCertificates = Theme.CreateNavButton("Certificates", 172);
        btnCertificates.Click += (s, e) => { SetActive(btnCertificates); ShowCertificates(); };

        btnInventory = Theme.CreateNavButton("Inventory", 216);
        btnInventory.Click += (s, e) => { SetActive(btnInventory); ShowInventory(); };

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
            Text = "Manager",
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
            Text = "Facility Manager",
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
        sidebar.Controls.Add(secMgmt);
        sidebar.Controls.Add(btnDashboard);
        sidebar.Controls.Add(btnCertificates);
        sidebar.Controls.Add(btnInventory);
        sidebar.Controls.Add(userArea);

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
            BackColor = Color.Transparent
        };
    }

    private void SetActive(Button btn)
    {
        foreach (var b in new[] { btnDashboard, btnCertificates, btnInventory })
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
        EmbedForm(new ManagerDashboardForm { ShowInTaskbar = false });
    }

    private void ShowCertificates()
    {
        EmbedForm(new CertificateOfDestructionForm { ShowInTaskbar = false });
    }

    private void ShowInventory()
    {
        // Manager: view-only inventory overview
        EmbedForm(new ManagerInventoryForm() { ShowInTaskbar = false });
    }
}