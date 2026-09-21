using SCRAP.winforms;
using SCRAP.winforms.Forms;

using SCRAP.winforms.Forms.Technical;

ApplicationConfiguration.Initialize();

while (true)
{
    using (var login = new LoginForm())
    {
        if (login.ShowDialog() != DialogResult.OK) break;
    }

    Form mainForm = CurrentSession.Role switch
    {
        "Admin" or "Superadmin" => new MainForm(),
        "Manager" => new ManagerMainForm(),
        "TechStaff" => new TechnicalMainForm(),
        "SalesStaff" => new SalesMainForm(),
        _ => new LoginForm()
    };

    Application.Run(mainForm);

    if (!CurrentSession.LogoutRequested) break;
    CurrentSession.LogoutRequested = false;
}