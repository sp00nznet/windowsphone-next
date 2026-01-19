# PiSugarLib - PiSugar2 Battery Management Library

A .NET 8.0 library for interfacing with the PiSugar2 and PiSugar2 Plus battery management system via TCP socket communication.

## Overview

PiSugarLib provides a comprehensive C# API for monitoring and controlling PiSugar2 battery systems. It communicates with the `pisugar-server` daemon running on the system via TCP sockets (localhost:8423).

## Features

### Battery Monitoring
- **Battery Level**: Real-time percentage (0-100%)
- **Battery Voltage**: Voltage in volts
- **Battery Current**: Current in amps (PiSugar2 only)
- **Charging Status**: Whether battery is actively charging
- **Power Plugged**: Whether USB power is connected
- **Comprehensive Status**: Get all battery info in one call

### Battery Management
- **Charging Control**: Enable/disable charging when power is plugged
- **Charging Range**: Configure start/stop charge percentages (e.g., 20%-90%)
- **Input Protection**: Enable/disable hardware input protection
- **Battery Output**: Control battery power output

## Prerequisites

The PiSugar2 power manager daemon must be installed and running:

```bash
wget https://cdn.pisugar.com/release/pisugar-power-manager.sh
bash pisugar-power-manager.sh -c release
```

This will start the `pisugar-server` daemon on `127.0.0.1:8423`.

## Usage

### Basic Battery Monitoring

```csharp
using WindowsPhoneNext.PiSugarLib;

var battery = new PiSugarController();

// Check if PiSugar2 is available
if (await battery.IsAvailableAsync())
{
    // Get battery level
    int level = await battery.GetBatteryLevelAsync();
    Console.WriteLine($"Battery: {level}%");

    // Get charging status
    bool isCharging = await battery.IsChargingAsync();
    bool isPowered = await battery.IsPowerPluggedAsync();

    // Get comprehensive status
    var status = await battery.GetBatteryStatusAsync();
    Console.WriteLine($"Level: {status.Level}%");
    Console.WriteLine($"Voltage: {status.Voltage:F2}V");
    Console.WriteLine($"Status: {status.StatusText}");
}
```

### Battery Management

```csharp
// Set charging range (start at 20%, stop at 90%)
await battery.SetChargingRangeAsync(20, 90);

// Disable charging (useful for battery calibration)
await battery.SetChargingAllowedAsync(false);

// Enable input protection
await battery.SetInputProtectionAsync(true);

// Control battery output
await battery.SetOutputEnabledAsync(true);
```

### Real-time Monitoring

```csharp
using System.Windows.Threading;

var timer = new DispatcherTimer
{
    Interval = TimeSpan.FromSeconds(30)
};

timer.Tick += async (sender, e) =>
{
    var status = await battery.GetBatteryStatusAsync();
    if (status != null)
    {
        BatteryText.Text = $"{status.Level}%";
        BatteryStatusText.Text = status.StatusText;

        if (status.IsCharging)
        {
            BatteryIcon.Foreground = Brushes.Blue; // Charging indicator
        }
    }
};

timer.Start();
```

## API Reference

### PiSugarController Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `IsAvailableAsync()` | `Task<bool>` | Check if PiSugar2 daemon is running |
| `GetBatteryLevelAsync()` | `Task<int>` | Get battery percentage (0-100) |
| `GetBatteryVoltageAsync()` | `Task<double>` | Get battery voltage in volts |
| `GetBatteryCurrentAsync()` | `Task<double>` | Get battery current in amps |
| `IsChargingAsync()` | `Task<bool>` | Check if battery is charging |
| `IsPowerPluggedAsync()` | `Task<bool>` | Check if USB power is connected |
| `GetBatteryStatusAsync()` | `Task<BatteryStatus?>` | Get comprehensive battery status |
| `GetChargingRangeAsync()` | `Task<(int, int)>` | Get charging range (start%, stop%) |
| `SetChargingRangeAsync(start, stop)` | `Task<bool>` | Set charging range |
| `IsChargingAllowedAsync()` | `Task<bool>` | Check if charging is allowed |
| `SetChargingAllowedAsync(allowed)` | `Task<bool>` | Enable/disable charging |
| `IsInputProtectionEnabledAsync()` | `Task<bool>` | Check input protection status |
| `SetInputProtectionAsync(enabled)` | `Task<bool>` | Enable/disable input protection |
| `IsOutputEnabledAsync()` | `Task<bool>` | Check battery output status |
| `SetOutputEnabledAsync(enabled)` | `Task<bool>` | Enable/disable battery output |

### BatteryStatus Properties

| Property | Type | Description |
|----------|------|-------------|
| `Level` | `int` | Battery level percentage (0-100) |
| `Voltage` | `double` | Battery voltage in volts |
| `Current` | `double` | Battery current in amps (negative when discharging) |
| `IsCharging` | `bool` | Whether battery is currently charging |
| `IsPowerPlugged` | `bool` | Whether USB power is connected |
| `StatusText` | `string` | Human-readable status (e.g., "Charging", "Excellent", "Low") |
| `EstimatedRuntime` | `double` | Estimated runtime in hours (when on battery) |

## Protocol Details

PiSugarLib communicates with the PiSugar2 daemon using a simple text-based protocol over TCP:

### Command Format
```
<command>\n
```

### Response Format
```
<key>: <value>\n
```

### Example Commands

```
get battery                      → battery: 85
get battery_v                    → battery_v: 4.15
get battery_charging             → battery_charging: true
get battery_power_plugged        → battery_power_plugged: true
set_battery_charging_range 20 90 → (no response on success)
set_allow_charging true          → (no response on success)
```

## Error Handling

All async methods return default values or `null` on error:

- `GetBatteryLevelAsync()` returns `-1` on error
- `GetBatteryVoltageAsync()` returns `-1` on error
- `GetBatteryStatusAsync()` returns `null` on error
- `Set*` methods return `false` on error

Always check availability before use:

```csharp
if (!await battery.IsAvailableAsync())
{
    // Handle PiSugar2 not available
    Console.WriteLine("PiSugar2 daemon not running");
    return;
}
```

## Integration Examples

### Windows Phone Next Launcher

The Launcher app uses PiSugarLib to display real-time battery status:

```csharp
private readonly PiSugarController _battery;
private bool _piSugarAvailable;

// In Window_Loaded
_piSugarAvailable = await _battery.IsAvailableAsync();

// In status update timer
if (_piSugarAvailable)
{
    var status = await _battery.GetBatteryStatusAsync();
    if (status != null)
    {
        UpdateBattery(status.Level, status.IsCharging);
    }
}
```

### Windows Phone Next Settings

The Settings app provides full battery management controls:

- Battery level display with progress bar
- Voltage and current monitoring
- Charging control toggle
- Charging range configuration (e.g., 20%-90%)
- Input protection toggle
- Battery output control

## Hardware Support

PiSugarLib is compatible with:

- **PiSugar2**: 1200mAh battery (for Raspberry Pi Zero)
- **PiSugar2 Plus**: 5000mAh battery (for Raspberry Pi 3/4/5)
- **PiSugar2 Pro**: High-capacity variants

All models use the same TCP socket protocol.

## Troubleshooting

### "PiSugar2 daemon not running"

**Solution**: Install and start the pisugar-power-manager:
```bash
bash pisugar-power-manager.sh -c release
systemctl enable pisugar-server
systemctl start pisugar-server
```

### Connection Timeout

**Symptoms**: `IsAvailableAsync()` returns `false`

**Solutions**:
1. Check if daemon is running: `systemctl status pisugar-server`
2. Verify TCP port is open: `netstat -tuln | grep 8423`
3. Check firewall settings

### Invalid Battery Readings

**Symptoms**: Battery level returns `-1` or voltage shows `0.0V`

**Solutions**:
1. Check I2C connection to PiSugar2
2. Verify battery is properly connected
3. Restart pisugar-server: `systemctl restart pisugar-server`

## Performance

- **Connection Timeout**: 2 seconds
- **Read Timeout**: 1 second
- **Typical Response Time**: < 50ms
- **Recommended Poll Interval**: 30 seconds (to avoid excessive I2C traffic)

## Thread Safety

PiSugarController is **not thread-safe**. Use separate instances per thread or implement your own locking.

## Dependencies

- .NET 8.0 Windows
- System.Net.Sockets (included in .NET)

No external NuGet packages required.

## References

- [PiSugar2 Official Documentation](https://docs.pisugar.com/)
- [PiSugar GitHub Repository](https://github.com/PiSugar/PiSugar)
- [PiSugar Power Manager](https://github.com/PiSugar/pisugar-power-manager-rs)
- [PiSugar2 Plus Product Page](https://www.pisugar.com/)

## License

This library is part of the Windows Phone Next project.
