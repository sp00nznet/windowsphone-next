using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowsPhoneNext.Terminal
{
    public partial class MainWindow : Window
    {
        private enum TerminalType
        {
            Cmd,
            PowerShell,
            Wsl
        }

        private TerminalType _currentTerminal = TerminalType.Cmd;
        private Process? _currentProcess;
        private readonly StringBuilder _outputBuffer = new();
        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;
        private string _currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Terminal colors
        private static readonly Color CmdColor = Color.FromRgb(0x1E, 0x1E, 0x1E);
        private static readonly Color PowerShellColor = Color.FromRgb(0x01, 0x24, 0x56);
        private static readonly Color WslColor = Color.FromRgb(0x30, 0x0A, 0x24);

        public MainWindow()
        {
            InitializeComponent();
            UpdateTerminalUI();
            CommandInput.Focus();
            PrintWelcomeMessage();
        }

        private void PrintWelcomeMessage()
        {
            var welcome = _currentTerminal switch
            {
                TerminalType.Cmd => "Microsoft Windows [Version 10.0]\n(c) Microsoft Corporation. All rights reserved.\n\n",
                TerminalType.PowerShell => "Windows PowerShell\nCopyright (C) Microsoft Corporation. All rights reserved.\n\nTry the new cross-platform PowerShell https://aka.ms/pscore6\n\n",
                TerminalType.Wsl => "Welcome to Windows Subsystem for Linux!\n\n",
                _ => ""
            };
            AppendOutput(welcome);
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                var newTerminal = tag switch
                {
                    "cmd" => TerminalType.Cmd,
                    "powershell" => TerminalType.PowerShell,
                    "wsl" => TerminalType.Wsl,
                    _ => TerminalType.Cmd
                };

                if (newTerminal != _currentTerminal)
                {
                    KillCurrentProcess();
                    _currentTerminal = newTerminal;
                    _outputBuffer.Clear();
                    OutputText.Text = "";
                    _currentDirectory = _currentTerminal == TerminalType.Wsl
                        ? "~"
                        : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    UpdateTerminalUI();
                    PrintWelcomeMessage();
                }
            }
        }

        private void UpdateTerminalUI()
        {
            // Update tab styles
            CmdTab.Style = (Style)FindResource(_currentTerminal == TerminalType.Cmd ? "ActiveTabButtonStyle" : "TabButtonStyle");
            PowerShellTab.Style = (Style)FindResource(_currentTerminal == TerminalType.PowerShell ? "ActiveTabButtonStyle" : "TabButtonStyle");
            WslTab.Style = (Style)FindResource(_currentTerminal == TerminalType.Wsl ? "ActiveTabButtonStyle" : "TabButtonStyle");

            // Update terminal background color
            var terminalColor = _currentTerminal switch
            {
                TerminalType.Cmd => CmdColor,
                TerminalType.PowerShell => PowerShellColor,
                TerminalType.Wsl => WslColor,
                _ => CmdColor
            };
            TerminalBackground.Color = terminalColor;

            // Update prompt
            PromptText.Text = _currentTerminal switch
            {
                TerminalType.Cmd => $"{_currentDirectory}> ",
                TerminalType.PowerShell => "PS> ",
                TerminalType.Wsl => $"{_currentDirectory}$ ",
                _ => "> "
            };

            // Update prompt color
            PromptText.Foreground = _currentTerminal switch
            {
                TerminalType.Cmd => new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                TerminalType.PowerShell => new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8)),
                TerminalType.Wsl => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                _ => new SolidColorBrush(Colors.White)
            };

            CommandInput.Focus();
        }

        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    ExecuteCommand();
                    e.Handled = true;
                    break;
                case Key.Up:
                    NavigateHistory(-1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    NavigateHistory(1);
                    e.Handled = true;
                    break;
                case Key.C when Keyboard.Modifiers == ModifierKeys.Control:
                    KillCurrentProcess();
                    AppendOutput("^C\n");
                    e.Handled = true;
                    break;
            }
        }

        private void NavigateHistory(int direction)
        {
            if (_commandHistory.Count == 0) return;

            _historyIndex += direction;
            _historyIndex = Math.Max(0, Math.Min(_historyIndex, _commandHistory.Count - 1));

            CommandInput.Text = _commandHistory[_historyIndex];
            CommandInput.CaretIndex = CommandInput.Text.Length;
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteCommand();
        }

        private async void ExecuteCommand()
        {
            var command = CommandInput.Text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            // Add to history
            _commandHistory.Add(command);
            _historyIndex = _commandHistory.Count;

            // Display command in output
            var prompt = _currentTerminal switch
            {
                TerminalType.Cmd => $"{_currentDirectory}> ",
                TerminalType.PowerShell => "PS> ",
                TerminalType.Wsl => $"{_currentDirectory}$ ",
                _ => "> "
            };
            AppendOutput($"{prompt}{command}\n");

            CommandInput.Text = "";

            // Handle built-in commands
            if (HandleBuiltInCommand(command)) return;

            // Execute the command
            await ExecuteExternalCommand(command);
        }

        private bool HandleBuiltInCommand(string command)
        {
            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1] : "";

            switch (cmd)
            {
                case "cls" when _currentTerminal == TerminalType.Cmd:
                case "clear" when _currentTerminal != TerminalType.Cmd:
                    ClearOutput();
                    return true;
                case "exit":
                    Close();
                    return true;
                case "cd" when !string.IsNullOrEmpty(args):
                    ChangeDirectory(args);
                    return true;
                default:
                    return false;
            }
        }

        private void ChangeDirectory(string path)
        {
            try
            {
                string newPath;
                if (_currentTerminal == TerminalType.Wsl)
                {
                    if (path == "~" || path == "$HOME")
                        newPath = "~";
                    else if (path.StartsWith("/") || path.StartsWith("~"))
                        newPath = path;
                    else
                        newPath = _currentDirectory == "~" ? $"~/{path}" : $"{_currentDirectory}/{path}";
                    _currentDirectory = newPath;
                }
                else
                {
                    if (Path.IsPathRooted(path))
                        newPath = path;
                    else
                        newPath = Path.GetFullPath(Path.Combine(_currentDirectory, path));

                    if (Directory.Exists(newPath))
                        _currentDirectory = newPath;
                    else
                    {
                        AppendOutput($"The system cannot find the path specified: {path}\n");
                        return;
                    }
                }
                UpdateTerminalUI();
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}\n");
            }
        }

        private async Task ExecuteExternalCommand(string command)
        {
            try
            {
                var startInfo = _currentTerminal switch
                {
                    TerminalType.Cmd => new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        WorkingDirectory = _currentDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    },
                    TerminalType.PowerShell => new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"{command}\"",
                        WorkingDirectory = _currentDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    },
                    TerminalType.Wsl => new ProcessStartInfo
                    {
                        FileName = "wsl.exe",
                        Arguments = $"-e bash -c \"{command.Replace("\"", "\\\"")}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    },
                    _ => throw new InvalidOperationException("Unknown terminal type")
                };

                _currentProcess = new Process { StartInfo = startInfo };

                _currentProcess.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        Dispatcher.Invoke(() => AppendOutput(e.Data + "\n"));
                    }
                };

                _currentProcess.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        Dispatcher.Invoke(() => AppendOutput(e.Data + "\n", isError: true));
                    }
                };

                _currentProcess.Start();
                _currentProcess.BeginOutputReadLine();
                _currentProcess.BeginErrorReadLine();

                await Task.Run(() => _currentProcess.WaitForExit());

                _currentProcess = null;
            }
            catch (Exception ex)
            {
                AppendOutput($"Error executing command: {ex.Message}\n", isError: true);
            }
        }

        private void AppendOutput(string text, bool isError = false)
        {
            _outputBuffer.Append(text);
            OutputText.Text = _outputBuffer.ToString();

            // Auto-scroll to bottom
            OutputScrollViewer.ScrollToEnd();
        }

        private void ClearOutput()
        {
            _outputBuffer.Clear();
            OutputText.Text = "";
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearOutput();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_commandHistory.Count == 0)
            {
                AppendOutput("No command history.\n");
                return;
            }

            AppendOutput("\n--- Command History ---\n");
            for (int i = 0; i < _commandHistory.Count; i++)
            {
                AppendOutput($"  {i + 1}. {_commandHistory[i]}\n");
            }
            AppendOutput("\n");
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void KillButton_Click(object sender, RoutedEventArgs e)
        {
            KillCurrentProcess();
            AppendOutput("^C\n");
        }

        private void KillCurrentProcess()
        {
            try
            {
                if (_currentProcess != null && !_currentProcess.HasExited)
                {
                    _currentProcess.Kill(true);
                    _currentProcess = null;
                }
            }
            catch
            {
                // Process may have already exited
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.D1 || e.Key == Key.NumPad1)
            {
                Tab_Click(CmdTab, new RoutedEventArgs());
            }
            else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
            {
                Tab_Click(PowerShellTab, new RoutedEventArgs());
            }
            else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
            {
                Tab_Click(WslTab, new RoutedEventArgs());
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            KillCurrentProcess();
            base.OnClosed(e);
        }
    }
}
