using System.IO;
using System.Windows;
using System.Windows.Input;

namespace WindowsPhoneNext.Settings;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadStorageInfo();
    }

    private void LoadStorageInfo()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:");
            var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            var totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
            var usedGB = totalGB - freeGB;

            StorageInfoText.Text = $"{usedGB:F1} GB used of {totalGB:F1} GB ({freeGB:F1} GB free)";
        }
        catch
        {
            StorageInfoText.Text = "Unable to read storage info";
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
