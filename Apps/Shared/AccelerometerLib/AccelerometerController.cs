using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsPhoneNext.AccelerometerLib
{
    /// <summary>
    /// Screen orientation based on accelerometer readings
    /// </summary>
    public enum ScreenOrientation
    {
        Portrait,           // Normal upright position
        PortraitFlipped,    // Upside down
        LandscapeLeft,      // Rotated 90° counter-clockwise
        LandscapeRight,     // Rotated 90° clockwise
        FaceUp,             // Lying flat, screen up
        FaceDown            // Lying flat, screen down
    }

    /// <summary>
    /// Acceleration data from the sensor
    /// </summary>
    public class AccelerationData
    {
        public double X { get; set; }  // m/s² or g
        public double Y { get; set; }
        public double Z { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    /// <summary>
    /// Event args for orientation change
    /// </summary>
    public class OrientationChangedEventArgs : EventArgs
    {
        public ScreenOrientation OldOrientation { get; set; }
        public ScreenOrientation NewOrientation { get; set; }
        public AccelerationData AccelerationData { get; set; } = new();
    }

    /// <summary>
    /// Event args for acceleration data
    /// </summary>
    public class AccelerationEventArgs : EventArgs
    {
        public AccelerationData Data { get; set; } = new();
    }

    /// <summary>
    /// Controller for BIGTREETECH S2DW V1.0 accelerometer (LIS2DW12 chip)
    /// Communicates via SPI through USB-to-SPI adapter or direct SPI pins
    /// </summary>
    public class AccelerometerController : IDisposable
    {
        // LIS2DW12 Register addresses
        private const byte REG_WHO_AM_I = 0x0F;
        private const byte REG_CTRL1 = 0x20;
        private const byte REG_CTRL2 = 0x21;
        private const byte REG_CTRL3 = 0x22;
        private const byte REG_CTRL4_INT1 = 0x23;
        private const byte REG_CTRL5_INT2 = 0x24;
        private const byte REG_CTRL6 = 0x25;
        private const byte REG_OUT_T = 0x26;
        private const byte REG_STATUS = 0x27;
        private const byte REG_OUT_X_L = 0x28;
        private const byte REG_OUT_X_H = 0x29;
        private const byte REG_OUT_Y_L = 0x2A;
        private const byte REG_OUT_Y_H = 0x2B;
        private const byte REG_OUT_Z_L = 0x2C;
        private const byte REG_OUT_Z_H = 0x2D;

        // LIS2DW12 WHO_AM_I value
        private const byte WHO_AM_I_VALUE = 0x44;

        // Sensitivity for ±2g range (mg/LSB)
        private const double SENSITIVITY_2G = 0.244;

        private SerialPort? _serialPort;
        private CancellationTokenSource? _cts;
        private Task? _readTask;
        private ScreenOrientation _currentOrientation = ScreenOrientation.Portrait;
        private bool _isDemoMode = false;
        private Random _demoRandom = new();

        // Orientation detection thresholds
        private const double GRAVITY = 9.81;
        private const double THRESHOLD_PORTRAIT = 7.0;      // ~0.7g
        private const double THRESHOLD_LANDSCAPE = 7.0;
        private const double THRESHOLD_FLAT = 8.0;          // ~0.8g for face up/down

        public bool IsConnected { get; private set; }
        public bool IsDemoMode => _isDemoMode;
        public ScreenOrientation CurrentOrientation => _currentOrientation;

        public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;
        public event EventHandler<AccelerationEventArgs>? AccelerationDataReceived;

        /// <summary>
        /// Attempts to connect to the accelerometer on available COM ports
        /// </summary>
        public async Task<bool> AutoConnectAsync()
        {
            var ports = SerialPort.GetPortNames();

            foreach (var portName in ports)
            {
                try
                {
                    if (await ConnectAsync(portName))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Try next port
                }
            }

            // No hardware found, enable demo mode
            _isDemoMode = true;
            IsConnected = true;
            return true;
        }

        /// <summary>
        /// Connect to accelerometer on specified COM port
        /// </summary>
        public async Task<bool> ConnectAsync(string portName)
        {
            try
            {
                _serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.Open();

                // Verify it's the right device by reading WHO_AM_I
                var whoAmI = await ReadRegisterAsync(REG_WHO_AM_I);
                if (whoAmI == WHO_AM_I_VALUE)
                {
                    await InitializeAsync();
                    IsConnected = true;
                    _isDemoMode = false;
                    return true;
                }

                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
            catch
            {
                _serialPort?.Dispose();
                _serialPort = null;
            }

            return false;
        }

        /// <summary>
        /// Initialize the LIS2DW12 sensor
        /// </summary>
        private async Task InitializeAsync()
        {
            // CTRL1: 100Hz ODR, High-Performance mode
            await WriteRegisterAsync(REG_CTRL1, 0x54);

            // CTRL2: Default settings
            await WriteRegisterAsync(REG_CTRL2, 0x00);

            // CTRL6: ±2g full scale, low noise enabled
            await WriteRegisterAsync(REG_CTRL6, 0x04);

            await Task.Delay(10);
        }

        /// <summary>
        /// Start continuous reading of accelerometer data
        /// </summary>
        public void StartReading(int intervalMs = 50)
        {
            if (_readTask != null)
                return;

            _cts = new CancellationTokenSource();
            _readTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var data = await ReadAccelerationAsync();
                        AccelerationDataReceived?.Invoke(this, new AccelerationEventArgs { Data = data });

                        var newOrientation = CalculateOrientation(data);
                        if (newOrientation != _currentOrientation)
                        {
                            var oldOrientation = _currentOrientation;
                            _currentOrientation = newOrientation;
                            OrientationChanged?.Invoke(this, new OrientationChangedEventArgs
                            {
                                OldOrientation = oldOrientation,
                                NewOrientation = newOrientation,
                                AccelerationData = data
                            });
                        }

                        await Task.Delay(intervalMs, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Continue on read errors
                        await Task.Delay(100, _cts.Token);
                    }
                }
            }, _cts.Token);
        }

        /// <summary>
        /// Stop continuous reading
        /// </summary>
        public void StopReading()
        {
            _cts?.Cancel();
            _readTask?.Wait(1000);
            _readTask = null;
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// Read current acceleration data
        /// </summary>
        public async Task<AccelerationData> ReadAccelerationAsync()
        {
            if (_isDemoMode)
            {
                return GenerateDemoData();
            }

            // Read X, Y, Z registers (6 bytes starting from OUT_X_L with auto-increment)
            var rawData = await ReadRegistersAsync(REG_OUT_X_L | 0x80, 6);  // 0x80 for auto-increment

            // Convert to signed 16-bit values
            short rawX = (short)((rawData[1] << 8) | rawData[0]);
            short rawY = (short)((rawData[3] << 8) | rawData[2]);
            short rawZ = (short)((rawData[5] << 8) | rawData[4]);

            // Convert to m/s² (sensitivity * raw value * gravity / 1000)
            return new AccelerationData
            {
                X = rawX * SENSITIVITY_2G * GRAVITY / 1000.0,
                Y = rawY * SENSITIVITY_2G * GRAVITY / 1000.0,
                Z = rawZ * SENSITIVITY_2G * GRAVITY / 1000.0,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// Calculate screen orientation from acceleration data
        /// </summary>
        private ScreenOrientation CalculateOrientation(AccelerationData data)
        {
            double absX = Math.Abs(data.X);
            double absY = Math.Abs(data.Y);
            double absZ = Math.Abs(data.Z);

            // Check for flat positions first (Z dominant)
            if (absZ > THRESHOLD_FLAT && absZ > absX && absZ > absY)
            {
                return data.Z > 0 ? ScreenOrientation.FaceUp : ScreenOrientation.FaceDown;
            }

            // Check for landscape (X dominant)
            if (absX > THRESHOLD_LANDSCAPE && absX > absY)
            {
                return data.X > 0 ? ScreenOrientation.LandscapeRight : ScreenOrientation.LandscapeLeft;
            }

            // Check for portrait (Y dominant)
            if (absY > THRESHOLD_PORTRAIT)
            {
                return data.Y > 0 ? ScreenOrientation.Portrait : ScreenOrientation.PortraitFlipped;
            }

            // Default to current orientation if unclear
            return _currentOrientation;
        }

        /// <summary>
        /// Generate demo acceleration data for testing
        /// </summary>
        private AccelerationData GenerateDemoData()
        {
            // Simulate slight movement with gravity pointing down (Portrait mode)
            double noise = 0.1;
            return new AccelerationData
            {
                X = (_demoRandom.NextDouble() - 0.5) * noise,
                Y = GRAVITY + (_demoRandom.NextDouble() - 0.5) * noise,  // Gravity in Y for portrait
                Z = (_demoRandom.NextDouble() - 0.5) * noise,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// Simulate orientation change for demo mode
        /// </summary>
        public void SimulateOrientation(ScreenOrientation orientation)
        {
            if (!_isDemoMode)
                return;

            var oldOrientation = _currentOrientation;
            _currentOrientation = orientation;

            var data = orientation switch
            {
                ScreenOrientation.Portrait => new AccelerationData { X = 0, Y = GRAVITY, Z = 0 },
                ScreenOrientation.PortraitFlipped => new AccelerationData { X = 0, Y = -GRAVITY, Z = 0 },
                ScreenOrientation.LandscapeLeft => new AccelerationData { X = -GRAVITY, Y = 0, Z = 0 },
                ScreenOrientation.LandscapeRight => new AccelerationData { X = GRAVITY, Y = 0, Z = 0 },
                ScreenOrientation.FaceUp => new AccelerationData { X = 0, Y = 0, Z = GRAVITY },
                ScreenOrientation.FaceDown => new AccelerationData { X = 0, Y = 0, Z = -GRAVITY },
                _ => new AccelerationData { X = 0, Y = GRAVITY, Z = 0 }
            };

            OrientationChanged?.Invoke(this, new OrientationChangedEventArgs
            {
                OldOrientation = oldOrientation,
                NewOrientation = orientation,
                AccelerationData = data
            });
        }

        #region SPI Communication

        private async Task<byte> ReadRegisterAsync(byte register)
        {
            var data = await ReadRegistersAsync(register, 1);
            return data[0];
        }

        private async Task<byte[]> ReadRegistersAsync(byte startRegister, int count)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Serial port not connected");

            // SPI read command: register address with read bit set (MSB = 1)
            byte[] command = new byte[count + 1];
            command[0] = (byte)(startRegister | 0x80);  // Set read bit

            _serialPort.Write(command, 0, command.Length);

            await Task.Delay(5);

            byte[] response = new byte[count];
            int bytesRead = _serialPort.Read(response, 0, count);

            if (bytesRead != count)
                throw new IOException($"Expected {count} bytes, got {bytesRead}");

            return response;
        }

        private async Task WriteRegisterAsync(byte register, byte value)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Serial port not connected");

            // SPI write command: register address with write bit clear (MSB = 0)
            byte[] command = new byte[] { (byte)(register & 0x7F), value };

            _serialPort.Write(command, 0, command.Length);

            await Task.Delay(5);
        }

        #endregion

        public void Dispose()
        {
            StopReading();
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
            IsConnected = false;
        }
    }
}
