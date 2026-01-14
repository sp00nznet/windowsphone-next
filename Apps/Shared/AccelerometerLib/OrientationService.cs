using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsPhoneNext.AccelerometerLib
{
    /// <summary>
    /// Global orientation service that broadcasts orientation changes via named pipe
    /// Apps can subscribe to receive orientation updates
    /// </summary>
    public class OrientationService : IDisposable
    {
        private const string PIPE_NAME = "WindowsPhoneNextOrientation";
        private const string STATE_FILE = "orientation_state.json";

        private static OrientationService? _instance;
        private static readonly object _lock = new();

        private AccelerometerController? _accelerometer;
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cts;
        private Task? _serverTask;
        private ScreenOrientation _currentOrientation = ScreenOrientation.Portrait;
        private bool _isServer;

        public static OrientationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new OrientationService();
                    }
                }
                return _instance;
            }
        }

        public ScreenOrientation CurrentOrientation => _currentOrientation;
        public bool IsLandscape => _currentOrientation == ScreenOrientation.LandscapeLeft ||
                                   _currentOrientation == ScreenOrientation.LandscapeRight;
        public bool IsPortrait => _currentOrientation == ScreenOrientation.Portrait ||
                                  _currentOrientation == ScreenOrientation.PortraitFlipped;

        public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

        private OrientationService()
        {
            LoadState();
        }

        /// <summary>
        /// Start as the orientation server (only one app should do this - typically Launcher or Accelerometer app)
        /// </summary>
        public async Task StartAsServerAsync()
        {
            if (_isServer)
                return;

            _accelerometer = new AccelerometerController();
            await _accelerometer.AutoConnectAsync();
            _accelerometer.OrientationChanged += OnAccelerometerOrientationChanged;
            _accelerometer.StartReading(100);  // 10Hz updates

            _isServer = true;
            _cts = new CancellationTokenSource();

            // Start pipe server for broadcasting to other apps
            _serverTask = Task.Run(RunPipeServerAsync, _cts.Token);
        }

        /// <summary>
        /// Start as a client that listens for orientation changes
        /// </summary>
        public void StartAsClient()
        {
            if (_isServer)
                return;

            _cts = new CancellationTokenSource();
            Task.Run(RunPipeClientAsync, _cts.Token);
        }

        private async Task RunPipeServerAsync()
        {
            while (!_cts!.Token.IsCancellationRequested)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.Out,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                    await _pipeServer.WaitForConnectionAsync(_cts.Token);

                    // Send current orientation
                    await SendOrientationAsync(_pipeServer, _currentOrientation);

                    // Keep connection alive until client disconnects
                    while (_pipeServer.IsConnected && !_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Restart server on errors
                    await Task.Delay(500);
                }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            }
        }

        private async Task RunPipeClientAsync()
        {
            while (!_cts!.Token.IsCancellationRequested)
            {
                try
                {
                    using var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.In);

                    await pipeClient.ConnectAsync(5000, _cts.Token);

                    using var reader = new StreamReader(pipeClient);

                    while (pipeClient.IsConnected && !_cts.Token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line != null)
                        {
                            var message = JsonSerializer.Deserialize<OrientationMessage>(line);
                            if (message != null)
                            {
                                var oldOrientation = _currentOrientation;
                                _currentOrientation = message.Orientation;

                                if (oldOrientation != _currentOrientation)
                                {
                                    OrientationChanged?.Invoke(this, new OrientationChangedEventArgs
                                    {
                                        OldOrientation = oldOrientation,
                                        NewOrientation = _currentOrientation
                                    });
                                }
                            }
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Server not running, try again later
                    await Task.Delay(2000, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(1000, _cts.Token);
                }
            }
        }

        private void OnAccelerometerOrientationChanged(object? sender, OrientationChangedEventArgs e)
        {
            _currentOrientation = e.NewOrientation;
            SaveState();

            // Broadcast to connected clients
            if (_pipeServer?.IsConnected == true)
            {
                _ = SendOrientationAsync(_pipeServer, e.NewOrientation);
            }

            // Also notify local subscribers
            OrientationChanged?.Invoke(this, e);
        }

        private async Task SendOrientationAsync(NamedPipeServerStream pipe, ScreenOrientation orientation)
        {
            try
            {
                var message = new OrientationMessage { Orientation = orientation };
                var json = JsonSerializer.Serialize(message) + "\n";
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                await pipe.WriteAsync(bytes, 0, bytes.Length);
                await pipe.FlushAsync();
            }
            catch
            {
                // Ignore send errors
            }
        }

        /// <summary>
        /// Manually set orientation (for demo/testing)
        /// </summary>
        public void SetOrientation(ScreenOrientation orientation)
        {
            var oldOrientation = _currentOrientation;
            _currentOrientation = orientation;
            SaveState();

            if (_accelerometer?.IsDemoMode == true)
            {
                _accelerometer.SimulateOrientation(orientation);
            }
            else
            {
                OrientationChanged?.Invoke(this, new OrientationChangedEventArgs
                {
                    OldOrientation = oldOrientation,
                    NewOrientation = orientation
                });
            }
        }

        private void LoadState()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WindowsPhoneNext");

                var statePath = Path.Combine(appDataPath, STATE_FILE);

                if (File.Exists(statePath))
                {
                    var json = File.ReadAllText(statePath);
                    var state = JsonSerializer.Deserialize<OrientationState>(json);
                    if (state != null)
                    {
                        _currentOrientation = state.Orientation;
                    }
                }
            }
            catch
            {
                // Use default
            }
        }

        private void SaveState()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WindowsPhoneNext");

                Directory.CreateDirectory(appDataPath);

                var statePath = Path.Combine(appDataPath, STATE_FILE);
                var state = new OrientationState { Orientation = _currentOrientation };
                var json = JsonSerializer.Serialize(state);
                File.WriteAllText(statePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _serverTask?.Wait(1000);
            _accelerometer?.Dispose();
            _pipeServer?.Dispose();
            _cts?.Dispose();
        }

        private class OrientationMessage
        {
            public ScreenOrientation Orientation { get; set; }
        }

        private class OrientationState
        {
            public ScreenOrientation Orientation { get; set; }
        }
    }
}
