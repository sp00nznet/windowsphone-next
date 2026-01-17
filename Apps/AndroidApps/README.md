# Android Apps

APK sideloader using Windows Subsystem for Android (WSA).

## Features

- **APK Installation** - Sideload Android apps
- **App Library** - View installed apps
- **WSA Status** - Connection monitoring
- **App Launching** - Run installed apps
- **App Removal** - Uninstall apps

## Requirements

- Windows 11
- Windows Subsystem for Android installed
- ADB (Android Debug Bridge) in PATH

### Installing WSA
1. Open Microsoft Store
2. Search for "Windows Subsystem for Android"
3. Install and launch once to set up

### Installing ADB
```powershell
# Option 1: Via winget
winget install Google.PlatformTools

# Option 2: Download from Google
# https://developer.android.com/studio/releases/platform-tools
# Extract and add to PATH
```

## Usage

### Checking Status
- App shows WSA connection status on launch
- Green = Connected and ready
- Yellow = WSA installed but not running
- Red = WSA not available

### Installing an APK
1. Ensure WSA is running
2. Tap **Install APK**
3. Select an APK file
4. Wait for installation to complete
5. App appears in your library

### Launching an App
1. Find the app in your library
2. Tap the app tile
3. WSA opens the app

### Uninstalling
1. Find the app in your library
2. Tap the **Delete** button
3. Confirm removal

## Status Indicators

| Status | Meaning |
|--------|---------|
| WSA Connected | Ready to install/run apps |
| WSA Not Running | Start WSA first |
| WSA Not Available | Install WSA from Store |

## Technical Details

- Uses ADB for app installation
- Connects to WSA at `127.0.0.1:58526`
- Stores app list locally

## Data Storage

Installed apps list stored in:
```
%LOCALAPPDATA%\WindowsPhoneNext\android_apps.json
```

## Troubleshooting

### "ADB not found"
Install platform-tools and add to PATH

### "WSA not running"
1. Open Windows Subsystem for Android Settings
2. Turn on "Developer mode"
3. Click "Manage developer settings" to start WSA

### "Installation failed"
- Check APK is valid
- Try restarting WSA
- Check APK is compatible with x86

## Building

```powershell
cd Apps/AndroidApps
dotnet build
```

## Project Structure

```
AndroidApps/
├── MainWindow.xaml        # App library UI
├── MainWindow.xaml.cs     # WSA/ADB integration
├── App.xaml               # Application resources
├── App.xaml.cs            # Theme initialization
├── Themes/
│   └── AndroidAppsTheme.xaml
└── WindowsPhoneAndroidApps.csproj
```
