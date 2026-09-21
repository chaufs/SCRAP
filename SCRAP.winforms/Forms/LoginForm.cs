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

        public LoginForm()
        {
            Text = "S.C.R.A.P — Login";
            Width = 860;
            Height = 520;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(240, 242, 245);
            DoubleBuffered = true;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // ── Outer card ──────────────────────────────────────────────
            var card = new Panel
            {
                Width = 780,
                Height = 440,
                Left = (ClientSize.Width - 780) / 2,
                Top = (ClientSize.Height - 440) / 2,
                BackColor = Color.White
            };
            // Center the card when form resizes (fixed size, but safe)
            card.Left = 40;
            card.Top = 40;

            // ── Left panel (form) ───────────────────────────────────────
            var leftPanel = new Panel
            {
                Left = 0,
                Top = 0,
                Width = 390,
                Height = 440,
                BackColor = Color.White
            };

            var lblTitle = new Label
            {
                Text = "Sign in",
                Left = 48,
                Top = 70,
                Width = 290,
                Height = 42,
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.Transparent
            };

            var lblUser = new Label
            {
                Text = "Username",
                Left = 48,
                Top = 140,
                Width = 290,
                Height = 20,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.Transparent
            };

            txtUsername = new TextBox
            {
                Left = 48,
                Top = 162,
                Width = 290,
                Height = 42,
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            // Soft border look
            txtUsername.BorderStyle = BorderStyle.None;
            var userWrapper = CreateInputWrapper(48, 162, 290, 42);
            userWrapper.Controls.Add(txtUsername);
            txtUsername.Dock = DockStyle.Fill;
            txtUsername.Padding = new Padding(12, 10, 12, 10);

            var lblPass = new Label
            {
                Text = "Password",
                Left = 48,
                Top = 220,
                Width = 290,
                Height = 20,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.Transparent
            };

            txtPassword = new TextBox
            {
                Left = 0,
                Top = 0,
                Width = 290,
                Height = 42,
                Font = new Font("Segoe UI", 11f),
                PasswordChar = '•',
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            var passWrapper = CreateInputWrapper(48, 242, 290, 42);
            passWrapper.Controls.Add(txtPassword);
            txtPassword.Dock = DockStyle.Fill;

            txtPassword.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await DoLogin();
                }
            };

            lblError = new Label
            {
                Left = 48,
                Top = 295,
                Width = 290,
                Height = 22,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(185, 28, 28),
                BackColor = Color.Transparent,
                Text = ""
            };

            btnLogin = new Button
            {
                Text = "Sign in",
                Left = 48,
                Top = 330,
                Width = 290,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(16, 185, 129),   // emerald
                ForeColor = Color.White
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(5, 150, 105);
            btnLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(4, 120, 87);
            btnLogin.Click += async (s, e) => await DoLogin();

            leftPanel.Controls.Add(lblTitle);
            leftPanel.Controls.Add(lblUser);
            leftPanel.Controls.Add(userWrapper);
            leftPanel.Controls.Add(lblPass);
            leftPanel.Controls.Add(passWrapper);
            leftPanel.Controls.Add(lblError);
            leftPanel.Controls.Add(btnLogin);

            // ── Right panel (welcome) ───────────────────────────────────
            var rightPanel = new Panel
            {
                Left = 390,
                Top = 0,
                Width = 390,
                Height = 440,
                BackColor = Color.FromArgb(16, 185, 129)
            };

            // Soft gradient feel (simple solid + overlay)
            var gradientOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(5, 150, 105)
            };
            // We keep a solid green; WinForms gradients need custom paint.
            // Using a single modern green is clean and professional.

            var lblWelcome = new Label
            {
                Text = "Welcome back!",
                Left = 40,
                Top = 140,
                Width = 310,
                Height = 40,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblWelcomeSub = new Label
            {
                Text = "Sign in to continue to the S.C.R.A.P console.\nManage inventory, operations, and more.",
                Left = 40,
                Top = 195,
                Width = 310,
                Height = 70,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(220, 252, 231),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter
            };

            rightPanel.Controls.Add(lblWelcome);
            rightPanel.Controls.Add(lblWelcomeSub);

            // Assemble
            card.Controls.Add(leftPanel);
            card.Controls.Add(rightPanel);
            Controls.Add(card);

            // Focus
            ActiveControl = txtUsername;
        }

        private Panel CreateInputWrapper(int left, int top, int width, int height)
        {
            var wrapper = new Panel
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 10, 12, 10)
            };

            // Subtle border via paint
            wrapper.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, wrapper.Width - 1, wrapper.Height - 1);
            };

            return wrapper;
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
            btnLogin.Text = "Signing in...";

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
                        CurrentSession.UserId = result.UserId;
                        CurrentSession.Username = result.Username;
                        CurrentSession.Role = result.Role;
                        ApiConfig.SetApiUser(result.Username);

                        DialogResult = DialogResult.OK;
                        Close();
                    }
                }
                else
                {
                    lblError.Text = "Invalid username or password.";
                }
            }
            catch (Exception ex)
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
            public string Username { get; set; } = "";
            public string Role { get; set; } = "";
        }
    }

    public static class CurrentSession
    {
        public static int UserId { get; set; }
        public static string FullName { get; set; } = "";
        public static string Username { get; set; } = "";
        public static string Role { get; set; } = "";
        public static bool LogoutRequested { get; set; }
    }
}