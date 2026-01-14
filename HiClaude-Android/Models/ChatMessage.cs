namespace ClaudeCodeAndroid.Models;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class Repository
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "GitHub";
    public string Url { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public DateTime LastUsed { get; set; } = DateTime.Now;
}

public class AppSettings
{
    public string ClaudeCodePath { get; set; } = "claude";
    public string SelectedProvider { get; set; } = "GitHub";
    public string CurrentRepoId { get; set; } = string.Empty;
    public List<Repository> Repositories { get; set; } = new();
}
