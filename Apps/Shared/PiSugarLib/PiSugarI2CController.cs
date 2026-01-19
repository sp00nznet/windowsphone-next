using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace WindowsPhoneNext.PiSugarLib;

/// <summary>
/// Controller for PiSugar2/PiSugar2 Plus battery management via I2C
/// Communicates directly with IP5209/IP5312 power management IC
/// Requires physical I2C connection to LattePanda Arduino headers
/// </summary>
public class PiSugarI2CController : IDisposable
{
    // IP5209/IP5312 I2C address
    private const byte I2C_ADDRESS = 0x75; // Try 0x32 if 0x75 doesn't work

    // Register addresses
    private const byte REG_SYS_CTL0 = 0x00;
    private const byte REG_SYS_CTL1 = 0x01;
    private const byte REG_SYS_CTL2 = 0x02;
    private const byte REG_READ0 = 0xA0;  // Battery voltage high byte
    private const byte REG_READ1 = 0xA1;  // Battery voltage low byte
    private const byte REG_READ2 = 0xA2;  // Battery current high byte
    private const byte REG_READ3 = 0xA3;  // Battery current low byte
    private const byte REG_READ4 = 0xA4;  // Battery percentage
    private const byte REG_CHG_DIG_CTL0 = 0x22;

    // Conversion constants
    private const double VOLTAGE_MULTIPLIER = 0.26855;
    private const double VOLTAGE_OFFSET = 2600.0; // mV
    private const double CURRENT_MULTIPLIER = 0.745985;

    private I2cDevice? _i2cDevice;
    private bool _isDisposed;

    /// <summary>
    /// Initializes the I2C connection to PiSugar2
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            // Get I2C controller selector
            var i2cSelector = I2cDevice.GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(i2cSelector);

            if (devices.Count == 0)
            {
                return false; // No I2C controllers found
            }

            // Configure I2C settings for IP5209/IP5312
            var settings = new I2cConnectionSettings(I2C_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.StandardMode, // 100kHz
                SharingMode = I2cSharingMode.Shared
            };

            // Create I2C device
            _i2cDevice = await I2cDevice.FromIdAsync(devices[0].Id, settings);

            if (_i2cDevice == null)
            {
                return false;
            }

            // Test communication by reading a register
            var testData = new byte[1];
            try
            {
                _i2cDevice.WriteRead(new byte[] { REG_READ4 }, testData);
                return true; // Communication successful
            }
            catch
            {
                _i2cDevice?.Dispose();
                _i2cDevice = null;
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if PiSugar2 is available on I2C bus
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        if (_i2cDevice != null)
        {
            return true;
        }

        return await InitializeAsync();
    }

    /// <summary>
    /// Gets battery level percentage (0-100)
    /// </summary>
    public async Task<int> GetBatteryLevelAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var data = ReadRegister(REG_READ4);
            var percentage = data % 101; // Ensure 0-100 range
            return percentage;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Gets battery voltage in volts
    /// </summary>
    public async Task<double> GetBatteryVoltageAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var highByte = ReadRegister(REG_READ0);
            var lowByte = ReadRegister(REG_READ1);

            var rawValue = (highByte << 8) | lowByte;
            var voltageMillivolts = (rawValue * VOLTAGE_MULTIPLIER) + VOLTAGE_OFFSET;
            var voltageVolts = voltageMillivolts / 1000.0;

            return voltageVolts;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Gets battery current in amps (positive = charging, negative = discharging)
    /// </summary>
    public async Task<double> GetBatteryCurrentAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var highByte = ReadRegister(REG_READ2);
            var lowByte = ReadRegister(REG_READ3);

            var rawValue = (highByte << 8) | lowByte;

            // Convert to signed 16-bit
            if (rawValue > 32767)
            {
                rawValue = rawValue - 65536;
            }

            var currentMilliamps = rawValue * CURRENT_MULTIPLIER;
            var currentAmps = currentMilliamps / 1000.0;

            return currentAmps;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks if battery is currently charging (based on current direction)
    /// </summary>
    public async Task<bool> IsChargingAsync()
    {
        var current = await GetBatteryCurrentAsync();
        return current > 0.05; // Charging if current > 50mA
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

            var voltage = await GetBatteryVoltageAsync();
            var current = await GetBatteryCurrentAsync();
            var isCharging = current > 0.05;

            // Detect if power is plugged (charging or trickle charge)
            var isPowerPlugged = isCharging || (level >= 99 && Math.Abs(current) < 0.01);

            return new BatteryStatus
            {
                Level = level,
                Voltage = voltage,
                Current = current,
                IsCharging = isCharging,
                IsPowerPlugged = isPowerPlugged
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a single register from the I2C device
    /// </summary>
    private byte ReadRegister(byte register)
    {
        if (_i2cDevice == null)
        {
            throw new InvalidOperationException("I2C device not initialized");
        }

        var writeBuffer = new byte[] { register };
        var readBuffer = new byte[1];

        _i2cDevice.WriteRead(writeBuffer, readBuffer);

        return readBuffer[0];
    }

    /// <summary>
    /// Writes a single register to the I2C device
    /// </summary>
    private void WriteRegister(byte register, byte value)
    {
        if (_i2cDevice == null)
        {
            throw new InvalidOperationException("I2C device not initialized");
        }

        var buffer = new byte[] { register, value };
        _i2cDevice.Write(buffer);
    }

    /// <summary>
    /// Ensures I2C device is initialized
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_i2cDevice == null)
        {
            var success = await InitializeAsync();
            if (!success)
            {
                throw new InvalidOperationException("Failed to initialize I2C connection to PiSugar2");
            }
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _i2cDevice?.Dispose();
            _i2cDevice = null;
            _isDisposed = true;
        }
    }
}
