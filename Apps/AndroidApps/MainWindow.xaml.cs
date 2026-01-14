using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace WindowsPhoneNext.AndroidApps;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<AndroidApp> _apps = new();
    private readonly string _appsFilePath;
    private bool _wsaAvailable;

    public MainWindow()
    {
        InitializeComponent();

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPhoneNext");
        Directory.CreateDirectory(appData);
        _appsFilePath = Path.Combine(appData, "android_apps.json");

        AppList.ItemsSource = _apps;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckWSAStatusAsync();
        LoadInstalledApps();
        UpdateEmptyState();
    }

    private async Task CheckWSAStatusAsync()
    {
        try
        {
            // Check if WSA is installed by looking for WsaClient
            var wsaPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "WindowsApps");

            // Try to connect to WSA's ADB
            var result = await RunAdbCommandAsync("devices");

            if (result.Contains("127.0.0.1") || result.Contains("emulator"))
            {
                _wsaAvailable = true;
                StatusIndicator.Fill = FindResource("SuccessBrush") as Brush;
                StatusText.Text = "WSA Connected";
                StatusDescription.Text = "Ready to install and run Android apps";
                InstallButton.IsEnabled = true;
            }
            else
            {
                // WSA might be installed but not running
                _wsaAvailable = false;
                StatusIndicator.Fill = FindResource("AccentBrush") as Brush;
                StatusText.Text = "WSA Not Running";
                StatusDescription.Text = "Start WSA to install Android apps";
                InstallButton.IsEnabled = true;
            }
        }
        catch
        {
            _wsaAvailable = false;
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            StatusText.Text = "WSA Not Available";
            StatusDescription.Text = "Install Windows Subsystem for Android from Microsoft Store";
            InstallButton.IsEnabled = false;
        }
    }

    private async Task<string> RunAdbCommandAsync(string arguments)
    {
        try
        {
            // Try to find ADB in common locations
            var adbPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Android", "Sdk", "platform-tools", "adb.exe"),
                @"C:\platform-tools\adb.exe",
                "adb.exe" // In PATH
            };

            string? adbPath = null;
            foreach (var path in adbPaths)
            {
                if (File.Exists(path) || path == "adb.exe")
                {
                    adbPath = path;
                    break;
                }
            }

            if (adbPath == null)
            {
                throw new FileNotFoundException("ADB not found");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return "";

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output;
        }
        catch
        {
            return "";
        }
    }

    private void LoadInstalledApps()
    {
        _apps.Clear();

        if (File.Exists(_appsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_appsFilePath);
                var apps = JsonSerializer.Deserialize<List<AndroidApp>>(json);
                if (apps != null)
                {
                    foreach (var app in apps.OrderBy(a => a.Name))
                    {
                        _apps.Add(app);
                    }
                }
            }
            catch
            {
                // Failed to load, start fresh
            }
        }

        // Add sample apps for demonstration
        if (_apps.Count == 0)
        {
            // These are just placeholders - real apps would be detected from WSA
        }
    }

    private void SaveInstalledApps()
    {
        try
        {
            var json = JsonSerializer.Serialize(_apps.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_appsFilePath, json);
        }
        catch
        {
            // Failed to save
        }
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility = _apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void InstallApk_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Android Package|*.apk|All Files|*.*",
            Title = "Select APK to Install"
        };

        if (dialog.ShowDialog() != true) return;

        await InstallApkAsync(dialog.FileName);
    }

    private async Task InstallApkAsync(string apkPath)
    {
        LoadingText.Text = "Installing APK...";
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            // First, try to connect to WSA
            await RunAdbCommandAsync("connect 127.0.0.1:58526");
            await Task.Delay(1000);

            // Install the APK
            var result = await RunAdbCommandAsync($"install \"{apkPath}\"");

            if (result.Contains("Success"))
            {
                // Extract app name from APK filename
                var appName = Path.GetFileNameWithoutExtension(apkPath);
                var packageName = $"com.sideloaded.{appName.ToLower().Replace(" ", "")}";

                // Try to get actual package name from APK
                var aapt = await RunAdbCommandAsync($"shell pm list packages -f");

                var newApp = new AndroidApp
                {
                    Name = CleanAppName(appName),
                    PackageName = packageName,
                    ApkPath = apkPath,
                    InstalledDate = DateTime.Now,
                    IconText = appName.Length > 0 ? appName[0].ToString().ToUpper() : "A"
                };

                _apps.Add(newApp);
                SaveInstalledApps();
                UpdateEmptyState();

                MessageBox.Show($"Successfully installed {newApp.Name}!", "Installation Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (result.Contains("INSTALL_FAILED"))
            {
                MessageBox.Show($"Installation failed: {result}", "Installation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                // Fallback: Add to list anyway (user can manually manage)
                var appName = Path.GetFileNameWithoutExtension(apkPath);
                var newApp = new AndroidApp
                {
                    Name = CleanAppName(appName),
                    PackageName = $"com.sideloaded.{appName.ToLower().Replace(" ", "")}",
                    ApkPath = apkPath,
                    InstalledDate = DateTime.Now,
                    IconText = appName.Length > 0 ? appName[0].ToString().ToUpper() : "A"
                };

                _apps.Add(newApp);
                SaveInstalledApps();
                UpdateEmptyState();

                MessageBox.Show($"Added {newApp.Name} to the list.\nNote: WSA may need to be running to launch the app.",
                    "App Added", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to install APK: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private static string CleanAppName(string filename)
    {
        // Remove common suffixes and clean up the name
        var name = filename
            .Replace("_", " ")
            .Replace("-", " ")
            .Replace(".", " ");

        // Remove version numbers at the end
        var parts = name.Split(' ');
        var cleanParts = parts.Where(p => !p.All(c => char.IsDigit(c) || c == '.')).ToList();

        return string.Join(" ", cleanParts).Trim();
    }

    private async void AppItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;
        var packageName = button.Tag as string;
        if (string.IsNullOrEmpty(packageName)) return;

        await LaunchAppAsync(packageName);
    }

    private async Task LaunchAppAsync(string packageName)
    {
        LoadingText.Text = "Launching app...";
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            // Connect to WSA first
            await RunAdbCommandAsync("connect 127.0.0.1:58526");
            await Task.Delay(500);

            // Launch the app using monkey (generic app launcher)
            var result = await RunAdbCommandAsync(
                $"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1");

            if (!result.Contains("Events injected"))
            {
                // Try alternative launch method
                await RunAdbCommandAsync(
                    $"shell am start -n {packageName}/.MainActivity");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch app: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void OpenWSASettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Try to open WSA settings
            Process.Start(new ProcessStartInfo
            {
                FileName = "wsa://com.amazon.appstore",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                // Fallback: open Windows Settings for Apps
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:appsfeatures",
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Could not open WSA Settings. Please open it manually from the Start menu.",
                    "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckWSAStatusAsync();
        LoadInstalledApps();
        UpdateEmptyState();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}

public class AndroidApp
{
    public string Name { get; set; } = "";
    public string PackageName { get; set; } = "";
    public string ApkPath { get; set; } = "";
    public DateTime InstalledDate { get; set; }
    public string IconText { get; set; } = "A";
}
