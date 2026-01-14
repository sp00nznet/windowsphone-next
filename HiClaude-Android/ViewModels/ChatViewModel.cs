using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeAndroid.Models;
using ClaudeCodeAndroid.Services;

namespace ClaudeCodeAndroid.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ISpeechService _speechService;
    private readonly IClaudeCodeService _claudeCodeService;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _isSettingsVisible;

    [ObservableProperty]
    private string _selectedProvider = "GitHub";

    [ObservableProperty]
    private string _repositoryUrl = string.Empty;

    [ObservableProperty]
    private string _localPath = string.Empty;

    [ObservableProperty]
    private Repository? _currentRepository;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<Repository> SavedRepositories { get; } = new();
    public List<string> Providers { get; } = new() { "GitHub", "GitLab", "Gitea", "Bitbucket", "Local" };

    public ChatViewModel(ISettingsService settingsService, ISpeechService speechService, IClaudeCodeService claudeCodeService)
    {
        _settingsService = settingsService;
        _speechService = speechService;
        _claudeCodeService = claudeCodeService;

        // Subscribe to speech events
        _speechService.SpeechRecognized += OnSpeechRecognized;
        _speechService.SpeechError += OnSpeechError;
        _speechService.ListeningStarted += (s, e) => IsListening = true;
        _speechService.ListeningStopped += (s, e) => IsListening = false;

        // Subscribe to Claude Code events
        _claudeCodeService.ResponseReceived += OnResponseReceived;
        _claudeCodeService.ErrorOccurred += OnError;

        // Add welcome message
        Messages.Add(new ChatMessage
        {
            Content = "Hello! I'm Claude Code. I can help you with coding tasks in your repositories.\n\n" +
                     "Tap the settings icon (gear) to configure a repository, or just start chatting!",
            IsUser = false
        });
    }

    public async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        LoadRepositories();

        CurrentRepository = _settingsService.GetCurrentRepository();
        if (CurrentRepository != null)
        {
            SelectedProvider = CurrentRepository.Provider;
            RepositoryUrl = CurrentRepository.Url;
            LocalPath = CurrentRepository.LocalPath;
            StatusText = $"Repository: {CurrentRepository.Name}";
        }
    }

    private void LoadRepositories()
    {
        SavedRepositories.Clear();
        foreach (var repo in _settingsService.Settings.Repositories.OrderByDescending(r => r.LastUsed))
        {
            SavedRepositories.Add(repo);
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        var userMessage = MessageText.Trim();
        MessageText = string.Empty;

        // Add user message
        Messages.Add(new ChatMessage
        {
            Content = userMessage,
            IsUser = true
        });

        // Check for repo commands
        if (TryParseRepoCommand(userMessage, out var repoInfo))
        {
            await HandleRepoCommand(repoInfo);
            return;
        }

        // Send to Claude Code
        IsProcessing = true;
        StatusText = "Thinking...";

        try
        {
            var response = await _claudeCodeService.SendPromptAsync(userMessage, CurrentRepository);
            // Response is handled via event
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Content = $"Error: {ex.Message}",
                IsUser = false
            });
        }
        finally
        {
            IsProcessing = false;
            StatusText = CurrentRepository != null ? $"Repository: {CurrentRepository.Name}" : "Ready";
        }
    }

    [RelayCommand]
    private async Task ToggleListening()
    {
        if (IsListening)
        {
            await _speechService.StopListeningAsync();
        }
        else
        {
            StatusText = "Listening...";
            await _speechService.StartListeningAsync();
        }
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsVisible = !IsSettingsVisible;
    }

    [RelayCommand]
    private async Task SaveRepository()
    {
        if (string.IsNullOrWhiteSpace(RepositoryUrl) && SelectedProvider != "Local")
            return;

        if (SelectedProvider == "Local" && string.IsNullOrWhiteSpace(LocalPath))
            return;

        var name = ExtractRepoName(RepositoryUrl, LocalPath, SelectedProvider);

        var repo = new Repository
        {
            Name = name,
            Provider = SelectedProvider,
            Url = RepositoryUrl,
            LocalPath = LocalPath,
            LastUsed = DateTime.Now
        };

        _settingsService.AddRepository(repo);
        await _settingsService.SaveAsync();

        LoadRepositories();
        CurrentRepository = _settingsService.GetCurrentRepository();
        StatusText = $"Repository: {CurrentRepository?.Name}";
        IsSettingsVisible = false;

        Messages.Add(new ChatMessage
        {
            Content = $"Repository configured: {name}\nI'm ready to help with this project!",
            IsUser = false
        });
    }

    [RelayCommand]
    private async Task SelectRepository(Repository repo)
    {
        _settingsService.SetCurrentRepository(repo.Id);
        await _settingsService.SaveAsync();

        CurrentRepository = repo;
        SelectedProvider = repo.Provider;
        RepositoryUrl = repo.Url;
        LocalPath = repo.LocalPath;
        StatusText = $"Repository: {repo.Name}";
        IsSettingsVisible = false;

        Messages.Add(new ChatMessage
        {
            Content = $"Switched to repository: {repo.Name}",
            IsUser = false
        });
    }

    [RelayCommand]
    private async Task DeleteRepository(Repository repo)
    {
        _settingsService.RemoveRepository(repo.Id);
        await _settingsService.SaveAsync();
        LoadRepositories();

        if (CurrentRepository?.Id == repo.Id)
        {
            CurrentRepository = _settingsService.GetCurrentRepository();
            StatusText = CurrentRepository != null ? $"Repository: {CurrentRepository.Name}" : "Ready";
        }
    }

    private void OnSpeechRecognized(object? sender, string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MessageText = text;
            StatusText = CurrentRepository != null ? $"Repository: {CurrentRepository.Name}" : "Ready";
        });
    }

    private void OnSpeechError(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new ChatMessage
            {
                Content = $"Speech error: {error}",
                IsUser = false
            });
            StatusText = CurrentRepository != null ? $"Repository: {CurrentRepository.Name}" : "Ready";
        });
    }

    private void OnResponseReceived(object? sender, string response)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new ChatMessage
            {
                Content = response,
                IsUser = false
            });
        });
    }

    private void OnError(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new ChatMessage
            {
                Content = error,
                IsUser = false
            });
        });
    }

    private bool TryParseRepoCommand(string message, out (string provider, string repo) repoInfo)
    {
        repoInfo = (string.Empty, string.Empty);
        var lower = message.ToLowerInvariant();

        // Check for repo commands like "work on owner/repo" or "open repo myproject"
        var patterns = new[]
        {
            @"(?:work on|open repo|switch to|use repo)\s+(\S+)",
            @"(?:github|gitlab|gitea|bitbucket)[:/]\s*(\S+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var repo = match.Groups[1].Value;

                // Determine provider
                string provider = "GitHub";
                if (lower.Contains("gitlab")) provider = "GitLab";
                else if (lower.Contains("gitea")) provider = "Gitea";
                else if (lower.Contains("bitbucket")) provider = "Bitbucket";

                repoInfo = (provider, repo);
                return true;
            }
        }

        return false;
    }

    private async Task HandleRepoCommand((string provider, string repo) repoInfo)
    {
        var repo = new Repository
        {
            Name = repoInfo.repo.Split('/').LastOrDefault() ?? repoInfo.repo,
            Provider = repoInfo.provider,
            Url = repoInfo.repo,
            LastUsed = DateTime.Now
        };

        _settingsService.AddRepository(repo);
        await _settingsService.SaveAsync();

        LoadRepositories();
        CurrentRepository = _settingsService.GetCurrentRepository();
        StatusText = $"Repository: {CurrentRepository?.Name}";

        Messages.Add(new ChatMessage
        {
            Content = $"Configured {repoInfo.provider} repository: {repoInfo.repo}\n\nI'm ready to help with this project!",
            IsUser = false
        });
    }

    private string ExtractRepoName(string url, string localPath, string provider)
    {
        if (provider == "Local")
        {
            return Path.GetFileName(localPath.TrimEnd('/'));
        }

        // Extract owner/repo from URL
        var parts = url.Split('/');
        if (parts.Length >= 2)
        {
            return $"{parts[^2]}/{parts[^1]}".TrimEnd('/');
        }

        return url;
    }
}
