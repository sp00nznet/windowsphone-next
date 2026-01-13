using System.IO.Ports;
using System.Text.RegularExpressions;

namespace WindowsPhoneNext.Maps;

/// <summary>
/// GPS Controller for VK-172 USB GPS dongle
/// Parses NMEA sentences from the serial port
/// </summary>
public class GpsController : IDisposable
{
    private SerialPort? _serialPort;
    private bool _isRunning;
    private Task? _readTask;
    private CancellationTokenSource? _cts;

    public event EventHandler<GpsPositionEventArgs>? PositionChanged;
    public event EventHandler<GpsStatusEventArgs>? StatusChanged;

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double Altitude { get; private set; }
    public double Speed { get; private set; } // km/h
    public double Heading { get; private set; } // degrees
    public int Satellites { get; private set; }
    public bool HasFix { get; private set; }
    public DateTime UtcTime { get; private set; }

    public bool IsConnected => _serialPort?.IsOpen ?? false;

    /// <summary>
    /// Attempts to auto-detect and connect to VK-172 GPS
    /// </summary>
    public async Task<bool> AutoConnectAsync()
    {
        var ports = SerialPort.GetPortNames();

        foreach (var port in ports)
        {
            try
            {
                if (await TryConnectAsync(port))
                {
                    StatusChanged?.Invoke(this, new GpsStatusEventArgs($"Connected to {port}"));
                    return true;
                }
            }
            catch
            {
                // Try next port
            }
        }

        StatusChanged?.Invoke(this, new GpsStatusEventArgs("GPS not found"));
        return false;
    }

    /// <summary>
    /// Connect to a specific COM port
    /// </summary>
    public async Task<bool> TryConnectAsync(string portName)
    {
        try
        {
            _serialPort?.Dispose();

            _serialPort = new SerialPort(portName)
            {
                BaudRate = 9600,  // VK-172 default baud rate
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 2000,
                WriteTimeout = 1000
            };

            _serialPort.Open();

            // Wait for valid NMEA data
            var timeout = DateTime.Now.AddSeconds(3);
            while (DateTime.Now < timeout)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    var line = _serialPort.ReadLine();
                    if (line.StartsWith("$GP") || line.StartsWith("$GN"))
                    {
                        StartReading();
                        return true;
                    }
                }
                await Task.Delay(100);
            }

            _serialPort.Close();
            return false;
        }
        catch
        {
            _serialPort?.Dispose();
            _serialPort = null;
            return false;
        }
    }

    private void StartReading()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();

        _readTask = Task.Run(async () =>
        {
            while (_isRunning && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_serialPort?.IsOpen == true && _serialPort.BytesToRead > 0)
                    {
                        var line = _serialPort.ReadLine();
                        ParseNmea(line.Trim());
                    }
                    else
                    {
                        await Task.Delay(50, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(100, _cts.Token);
                }
            }
        }, _cts.Token);
    }

    private void ParseNmea(string sentence)
    {
        if (string.IsNullOrEmpty(sentence) || !sentence.StartsWith("$"))
            return;

        // Remove checksum
        var checksumIndex = sentence.IndexOf('*');
        if (checksumIndex > 0)
            sentence = sentence[..checksumIndex];

        var parts = sentence.Split(',');
        if (parts.Length < 2) return;

        var type = parts[0];

        switch (type)
        {
            case "$GPGGA":
            case "$GNGGA":
                ParseGGA(parts);
                break;
            case "$GPRMC":
            case "$GNRMC":
                ParseRMC(parts);
                break;
            case "$GPVTG":
            case "$GNVTG":
                ParseVTG(parts);
                break;
            case "$GPGSA":
            case "$GNGSA":
                ParseGSA(parts);
                break;
        }
    }

    /// <summary>
    /// Parse GGA - Global Positioning System Fix Data
    /// </summary>
    private void ParseGGA(string[] parts)
    {
        if (parts.Length < 10) return;

        // Fix quality: 0=invalid, 1=GPS, 2=DGPS
        if (int.TryParse(parts[6], out int fixQuality))
        {
            HasFix = fixQuality > 0;
        }

        // Number of satellites
        if (int.TryParse(parts[7], out int sats))
        {
            Satellites = sats;
        }

        if (HasFix)
        {
            // Latitude
            if (TryParseCoordinate(parts[2], parts[3], out double lat))
            {
                Latitude = lat;
            }

            // Longitude
            if (TryParseCoordinate(parts[4], parts[5], out double lon))
            {
                Longitude = lon;
            }

            // Altitude
            if (double.TryParse(parts[9], out double alt))
            {
                Altitude = alt;
            }

            NotifyPositionChanged();
        }
    }

    /// <summary>
    /// Parse RMC - Recommended Minimum Navigation Information
    /// </summary>
    private void ParseRMC(string[] parts)
    {
        if (parts.Length < 10) return;

        // Status: A=active, V=void
        HasFix = parts[2] == "A";

        if (HasFix)
        {
            // Latitude
            if (TryParseCoordinate(parts[3], parts[4], out double lat))
            {
                Latitude = lat;
            }

            // Longitude
            if (TryParseCoordinate(parts[5], parts[6], out double lon))
            {
                Longitude = lon;
            }

            // Speed in knots -> km/h
            if (double.TryParse(parts[7], out double knots))
            {
                Speed = knots * 1.852;
            }

            // Heading/Track
            if (double.TryParse(parts[8], out double heading))
            {
                Heading = heading;
            }

            // Time
            if (parts[1].Length >= 6 && parts[9].Length >= 6)
            {
                try
                {
                    var timeStr = parts[1];
                    var dateStr = parts[9];
                    UtcTime = new DateTime(
                        2000 + int.Parse(dateStr[4..6]),
                        int.Parse(dateStr[2..4]),
                        int.Parse(dateStr[..2]),
                        int.Parse(timeStr[..2]),
                        int.Parse(timeStr[2..4]),
                        int.Parse(timeStr[4..6]),
                        DateTimeKind.Utc);
                }
                catch { }
            }

            NotifyPositionChanged();
        }
    }

    /// <summary>
    /// Parse VTG - Track Made Good and Ground Speed
    /// </summary>
    private void ParseVTG(string[] parts)
    {
        if (parts.Length < 8) return;

        // True track
        if (double.TryParse(parts[1], out double heading))
        {
            Heading = heading;
        }

        // Speed in km/h
        if (double.TryParse(parts[7], out double kmh))
        {
            Speed = kmh;
        }
    }

    /// <summary>
    /// Parse GSA - GPS DOP and active satellites
    /// </summary>
    private void ParseGSA(string[] parts)
    {
        if (parts.Length < 3) return;

        // Fix type: 1=no fix, 2=2D, 3=3D
        if (int.TryParse(parts[2], out int fixType))
        {
            HasFix = fixType >= 2;
        }
    }

    private static bool TryParseCoordinate(string value, string direction, out double result)
    {
        result = 0;
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(direction))
            return false;

        try
        {
            // NMEA format: DDDMM.MMMM or DDMM.MMMM
            var decimalPos = value.IndexOf('.');
            if (decimalPos < 2) return false;

            var degreeLength = decimalPos - 2;
            var degrees = double.Parse(value[..degreeLength]);
            var minutes = double.Parse(value[degreeLength..]);

            result = degrees + (minutes / 60.0);

            if (direction == "S" || direction == "W")
                result = -result;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void NotifyPositionChanged()
    {
        PositionChanged?.Invoke(this, new GpsPositionEventArgs(
            Latitude, Longitude, Altitude, Speed, Heading, Satellites, HasFix));
    }

    public void Disconnect()
    {
        _isRunning = false;
        _cts?.Cancel();

        try
        {
            _serialPort?.Close();
        }
        catch { }

        StatusChanged?.Invoke(this, new GpsStatusEventArgs("Disconnected"));
    }

    public void Dispose()
    {
        Disconnect();
        _serialPort?.Dispose();
        _cts?.Dispose();
    }
}

public class GpsPositionEventArgs : EventArgs
{
    public double Latitude { get; }
    public double Longitude { get; }
    public double Altitude { get; }
    public double Speed { get; }
    public double Heading { get; }
    public int Satellites { get; }
    public bool HasFix { get; }

    public GpsPositionEventArgs(double lat, double lon, double alt, double speed, double heading, int sats, bool fix)
    {
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
        Speed = speed;
        Heading = heading;
        Satellites = sats;
        HasFix = fix;
    }
}

public class GpsStatusEventArgs : EventArgs
{
    public string Message { get; }

    public GpsStatusEventArgs(string message)
    {
        Message = message;
    }
}
