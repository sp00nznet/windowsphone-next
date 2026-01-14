using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Speech.Recognition;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowsPhoneNext.ClaudeCode
{
    public partial class MainWindow : Window
    {
        private readonly string _settingsFilePath;
        private readonly ObservableCollection<ChatMessage> _messages = new();
        private readonly ObservableCollection<SavedRepository> _savedRepos = new();
        private SpeechRecognitionEngine? _speechRecognizer;
        private bool _isListening;
        private string _selectedProvider = "github";
        private SavedRepository? _currentRepo;
        private Process? _claudeProcess;
        private StringBuilder _claudeOutputBuffer = new();

        public MainWindow()
        {
            InitializeComponent();

            // Setup settings storage
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WindowsPhoneNext");
            Directory.CreateDirectory(appData);
            _settingsFilePath = Path.Combine(appData, "claudecode_settings.json");

            // Bind collections
            ChatMessages.ItemsSource = _messages;
            SavedReposList.ItemsSource = _savedRepos;

            LoadSettings();
            InitializeSpeechRecognition();

            // Welcome message
            AddMessage("Hello! I'm Claude Code. Tell me which repository you'd like to work with, or tap the settings icon to configure one. You can type or use voice input!", false);
        }

        #region Speech Recognition

        private void InitializeSpeechRecognition()
        {
            try
            {
                _speechRecognizer = new SpeechRecognitionEngine();

                // Create a grammar that accepts any speech
                var dictationGrammar = new DictationGrammar();
                dictationGrammar.Name = "Dictation";
                _speechRecognizer.LoadGrammar(dictationGrammar);

                _speechRecognizer.SetInputToDefaultAudioDevice();

                _speechRecognizer.SpeechRecognized += SpeechRecognizer_SpeechRecognized;
                _speechRecognizer.SpeechRecognitionRejected += SpeechRecognizer_SpeechRejected;

                StatusText.Text = "Voice ready";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Voice unavailable";
                MicButton.IsEnabled = false;
                MicButton.ToolTip = $"Speech recognition not available: {ex.Message}";
            }
        }

        private void SpeechRecognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Result.Confidence > 0.3)
                {
                    MessageInput.Text = e.Result.Text;
                    StopListening();

                    // Auto-send after voice input
                    if (!string.IsNullOrWhiteSpace(MessageInput.Text))
                    {
                        SendMessage();
                    }
                }
            });
        }

        private void SpeechRecognizer_SpeechRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = "Didn't catch that, try again";
            });
        }

        private void StartListening()
        {
            if (_speechRecognizer == null) return;

            try
            {
                _isListening = true;
                MicIcon.Text = "\U0001F534"; // Red circle - recording
                MicButton.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)); // Green
                StatusText.Text = "Listening...";

                _speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                StopListening();
            }
        }

        private void StopListening()
        {
            if (_speechRecognizer == null) return;

            try
            {
                _isListening = false;
                MicIcon.Text = "\U0001F3A4"; // Microphone
                MicButton.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)); // Red

                _speechRecognizer.RecognizeAsyncStop();
                StatusText.Text = _currentRepo != null ? $"Connected: {_currentRepo.Name}" : "Ready";
            }
            catch
            {
                // Ignore stop errors
            }
        }

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isListening)
            {
                StopListening();
            }
            else
            {
                StartListening();
            }
        }

        #endregion

        #region Chat & Claude Code Integration

        private void AddMessage(string content, bool isUser)
        {
            var message = new ChatMessage
            {
                Content = content,
                IsUser = isUser,
                Timestamp = DateTime.Now.ToString("HH:mm"),
                Alignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                BackgroundBrush = isUser
                    ? new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4))
                    : new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37))
            };

            _messages.Add(message);

            // Scroll to bottom
            ChatScrollViewer.ScrollToEnd();
        }

        private void SendMessage()
        {
            var text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AddMessage(text, true);
            MessageInput.Text = "";
            SendButton.IsEnabled = false;

            // Process the message
            ProcessUserMessage(text);
        }

        private async void ProcessUserMessage(string message)
        {
            // Check if user is trying to set a repo
            var lowerMessage = message.ToLowerInvariant();

            if (lowerMessage.Contains("use repo") || lowerMessage.Contains("work on") ||
                lowerMessage.Contains("open repo") || lowerMessage.Contains("switch to"))
            {
                // Try to extract repo name
                var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Contains('/') && !word.StartsWith("http"))
                    {
                        // Looks like owner/repo format
                        SetRepository(_selectedProvider, word);
                        AddMessage($"I'll work with the repository: {word}. What would you like me to do?", false);
                        return;
                    }
                }
            }

            // If no repo selected, prompt user
            if (_currentRepo == null)
            {
                AddMessage("Please select a repository first. You can say something like 'work on owner/repo' or tap the settings icon to configure one.", false);
                return;
            }

            // Send to Claude Code
            await SendToClaudeCode(message);
        }

        private async System.Threading.Tasks.Task SendToClaudeCode(string prompt)
        {
            TypingIndicator.Visibility = Visibility.Visible;
            StatusText.Text = "Claude is working...";

            try
            {
                var claudePath = ClaudeCodePathInput.Text;
                var repoPath = _currentRepo?.Path ?? _currentRepo?.Url ?? "";

                // Build the command
                var startInfo = new ProcessStartInfo
                {
                    FileName = claudePath,
                    Arguments = $"--print \"{prompt.Replace("\"", "\\\"")}\"",
                    WorkingDirectory = repoPath.StartsWith("/") || repoPath.Contains(":\\")
                        ? repoPath
                        : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                _claudeOutputBuffer.Clear();
                _claudeProcess = new Process { StartInfo = startInfo };

                _claudeProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _claudeOutputBuffer.AppendLine(e.Data);
                    }
                };

                _claudeProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _claudeOutputBuffer.AppendLine(e.Data);
                    }
                };

                _claudeProcess.Start();
                _claudeProcess.BeginOutputReadLine();
                _claudeProcess.BeginErrorReadLine();

                await System.Threading.Tasks.Task.Run(() => _claudeProcess.WaitForExit());

                var response = _claudeOutputBuffer.ToString().Trim();

                Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrEmpty(response))
                    {
                        AddMessage(response, false);
                    }
                    else
                    {
                        AddMessage("I completed the task. Is there anything else you'd like me to do?", false);
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    AddMessage($"Error communicating with Claude Code: {ex.Message}\n\nMake sure Claude Code CLI is installed and accessible.", false);
                });
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    TypingIndicator.Visibility = Visibility.Collapsed;
                    StatusText.Text = _currentRepo != null ? $"Connected: {_currentRepo.Name}" : "Ready";
                    _claudeProcess = null;
                });
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(MessageInput.Text))
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void MessageInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageInput.Text);
        }

        private void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            _messages.Clear();
            AddMessage("Chat cleared. How can I help you?", false);
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            AddMessage("Chat history feature coming soon! For now, this chat persists during your session.", false);
        }

        #endregion

        #region Repository Management

        private void SetRepository(string provider, string repoIdentifier)
        {
            var repoName = repoIdentifier.Contains('/')
                ? repoIdentifier.Split('/').Last()
                : repoIdentifier;

            _currentRepo = new SavedRepository
            {
                Provider = provider,
                Url = GetFullRepoUrl(provider, repoIdentifier),
                Name = repoIdentifier,
                Path = repoIdentifier.StartsWith("/") || repoIdentifier.Contains(":\\")
                    ? repoIdentifier
                    : null
            };

            UpdateRepoDisplay();

            // Save to list if not already there
            if (!_savedRepos.Any(r => r.Url == _currentRepo.Url))
            {
                _savedRepos.Add(_currentRepo);
                UpdateSavedReposUI();
                SaveSettings();
            }
        }

        private string GetFullRepoUrl(string provider, string identifier)
        {
            if (identifier.StartsWith("http")) return identifier;
            if (identifier.StartsWith("/") || identifier.Contains(":\\")) return identifier;

            return provider switch
            {
                "github" => $"https://github.com/{identifier}",
                "gitlab" => $"https://gitlab.com/{identifier}",
                "gitea" => identifier, // User needs to provide full URL
                "local" => identifier,
                _ => identifier
            };
        }

        private void UpdateRepoDisplay()
        {
            if (_currentRepo != null)
            {
                RepoNameText.Text = _currentRepo.Name;
                RepoProviderText.Text = GetProviderDisplayName(_currentRepo.Provider);
                StatusText.Text = $"Connected: {_currentRepo.Name}";
            }
            else
            {
                RepoNameText.Text = "No repository selected";
                RepoProviderText.Text = "Tap settings to configure";
                StatusText.Text = "Ready";
            }
        }

        private string GetProviderDisplayName(string provider)
        {
            return provider switch
            {
                "github" => "GitHub",
                "gitlab" => "GitLab",
                "gitea" => "Gitea",
                "local" => "Local Repository",
                _ => provider
            };
        }

        private void ProviderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string provider)
            {
                _selectedProvider = provider;

                // Update hint text
                RepoUrlHint.Text = provider switch
                {
                    "github" => "e.g., owner/repo or https://github.com/owner/repo",
                    "gitlab" => "e.g., owner/repo or https://gitlab.com/owner/repo",
                    "gitea" => "e.g., https://your-gitea.com/owner/repo",
                    "local" => "e.g., C:\\Projects\\my-repo or /home/user/projects/repo",
                    _ => "Enter repository identifier"
                };

                // Visual feedback
                foreach (Button btn in ProviderList.Children)
                {
                    btn.Background = btn.Tag?.ToString() == provider
                        ? new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37))
                        : new SolidColorBrush(Color.FromRgb(0x16, 0x21, 0x3E));
                }
            }
        }

        private void SaveRepoButton_Click(object sender, RoutedEventArgs e)
        {
            var repoInput = RepoUrlInput.Text.Trim();
            if (string.IsNullOrEmpty(repoInput))
            {
                AddMessage("Please enter a repository URL or path.", false);
                return;
            }

            SetRepository(_selectedProvider, repoInput);
            SettingsPanel.Visibility = Visibility.Collapsed;
            RepoUrlInput.Text = "";

            AddMessage($"Repository set to: {_currentRepo?.Name}. What would you like me to help with?", false);
        }

        private void SelectSavedRepo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SavedRepository repo)
            {
                _currentRepo = repo;
                _selectedProvider = repo.Provider;
                UpdateRepoDisplay();
                SettingsPanel.Visibility = Visibility.Collapsed;

                AddMessage($"Switched to repository: {repo.Name}. How can I help?", false);
            }
        }

        private void DeleteSavedRepo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SavedRepository repo)
            {
                _savedRepos.Remove(repo);
                if (_currentRepo == repo)
                {
                    _currentRepo = null;
                    UpdateRepoDisplay();
                }
                UpdateSavedReposUI();
                SaveSettings();
            }
        }

        private void ChangeRepoButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void UpdateSavedReposUI()
        {
            NoSavedReposText.Visibility = _savedRepos.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BrowseClaudePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Claude Code CLI",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                FileName = "claude.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                ClaudeCodePathInput.Text = dialog.FileName;
                SaveSettings();
            }
        }

        #endregion

        #region Settings Panel

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void SettingsPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Close panel when clicking outside the settings content
            if (e.OriginalSource == SettingsPanel)
            {
                SettingsPanel.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Settings Persistence

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<ClaudeCodeSettings>(json);

                    if (settings != null)
                    {
                        foreach (var repo in settings.SavedRepositories)
                        {
                            _savedRepos.Add(repo);
                        }

                        if (!string.IsNullOrEmpty(settings.ClaudeCodePath))
                        {
                            ClaudeCodePathInput.Text = settings.ClaudeCodePath;
                        }

                        if (!string.IsNullOrEmpty(settings.LastSelectedRepo))
                        {
                            _currentRepo = _savedRepos.FirstOrDefault(r => r.Url == settings.LastSelectedRepo);
                            if (_currentRepo != null)
                            {
                                _selectedProvider = _currentRepo.Provider;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silently fail - start fresh
            }

            UpdateRepoDisplay();
            UpdateSavedReposUI();
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new ClaudeCodeSettings
                {
                    SavedRepositories = _savedRepos.ToList(),
                    ClaudeCodePath = ClaudeCodePathInput.Text,
                    LastSelectedRepo = _currentRepo?.Url
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Silently fail
            }
        }

        #endregion

        #region Window Events

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (SettingsPanel.Visibility == Visibility.Visible)
                {
                    SettingsPanel.Visibility = Visibility.Collapsed;
                }
                else if (_isListening)
                {
                    StopListening();
                }
                else
                {
                    Close();
                }
                e.Handled = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            StopListening();
            _speechRecognizer?.Dispose();
            _claudeProcess?.Kill();
            _claudeProcess?.Dispose();
            SaveSettings();
            base.OnClosed(e);
        }

        #endregion
    }

    #region Data Models

    public class ChatMessage
    {
        public string Content { get; set; } = "";
        public bool IsUser { get; set; }
        public string Timestamp { get; set; } = "";
        public HorizontalAlignment Alignment { get; set; }
        public Brush BackgroundBrush { get; set; } = Brushes.Gray;
    }

    public class SavedRepository
    {
        public string Provider { get; set; } = "github";
        public string Url { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Path { get; set; }
    }

    public class ClaudeCodeSettings
    {
        public List<SavedRepository> SavedRepositories { get; set; } = new();
        public string ClaudeCodePath { get; set; } = "claude";
        public string? LastSelectedRepo { get; set; }
    }

    #endregion
}
