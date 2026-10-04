using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms
{
    [DesignerCategory("Code")]
    public class LoginForm : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Label lblError = null!;
        private Panel leftPanel = null!;
        private Panel formStack = null!;
        private Panel welcomeStack = null!;

        private static readonly Color Ink = Color.FromArgb(11, 18, 32);
        private static readonly Color Paper = Color.FromArgb(247, 248, 250);
        private static readonly Color Fg = Color.FromArgb(15, 23, 42);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color Subtle = Color.FromArgb(148, 163, 184);
        private static readonly Color Accent = Color.FromArgb(7, 122, 94);    
        private static readonly Color AccentDeep = Color.FromArgb(5, 96, 74);   
        private static readonly Color AccentFg = Color.FromArgb(236, 253, 245);
        private static readonly Color Danger = Color.FromArgb(180, 35, 24);

        public LoginForm()
        {
            Text = "S.C.R.A.P — Login";
            if (Theme.AppLogo != null)
            {
                try
                {
                    using var bmp = new Bitmap(Theme.AppLogo);
                    Icon = Icon.FromHandle(bmp.GetHicon());
                }
                catch { }
            }
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(960, 600);
            DoubleBuffered = true;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            KeyPreview = true;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
            BuildUi();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = Padding.Empty
            };

            formStack = new Panel { Width = 380, Height = 415, BackColor = Color.White };

            var logoBox = new PictureBox
            {
                Left = 0,
                Top = 0,
                Width = 260,
                Height = 86,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = Theme.AppLogo
            };
            formStack.Controls.Add(logoBox);

            formStack.Controls.Add(new Label
            {
                Text = "Sign in",
                Left = 0,
                Top = 98,
                Width = 380,
                Height = 38,
                AutoSize = false,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Fg,
                BackColor = Color.Transparent
            });
            formStack.Controls.Add(new Label
            {
                Text = "REUSING RESOURCES FOR A GREENER FUTURE",
                Left = 0,
                Top = 140,
                Width = 380,
                Height = 20,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Muted,
                BackColor = Color.Transparent
            });

            formStack.Controls.Add(FieldLabel("Username", 172));
            var userHost = FieldHost(196);
            txtUsername = InnerBox(userHost, password: false);
            formStack.Controls.Add(userHost);

            formStack.Controls.Add(FieldLabel("Password", 254));
            var passHost = FieldHost(278);
            txtPassword = InnerBox(passHost, password: true);
            formStack.Controls.Add(passHost);
            txtPassword.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                await DoLogin();
            };

            lblError = new Label
            {
                Left = 0,
                Top = 332,
                Width = 380,
                Height = 22,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Danger,
                BackColor = Color.Transparent,
                Text = ""
            };
            formStack.Controls.Add(lblError);

            btnLogin = new Button
            {
                Text = "Sign in",
                Left = 0,
                Top = 358,
                Width = 380,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = AccentDeep;
            btnLogin.Click += async (_, __) => await DoLogin();
            formStack.Controls.Add(btnLogin);

            leftPanel.Controls.Add(formStack);
            leftPanel.Resize += (_, __) => CenterForm();

            var rightPanel = new AccentPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            welcomeStack = new Panel
            {
                Width = 420,
                Height = 260,
                BackColor = Color.Transparent
            };
            welcomeStack.Controls.Add(new Label
            {
                Text = "OPERATIONS CONSOLE",
                Left = 0,
                Top = 0,
                Width = 420,
                Height = 22,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 252, 231),
                BackColor = Color.Transparent
            });
            welcomeStack.Controls.Add(new Label
            {
                Text = "Welcome back.",
                Left = 0,
                Top = 36,
                Width = 420,
                Height = 70,
                Font = new Font("Segoe UI", 34f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            });
            welcomeStack.Controls.Add(new Label
            {
                Text = "Sign in to continue to inventory, teardown, certificates, and floor operations.",
                Left = 0,
                Top = 116,
                Width = 400,
                Height = 64,
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(220, 252, 231),
                BackColor = Color.Transparent
            });
            welcomeStack.Controls.Add(new Label
            {
                Text = "Inventory    ·    Teardown    ·    Certificates",
                Left = 0,
                Top = 210,
                Width = 420,
                Height = 22,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(200, 240, 220),
                BackColor = Color.Transparent
            });
            rightPanel.Controls.Add(welcomeStack);
            rightPanel.Resize += (_, __) => CenterWelcome();

            root.Controls.Add(leftPanel, 0, 0);
            root.Controls.Add(rightPanel, 1, 0);
            Controls.Add(root);

            Shown += (_, __) =>
            {
                CenterForm();
                CenterWelcome();
                txtUsername.Focus();
            };
        }

        private void CenterForm()
        {
            if (leftPanel == null || formStack == null) return;
            formStack.Left = Math.Max(36, (leftPanel.ClientSize.Width - formStack.Width) / 2);
            formStack.Top = Math.Max(24, (leftPanel.ClientSize.Height - formStack.Height) / 2);
        }

        private void CenterWelcome()
        {
            if (welcomeStack?.Parent == null) return;
            var p = welcomeStack.Parent;
            welcomeStack.Left = Math.Max(40, (p.ClientSize.Width - welcomeStack.Width) / 2);
            welcomeStack.Top = Math.Max(40, (p.ClientSize.Height - welcomeStack.Height) / 2);
        }

        private static Label FieldLabel(string text, int top) => new Label
        {
            Text = text,
            Left = 0,
            Top = top,
            Width = 380,
            Height = 20,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Fg,
            BackColor = Color.Transparent
        };

        private static Panel FieldHost(int top)
        {
            var host = new Panel
            {
                Left = 0,
                Top = top,
                Width = 380,
                Height = 48,
                BackColor = Paper,
                Padding = new Padding(14, 12, 14, 12)
            };
            host.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240));
                e.Graphics.DrawRectangle(pen, 0, 0, host.Width - 1, host.Height - 1);
            };
            return host;
        }

        private static TextBox InnerBox(Panel host, bool password)
        {
            var box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f),
                BackColor = Paper,
                ForeColor = Fg
            };
            if (password) box.PasswordChar = '•';
            host.Controls.Add(box);
            return box;
        }

        private async Task DoLogin()
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblError.Text = "Enter both username and password.";
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Signing in…";
            try
            {
                var res = await ApiConfig.Http.PostAsJsonAsync("api/Auth/login", new
                {
                    Username = txtUsername.Text.Trim(),
                    Password = txtPassword.Text
                });

                if (res.IsSuccessStatusCode)
                {
                    var result = await res.Content.ReadFromJsonAsync<LoginResult>();
                    if (result != null)
                    {
                        // Auto-switch to the resolved company code from the credentials
                        if (!string.IsNullOrWhiteSpace(result.CompanyCode))
                        {
                            ApiConfig.SetCompanyCode(result.CompanyCode);
                        }

                        CurrentSession.UserId = result.UserId;
                        CurrentSession.EmployeeId = result.EmployeeId;
                        CurrentSession.Username = result.Username;
                        CurrentSession.Role = result.Role;
                        CurrentSession.FullName = result.FullName;
                        CurrentSession.BranchId = result.BranchId;
                        CurrentSession.BranchName = result.BranchName ?? "";
                        CurrentSession.CompanyCode = ApiConfig.CompanyCode;
                        CurrentSession.CompanyName = result.CompanyName ?? "";
                        CurrentSession.EnabledModules = result.EnabledModules ?? "";
                        ApiConfig.SetApiUser(result.Username);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                }
                else
                {
                    string errorMsg = "Invalid username or password.";
                    try
                    {
                        var err = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
                        if (!string.IsNullOrWhiteSpace(err?.Message))
                            errorMsg = err.Message;
                    }
                    catch { }

                    lblError.Text = errorMsg;
                    if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        MessageBox.Show(this, errorMsg, "Account / Subscription Suspended", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch
            {
                lblError.Text = "Cannot connect to server.";
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Sign in";
            }
        }


        private class LoginResult
        {
            public int UserId { get; set; }
            public int? EmployeeId { get; set; }
            public string Username { get; set; } = "";
            public string Role { get; set; } = "";
            public int? BranchId { get; set; }
            public string? BranchName { get; set; }
            public string FullName { get; set; } = "";
            public string EnabledModules { get; set; } = "";
            public string CompanyCode { get; set; } = "";
            public string? CompanyName { get; set; }
        }

        private class ApiErrorResponse
        {
            public string? Message { get; set; }
        }

        private sealed class AccentPanel : Panel
        {
            public AccentPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                BackColor = Accent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var r = ClientRectangle;
                if (r.Width <= 0 || r.Height <= 0) return;

                using var brush = new LinearGradientBrush(
                    r,
                    Color.FromArgb(8, 128, 100),
                    Color.FromArgb(5, 90, 72),
                    135f);

                using var grid = new Pen(Color.FromArgb(36, 255, 255, 255), 1);
                for (int x = 0; x < r.Width; x += 48)
                    e.Graphics.DrawLine(grid, x, 0, x, r.Height);
                for (int y = 0; y < r.Height; y += 48)
                    e.Graphics.DrawLine(grid, 0, y, r.Width, y);
            }
        }
    }

    public static class CurrentSession
    {
        public static int UserId { get; set; }
        public static int? EmployeeId { get; set; }
        public static string FullName { get; set; } = "";
        public static string Username { get; set; } = "";
        public static string Role { get; set; } = "";
        public static int? BranchId { get; set; }
        public static string BranchName { get; set; } = "";
        public static string CompanyCode { get; set; } = "";
        public static string CompanyName { get; set; } = "";
        public static string EnabledModules { get; set; } = "";
        public static bool LogoutRequested { get; set; }
        
        public static string GetCompanyDisplay()
        {
            if (!string.IsNullOrWhiteSpace(CompanyName) && !string.IsNullOrWhiteSpace(CompanyCode) && !CompanyName.Equals(CompanyCode, StringComparison.OrdinalIgnoreCase))
            {
                return $"{CompanyName} ({CompanyCode})";
            }
            if (!string.IsNullOrWhiteSpace(CompanyName))
            {
                return CompanyName;
            }
            if (string.IsNullOrWhiteSpace(CompanyCode) || CompanyCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase))
            {
                return "Platform Administration";
            }
            if (!string.IsNullOrWhiteSpace(CompanyName))
            {
                return $"{CompanyName} ({CompanyCode})";
            }
            return CompanyCode.ToUpperInvariant();
        }

        public static bool HasModuleAccess(string moduleName)
        {
            if (string.Equals(Role, "Superadmin", StringComparison.OrdinalIgnoreCase)) return true;
            // Technical / Teardown is accessible across all subscription plans
            if (string.Equals(moduleName, "Technical", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrWhiteSpace(EnabledModules)) return false;
            if (EnabledModules.Equals("All", StringComparison.OrdinalIgnoreCase)) return true;
            return EnabledModules.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(m => m.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
        }
    }
}