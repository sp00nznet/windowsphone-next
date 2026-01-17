# Claude Code

Voice and text AI assistant for repository management.

## Features

- **Voice Input** - Speak commands with microphone
- **Text Chat** - Type messages to Claude
- **Repository Management** - Configure GitHub, GitLab, Gitea, or local repos
- **Task Queue** - Queue tasks while Claude is working
- **Multi-Provider** - Supports multiple Git providers
- **Chat History** - Persistent conversation during session

## Usage

### Setting Up a Repository
1. Tap the **Settings** (gear) icon
2. Select provider: GitHub, GitLab, Gitea, or Local
3. Enter repository: `owner/repo` or full URL or local path
4. Tap **Save Repository**

### Voice Input
1. Tap the **Microphone** button (turns green)
2. Speak your command clearly
3. Voice is transcribed and sent automatically

### Text Input
1. Type your message in the input field
2. Press **Enter** or tap **Send**

### Queuing Tasks
When Claude is busy:
1. Type or speak your next task
2. Tap **Queue** instead of Send
3. Tasks run automatically when Claude finishes

### Managing Repositories
- Saved repos appear in settings
- Tap a repo to switch to it
- Delete repos you no longer need

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Send message |
| `Escape` | Close settings / Stop listening / Exit |

## Providers

| Provider | Format |
|----------|--------|
| GitHub | `owner/repo` or `https://github.com/owner/repo` |
| GitLab | `owner/repo` or `https://gitlab.com/owner/repo` |
| Gitea | Full URL required |
| Local | `C:\Projects\repo` or `/home/user/repo` |

## Requirements

- Claude Code CLI installed and in PATH
- Microphone (for voice input)
- Windows Speech Recognition enabled

### Installing Claude Code CLI
```powershell
npm install -g @anthropic/claude-code
```

## Data Storage

Settings stored in:
```
%LOCALAPPDATA%\WindowsPhoneNext\claudecode_settings.json
```

## Building

```powershell
cd Apps/ClaudeCode
dotnet build
```

## Project Structure

```
ClaudeCode/
├── MainWindow.xaml        # Chat UI
├── MainWindow.xaml.cs     # Voice, chat, and CLI logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── ClaudeCodeTheme.xaml
└── WindowsPhoneClaudeCode.csproj
```
