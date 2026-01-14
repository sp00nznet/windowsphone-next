using ClaudeCodeAndroid.Models;

namespace ClaudeCodeAndroid.Services;

public interface IClaudeCodeService
{
    bool IsProcessing { get; }
    event EventHandler<string>? ResponseReceived;
    event EventHandler<string>? ErrorOccurred;
    Task<string> SendPromptAsync(string prompt, Repository? repository);
}

public class ClaudeCodeService : IClaudeCodeService
{
    private readonly ISettingsService _settingsService;
    private bool _isProcessing;

    public bool IsProcessing => _isProcessing;

    public event EventHandler<string>? ResponseReceived;
    public event EventHandler<string>? ErrorOccurred;

    public ClaudeCodeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<string> SendPromptAsync(string prompt, Repository? repository)
    {
        if (_isProcessing)
        {
            return "Already processing a request. Please wait.";
        }

        _isProcessing = true;

        try
        {
            // Build the request for Claude Code API
            // On Android, we'll use HTTP API instead of CLI
            var response = await CallClaudeCodeApiAsync(prompt, repository);

            ResponseReceived?.Invoke(this, response);
            return response;
        }
        catch (Exception ex)
        {
            var error = $"Error: {ex.Message}";
            ErrorOccurred?.Invoke(this, error);
            return error;
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task<string> CallClaudeCodeApiAsync(string prompt, Repository? repository)
    {
        // In a real implementation, this would call Claude's API
        // For now, we simulate the response structure

        var repoContext = repository != null
            ? $"Repository: {repository.Name} ({repository.Provider})\nPath: {repository.LocalPath}\n\n"
            : "No repository selected.\n\n";

        // Simulate API call
        await Task.Delay(1000);

        // In production, replace with actual API call:
        // using var client = new HttpClient();
        // var request = new { prompt, repository = repository?.Url };
        // var response = await client.PostAsJsonAsync("https://api.anthropic.com/v1/messages", request);

        return $"{repoContext}I received your message: \"{prompt}\"\n\n" +
               "To fully integrate with Claude Code:\n" +
               "1. Set up the Anthropic API key in app settings\n" +
               "2. Configure the repository you want to work with\n" +
               "3. I'll help you with code tasks in that repository\n\n" +
               "This is a demo response. In production, this would connect to the Claude API.";
    }
}
