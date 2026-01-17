# Launcher

Home screen with app grid and status bar.

## Features

- **App Grid** - Scrollable grid of 18 applications
- **Status Bar** - Time, battery, signal, network
- **Quick Access** - Phone, Home, Messages buttons
- **Keyboard Shortcuts** - Letter keys launch apps
- **Incoming Call Overlay** - Answer/decline calls
- **Live Clock** - Updates every second
- **Signal Monitoring** - Real-time modem status

## Included Apps

| App | Shortcut | App | Shortcut |
|-----|----------|-----|----------|
| Phone | `P` | Messages | `M` |
| Contacts | `O` | Browser | `B` |
| Gmail | `E` | Maps | `N` |
| Music | `U` | Video | `V` |
| Calendar | `C` | Gallery | `G` |
| Camera | `K` | Settings | `S` |
| Files | `F` | Android | `A` |
| Terminal | `T` | Claude | `L` |
| Solitaire | - | Mahjong | - |

## Usage

### Launching Apps
- Tap an app tile
- Or press the keyboard shortcut letter
- Or press `1-9` for first 9 apps

### Status Bar
- **Time** - 24-hour format
- **Battery** - Percentage (from PiSugar2)
- **Signal** - Bars show modem signal strength
- **Network** - LTE, LTE R (roaming), or No Signal

### Incoming Calls
- Overlay appears with caller ID
- **Accept** - Answer and open Dialer
- **Decline** - Hang up

### Quick Bar
- **Phone** - Opens Dialer
- **Home** - Returns to launcher (already there)
- **Messages** - Opens Messaging

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `P` | Phone |
| `M` | Messages |
| `O` | Contacts |
| `B` | Browser |
| `E` | Gmail |
| `N` | Maps |
| `U` | Music |
| `V` | Video |
| `C` | Calendar |
| `G` | Gallery |
| `K` | Camera |
| `S` | Settings |
| `F` | Files |
| `A` | Android |
| `T` | Terminal |
| `L` | Claude |
| `1-9` | Launch app by position |

## Status Updates

- Clock: Every 1 second
- Signal/Battery: Every 30 seconds
- Network: On modem events

## Technical Details

- Uses ModemLib for call handling and signal
- Integrates with all 18 child applications
- Designed for 720x1560 portrait display

## Building

```powershell
cd Apps/Launcher
dotnet build
```

## Project Structure

```
Launcher/
├── MainWindow.xaml        # Home screen UI
├── MainWindow.xaml.cs     # App launching, modem, status
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── LauncherTheme.xaml
└── WindowsPhoneLauncher.csproj
```
