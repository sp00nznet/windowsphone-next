# Settings

System configuration for Bluetooth, WiFi, themes, and device info.

## Features

- **Bluetooth** - Toggle, scan, pair/forget devices
- **WiFi** - Connect to networks, save passwords
- **Themes** - 9 built-in color themes
- **Storage** - View disk usage
- **About Phone** - Modem info, IMEI, signal strength

## Sections

### Bluetooth
- Toggle Bluetooth on/off
- Scan for nearby devices
- Pair with available devices
- Manage paired devices (forget)

### WiFi
- Toggle WiFi on/off
- View connected network
- Scan for available networks
- Connect with password entry
- Save networks for auto-connect
- Disconnect / forget networks

### Appearance
- Preview and select themes
- 9 built-in themes:
  - Dark (default)
  - Light
  - Midnight Blue
  - Forest Green
  - Purple Night
  - Sunset Orange
  - Rose Pink
  - Ocean Teal
  - High Contrast
- Theme applies immediately
- Persists across app restarts

### Storage
- Shows used vs total space
- Free space remaining

### About Phone
- Modem manufacturer/model
- IMEI number
- Phone number (from SIM)
- Network operator
- Signal strength

## Usage

### Connecting to WiFi
1. Enable WiFi toggle
2. Tap **Scan for Networks**
3. Tap a network
4. Enter password if required
5. Tap **Connect**

### Changing Theme
1. Scroll to **Appearance** section
2. Tap desired theme tile
3. Theme applies immediately

### Pairing Bluetooth
1. Enable Bluetooth toggle
2. Tap **Scan for Devices**
3. Tap **Pair** next to device
4. Device appears in paired list

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Escape` | Close dialogs / Exit |

## Data Storage

Settings stored in:
```
%LOCALAPPDATA%\WindowsPhoneNext\connectivity_settings.json
%LOCALAPPDATA%\WindowsPhoneNext\theme_settings.json
```

## Technical Details

- Uses `netsh` for WiFi management
- Uses PowerShell for Bluetooth enumeration
- Reads modem via ModemLib AT commands
- Themes use SharedServices ThemeManager

## Building

```powershell
cd Apps/Settings
dotnet build
```

## Project Structure

```
Settings/
├── MainWindow.xaml        # Settings UI
├── MainWindow.xaml.cs     # Connectivity & theme logic
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── SettingsTheme.xaml
└── WindowsPhoneSettings.csproj
```
