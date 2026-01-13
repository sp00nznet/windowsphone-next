using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace WindowsPhoneNext.Settings;

public partial class MainWindow : Window
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsFolder = Path.Combine(appData, "WindowsPhoneNext");
        Directory.CreateDirectory(settingsFolder);
        _settingsPath = Path.Combine(settingsFolder, "settings.json");

        _settings = new AppSettings();
        LoadSettings();
        UpdateUI();
        LoadStorageInfo();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateUI()
    {
        GalleryPathBox.Text = string.IsNullOrEmpty(_settings.GalleryPath)
            ? "(Not set - Gallery will ask on launch)"
            : _settings.GalleryPath;

        MusicPathBox.Text = string.IsNullOrEmpty(_settings.MusicPath)
            ? "(Not set - Music will use default)"
            : _settings.MusicPath;
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

    private void BrowseGalleryPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Gallery Image Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            _settings.GalleryPath = dialog.FolderName;
            SaveSettings();
            UpdateUI();
        }
    }

    private void BrowseMusicPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Music Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            _settings.MusicPath = dialog.FolderName;
            SaveSettings();
            UpdateUI();
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class AppSettings
{
    public string GalleryPath { get; set; } = "";
    public string MusicPath { get; set; } = "";
}
