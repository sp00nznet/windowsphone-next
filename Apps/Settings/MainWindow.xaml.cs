using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowsPhoneNext.ModemLib;

namespace WindowsPhoneNext.Settings;

public partial class MainWindow : Window
{
    private readonly ModemController _modem;
    private readonly string _settingsFilePath;
    private readonly ObservableCollection<BluetoothDeviceInfo> _pairedBluetoothDevices = new();
    private readonly ObservableCollection<BluetoothDeviceInfo> _availableBluetoothDevices = new();
    private readonly ObservableCollection<WifiNetworkInfo> _savedWifiNetworks = new();
    private readonly ObservableCollection<WifiNetworkInfo> _availableWifiNetworks = new();
    private string? _pendingWifiSsid;
    private bool _isScanning;

    public MainWindow()
    {
        InitializeComponent();

        _modem = new ModemController();

        // Setup settings storage path
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPhoneNext");
        Directory.CreateDirectory(appData);
        _settingsFilePath = Path.Combine(appData, "connectivity_settings.json");

        // Bind collections
        PairedBluetoothDevices.ItemsSource = _pairedBluetoothDevices;
        AvailableBluetoothDevices.ItemsSource = _availableBluetoothDevices;
        SavedWifiNetworks.ItemsSource = _savedWifiNetworks;
        AvailableWifiNetworks.ItemsSource = _availableWifiNetworks;

        LoadSettings();
        LoadStorageInfo();
        _ = InitializeAsync();
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        await LoadModemInfoAsync();
        await CheckCurrentWifiConnectionAsync();
    }

    #region Storage

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

    #endregion

    #region Modem / About Phone

    private async System.Threading.Tasks.Task LoadModemInfoAsync()
    {
        try
        {
            if (await _modem.AutoConnectAsync())
            {
                await _modem.InitializeAsync();

                // Get modem info
                var modemInfo = await _modem.GetModemInfoAsync();
                if (modemInfo != null)
                {
                    ModemInfoText.Text = $"{modemInfo.Manufacturer} {modemInfo.Model}";
                    ImeiText.Text = modemInfo.IMEI ?? "Not available";
                }
                else
                {
                    ModemInfoText.Text = "Connected";
                    ImeiText.Text = "Not available";
                }

                // Get phone number
                var phoneNumber = await _modem.GetPhoneNumberAsync();
                PhoneNumberText.Text = string.IsNullOrEmpty(phoneNumber) ? "Not available" : phoneNumber;

                // Get operator name
                var operatorName = await _modem.GetOperatorNameAsync();
                OperatorText.Text = string.IsNullOrEmpty(operatorName) ? "Not available" : operatorName;

                // Get signal strength
                var signalStrength = await _modem.GetSignalStrengthAsync();
                var signalDesc = signalStrength switch
                {
                    >= 20 => "Excellent",
                    >= 15 => "Good",
                    >= 10 => "Fair",
                    >= 1 => "Poor",
                    _ => "No signal"
                };
                SignalStrengthText.Text = $"{signalDesc} ({signalStrength}/31)";
            }
            else
            {
                ModemInfoText.Text = "Not connected";
                ImeiText.Text = "Modem not found";
                PhoneNumberText.Text = "Not available";
                OperatorText.Text = "Not available";
                SignalStrengthText.Text = "Not available";
            }
        }
        catch (Exception ex)
        {
            ModemInfoText.Text = "Error reading modem";
            ImeiText.Text = ex.Message;
            PhoneNumberText.Text = "Error";
            OperatorText.Text = "Error";
            SignalStrengthText.Text = "Error";
        }
    }

    #endregion

    #region Bluetooth

    private void BluetoothToggle_Changed(object sender, RoutedEventArgs e)
    {
        var isEnabled = BluetoothToggle.IsChecked == true;
        BluetoothStatusText.Text = isEnabled ? "On" : "Off";
        BluetoothScanButton.IsEnabled = isEnabled;

        if (!isEnabled)
        {
            AvailableBluetoothSection.Visibility = Visibility.Collapsed;
            _availableBluetoothDevices.Clear();
        }

        // Enable/Disable Bluetooth adapter via PowerShell
        _ = SetBluetoothStateAsync(isEnabled);
    }

    private async System.Threading.Tasks.Task SetBluetoothStateAsync(bool enable)
    {
        try
        {
            var action = enable ? "Enable" : "Disable";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Get-PnpDevice -Class Bluetooth | {action}-PnpDevice -Confirm:$false\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }
        catch
        {
            // Silently fail - Bluetooth control may require admin rights
        }
    }

    private async void BluetoothScan_Click(object sender, RoutedEventArgs e)
    {
        if (_isScanning) return;

        _isScanning = true;
        BluetoothScanButtonText.Text = "Scanning...";
        BluetoothScanButton.IsEnabled = false;
        AvailableBluetoothSection.Visibility = Visibility.Visible;
        NoAvailableBluetoothText.Visibility = Visibility.Visible;
        NoAvailableBluetoothText.Text = "Scanning...";
        _availableBluetoothDevices.Clear();

        try
        {
            // Use PowerShell to scan for Bluetooth devices
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"Get-PnpDevice -Class Bluetooth | Select-Object FriendlyName, InstanceId, Status | ConvertTo-Json\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    var devices = JsonSerializer.Deserialize<JsonElement>(output);
                    if (devices.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var device in devices.EnumerateArray())
                        {
                            var name = device.GetProperty("FriendlyName").GetString();
                            var id = device.GetProperty("InstanceId").GetString();
                            if (!string.IsNullOrEmpty(name) && !name.Contains("Radio"))
                            {
                                _availableBluetoothDevices.Add(new BluetoothDeviceInfo
                                {
                                    Name = name,
                                    Id = id ?? "",
                                    Address = id?.Split('\\').LastOrDefault() ?? ""
                                });
                            }
                        }
                    }
                    else if (devices.ValueKind == JsonValueKind.Object)
                    {
                        var name = devices.GetProperty("FriendlyName").GetString();
                        var id = devices.GetProperty("InstanceId").GetString();
                        if (!string.IsNullOrEmpty(name) && !name.Contains("Radio"))
                        {
                            _availableBluetoothDevices.Add(new BluetoothDeviceInfo
                            {
                                Name = name,
                                Id = id ?? "",
                                Address = id?.Split('\\').LastOrDefault() ?? ""
                            });
                        }
                    }
                }
                catch
                {
                    // JSON parsing failed
                }
            }

            NoAvailableBluetoothText.Text = _availableBluetoothDevices.Count == 0
                ? "No devices found"
                : "";
            NoAvailableBluetoothText.Visibility = _availableBluetoothDevices.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            NoAvailableBluetoothText.Text = $"Scan failed: {ex.Message}";
        }
        finally
        {
            _isScanning = false;
            BluetoothScanButtonText.Text = "Scan for Devices";
            BluetoothScanButton.IsEnabled = BluetoothToggle.IsChecked == true;
        }
    }

    private void PairBluetoothDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string deviceId)
        {
            var device = _availableBluetoothDevices.FirstOrDefault(d => d.Id == deviceId);
            if (device != null)
            {
                // Add to paired devices
                _pairedBluetoothDevices.Add(new BluetoothDeviceInfo
                {
                    Name = device.Name,
                    Id = device.Id,
                    Address = device.Address,
                    Status = "Paired"
                });

                // Remove from available
                _availableBluetoothDevices.Remove(device);

                UpdateBluetoothUI();
                SaveSettings();
            }
        }
    }

    private void ForgetBluetoothDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string deviceId)
        {
            var device = _pairedBluetoothDevices.FirstOrDefault(d => d.Id == deviceId);
            if (device != null)
            {
                _pairedBluetoothDevices.Remove(device);
                UpdateBluetoothUI();
                SaveSettings();
            }
        }
    }

    private void UpdateBluetoothUI()
    {
        NoPairedBluetoothText.Visibility = _pairedBluetoothDevices.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    #endregion

    #region WiFi

    private void WifiToggle_Changed(object sender, RoutedEventArgs e)
    {
        var isEnabled = WifiToggle.IsChecked == true;
        WifiStatusText.Text = isEnabled ? "On" : "Off";
        WifiScanButton.IsEnabled = isEnabled;

        if (!isEnabled)
        {
            AvailableWifiSection.Visibility = Visibility.Collapsed;
            ConnectedWifiSection.Visibility = Visibility.Collapsed;
            _availableWifiNetworks.Clear();
        }
        else
        {
            _ = CheckCurrentWifiConnectionAsync();
        }

        // Enable/Disable WiFi adapter
        _ = SetWifiStateAsync(isEnabled);
    }

    private async System.Threading.Tasks.Task SetWifiStateAsync(bool enable)
    {
        try
        {
            var action = enable ? "Enable" : "Disable";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface set interface \"Wi-Fi\" {action.ToLower()}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }
        catch
        {
            // May require admin rights
        }
    }

    private async System.Threading.Tasks.Task CheckCurrentWifiConnectionAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse connected network
            var lines = output.Split('\n');
            string? ssid = null;
            string? state = null;

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("SSID") && !line.Contains("BSSID"))
                {
                    ssid = line.Split(':').LastOrDefault()?.Trim();
                }
                else if (line.Trim().StartsWith("State"))
                {
                    state = line.Split(':').LastOrDefault()?.Trim();
                }
            }

            if (!string.IsNullOrEmpty(ssid) && state?.Contains("connected") == true)
            {
                ConnectedWifiName.Text = ssid;
                ConnectedWifiStatus.Text = "Connected";
                ConnectedWifiSection.Visibility = Visibility.Visible;
                WifiStatusText.Text = $"Connected to {ssid}";
                WifiToggle.IsChecked = true;
            }
            else
            {
                ConnectedWifiSection.Visibility = Visibility.Collapsed;

                // Check if WiFi interface exists
                if (output.Contains("Wi-Fi") || output.Contains("Wireless"))
                {
                    WifiToggle.IsChecked = true;
                    WifiStatusText.Text = "On";
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }

    private async void WifiScan_Click(object sender, RoutedEventArgs e)
    {
        if (_isScanning) return;

        _isScanning = true;
        WifiScanButtonText.Text = "Scanning...";
        WifiScanButton.IsEnabled = false;
        AvailableWifiSection.Visibility = Visibility.Visible;
        NoAvailableWifiText.Visibility = Visibility.Visible;
        NoAvailableWifiText.Text = "Scanning...";
        _availableWifiNetworks.Clear();

        try
        {
            // First refresh the network list
            var refreshProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show networks mode=bssid",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            refreshProcess.Start();
            var output = await refreshProcess.StandardOutput.ReadToEndAsync();
            await refreshProcess.WaitForExitAsync();

            // Parse networks
            var lines = output.Split('\n');
            string? currentSsid = null;
            string? currentSecurity = null;
            string? currentSignal = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("SSID") && !trimmed.Contains("BSSID"))
                {
                    // Save previous network
                    if (!string.IsNullOrEmpty(currentSsid))
                    {
                        AddWifiNetwork(currentSsid, currentSecurity, currentSignal);
                    }

                    currentSsid = trimmed.Split(':').Skip(1).FirstOrDefault()?.Trim();
                    currentSecurity = null;
                    currentSignal = null;
                }
                else if (trimmed.StartsWith("Authentication"))
                {
                    currentSecurity = trimmed.Split(':').LastOrDefault()?.Trim();
                }
                else if (trimmed.StartsWith("Signal"))
                {
                    currentSignal = trimmed.Split(':').LastOrDefault()?.Trim();
                }
            }

            // Add last network
            if (!string.IsNullOrEmpty(currentSsid))
            {
                AddWifiNetwork(currentSsid, currentSecurity, currentSignal);
            }

            NoAvailableWifiText.Text = _availableWifiNetworks.Count == 0
                ? "No networks found"
                : "";
            NoAvailableWifiText.Visibility = _availableWifiNetworks.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            NoAvailableWifiText.Text = $"Scan failed: {ex.Message}";
        }
        finally
        {
            _isScanning = false;
            WifiScanButtonText.Text = "Scan for Networks";
            WifiScanButton.IsEnabled = WifiToggle.IsChecked == true;
        }
    }

    private void AddWifiNetwork(string ssid, string? security, string? signal)
    {
        if (string.IsNullOrWhiteSpace(ssid)) return;

        // Don't add duplicates or currently connected network
        if (_availableWifiNetworks.Any(n => n.Ssid == ssid)) return;
        if (ConnectedWifiName.Text == ssid) return;

        _availableWifiNetworks.Add(new WifiNetworkInfo
        {
            Ssid = ssid,
            Security = security ?? "Open",
            SignalStrength = signal ?? "Unknown"
        });
    }

    private void ConnectWifi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string ssid)
        {
            var network = _availableWifiNetworks.FirstOrDefault(n => n.Ssid == ssid);
            if (network != null)
            {
                if (network.Security != "Open")
                {
                    // Show password dialog
                    _pendingWifiSsid = ssid;
                    WifiDialogNetworkName.Text = ssid;
                    WifiPasswordInput.Password = "";
                    WifiPasswordDialog.Visibility = Visibility.Visible;
                    WifiPasswordInput.Focus();
                }
                else
                {
                    // Connect directly to open network
                    _ = ConnectToWifiAsync(ssid, null);
                }
            }
        }
    }

    private void ConnectSavedWifi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string ssid)
        {
            var network = _savedWifiNetworks.FirstOrDefault(n => n.Ssid == ssid);
            if (network != null)
            {
                _ = ConnectToWifiAsync(ssid, network.Password);
            }
        }
    }

    private void WifiPasswordCancel_Click(object sender, RoutedEventArgs e)
    {
        WifiPasswordDialog.Visibility = Visibility.Collapsed;
        _pendingWifiSsid = null;
    }

    private void WifiPasswordConnect_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_pendingWifiSsid))
        {
            var password = WifiPasswordInput.Password;
            WifiPasswordDialog.Visibility = Visibility.Collapsed;
            _ = ConnectToWifiAsync(_pendingWifiSsid, password);
            _pendingWifiSsid = null;
        }
    }

    private async System.Threading.Tasks.Task ConnectToWifiAsync(string ssid, string? password)
    {
        try
        {
            WifiStatusText.Text = $"Connecting to {ssid}...";

            // Create a temporary profile XML
            var profileXml = CreateWifiProfileXml(ssid, password);
            var profilePath = Path.Combine(Path.GetTempPath(), "wifi_profile.xml");
            await File.WriteAllTextAsync(profilePath, profileXml);

            // Add the profile
            var addProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan add profile filename=\"{profilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            addProcess.Start();
            await addProcess.WaitForExitAsync();

            // Connect to the network
            var connectProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan connect name=\"{ssid}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            connectProcess.Start();
            var output = await connectProcess.StandardOutput.ReadToEndAsync();
            await connectProcess.WaitForExitAsync();

            // Clean up temp file
            try { File.Delete(profilePath); } catch { }

            // Wait a moment and check connection
            await System.Threading.Tasks.Task.Delay(2000);
            await CheckCurrentWifiConnectionAsync();

            // Save to saved networks if successful and has password
            if (ConnectedWifiName.Text == ssid && !string.IsNullOrEmpty(password))
            {
                if (!_savedWifiNetworks.Any(n => n.Ssid == ssid))
                {
                    _savedWifiNetworks.Add(new WifiNetworkInfo
                    {
                        Ssid = ssid,
                        Password = password,
                        Security = "WPA2"
                    });
                    UpdateWifiUI();
                    SaveSettings();
                }
            }
        }
        catch (Exception ex)
        {
            WifiStatusText.Text = $"Connection failed: {ex.Message}";
        }
    }

    private string CreateWifiProfileXml(string ssid, string? password)
    {
        var isOpen = string.IsNullOrEmpty(password);
        var authentication = isOpen ? "open" : "WPA2PSK";
        var encryption = isOpen ? "none" : "AES";

        var xml = $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{ssid}</name>
    <SSIDConfig>
        <SSID>
            <name>{ssid}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>{authentication}</authentication>
                <encryption>{encryption}</encryption>
                <useOneX>false</useOneX>
            </authEncryption>";

        if (!isOpen)
        {
            xml += $@"
            <sharedKey>
                <keyType>passPhrase</keyType>
                <protected>false</protected>
                <keyMaterial>{password}</keyMaterial>
            </sharedKey>";
        }

        xml += @"
        </security>
    </MSM>
</WLANProfile>";

        return xml;
    }

    private async void DisconnectWifi_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan disconnect",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();

            ConnectedWifiSection.Visibility = Visibility.Collapsed;
            WifiStatusText.Text = "On";
        }
        catch
        {
            // Silently fail
        }
    }

    private void ForgetWifi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string ssid)
        {
            var network = _savedWifiNetworks.FirstOrDefault(n => n.Ssid == ssid);
            if (network != null)
            {
                _savedWifiNetworks.Remove(network);

                // Also remove the profile from Windows
                _ = RemoveWifiProfileAsync(ssid);

                UpdateWifiUI();
                SaveSettings();
            }
        }
    }

    private async System.Threading.Tasks.Task RemoveWifiProfileAsync(string ssid)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan delete profile name=\"{ssid}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }
        catch
        {
            // Silently fail
        }
    }

    private void UpdateWifiUI()
    {
        NoSavedWifiText.Visibility = _savedWifiNetworks.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    #endregion

    #region Settings Persistence

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<ConnectivitySettings>(json);

                if (settings != null)
                {
                    foreach (var device in settings.PairedBluetoothDevices)
                    {
                        _pairedBluetoothDevices.Add(device);
                    }

                    foreach (var network in settings.SavedWifiNetworks)
                    {
                        _savedWifiNetworks.Add(network);
                    }
                }
            }
        }
        catch
        {
            // Silently fail - will start with empty lists
        }

        UpdateBluetoothUI();
        UpdateWifiUI();
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new ConnectivitySettings
            {
                PairedBluetoothDevices = _pairedBluetoothDevices.ToList(),
                SavedWifiNetworks = _savedWifiNetworks.ToList()
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Silently fail
        }
    }

    #endregion

    #region Window Events

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (WifiPasswordDialog.Visibility == Visibility.Visible)
            {
                WifiPasswordDialog.Visibility = Visibility.Collapsed;
                _pendingWifiSsid = null;
            }
            else
            {
                Close();
            }
            e.Handled = true;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _modem.Dispose();
        base.OnClosed(e);
    }

    #endregion
}

#region Data Models

public class BluetoothDeviceInfo
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string Address { get; set; } = "";
    public string Status { get; set; } = "";
}

public class WifiNetworkInfo
{
    public string Ssid { get; set; } = "";
    public string Security { get; set; } = "";
    public string SignalStrength { get; set; } = "";
    public string? Password { get; set; }
}

public class ConnectivitySettings
{
    public List<BluetoothDeviceInfo> PairedBluetoothDevices { get; set; } = new();
    public List<WifiNetworkInfo> SavedWifiNetworks { get; set; } = new();
}

#endregion
