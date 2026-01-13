using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using WindowsPhoneNext.ModemLib;

namespace WindowsPhoneNext.Launcher;

public partial class MainWindow : Window
{
    private readonly ModemController _modem;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _statusTimer;
    private readonly List<AppInfo> _apps;
    private readonly string _appsBasePath;

    public MainWindow()
    {
        InitializeComponent();

        _modem = new ModemController();
        _appsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");

        // Initialize apps list
        _apps = new List<AppInfo>
        {
            new AppInfo { Name = "Phone", Icon = "\U0001F4DE", AppPath = "Dialer" },
            new AppInfo { Name = "Messages", Icon = "\U0001F4AC", AppPath = "Messaging" },
            new AppInfo { Name = "Browser", Icon = "\U0001F310", AppPath = "Browser" },
            new AppInfo { Name = "Maps", Icon = "\U0001F5FA\uFE0F", AppPath = "Maps" },
            new AppInfo { Name = "Music", Icon = "\U0001F3B5", AppPath = "Music" },
            new AppInfo { Name = "Calendar", Icon = "\U0001F4C5", AppPath = "Calendar" },
            new AppInfo { Name = "Gallery", Icon = "\U0001F5BC\uFE0F", AppPath = "Gallery" },
            new AppInfo { Name = "Camera", Icon = "\U0001F4F7", AppPath = "Camera" },
            new AppInfo { Name = "Settings", Icon = "\u2699\uFE0F", AppPath = "Settings" }
        };

        AppGrid.ItemsSource = _apps;

        // Setup clock timer
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();

        // Setup status timer (signal, battery, etc.)
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();

        // Subscribe to modem events
        _modem.IncomingCall += Modem_IncomingCall;
        _modem.CallStateChanged += Modem_CallStateChanged;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateClock();
        await InitializeModemAsync();
        await UpdateStatusAsync();
    }

    private async Task InitializeModemAsync()
    {
        try
        {
            if (await _modem.AutoConnectAsync())
            {
                await _modem.InitializeAsync();
                NetworkText.Text = "LTE";
            }
            else
            {
                NetworkText.Text = "No Signal";
            }
        }
        catch
        {
            NetworkText.Text = "Error";
        }
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
    }

    private async void StatusTimer_Tick(object? sender, EventArgs e)
    {
        await UpdateStatusAsync();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("HH:mm");
        BigTimeText.Text = now.ToString("HH:mm");
        DateText.Text = now.ToString("dddd, MMMM d");
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            if (_modem.IsConnected)
            {
                // Update signal strength
                var signal = await _modem.GetSignalStrengthAsync();
                UpdateSignalBars(signal);

                // Update network status
                var network = await _modem.GetNetworkStatusAsync();
                NetworkText.Text = network switch
                {
                    NetworkStatus.RegisteredHome => "LTE",
                    NetworkStatus.RegisteredRoaming => "LTE R",
                    NetworkStatus.Searching => "...",
                    _ => "No Signal"
                };
            }

            // Update battery (simulated - would need PiSugar2 API)
            UpdateBattery(85);
        }
        catch
        {
            // Ignore status update errors
        }
    }

    private void UpdateSignalBars(int strength)
    {
        // CSQ values: 0-9 = poor, 10-14 = fair, 15-19 = good, 20-31 = excellent
        var bars = strength switch
        {
            >= 20 => 4,
            >= 15 => 3,
            >= 10 => 2,
            >= 1 => 1,
            _ => 0
        };

        var activeColor = FindResource("TextPrimaryBrush") as System.Windows.Media.Brush;
        var inactiveColor = FindResource("TextSecondaryBrush") as System.Windows.Media.Brush;

        Signal3.Fill = bars >= 3 ? activeColor : inactiveColor;
        Signal4.Fill = bars >= 4 ? activeColor : inactiveColor;

        SignalText.Text = bars switch
        {
            4 => "Excellent",
            3 => "Good",
            2 => "Fair",
            1 => "Poor",
            _ => "No Signal"
        };
    }

    private void UpdateBattery(int percent)
    {
        BatteryText.Text = $"{percent}%";
        BatteryLevel.Width = 24 * (percent / 100.0);

        if (percent < 20)
        {
            BatteryLevel.Fill = FindResource("ErrorBrush") as System.Windows.Media.Brush;
        }
        else if (percent < 50)
        {
            BatteryLevel.Fill = FindResource("WarningBrush") as System.Windows.Media.Brush;
        }
        else
        {
            BatteryLevel.Fill = FindResource("SuccessBrush") as System.Windows.Media.Brush;
        }
    }

    private void AppTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string appPath)
        {
            LaunchApp(appPath);
        }
    }

    private void PhoneButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchApp("Dialer");
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        // Already on home screen, could add animation or refresh
    }

    private void MessagesButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchApp("Messaging");
    }

    private void LaunchApp(string appName)
    {
        try
        {
            var appPath = Path.Combine(_appsBasePath, appName, $"WindowsPhone{appName}.exe");

            if (File.Exists(appPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    UseShellExecute = true
                });
            }
            else
            {
                // Try alternative path
                var altPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "WindowsPhoneNext", appName, $"WindowsPhone{appName}.exe");

                if (File.Exists(altPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = altPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show($"{appName} is not installed yet.", "App Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch {appName}: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Modem_IncomingCall(object? sender, IncomingCallEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            CallerIdText.Text = e.CallerId;
            IncomingCallOverlay.Visibility = Visibility.Visible;
        });
    }

    private void Modem_CallStateChanged(object? sender, CallStateChangedEventArgs e)
    {
        if (e.Status == CallStatus.Idle)
        {
            Dispatcher.Invoke(() =>
            {
                IncomingCallOverlay.Visibility = Visibility.Collapsed;
            });
        }
    }

    private async void AcceptCall_Click(object sender, RoutedEventArgs e)
    {
        await _modem.AnswerCallAsync();
        IncomingCallOverlay.Visibility = Visibility.Collapsed;

        // Launch dialer for active call
        LaunchApp("Dialer");
    }

    private async void DeclineCall_Click(object sender, RoutedEventArgs e)
    {
        await _modem.HangUpAsync();
        IncomingCallOverlay.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        _clockTimer.Stop();
        _statusTimer.Stop();
        _modem.Dispose();
        base.OnClosed(e);
    }
}

public class AppInfo
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string AppPath { get; set; } = "";
}
