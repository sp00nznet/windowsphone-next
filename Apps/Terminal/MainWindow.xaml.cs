using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowsPhoneNext.Terminal
{
    public partial class MainWindow : Window
    {
        private Process? _wslProcess;
        private StreamWriter? _inputWriter;
        private StringBuilder _outputBuffer = new StringBuilder();
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private string _currentDistro = "Ubuntu";
        private List<string> _availableDistros = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Check if WSL is available
            if (!await IsWslAvailable())
            {
                NotInstalledOverlay.Visibility = Visibility.Visible;
                return;
            }

            // Get available distributions
            await LoadDistributions();

            if (_availableDistros.Count == 0)
            {
                NotInstalledOverlay.Visibility = Visibility.Visible;
                return;
            }

            // Start default distro (Ubuntu if available, otherwise first)
            _currentDistro = _availableDistros.Contains("Ubuntu") ? "Ubuntu" : _availableDistros[0];
            await StartWslSession(_currentDistro);

            InputTextBox.Focus();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            StopWslSession();
        }

        private async Task<bool> IsWslAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                // WSL not available
            }

            return false;
        }

        private async Task LoadDistributions()
        {
            _availableDistros.Clear();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--list --quiet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Unicode
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    // Parse distro names
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var distro = line.Trim().Replace("\0", "");
                        if (!string.IsNullOrWhiteSpace(distro))
                        {
                            _availableDistros.Add(distro);
                        }
                    }
                }
            }
            catch
            {
                // Failed to get distros
            }
        }

        private async Task StartWslSession(string distro)
        {
            StopWslSession();

            _currentDistro = distro;
            TitleText.Text = distro;
            _outputBuffer.Clear();

            AppendOutput($"Starting {distro}...\n");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {distro}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                _wslProcess = new Process { StartInfo = psi };
                _wslProcess.OutputDataReceived += WslProcess_OutputDataReceived;
                _wslProcess.ErrorDataReceived += WslProcess_ErrorDataReceived;
                _wslProcess.Exited += WslProcess_Exited;
                _wslProcess.EnableRaisingEvents = true;

                _wslProcess.Start();
                _inputWriter = _wslProcess.StandardInput;
                _inputWriter.AutoFlush = true;

                _wslProcess.BeginOutputReadLine();
                _wslProcess.BeginErrorReadLine();

                // Give it a moment to start
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                AppendOutput($"Error starting {distro}: {ex.Message}\n");
            }
        }

        private void StopWslSession()
        {
            try
            {
                _inputWriter?.Close();
                _inputWriter = null;

                if (_wslProcess != null && !_wslProcess.HasExited)
                {
                    _wslProcess.Kill(true);
                }
                _wslProcess?.Dispose();
                _wslProcess = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private void WslProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(() => AppendOutput(e.Data + "\n"));
            }
        }

        private void WslProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(() => AppendOutput(e.Data + "\n"));
            }
        }

        private void WslProcess_Exited(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AppendOutput($"\n[{_currentDistro} session ended]\n");
            });
        }

        private void AppendOutput(string text)
        {
            _outputBuffer.Append(text);

            // Limit buffer size
            if (_outputBuffer.Length > 50000)
            {
                _outputBuffer.Remove(0, _outputBuffer.Length - 40000);
            }

            OutputText.Text = _outputBuffer.ToString();

            // Scroll to bottom
            OutputScrollViewer.ScrollToEnd();
        }

        private void SendCommand(string command)
        {
            if (_wslProcess == null || _wslProcess.HasExited || _inputWriter == null)
            {
                AppendOutput("[Session not active]\n");
                return;
            }

            // Add to history
            if (!string.IsNullOrWhiteSpace(command))
            {
                _commandHistory.Add(command);
                _historyIndex = _commandHistory.Count;
            }

            // Echo command
            AppendOutput($"$ {command}\n");

            // Send to WSL
            try
            {
                _inputWriter.WriteLine(command);
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}\n");
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    var command = InputTextBox.Text;
                    InputTextBox.Clear();
                    SendCommand(command);
                    e.Handled = true;
                    break;

                case Key.Up:
                    // History navigation - previous
                    if (_commandHistory.Count > 0 && _historyIndex > 0)
                    {
                        _historyIndex--;
                        InputTextBox.Text = _commandHistory[_historyIndex];
                        InputTextBox.CaretIndex = InputTextBox.Text.Length;
                    }
                    e.Handled = true;
                    break;

                case Key.Down:
                    // History navigation - next
                    if (_commandHistory.Count > 0 && _historyIndex < _commandHistory.Count - 1)
                    {
                        _historyIndex++;
                        InputTextBox.Text = _commandHistory[_historyIndex];
                        InputTextBox.CaretIndex = InputTextBox.Text.Length;
                    }
                    else if (_historyIndex >= _commandHistory.Count - 1)
                    {
                        _historyIndex = _commandHistory.Count;
                        InputTextBox.Clear();
                    }
                    e.Handled = true;
                    break;

                case Key.C:
                    // Ctrl+C to send interrupt
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        SendCtrlC();
                        e.Handled = true;
                    }
                    break;

                case Key.L:
                    // Ctrl+L to clear
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        _outputBuffer.Clear();
                        OutputText.Text = "";
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    if (DistroOverlay.Visibility == Visibility.Visible)
                    {
                        DistroOverlay.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Close();
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void SendCtrlC()
        {
            // Send Ctrl+C character
            try
            {
                _inputWriter?.Write("\x03");
                _inputWriter?.Flush();
            }
            catch
            {
                // Ignore
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var command = InputTextBox.Text;
            InputTextBox.Clear();
            SendCommand(command);
            InputTextBox.Focus();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDistroSelection();
        }

        private async void ShowDistroSelection()
        {
            await LoadDistributions();

            DistroList.Items.Clear();

            foreach (var distro in _availableDistros)
            {
                var button = new Button
                {
                    Content = distro,
                    Style = (Style)FindResource("DistroButtonStyle"),
                    Tag = distro
                };
                button.Click += DistroButton_Click;
                DistroList.Items.Add(button);
            }

            if (_availableDistros.Count == 0)
            {
                var label = new TextBlock
                {
                    Text = "No distributions installed.\nRun 'wsl --install -d Ubuntu' in PowerShell.",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                DistroList.Items.Add(label);
            }

            DistroOverlay.Visibility = Visibility.Visible;
        }

        private async void DistroButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string distro)
            {
                DistroOverlay.Visibility = Visibility.Collapsed;
                await StartWslSession(distro);
                InputTextBox.Focus();
            }
        }

        private void CancelDistroSelection_Click(object sender, RoutedEventArgs e)
        {
            DistroOverlay.Visibility = Visibility.Collapsed;
            InputTextBox.Focus();
        }

        private void OutputText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Focus input when clicking output area
            InputTextBox.Focus();
        }
    }
}
