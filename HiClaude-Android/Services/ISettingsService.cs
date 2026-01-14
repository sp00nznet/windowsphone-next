using ClaudeCodeAndroid.Models;

namespace ClaudeCodeAndroid.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
    Repository? GetCurrentRepository();
    void AddRepository(Repository repo);
    void RemoveRepository(string id);
    void SetCurrentRepository(string id);
}

public class SettingsService : ISettingsService
{
    private const string SettingsFileName = "claudecode_settings.json";
    private readonly string _settingsPath;

    public AppSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        _settingsPath = Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            Settings = new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public Repository? GetCurrentRepository()
    {
        if (string.IsNullOrEmpty(Settings.CurrentRepoId))
            return null;

        return Settings.Repositories.FirstOrDefault(r => r.Id == Settings.CurrentRepoId);
    }

    public void AddRepository(Repository repo)
    {
        var existing = Settings.Repositories.FirstOrDefault(r => r.Url == repo.Url);
        if (existing != null)
        {
            existing.LastUsed = DateTime.Now;
            Settings.CurrentRepoId = existing.Id;
        }
        else
        {
            Settings.Repositories.Add(repo);
            Settings.CurrentRepoId = repo.Id;
        }
    }

    public void RemoveRepository(string id)
    {
        var repo = Settings.Repositories.FirstOrDefault(r => r.Id == id);
        if (repo != null)
        {
            Settings.Repositories.Remove(repo);
            if (Settings.CurrentRepoId == id)
            {
                Settings.CurrentRepoId = Settings.Repositories.FirstOrDefault()?.Id ?? string.Empty;
            }
        }
    }

    public void SetCurrentRepository(string id)
    {
        var repo = Settings.Repositories.FirstOrDefault(r => r.Id == id);
        if (repo != null)
        {
            repo.LastUsed = DateTime.Now;
            Settings.CurrentRepoId = id;
        }
    }
}
