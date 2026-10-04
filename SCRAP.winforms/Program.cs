using System;
using System.Windows.Forms;
using SCRAP.winforms.Forms;
using SCRAP.winforms.Forms.Sales;
using SCRAP.winforms.Forms.Technical;

namespace SCRAP.winforms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// [STAThread] is required for COM/OLE operations including SaveFileDialog, OpenFileDialog, and Clipboard.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) break;
                }

                Form mainForm = CurrentSession.Role switch
                {
                    "Admin" => new MainForm(),
                    "Superadmin" => new SuperAdminMainForm(),
                    "Manager" => new ManagerMainForm(),
                    "TechStaff" => new TechnicalMainForm(),
                    "SalesStaff" => new SalesMainForm(),
                    _ => new LoginForm()
                };

                Application.Run(mainForm);

                if (!CurrentSession.LogoutRequested) break;
                CurrentSession.LogoutRequested = false;
            }
        }
    }
}