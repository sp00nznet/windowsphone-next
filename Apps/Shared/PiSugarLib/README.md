# PiSugarLib - PiSugar2 Battery Management Library

A .NET 8.0 library for interfacing with the PiSugar2 and PiSugar2 Plus battery management system via **direct I2C communication** on LattePanda 3 Delta.

## Overview

PiSugarLib provides a comprehensive C# API for monitoring PiSugar2 battery systems. It communicates directly with the IP5209/IP5312 power management IC via I2C, bypassing the need for Raspberry Pi-specific daemon software.

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

### Hardware Requirements

1. **PiSugar2 Plus** 5000mAh battery
2. **LattePanda 3 Delta** with Arduino Leonardo GPIO headers
3. **Physical I2C Connection**: Wires soldered from PiSugar2 to LattePanda I2C pins
   - SDA → Pin 20 (D2)
   - SCL → Pin 21 (D3)
   - GND → GND
4. **Pull-up Resistors**: 4.7kΩ on SDA and SCL (if not built-in)

**📖 See [I2C_SETUP_GUIDE.md](I2C_SETUP_GUIDE.md) for complete wiring instructions and parts list.**

### Software Requirements

- Windows 10/11 with I2C support
- .NET 8.0 Windows SDK
- LattePanda Arduino Leonardo drivers
- Administrator privileges (for I2C access)

## Usage

### Basic Battery Monitoring

```csharp
using WindowsPhoneNext.PiSugarLib;

var battery = new PiSugarI2CController();

// Initialize I2C connection
if (await battery.InitializeAsync())
{
    Console.WriteLine("PiSugar2 connected via I2C!");

    // Get battery level
    int level = await battery.GetBatteryLevelAsync();
    Console.WriteLine($"Battery: {level}%");

    // Get voltage and current
    double voltage = await battery.GetBatteryVoltageAsync();
    double current = await battery.GetBatteryCurrentAsync();
    Console.WriteLine($"Voltage: {voltage:F2}V");
    Console.WriteLine($"Current: {current:F3}A");

    // Get charging status
    bool isCharging = await battery.IsChargingAsync();
    Console.WriteLine($"Charging: {isCharging}");

    // Get comprehensive status
    var status = await battery.GetBatteryStatusAsync();
    Console.WriteLine($"Status: {status.StatusText}");
}
else
{
    Console.WriteLine("Failed to initialize I2C connection");
    Console.WriteLine("Check wiring and I2C setup");
}
```

### Advanced Usage

```csharp
// Real-time monitoring loop
while (true)
{
    var status = await battery.GetBatteryStatusAsync();

    if (status != null)
    {
        Console.Clear();
        Console.WriteLine($"Battery Level: {status.Level}%");
        Console.WriteLine($"Voltage: {status.Voltage:F2}V");
        Console.WriteLine($"Current: {status.Current:F3}A");
        Console.WriteLine($"Status: {status.StatusText}");
        Console.WriteLine($"Charging: {(status.IsCharging ? "Yes" : "No")}");
    }

    await Task.Delay(5000); // Update every 5 seconds
}
```

**Note**: The I2C version currently supports **read-only** operations. Write operations (charging control, etc.) require additional register manipulation and will be added in future updates.

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

### PiSugarI2CController Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `InitializeAsync()` | `Task<bool>` | Initialize I2C connection to PiSugar2 |
| `IsAvailableAsync()` | `Task<bool>` | Check if PiSugar2 is detected on I2C bus |
| `GetBatteryLevelAsync()` | `Task<int>` | Get battery percentage (0-100) |
| `GetBatteryVoltageAsync()` | `Task<double>` | Get battery voltage in volts |
| `GetBatteryCurrentAsync()` | `Task<double>` | Get battery current in amps |
| `IsChargingAsync()` | `Task<bool>` | Check if battery is charging (current > 50mA) |
| `GetBatteryStatusAsync()` | `Task<BatteryStatus?>` | Get comprehensive battery status |
| `Dispose()` | `void` | Clean up I2C resources |

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

PiSugarLib communicates directly with the IP5209/IP5312 power management IC via I2C:

### I2C Configuration

- **I2C Address**: `0x75` (or `0x32` for some models)
- **Clock Speed**: 100kHz (Standard Mode)
- **Logic Level**: 3.3V
- **Pull-up Resistors**: Required (4.7kΩ)

### Register Map

| Register | Address | Description | Data Type |
|----------|---------|-------------|-----------|
| READ0 | 0xA0 | Battery voltage [15:8] | uint8 |
| READ1 | 0xA1 | Battery voltage [7:0] | uint8 |
| READ2 | 0xA2 | Battery current [15:8] | uint8 |
| READ3 | 0xA3 | Battery current [7:0] | uint8 |
| READ4 | 0xA4 | Battery percentage | uint8 |

### Data Conversion

**Battery Voltage**:
```
Raw Value = (READ0 << 8) | READ1
Voltage (mV) = (Raw × 0.26855) + 2600
Voltage (V) = Voltage_mV / 1000
```

**Battery Current** (signed 16-bit):
```
Raw Value = (READ2 << 8) | READ3
If Raw > 32767: Raw = Raw - 65536  // Convert to signed
Current (mA) = Raw × 0.745985
Current (A) = Current_mA / 1000
```

**Battery Percentage**:
```
Percentage = READ4 % 101
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

### I2C Device Not Detected

**Symptoms**: `InitializeAsync()` returns `false`

**Solutions**:
1. **Check Physical Connections**:
   - Use multimeter to verify continuity
   - Ensure SDA/SCL connected to correct pins
   - Verify GND connection
2. **Check Pull-up Resistors**:
   - Measure SDA/SCL voltage (should be ~3.3V when idle)
   - Add 4.7kΩ pull-ups if missing
3. **Try Alternative I2C Address**:
   ```csharp
   // Change in PiSugarI2CController.cs:
   private const byte I2C_ADDRESS = 0x32; // Try this if 0x75 fails
   ```
4. **Verify LattePanda I2C**:
   - Install Arduino Leonardo drivers
   - Check Device Manager for I2C controller
   - Enable Arduino co-processor in BIOS

### Invalid Battery Readings

**Symptoms**: Battery level returns `-1` or voltage shows `0.0V`

**Solutions**:
1. **Check Power**:
   - Verify PiSugar2 has battery installed
   - Ensure PiSugar2 powered via USB-C
2. **Verify IC Model**:
   - IP5209 vs IP5312 may have different registers
   - Check PiSugar2 version and adjust constants
3. **Test I2C Communication**:
   - Use logic analyzer to verify I2C signals
   - Check for ACK/NACK responses

### Intermittent Connection

**Symptoms**: Readings work sometimes, fail other times

**Solutions**:
1. Check solder joints for cold solder
2. Add strain relief to wires
3. Move away from electromagnetic interference sources
4. Ensure stable power supply to PiSugar2

### See Also

📖 **[I2C_SETUP_GUIDE.md](I2C_SETUP_GUIDE.md)** - Complete troubleshooting guide with multimeter tests and wiring verification

## Performance

- **I2C Clock Speed**: 100kHz (Standard Mode)
- **Register Read Time**: ~1-2ms per register
- **Typical Status Read**: ~10ms (all registers)
- **Recommended Poll Interval**: 30 seconds (battery status changes slowly)
- **I2C Bus Load**: Minimal (~0.1% at 30s interval)

## Thread Safety

PiSugarI2CController is **not thread-safe**. Use separate instances per thread or implement your own locking when accessing I2C from multiple threads.

## Dependencies

- **.NET 8.0 Windows** (with Windows 10 SDK 19041)
- **Windows.Devices.I2c** namespace (UWP APIs)
- **Microsoft.Windows.SDK.Contracts** NuGet package

All dependencies are included in the project file.

## References

### Documentation
- **[I2C_SETUP_GUIDE.md](I2C_SETUP_GUIDE.md)** - Complete hardware setup guide (wiring, parts list, troubleshooting)
- [PiSugar2 Plus Wiki](https://github.com/PiSugar/PiSugar/wiki/PiSugar2-Plus)
- [IP5209 Datasheet](https://github.com/PiSugar/PiSugar/tree/master/hardware)
- [LattePanda Documentation](https://docs.lattepanda.com/)

### API References
- [Windows.Devices.I2c](https://docs.microsoft.com/en-us/uwp/api/windows.devices.i2c)
- [UWP Device APIs](https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/)

### Hardware
- [PiSugar2 Plus Product](https://www.pisugar.com/)
- [LattePanda 3 Delta](https://www.lattepanda.com/lattepanda-3-delta)
- [PiSugar GitHub](https://github.com/PiSugar/PiSugar)

## License

This library is part of the Windows Phone Next project.
