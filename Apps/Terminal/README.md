# Terminal

Tabbed terminal emulator with CMD, PowerShell, and WSL support.

## Features

- **Three Shells** - CMD, PowerShell, and WSL (Linux)
- **Tabbed Interface** - Quick switch between shells
- **Command History** - Arrow keys navigate history
- **Colored Output** - Shell-specific color themes
- **Built-in Commands** - cd, clear/cls, exit
- **Process Control** - Ctrl+C to kill commands

## Shells

| Shell | Color | Description |
|-------|-------|-------------|
| **CMD** | Dark gray | Windows Command Prompt |
| **PowerShell** | Navy blue | Windows PowerShell |
| **WSL** | Purple | Windows Subsystem for Linux |

## Usage

### Switching Shells
- Tap the shell tab (CMD, PS, WSL)
- Or press `1`, `2`, `3` on keyboard
- Each shell maintains its own session

### Running Commands
1. Type command in input field
2. Press **Enter** or tap **Run**
3. Output appears above

### Navigation
- `Up Arrow` - Previous command in history
- `Down Arrow` - Next command in history
- `Ctrl+C` - Kill running command

### Built-in Commands
| Command | Shell | Action |
|---------|-------|--------|
| `cls` | CMD | Clear screen |
| `clear` | PS/WSL | Clear screen |
| `cd <path>` | All | Change directory |
| `exit` | All | Close terminal |

### Clear & History Buttons
- **Clear** - Clears the terminal output
- **History** - Shows all previously run commands

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Execute command |
| `Up Arrow` | Previous history |
| `Down Arrow` | Next history |
| `Ctrl+C` | Kill process |
| `1` | Switch to CMD |
| `2` | Switch to PowerShell |
| `3` | Switch to WSL |
| `Escape` | Close terminal |

## Requirements

- Windows 10/11
- WSL installed for Linux shell (optional)
- PowerShell 5.1 or later

## Building

```powershell
cd Apps/Terminal
dotnet build
```

## Project Structure

```
Terminal/
├── MainWindow.xaml        # Terminal UI
├── MainWindow.xaml.cs     # Shell execution logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── TerminalTheme.xaml
└── WindowsPhoneTerminal.csproj
```
