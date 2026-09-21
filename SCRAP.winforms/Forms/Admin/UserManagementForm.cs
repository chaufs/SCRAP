using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SCRAP.domain.entities;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class UserManagementForm : Form
    {
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnRefresh = null!;

        private Panel cardList = null!;
        private DataGridView dgvUsers = null!;

        private Panel formPanel = null!;
        private TextBox txtFirstName = null!;
        private TextBox txtMiddleName = null!;
        private TextBox txtLastName = null!;
        private TextBox txtEmail = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private ComboBox cmbRole = null!;
        private CheckBox chkActive = null!;

        private Button btnSave = null!;
        private Button btnNew = null!;
        private Button btnDeactivate = null!;

        private int? _editingId;

        // Roles Admin is allowed to assign — Admin/Superadmin excluded
        private static readonly UserRole[] AssignableRoles =
        {
            UserRole.Manager,
            UserRole.TechStaff,
            UserRole.SalesStaff
        };

        public UserManagementForm()
        {
            Text = "User Management";
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "User Management",
                Left = 32,
                Top = 28,
                Width = 420,
                Height = 36,
                Font = Theme.TitleFont,
                ForeColor = Theme.DarkText,
                BackColor = Color.Transparent
            };

            lblSubtitle = new Label
            {
                Text = "Create staff accounts and assign roles",
                Left = 32,
                Top = 64,
                Width = 520,
                Height = 22,
                Font = Theme.SubtitleFont,
                ForeColor = Theme.MutedText,
                BackColor = Color.Transparent
            };

            btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Width = 110,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadUsers();

            // ----- user list -----
            cardList = MakeCard();
            dgvUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            Theme.StyleGrid(dgvUsers);
            dgvUsers.SelectionChanged += (s, e) => LoadSelectedIntoForm();
            cardList.Controls.Add(dgvUsers);

            // ----- edit form -----
            formPanel = new Panel { BackColor = Theme.Background, Padding = new Padding(8) };

            var lblFirst = MakeFieldLabel("First Name", 8, 8);
            txtFirstName = MakeText(8, 28, 180, "First name");

            var lblMid = MakeFieldLabel("Middle Name", 196, 8);
            txtMiddleName = MakeText(196, 28, 130, "Middle name (optional)");

            var lblLast = MakeFieldLabel("Last Name", 336, 8);
            txtLastName = MakeText(336, 28, 180, "Last name");

            var lblEmail = MakeFieldLabel("Email", 524, 8);
            txtEmail = MakeText(524, 28, 220, "name@example.com");

            var lblUsername = MakeFieldLabel("Username", 8, 68);
            txtUsername = MakeText(8, 88, 180, "Username");

            var lblPassword = MakeFieldLabel("Password", 196, 68);
            txtPassword = new TextBox { Left = 196, Top = 88, Width = 180, PasswordChar = '•', PlaceholderText = "Leave blank to keep unchanged" };
            Theme.StyleTextBox(txtPassword);

            var lblRole = MakeFieldLabel("Role", 384, 68);
            cmbRole = new ComboBox { Left = 384, Top = 88, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbRole);
            cmbRole.DataSource = AssignableRoles;

            chkActive = new CheckBox { Text = "Active", Left = 552, Top = 90, Width = 100, Checked = true };

            btnSave = new Button { Text = "Save User", Left = 8, Top = 132, Width = 140, Height = 34 };
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += async (s, e) => await SaveUser();

            btnNew = new Button { Text = "New / Clear", Left = 158, Top = 132, Width = 130, Height = 34 };
            Theme.StyleOutlineButton(btnNew);
            btnNew.Click += (s, e) => ClearForm();

            btnDeactivate = new Button { Text = "Deactivate Selected", Left = 298, Top = 132, Width = 170, Height = 34 };
            Theme.StyleOutlineButton(btnDeactivate);
            btnDeactivate.Click += async (s, e) => await DeactivateSelected();

            formPanel.Controls.AddRange(new Control[]
            {
                lblFirst, txtFirstName, lblMid, txtMiddleName, lblLast, txtLastName, lblEmail, txtEmail,
                lblUsername, txtUsername, lblPassword, txtPassword, lblRole, cmbRole, chkActive,
                btnSave, btnNew, btnDeactivate
            });

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(btnRefresh);
            Controls.Add(cardList);
            Controls.Add(formPanel);

            Resize += (s, e) => LayoutPage();
            LayoutPage();

            HandleCreated += async (s, e) => await LoadUsers();
        }

        private TextBox MakeText(int left, int top, int width, string placeholder)
        {
            var t = new TextBox { Left = left, Top = top, Width = width, PlaceholderText = placeholder };
            Theme.StyleTextBox(t);
            return t;
        }

        private Label MakeFieldLabel(string text, int left, int top) => new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 180,
            Height = 18,
            Font = Theme.StatLabelFont,
            ForeColor = Theme.MutedText,
            BackColor = Color.Transparent
        };

        private Panel MakeCard()
        {
            var p = new Panel { BackColor = Theme.White };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private void LayoutPage()
        {
            btnRefresh.Left = ClientSize.Width - btnRefresh.Width - 32;
            btnRefresh.Top = 36;

            int formHeight = 190;
            int left = 32;
            int width = Math.Max(500, ClientSize.Width - 64);

            cardList.Left = left;
            cardList.Top = 132;
            cardList.Width = width;
            cardList.Height = Math.Max(180, ClientSize.Height - 132 - formHeight - 32);

            formPanel.Left = left;
            formPanel.Top = cardList.Top + cardList.Height + 12;
            formPanel.Width = width;
            formPanel.Height = formHeight;
        }

        private async Task LoadUsers()
        {
            try
            {
                var res = await ApiConfig.Http.GetAsync("api/Users");
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<List<UserManagement>>(ApiConfig.JsonOptions);
                    dgvUsers.DataSource = null;
                    dgvUsers.DataSource = data;
                    if (dgvUsers.Columns.Count > 0) ConfigureColumns();
                }
                else
                {
                    MessageBox.Show("Error loading users: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureColumns()
        {
            Theme.ConfigureColumns(
                dgvUsers,
                new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FullName"] = ("Full Name", 220),
                    ["Username"] = ("Username", 140),
                    ["Email"] = ("Email", 200),
                    ["Role"] = ("Role", 120),
                    ["IsActive"] = ("Active", 80)
                },
                "Id", "FirstName", "MiddleInitial", "LastName", "PasswordHash");
        }

        private void LoadSelectedIntoForm()
        {
            if (dgvUsers.CurrentRow?.DataBoundItem is not UserManagement user) return;

            _editingId = user.Id;
            txtFirstName.Text = user.FirstName;
            txtMiddleName.Text = user.MiddleName ?? "";
            txtLastName.Text = user.LastName;
            txtEmail.Text = user.Email;
            txtUsername.Text = user.Username;
            txtPassword.Text = "";
            cmbRole.SelectedItem = user.Role;
            chkActive.Checked = user.IsActive;
            btnSave.Text = "Update User";
        }

        private void ClearForm()
        {
            _editingId = null;
            txtFirstName.Clear();
            txtMiddleName.Clear();
            txtLastName.Clear();
            txtEmail.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
            if (cmbRole.Items.Count > 0) cmbRole.SelectedIndex = 0;
            chkActive.Checked = true;
            dgvUsers.ClearSelection();
            btnSave.Text = "Save User";
        }

        private async Task SaveUser()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("First name, last name, username, and email are required.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_editingId is null && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Password is required for a new user.", "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (cmbRole.SelectedItem is not UserRole role)
            {
                MessageBox.Show("Select a role.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var passwordHash = string.IsNullOrWhiteSpace(txtPassword.Text)
                ? null
                : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(txtPassword.Text));

            var payload = new
            {
                EmployeeId = (int)cmbEmployee.SelectedValue!,
                Username = txtUsername.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Password = txtPassword.Text,   // ← plain text now, controller encodes it
                Role = role,
                IsActive = chkActive.Checked
            };

            var res = _editingId is null
                ? await ApiConfig.Http.PostAsJsonAsync("api/Users", payload)
                : await ApiConfig.Http.PutAsJsonAsync($"api/Users/{_editingId}", new
                {
                    Username = txtUsername.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Password = string.IsNullOrWhiteSpace(txtPassword.Text) ? null : txtPassword.Text,
                    Role = role,
                    IsActive = chkActive.Checked
                });

            btnSave.Enabled = false;
            try
            {
                var res = _editingId is null
                    ? await ApiConfig.Http.PostAsJsonAsync("api/Users", payload)
                    : await ApiConfig.Http.PutAsJsonAsync($"api/Users/{_editingId}", payload);

                if (res.IsSuccessStatusCode)
                {
                    ClearForm();
                    await LoadUsers();
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Save failed: " + res.StatusCode + "\n" + body, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving user: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        private async Task DeactivateSelected()
        {
            if (dgvUsers.CurrentRow?.DataBoundItem is not UserManagement user)
            {
                MessageBox.Show("Select a user first.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (user.Role == UserRole.Admin)
            {
                MessageBox.Show("Admin accounts cannot be managed here.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Deactivate {user.FirstName} {user.LastName}?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                var payload = new
                {
                    Id = user.Id,
                    user.FirstName,
                    user.MiddleName,
                    user.LastName,
                    user.Email,
                    user.Username,
                    PasswordHash = (string?)null,
                    user.Role,
                    IsActive = false
                };

                var res = await ApiConfig.Http.PutAsJsonAsync($"api/Users/{user.Id}", payload);
                if (res.IsSuccessStatusCode)
                {
                    ClearForm();
                    await LoadUsers();
                }
                else
                {
                    MessageBox.Show("Deactivate failed: " + res.StatusCode, "S.C.R.A.P",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deactivating user: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}