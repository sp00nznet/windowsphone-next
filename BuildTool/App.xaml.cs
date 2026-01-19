using System.Windows;

namespace WindowsPhoneNextBuildTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure running as administrator
            if (!IsAdministrator())
            {
                MessageBox.Show(
                    "This application requires administrator privileges to modify Windows images.\n\n" +
                    "Please right-click the application and select 'Run as administrator'.",
                    "Administrator Privileges Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Shutdown();
            }
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
