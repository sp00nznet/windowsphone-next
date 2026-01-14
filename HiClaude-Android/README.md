# Claude Code Android

A .NET MAUI Android app for interacting with Claude Code. Talk to Claude about your code repositories using voice or text.

## Features

- **Chat Interface** - Conversational UI with Claude Code
- **Voice Input** - Speak commands using Android speech recognition
- **Text Input** - Type messages in the chat field
- **Repository Management**:
  - GitHub repository support
  - GitLab repository support
  - Gitea repository support
  - Bitbucket repository support
  - Local repository paths
- **Saved Repositories** - Quick switch between projects
- **Dark Theme** - Orange/amber accent colors on dark background

## Screenshots

The app features:
- Chat view with message bubbles (user messages on right, Claude on left)
- Microphone button for voice input
- Settings panel for repository configuration
- Swipe-to-delete on saved repositories

## Requirements

- .NET 8 SDK
- Android SDK (API 21+)
- Visual Studio 2022 or VS Code with MAUI extension

## Building

```bash
# Restore packages
dotnet restore

# Build for Android
dotnet build -f net8.0-android

# Create APK
dotnet publish -f net8.0-android -c Release
```

## Project Structure

```
ClaudeCodeAndroid/
├── Models/
│   └── ChatMessage.cs      # Data models
├── Services/
│   ├── ISettingsService.cs # Settings persistence
│   ├── ISpeechService.cs   # Speech recognition
│   └── IClaudeCodeService.cs # Claude API integration
├── ViewModels/
│   └── ChatViewModel.cs    # Main view logic
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml     # Theme colors
│   │   └── Styles.xaml     # UI styles
│   ├── AppIcon/            # App icons
│   └── Splash/             # Splash screen
├── Platforms/
│   └── Android/            # Android-specific code
├── MainPage.xaml           # Main chat UI
├── App.xaml                # Application root
└── MauiProgram.cs          # DI and startup
```

## Configuration

### API Integration

To connect to Claude's API, update the `ClaudeCodeService.cs`:

```csharp
private async Task<string> CallClaudeCodeApiAsync(string prompt, Repository? repository)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("x-api-key", "YOUR_API_KEY");
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

    var request = new
    {
        model = "claude-3-sonnet-20240229",
        max_tokens = 1024,
        messages = new[] { new { role = "user", content = prompt } }
    };

    var response = await client.PostAsJsonAsync(
        "https://api.anthropic.com/v1/messages",
        request
    );
    // Handle response...
}
```

### Permissions

The app requires:
- `RECORD_AUDIO` - For voice input
- `INTERNET` - For API calls

## Voice Commands

Speak naturally to configure repositories:
- "Work on owner/repo"
- "Open repo myproject"
- "Switch to username/repository"
- "Use GitHub owner/repo"

## Theme Colors

| Color | Hex | Usage |
|-------|-----|-------|
| Background | `#1A1A2E` | App background |
| Surface | `#16213E` | Cards, panels |
| Accent | `#FF6B35` | Buttons, highlights |
| User Message | `#FF6B35` | User chat bubbles |
| Assistant Message | `#2D3A5A` | Claude chat bubbles |

## License

This project is provided for personal and educational use.

---

Ported from [Windows Phone Next](https://github.com/sp00nznet/windowsphone-next) Claude Code app.
