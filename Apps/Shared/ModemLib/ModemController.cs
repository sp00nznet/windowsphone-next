using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsPhoneNext.ModemLib;

/// <summary>
/// Controller for EM06-A LTE modem using AT commands
/// </summary>
public class ModemController : IDisposable
{
    private SerialPort? _serialPort;
    private readonly object _lock = new();
    private bool _disposed;
    private readonly StringBuilder _responseBuffer = new();

    public event EventHandler<string>? DataReceived;
    public event EventHandler<IncomingCallEventArgs>? IncomingCall;
    public event EventHandler<SmsReceivedEventArgs>? SmsReceived;
    public event EventHandler<CallStateChangedEventArgs>? CallStateChanged;
    public event EventHandler<SignalStrengthEventArgs>? SignalStrengthChanged;

    public bool IsConnected => _serialPort?.IsOpen ?? false;
    public string? PortName => _serialPort?.PortName;

    public ModemController()
    {
    }

    /// <summary>
    /// Connect to the modem on the specified COM port
    /// </summary>
    public async Task<bool> ConnectAsync(string portName, int baudRate = 115200)
    {
        try
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _serialPort?.Dispose();

                    _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout = 5000,
                        WriteTimeout = 5000,
                        DtrEnable = true,
                        RtsEnable = true,
                        NewLine = "\r\n"
                    };

                    _serialPort.DataReceived += OnSerialDataReceived;
                    _serialPort.Open();
                }
            });

            // Test connection with AT command
            var response = await SendCommandAsync("AT");
            return response.Contains("OK");
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Disconnect from the modem
    /// </summary>
    public void Disconnect()
    {
        lock (_lock)
        {
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
            }
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }

    /// <summary>
    /// Auto-detect and connect to EM06-A modem
    /// </summary>
    public async Task<bool> AutoConnectAsync()
    {
        var ports = SerialPort.GetPortNames();

        foreach (var port in ports)
        {
            if (await ConnectAsync(port))
            {
                // Verify it's a Quectel modem
                var manufacturer = await SendCommandAsync("AT+CGMI");
                if (manufacturer.Contains("Quectel", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                Disconnect();
            }
        }

        return false;
    }

    /// <summary>
    /// Send AT command and wait for response
    /// </summary>
    public async Task<string> SendCommandAsync(string command, int timeoutMs = 5000)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Modem not connected");

        return await Task.Run(() =>
        {
            lock (_lock)
            {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                _serialPort.WriteLine(command);

                var response = new StringBuilder();
                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
                {
                    try
                    {
                        var line = _serialPort.ReadLine();
                        response.AppendLine(line);

                        if (line.Contains("OK") || line.Contains("ERROR") || line.Contains(">"))
                        {
                            break;
                        }
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                return response.ToString();
            }
        });
    }

    /// <summary>
    /// Initialize modem for voice and SMS
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            // Reset to factory defaults
            await SendCommandAsync("ATZ");
            await Task.Delay(500);

            // Echo off
            await SendCommandAsync("ATE0");

            // Set text mode for SMS
            await SendCommandAsync("AT+CMGF=1");

            // Enable caller ID
            await SendCommandAsync("AT+CLIP=1");

            // Set SMS storage to SIM
            await SendCommandAsync("AT+CPMS=\"SM\",\"SM\",\"SM\"");

            // Enable new SMS notification
            await SendCommandAsync("AT+CNMI=2,1,0,0,0");

            // Set audio mode for voice calls
            await SendCommandAsync("AT+QAUDMOD=0");

            return true;
        }
        catch
        {
            return false;
        }
    }

    #region Voice Call Functions

    /// <summary>
    /// Make a voice call
    /// </summary>
    public async Task<bool> DialAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Clean phone number
        var cleanNumber = Regex.Replace(phoneNumber, @"[^\d+]", "");

        var response = await SendCommandAsync($"ATD{cleanNumber};", 30000);
        return !response.Contains("ERROR");
    }

    /// <summary>
    /// Answer incoming call
    /// </summary>
    public async Task<bool> AnswerCallAsync()
    {
        var response = await SendCommandAsync("ATA", 10000);
        return response.Contains("OK");
    }

    /// <summary>
    /// Hang up current call
    /// </summary>
    public async Task<bool> HangUpAsync()
    {
        var response = await SendCommandAsync("ATH");
        return response.Contains("OK");
    }

    /// <summary>
    /// Send DTMF tone during call
    /// </summary>
    public async Task<bool> SendDtmfAsync(char digit)
    {
        if (!"0123456789*#ABCD".Contains(digit))
            return false;

        var response = await SendCommandAsync($"AT+VTS={digit}");
        return response.Contains("OK");
    }

    /// <summary>
    /// Get current call status
    /// </summary>
    public async Task<CallStatus> GetCallStatusAsync()
    {
        var response = await SendCommandAsync("AT+CLCC");

        if (response.Contains("+CLCC:"))
        {
            if (response.Contains(",0,")) return CallStatus.Active;
            if (response.Contains(",1,")) return CallStatus.Held;
            if (response.Contains(",2,")) return CallStatus.Dialing;
            if (response.Contains(",3,")) return CallStatus.Alerting;
            if (response.Contains(",4,")) return CallStatus.Incoming;
            if (response.Contains(",5,")) return CallStatus.Waiting;
        }

        return CallStatus.Idle;
    }

    #endregion

    #region SMS Functions

    /// <summary>
    /// Send SMS message
    /// </summary>
    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(message))
            return false;

        var cleanNumber = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // Set text mode
        await SendCommandAsync("AT+CMGF=1");

        // Start SMS
        var response = await SendCommandAsync($"AT+CMGS=\"{cleanNumber}\"", 10000);

        if (response.Contains(">"))
        {
            // Send message content with Ctrl+Z
            if (_serialPort != null)
            {
                lock (_lock)
                {
                    _serialPort.Write(message);
                    _serialPort.Write(new byte[] { 0x1A }, 0, 1); // Ctrl+Z
                }
            }

            // Wait for send confirmation
            await Task.Delay(5000);
            var result = await SendCommandAsync("", 10000);
            return result.Contains("+CMGS:") || result.Contains("OK");
        }

        return false;
    }

    /// <summary>
    /// Read all SMS messages
    /// </summary>
    public async Task<List<SmsMessage>> ReadAllSmsAsync()
    {
        var messages = new List<SmsMessage>();

        // Set text mode
        await SendCommandAsync("AT+CMGF=1");

        // Read all messages
        var response = await SendCommandAsync("AT+CMGL=\"ALL\"", 10000);

        var lines = response.Split('\n');
        SmsMessage? currentMessage = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("+CMGL:"))
            {
                // Parse header: +CMGL: index,"status","sender","","timestamp"
                var match = Regex.Match(trimmedLine, @"\+CMGL:\s*(\d+),""([^""]*)"",""([^""]*)"","""",""([^""]*)""");
                if (match.Success)
                {
                    currentMessage = new SmsMessage
                    {
                        Index = int.Parse(match.Groups[1].Value),
                        Status = match.Groups[2].Value,
                        Sender = match.Groups[3].Value,
                        Timestamp = ParseSmsTimestamp(match.Groups[4].Value)
                    };
                }
            }
            else if (currentMessage != null && !string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith("OK"))
            {
                currentMessage.Body = trimmedLine;
                messages.Add(currentMessage);
                currentMessage = null;
            }
        }

        return messages;
    }

    /// <summary>
    /// Delete SMS message by index
    /// </summary>
    public async Task<bool> DeleteSmsAsync(int index)
    {
        var response = await SendCommandAsync($"AT+CMGD={index}");
        return response.Contains("OK");
    }

    /// <summary>
    /// Delete all SMS messages
    /// </summary>
    public async Task<bool> DeleteAllSmsAsync()
    {
        var response = await SendCommandAsync("AT+CMGD=1,4");
        return response.Contains("OK");
    }

    private static DateTime ParseSmsTimestamp(string timestamp)
    {
        // Format: "yy/MM/dd,HH:mm:ss+tz"
        try
        {
            var match = Regex.Match(timestamp, @"(\d{2})/(\d{2})/(\d{2}),(\d{2}):(\d{2}):(\d{2})");
            if (match.Success)
            {
                return new DateTime(
                    2000 + int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value),
                    int.Parse(match.Groups[4].Value),
                    int.Parse(match.Groups[5].Value),
                    int.Parse(match.Groups[6].Value)
                );
            }
        }
        catch { }

        return DateTime.Now;
    }

    #endregion

    #region Network Functions

    /// <summary>
    /// Get signal strength (0-31, 99=unknown)
    /// </summary>
    public async Task<int> GetSignalStrengthAsync()
    {
        var response = await SendCommandAsync("AT+CSQ");

        var match = Regex.Match(response, @"\+CSQ:\s*(\d+),");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        return 99;
    }

    /// <summary>
    /// Get network registration status
    /// </summary>
    public async Task<NetworkStatus> GetNetworkStatusAsync()
    {
        var response = await SendCommandAsync("AT+CREG?");

        var match = Regex.Match(response, @"\+CREG:\s*\d+,(\d+)");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value) switch
            {
                1 => NetworkStatus.RegisteredHome,
                2 => NetworkStatus.Searching,
                3 => NetworkStatus.Denied,
                5 => NetworkStatus.RegisteredRoaming,
                _ => NetworkStatus.NotRegistered
            };
        }

        return NetworkStatus.NotRegistered;
    }

    /// <summary>
    /// Get carrier/operator name
    /// </summary>
    public async Task<string> GetOperatorNameAsync()
    {
        var response = await SendCommandAsync("AT+COPS?");

        var match = Regex.Match(response, @"\+COPS:\s*\d+,\d+,""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return "Unknown";
    }

    /// <summary>
    /// Get phone number from SIM
    /// </summary>
    public async Task<string?> GetPhoneNumberAsync()
    {
        // AT+CNUM returns the phone number stored on the SIM
        var response = await SendCommandAsync("AT+CNUM");

        var match = Regex.Match(response, @"\+CNUM:\s*""[^""]*"",""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// Get modem model info
    /// </summary>
    public async Task<ModemInfo> GetModemInfoAsync()
    {
        var info = new ModemInfo();

        var manufacturer = await SendCommandAsync("AT+CGMI");
        var match = Regex.Match(manufacturer, @"(?:OK\s*)?(\w+)");
        if (match.Success) info.Manufacturer = match.Groups[1].Value.Trim();

        var model = await SendCommandAsync("AT+CGMM");
        match = Regex.Match(model, @"(?:OK\s*)?(\w+)");
        if (match.Success) info.Model = match.Groups[1].Value.Trim();

        var imei = await SendCommandAsync("AT+CGSN");
        match = Regex.Match(imei, @"(\d{15})");
        if (match.Success) info.IMEI = match.Groups[1].Value;

        var firmware = await SendCommandAsync("AT+CGMR");
        info.FirmwareVersion = firmware.Replace("OK", "").Trim();

        return info;
    }

    #endregion

    #region Event Handling

    private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null) return;

        try
        {
            var data = _serialPort.ReadExisting();
            _responseBuffer.Append(data);

            var bufferContent = _responseBuffer.ToString();
            DataReceived?.Invoke(this, bufferContent);

            // Check for unsolicited responses
            ProcessUnsolicitedResponses(bufferContent);
        }
        catch { }
    }

    private void ProcessUnsolicitedResponses(string data)
    {
        // Incoming call
        if (data.Contains("RING") || data.Contains("+CLIP:"))
        {
            var match = Regex.Match(data, @"\+CLIP:\s*""([^""]+)""");
            var callerId = match.Success ? match.Groups[1].Value : "Unknown";
            IncomingCall?.Invoke(this, new IncomingCallEventArgs(callerId));
        }

        // Call ended
        if (data.Contains("NO CARRIER") || data.Contains("BUSY") || data.Contains("NO ANSWER"))
        {
            CallStateChanged?.Invoke(this, new CallStateChangedEventArgs(CallStatus.Idle));
        }

        // New SMS
        if (data.Contains("+CMTI:"))
        {
            var match = Regex.Match(data, @"\+CMTI:\s*""[^""]*"",(\d+)");
            if (match.Success)
            {
                var index = int.Parse(match.Groups[1].Value);
                SmsReceived?.Invoke(this, new SmsReceivedEventArgs(index));
            }
        }
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

#region Data Classes and Enums

public enum CallStatus
{
    Idle,
    Active,
    Held,
    Dialing,
    Alerting,
    Incoming,
    Waiting
}

public enum NetworkStatus
{
    NotRegistered,
    RegisteredHome,
    Searching,
    Denied,
    RegisteredRoaming
}

public class SmsMessage
{
    public int Index { get; set; }
    public string Status { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class ModemInfo
{
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public string IMEI { get; set; } = "";
    public string FirmwareVersion { get; set; } = "";
}

public class IncomingCallEventArgs : EventArgs
{
    public string CallerId { get; }
    public IncomingCallEventArgs(string callerId) => CallerId = callerId;
}

public class SmsReceivedEventArgs : EventArgs
{
    public int MessageIndex { get; }
    public SmsReceivedEventArgs(int index) => MessageIndex = index;
}

public class CallStateChangedEventArgs : EventArgs
{
    public CallStatus Status { get; }
    public CallStateChangedEventArgs(CallStatus status) => Status = status;
}

public class SignalStrengthEventArgs : EventArgs
{
    public int Strength { get; }
    public SignalStrengthEventArgs(int strength) => Strength = strength;
}

#endregion
