using System.Windows;
using WindowsPhoneNext.Shared.Services;

namespace WindowsPhoneNext.Solitaire;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply the current theme
        ThemeManager.ApplyTheme(Resources);
    }
}
