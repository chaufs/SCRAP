using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SCRAP.winforms.Forms.Admin
{
    [DesignerCategory("Code")]
    public class BranchManagementForm : Form
    {
        private DataGridView grid = null!;
        private Button btnNew    = null!;
        private Button btnEdit   = null!;
        private Button btnAssign = null!;
        private Button btnRefresh = null!;
        private Label lblTitle   = null!;

        private List<BranchDto> _branches = new();

        public BranchManagementForm()
        {
            Text = "Branch Management";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;
            InitializeComponent();
            _ = LoadData();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label
            {
                Text = "Branch Management",
                Left = 32, Top = 28, Width = 600, Height = 40,
                Font = Theme.TitleFont, ForeColor = Theme.DarkText,
                BackColor = Color.Transparent, AutoSize = false
            };

            var lblSub = new Label
            {
                Text = "Create branches and assign staff",
                Left = 32, Top = 68, Width = 500, Height = 22,
                Font = Theme.SubtitleFont, ForeColor = Theme.MutedText,
                BackColor = Color.Transparent, AutoSize = false
            };

            btnNew = new Button { Text = "+ New Branch", Width = 130, Height = 34 };
            Theme.StylePrimaryButton(btnNew);
            btnNew.Click += async (s, e) => await OpenBranchDialog(null);

            btnEdit = new Button { Text = "Edit", Width = 80, Height = 34 };
            Theme.StyleOutlineButton(btnEdit);
            btnEdit.Click += async (s, e) => await EditSelected();

            btnAssign = new Button { Text = "Assign Staff", Width = 110, Height = 34 };
            Theme.StyleOutlineButton(btnAssign);
            btnAssign.Click += async (s, e) => await OpenAssignDialog();

            btnRefresh = new Button { Text = "↻  Refresh", Width = 110, Height = 34 };
            Theme.StyleOutlineButton(btnRefresh);
            btnRefresh.Click += async (s, e) => await LoadData();

            grid = new DataGridView
            {
                Left = 32, Top = 110,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Theme.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.White,
                    ForeColor = Theme.DarkText,
                    SelectionBackColor = Theme.SoftBlue,
                    SelectionForeColor = Theme.DarkText
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.White,
                    ForeColor = Theme.MutedText,
                    Font = Theme.StatLabelFont
                },
                EnableHeadersVisualStyles = false
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id",      HeaderText = "ID",      Width = 50, FillWeight = 5 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code",    HeaderText = "Code",    FillWeight = 15 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",    HeaderText = "Name",    FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Address", HeaderText = "Address", FillWeight = 35 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Active",  HeaderText = "Active",  FillWeight = 10 });

            Controls.Add(lblTitle);
            Controls.Add(lblSub);
            Controls.Add(btnNew);
            Controls.Add(btnEdit);
            Controls.Add(btnAssign);
            Controls.Add(btnRefresh);
            Controls.Add(grid);

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private void LayoutControls()
        {
            int bx = Math.Max(0, ClientSize.Width - 32 - 130);
            btnNew.Left    = bx;     btnNew.Top    = 28;
            btnEdit.Left   = bx - 90;  btnEdit.Top  = 28;
            btnAssign.Left = bx - 210; btnAssign.Top = 28;
            btnRefresh.Left = bx - 330; btnRefresh.Top = 28;

            grid.Left   = 32;
            grid.Top    = 110;
            grid.Width  = ClientSize.Width - 64;
            grid.Height = ClientSize.Height - 130;
        }

        private async Task LoadData()
        {
            try
            {
                var branches = await ApiConfig.Http.GetFromJsonAsync<List<BranchDto>>(
                    "api/Branches", ApiConfig.JsonOptions);
                _branches = branches ?? new List<BranchDto>();

                grid.Rows.Clear();
                foreach (var b in _branches)
                    grid.Rows.Add(b.Id, b.Code, b.Name, b.Address ?? "", b.IsActive ? "Yes" : "No");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading branches: " + ex.Message, "S.C.R.A.P",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private BranchDto? SelectedBranch()
        {
            if (grid.SelectedRows.Count == 0) return null;
            var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
            return _branches.Find(b => b.Id == id);
        }

        private async Task EditSelected()
        {
            var branch = SelectedBranch();
            if (branch is null) { MessageBox.Show("Select a branch first.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            await OpenBranchDialog(branch);
        }

        private async Task OpenBranchDialog(BranchDto? existing)
        {
            using var dlg = new BranchEditDialog(existing);
            if (dlg.ShowDialog() == DialogResult.OK)
                await LoadData();
        }

        private async Task OpenAssignDialog()
        {
            var branch = SelectedBranch();
            if (branch is null) { MessageBox.Show("Select a branch first.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using var dlg = new BranchAssignDialog(branch.Id, branch.Name);
            if (dlg.ShowDialog() == DialogResult.OK)
                await LoadData();
        }

        public class BranchDto
        {
            public int Id { get; set; }
            public string Name    { get; set; } = string.Empty;
            public string Code    { get; set; } = string.Empty;
            public string? Address { get; set; }
            public bool IsActive  { get; set; }
        }
    }

    // ─── Branch create/edit dialog ────────────────────────────────────────────

    [DesignerCategory("Code")]
    public class BranchEditDialog : Form
    {
        private readonly BranchManagementForm.BranchDto? _existing;
        private TextBox txtName    = null!;
        private TextBox txtCode    = null!;
        private TextBox txtAddress = null!;
        private CheckBox chkActive = null!;
        private Button btnSave     = null!;
        private Button btnCancel   = null!;

        public BranchEditDialog(BranchManagementForm.BranchDto? existing)
        {
            _existing = existing;
            Text = existing is null ? "New Branch" : $"Edit — {existing.Name}";
            Width = 400; Height = 300;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = Theme.Background;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            int top = 20;

            var lblName = MakeLabel("Branch Name *", top);
            txtName = MakeText(top + 20);
            top += 56;

            var lblCode = MakeLabel("Branch Code (Auto-generated)", top);
            txtCode = MakeText(top + 20);
            txtCode.ReadOnly = true;
            txtCode.BackColor = Theme.SoftBlue;
            top += 56;

            var lblAddr = MakeLabel("Address", top);
            txtAddress = MakeText(top + 20);
            top += 56;

            chkActive = new CheckBox { Text = "Active", Left = 20, Top = top, Width = 200, Checked = true };

            if (_existing is not null)
            {
                txtName.Text    = _existing.Name;
                txtCode.Text    = _existing.Code;
                txtAddress.Text = _existing.Address ?? "";
                chkActive.Checked = _existing.IsActive;
            }
            else
            {
                txtCode.Text = "Loading...";
                _ = LoadNextCode();
            }

            top += 36;
            btnSave   = new Button { Text = "Save",   Left = 200, Top = top, Width = 80, Height = 32 };
            btnCancel = new Button { Text = "Cancel",  Left = 290, Top = top, Width = 80, Height = 32 };
            Theme.StylePrimaryButton(btnSave);
            Theme.StyleOutlineButton(btnCancel);
            btnSave.Click   += async (s, e) => await Save();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Height = top + 70;
            Controls.AddRange(new Control[] { lblName, txtName, lblCode, txtCode, lblAddr, txtAddress, chkActive, btnSave, btnCancel });
        }

        private async Task LoadNextCode()
        {
            try
            {
                var res = await ApiConfig.Http.GetFromJsonAsync<NextCodeDto>("api/Branches/next-code", ApiConfig.JsonOptions);
                if (res != null && !string.IsNullOrWhiteSpace(res.Code))
                {
                    txtCode.Text = res.Code;
                }
                else
                {
                    txtCode.Text = "(Auto-increment)";
                }
            }
            catch
            {
                txtCode.Text = "(Auto-increment)";
            }
        }

        private class NextCodeDto
        {
            public string Code { get; set; } = "";
        }

        private Label MakeLabel(string text, int top) => new Label
        {
            Text = text, Left = 20, Top = top, Width = 340, Height = 18,
            ForeColor = Theme.MutedText, Font = Theme.StatLabelFont
        };

        private TextBox MakeText(int top)
        {
            var t = new TextBox { Left = 20, Top = top, Width = 340 };
            Theme.StyleTextBox(t);
            return t;
        }

        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Branch Name is required.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnSave.Enabled = false;
            try
            {
                string? codeToSend = txtCode.Text.StartsWith("(") || txtCode.Text == "Loading..." ? null : txtCode.Text.Trim();
                var payload = new { Name = txtName.Text.Trim(), Code = codeToSend, Address = txtAddress.Text.Trim(), IsActive = chkActive.Checked };
                System.Net.Http.HttpResponseMessage res;

                if (_existing is null)
                    res = await ApiConfig.Http.PostAsJsonAsync("api/Branches", payload);
                else
                    res = await ApiConfig.Http.PutAsJsonAsync($"api/Branches/{_existing.Id}", payload);

                if (res.IsSuccessStatusCode)
                { DialogResult = DialogResult.OK; Close(); }
                else
                {
                    var body = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("Save failed: " + res.StatusCode + "\n" + body, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { btnSave.Enabled = true; }
        }
    }

    // ─── Assign staff to branch dialog ────────────────────────────────────────

    [DesignerCategory("Code")]
    public class BranchAssignDialog : Form
    {
        private readonly int _branchId;
        private readonly string _branchName;
        private ComboBox cmbUser  = null!;
        private Button btnAssign  = null!;
        private Button btnClose   = null!;
        private DataGridView gridCurrent = null!;

        private List<UserDto> _unassigned = new();

        public BranchAssignDialog(int branchId, string branchName)
        {
            _branchId   = branchId;
            _branchName = branchName;
            Text = $"Assign Staff — {branchName}";
            Width = 480; Height = 440;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = Theme.Background;
            InitializeComponent();
            _ = LoadData();
        }

        private void InitializeComponent()
        {
            var lblCurrent = new Label { Text = $"Currently assigned to {_branchName}", Left = 20, Top = 20, Width = 420, Height = 18, ForeColor = Theme.MutedText, Font = Theme.StatLabelFont };

            gridCurrent = new DataGridView
            {
                Left = 20, Top = 42, Width = 430, Height = 180,
                AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Theme.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridCurrent.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id",   HeaderText = "ID",       Width = 40, FillWeight = 8 });
            gridCurrent.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",  HeaderText = "Name",     FillWeight = 50 });
            gridCurrent.Columns.Add(new DataGridViewTextBoxColumn { Name = "Role",  HeaderText = "Role",     FillWeight = 30 });

            var lblAdd = new Label { Text = "Add unassigned user:", Left = 20, Top = 255, Width = 200, Height = 18, ForeColor = Theme.MutedText, Font = Theme.StatLabelFont };
            cmbUser = new ComboBox { Left = 20, Top = 277, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            Theme.StyleComboBox(cmbUser);

            btnAssign = new Button { Text = "Assign", Left = 330, Top = 277, Width = 100, Height = 32 };
            Theme.StylePrimaryButton(btnAssign);
            btnAssign.Click += async (s, e) => await DoAssign();

            var btnUnassign = new Button { Text = "Unassign Selected", Left = 20, Top = 220, Width = 140, Height = 28 };
            Theme.StyleOutlineButton(btnUnassign);
            btnUnassign.Click += async (s, e) => await DoUnassign();

            btnClose = new Button { Text = "Done", Left = 340, Top = 345, Width = 100, Height = 32 };
            Theme.StyleOutlineButton(btnClose);
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            Controls.AddRange(new Control[] { lblCurrent, gridCurrent, btnUnassign, lblAdd, cmbUser, btnAssign, btnClose });
        }

        private async Task LoadData()
        {
            try
            {
                // current members
                var current = await ApiConfig.Http.GetFromJsonAsync<List<UserDto>>(
                    $"api/Branches/{_branchId}/users", ApiConfig.JsonOptions) ?? new();
                gridCurrent.Rows.Clear();
                foreach (var u in current)
                    gridCurrent.Rows.Add(u.Id, $"{u.FirstName} {u.LastName} ({u.Username})", u.Role);

                // unassigned staff
                _unassigned = await ApiConfig.Http.GetFromJsonAsync<List<UserDto>>(
                    "api/Branches/unassigned-users", ApiConfig.JsonOptions) ?? new();
                cmbUser.Items.Clear();
                cmbUser.Items.AddRange(_unassigned.Select(u => (object)$"{u.FirstName} {u.LastName} ({u.Username})").ToArray());
                if (cmbUser.Items.Count > 0) cmbUser.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users: " + ex.Message, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DoAssign()
        {
            if (cmbUser.SelectedIndex < 0 || _unassigned.Count == 0) return;
            var user = _unassigned[cmbUser.SelectedIndex];
            var res = await ApiConfig.Http.PostAsync($"api/Branches/{_branchId}/assign-user/{user.Id}", null);
            if (res.IsSuccessStatusCode)
                await LoadData();
            else
            {
                var body = await res.Content.ReadAsStringAsync();
                MessageBox.Show("Failed: " + body, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DoUnassign()
        {
            if (gridCurrent.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a staff member from the list to unassign.", "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var userId = (int)gridCurrent.SelectedRows[0].Cells["Id"].Value;
            var res = await ApiConfig.Http.PostAsync($"api/Branches/{_branchId}/unassign-user/{userId}", null);
            if (res.IsSuccessStatusCode)
                await LoadData();
            else
            {
                var body = await res.Content.ReadAsStringAsync();
                MessageBox.Show("Failed to unassign: " + body, "S.C.R.A.P", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class UserDto
        {
            public int Id { get; set; }
            public string Username  { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName  { get; set; } = string.Empty;
            public string Role      { get; set; } = string.Empty;
        }
    }
}
