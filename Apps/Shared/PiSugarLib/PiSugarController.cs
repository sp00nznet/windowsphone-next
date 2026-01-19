using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace WindowsPhoneNext.PiSugarLib;

/// <summary>
/// Controller for PiSugar2/PiSugar2 Plus battery management system
/// Communicates with pisugar-server via TCP socket (127.0.0.1:8423)
/// </summary>
public class PiSugarController : IDisposable
{
    private const string SocketHost = "127.0.0.1";
    private const int SocketPort = 8423;
    private const int ConnectionTimeout = 2000; // 2 seconds
    private const int ReadTimeout = 1000; // 1 second

    private bool _isDisposed;

    /// <summary>
    /// Checks if PiSugar2 daemon is available
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(SocketHost, SocketPort).WaitAsync(TimeSpan.FromMilliseconds(ConnectionTimeout));
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets battery level percentage (0-100)
    /// </summary>
    public async Task<int> GetBatteryLevelAsync()
    {
        var response = await SendCommandAsync("get battery");
        if (response != null && response.StartsWith("battery: "))
        {
            var value = response.Substring("battery: ".Length).Trim();
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage))
            {
                return (int)Math.Round(percentage);
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets battery voltage in volts
    /// </summary>
    public async Task<double> GetBatteryVoltageAsync()
    {
        var response = await SendCommandAsync("get battery_v");
        if (response != null && response.StartsWith("battery_v: "))
        {
            var value = response.Substring("battery_v: ".Length).Trim();
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var voltage))
            {
                return voltage;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets battery current in amps (PiSugar2 only)
    /// </summary>
    public async Task<double> GetBatteryCurrentAsync()
    {
        var response = await SendCommandAsync("get battery_i");
        if (response != null && response.StartsWith("battery_i: "))
        {
            var value = response.Substring("battery_i: ".Length).Trim();
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var current))
            {
                return current;
            }
        }
        return -1;
    }

    /// <summary>
    /// Checks if battery is currently charging
    /// </summary>
    public async Task<bool> IsChargingAsync()
    {
        var response = await SendCommandAsync("get battery_charging");
        if (response != null && response.StartsWith("battery_charging: "))
        {
            var value = response.Substring("battery_charging: ".Length).Trim();
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// Checks if USB power is connected
    /// </summary>
    public async Task<bool> IsPowerPluggedAsync()
    {
        var response = await SendCommandAsync("get battery_power_plugged");
        if (response != null && response.StartsWith("battery_power_plugged: "))
        {
            var value = response.Substring("battery_power_plugged: ".Length).Trim();
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// Gets the charging range configuration (start%, stop%)
    /// </summary>
    public async Task<(int StartPercent, int StopPercent)> GetChargingRangeAsync()
    {
        var response = await SendCommandAsync("get battery_charging_range");
        if (response != null && response.StartsWith("battery_charging_range: "))
        {
            var value = response.Substring("battery_charging_range: ".Length).Trim();
            // Format: [start, stop] e.g., "[20, 90]"
            value = value.Trim('[', ']');
            var parts = value.Split(',');
            if (parts.Length == 2 &&
                int.TryParse(parts[0].Trim(), out var start) &&
                int.TryParse(parts[1].Trim(), out var stop))
            {
                return (start, stop);
            }
        }
        return (0, 100);
    }

    /// <summary>
    /// Sets the charging range (restart charging at start%, stop at stop%)
    /// </summary>
    public async Task<bool> SetChargingRangeAsync(int startPercent, int stopPercent)
    {
        if (startPercent < 0 || startPercent > 100 || stopPercent < 0 || stopPercent > 100 || startPercent >= stopPercent)
        {
            throw new ArgumentException("Invalid charging range");
        }

        var response = await SendCommandAsync($"set_battery_charging_range {startPercent} {stopPercent}");
        return response != null;
    }

    /// <summary>
    /// Checks if charging is allowed when power is plugged
    /// </summary>
    public async Task<bool> IsChargingAllowedAsync()
    {
        var response = await SendCommandAsync("get battery_allow_charging");
        if (response != null && response.StartsWith("battery_allow_charging: "))
        {
            var value = response.Substring("battery_allow_charging: ".Length).Trim();
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return true;
    }

    /// <summary>
    /// Enables or disables charging when power is plugged
    /// </summary>
    public async Task<bool> SetChargingAllowedAsync(bool allowed)
    {
        var response = await SendCommandAsync($"set_allow_charging {allowed.ToString().ToLower()}");
        return response != null;
    }

    /// <summary>
    /// Checks if input protection is enabled
    /// </summary>
    public async Task<bool> IsInputProtectionEnabledAsync()
    {
        var response = await SendCommandAsync("get battery_input_protect_enabled");
        if (response != null && response.StartsWith("battery_input_protect_enable: "))
        {
            var value = response.Substring("battery_input_protect_enable: ".Length).Trim();
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// Enables or disables input protection
    /// </summary>
    public async Task<bool> SetInputProtectionAsync(bool enabled)
    {
        var response = await SendCommandAsync($"set_battery_input_protect {enabled.ToString().ToLower()}");
        return response != null;
    }

    /// <summary>
    /// Checks if battery output is enabled
    /// </summary>
    public async Task<bool> IsOutputEnabledAsync()
    {
        var response = await SendCommandAsync("get battery_output_enabled");
        if (response != null && response.StartsWith("battery_output_enabled: "))
        {
            var value = response.Substring("battery_output_enabled: ".Length).Trim();
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return true;
    }

    /// <summary>
    /// Enables or disables battery output
    /// </summary>
    public async Task<bool> SetOutputEnabledAsync(bool enabled)
    {
        var response = await SendCommandAsync($"set_battery_output {enabled.ToString().ToLower()}");
        return response != null;
    }

    /// <summary>
    /// Gets comprehensive battery status
    /// </summary>
    public async Task<BatteryStatus?> GetBatteryStatusAsync()
    {
        try
        {
            var level = await GetBatteryLevelAsync();
            if (level < 0) return null;

            return new BatteryStatus
            {
                Level = level,
                Voltage = await GetBatteryVoltageAsync(),
                Current = await GetBatteryCurrentAsync(),
                IsCharging = await IsChargingAsync(),
                IsPowerPlugged = await IsPowerPluggedAsync()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sends a command to the PiSugar2 daemon and returns the response
    /// </summary>
    private async Task<string?> SendCommandAsync(string command)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(SocketHost, SocketPort).WaitAsync(TimeSpan.FromMilliseconds(ConnectionTimeout));

            if (!client.Connected)
                return null;

            using var stream = client.GetStream();
            stream.ReadTimeout = ReadTimeout;
            stream.WriteTimeout = ReadTimeout;

            // Send command
            var commandBytes = Encoding.UTF8.GetBytes(command + "\n");
            await stream.WriteAsync(commandBytes, 0, commandBytes.Length);

            // Read response
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                return response;
            }
        }
        catch
        {
            // Connection or timeout error
        }

        return null;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
        }
    }
}

/// <summary>
/// Battery status information
/// </summary>
public class BatteryStatus
{
    /// <summary>
    /// Battery level percentage (0-100)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Battery voltage in volts
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Battery current in amps (negative when discharging)
    /// </summary>
    public double Current { get; set; }

    /// <summary>
    /// Whether battery is currently charging
    /// </summary>
    public bool IsCharging { get; set; }

    /// <summary>
    /// Whether USB power is connected
    /// </summary>
    public bool IsPowerPlugged { get; set; }

    /// <summary>
    /// Gets battery status description
    /// </summary>
    public string StatusText
    {
        get
        {
            if (IsCharging)
                return "Charging";
            if (IsPowerPlugged)
                return "Plugged in";
            if (Level > 80)
                return "Excellent";
            if (Level > 50)
                return "Good";
            if (Level > 20)
                return "Low";
            return "Critical";
        }
    }

    /// <summary>
    /// Gets estimated runtime in hours (rough estimate)
    /// </summary>
    public double EstimatedRuntime
    {
        get
        {
            if (IsCharging || IsPowerPlugged || Current >= 0)
                return -1; // Cannot estimate

            // Assuming ~5000mAh capacity for PiSugar2 Plus
            var capacityAh = 5.0;
            var remainingAh = capacityAh * (Level / 100.0);
            var currentA = Math.Abs(Current);

            if (currentA > 0)
                return remainingAh / currentA;

            return -1;
        }
    }
}
