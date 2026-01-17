using System.Windows;
using WindowsPhoneNext.Shared.Services;

namespace WindowsPhoneNext.Messaging;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply the current theme
        ThemeManager.ApplyTheme(Resources);

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"An error occurred: {args.Exception.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
