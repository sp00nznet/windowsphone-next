# Windows Phone Next - Development Guide

Technical documentation for developers working on the Windows Phone Next platform.

---

## Project Structure

```
windowsphone-next/
├── Apps/
│   ├── Shared/
│   │   ├── ModemLib/           # AT command modem library
│   │   ├── BlockingService/    # Call/message blocking service
│   │   ├── Services/           # Theme manager
│   │   └── Themes/             # Shared theme resources
│   ├── Launcher/               # Home screen launcher
│   ├── Dialer/                 # Phone/call application
│   ├── Messaging/              # SMS messaging
│   ├── Contacts/               # Contact management
│   ├── Browser/                # Web browser (WebView2)
│   ├── Gmail/                  # Gmail client
│   ├── Maps/                   # GPS navigation
│   ├── Music/                  # Music player (NAudio)
│   ├── Video/                  # Video player
│   ├── Calendar/               # Calendar app
│   ├── Gallery/                # Image viewer
│   ├── Settings/               # System settings
│   ├── Terminal/               # Tabbed terminal
│   ├── ClaudeCode/             # AI assistant
│   ├── AndroidApps/            # APK sideloader
│   ├── Camera/                 # Camera capture
│   ├── Files/                  # File browser
│   ├── Solitaire/              # Card game
│   └── Mahjong/                # Tile game
├── Build/
│   ├── build-all.ps1           # Build script
│   ├── download-drivers.ps1    # Driver download
│   ├── create-image.ps1        # Image creation
│   └── deploy.ps1              # Master deployment
├── Setup/
│   ├── Autounattend.xml        # Unattended install config
│   ├── setup.ps1               # Post-install setup
│   └── configure-autostart.ps1 # Launcher autostart
├── Output/                     # Built applications
└── docs/                       # Documentation
```

---

## Build System

### Requirements
- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (optional)

### Commands

```powershell
# Build all apps (Release)
.\Build\build-all.ps1

# Debug build
.\Build\build-all.ps1 -Configuration Debug

# Clean build
.\Build\build-all.ps1 -Clean

# Custom output directory
.\Build\build-all.ps1 -OutputPath "C:\MyBuilds"
```

### Output
- `Output/` - Individual app builds
- `Output/apps-manifest.json` - Build metadata

---

## Shared Libraries

### ModemLib

AT command communication with the Quectel EM06-A modem.

```csharp
using WindowsPhoneNext.ModemLib;

var modem = new ModemController();

// Connect
await modem.AutoConnectAsync();
await modem.InitializeAsync();

// Voice calls
await modem.DialAsync("+1234567890");
await modem.AnswerCallAsync();
await modem.HangUpAsync();
await modem.SendDtmfAsync('5');

// SMS
await modem.SendSmsAsync(number, message);
var messages = await modem.ReadAllSmsAsync();

// Status
var signal = await modem.GetSignalStrengthAsync();
var status = await modem.GetNetworkStatusAsync();

// Events
modem.IncomingCall += (s, e) => { /* e.CallerId */ };
modem.SmsReceived += (s, e) => { /* e.MessageIndex */ };
modem.CallStateChanged += (s, e) => { /* e.Status */ };
```

### BlockingService

Manages blocked phone numbers for calls and messages.

```csharp
using WindowsPhoneNext.Shared.BlockingService;

// Check if blocked
bool blockedForCalls = BlockingService.Instance.IsBlockedForCalls(phoneNumber);
bool blockedForMessages = BlockingService.Instance.IsBlockedForMessages(phoneNumber);

// Block a number
BlockingService.Instance.BlockNumber(phoneNumber, blockCalls: true, blockMessages: true);

// Unblock
BlockingService.Instance.UnblockNumber(phoneNumber);

// Get all blocked numbers
var blockedNumbers = BlockingService.Instance.GetBlockedNumbers();
```

### SharedServices (ThemeManager)

System-wide theme management with persistence.

```csharp
using WindowsPhoneNext.Shared.Services;

// Get current theme
string themeName = ThemeManager.CurrentTheme;
ThemeDefinition theme = ThemeManager.GetCurrentThemeDefinition();

// Set theme
ThemeManager.SetTheme("MidnightBlue");

// Apply theme to app resources (call in App.xaml.cs)
ThemeManager.ApplyTheme(Application.Current.Resources);

// Theme change event
ThemeManager.ThemeChanged += (s, themeName) => {
    // Refresh UI
};

// Available themes
foreach (var t in ThemeManager.AvailableThemes)
{
    Console.WriteLine($"{t.Key}: {t.Value.DisplayName}");
}
```

---

## Theming System

### Available Themes

| Theme | Primary | Accent | Background |
|-------|---------|--------|------------|
| **Dark** | `#0078D4` | `#00B4D8` | `#1A1A2E` |
| **Light** | `#0078D4` | `#0078D4` | `#F5F5F5` |
| **MidnightBlue** | `#3B82F6` | `#60A5FA` | `#0F172A` |
| **Forest** | `#22C55E` | `#4ADE80` | `#14231A` |
| **Purple** | `#A855F7` | `#C084FC` | `#1E1B2E` |
| **Sunset** | `#F97316` | `#FB923C` | `#1C1410` |
| **Rose** | `#EC4899` | `#F472B6` | `#1C1418` |
| **Ocean** | `#14B8A6` | `#2DD4BF` | `#0F1419` |
| **HighContrast** | `#00FF00` | `#00FFFF` | `#000000` |

### Theme Resources

Each theme defines these color resources:

| Resource | Purpose |
|----------|---------|
| `BackgroundColor/Brush` | Main background |
| `SurfaceColor/Brush` | Cards, panels |
| `CardColor/Brush` | Elevated elements |
| `PrimaryColor/Brush` | Primary actions |
| `AccentColor/Brush` | Secondary accent |
| `TextPrimaryColor/Brush` | Main text |
| `TextSecondaryColor/Brush` | Muted text |
| `BorderColor/Brush` | Borders |
| `SuccessColor/Brush` | Positive states |
| `ErrorColor/Brush` | Error states |
| `WarningColor/Brush` | Warning states |

### Applying Themes in Apps

**App.xaml.cs:**
```csharp
using WindowsPhoneNext.Shared.Services;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.ApplyTheme(Resources);
    }
}
```

**XAML Usage:**
```xml
<Border Background="{StaticResource BackgroundBrush}">
    <TextBlock Foreground="{StaticResource TextPrimaryBrush}"
               Text="Hello World"/>
</Border>
```

### Theme Persistence

Theme settings are stored at:
```
%LOCALAPPDATA%\WindowsPhoneNext\theme_settings.json
```

---

## Curved Bezel Support

All apps support curved screen edges using the shared BezelTheme.

### BezelTheme Resources

| Resource | Value | Purpose |
|----------|-------|---------|
| `BezelCornerRadius` | 32 | Window corner radius |
| `BezelInnerCornerRadius` | 28 | Inner element radius |
| `BezelSafeMargin` | 12,16,12,16 | Content safe area |
| `BezelStatusBarMargin` | 16,8,16,0 | Top bar margin |
| `BezelBottomNavMargin` | 16,0,16,12 | Bottom nav margin |

### Bezel Styles

```xml
<!-- Window wrapper -->
<Border Style="{StaticResource BezelWindowBorderStyle}">
    <Grid>
        <!-- Top bar -->
        <Border Style="{StaticResource BezelStatusBarStyle}">
            ...
        </Border>

        <!-- Content -->
        <Grid Margin="12,0,12,16">
            ...
        </Grid>

        <!-- Bottom navigation -->
        <Border Style="{StaticResource BezelBottomNavStyle}">
            ...
        </Border>
    </Grid>
</Border>
```

### Window Template

```xml
<Window ...
        Background="{StaticResource BackgroundBrush}">

    <!-- Bezel-safe container -->
    <Border Style="{StaticResource BezelWindowBorderStyle}"
            Background="{StaticResource BackgroundBrush}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="60"/>    <!-- Header -->
                <RowDefinition Height="*"/>     <!-- Content -->
                <RowDefinition Height="100"/>   <!-- Bottom nav -->
            </Grid.RowDefinitions>

            <!-- Header with bezel-safe margins -->
            <Border Grid.Row="0"
                    Style="{StaticResource BezelStatusBarStyle}"
                    Background="{StaticResource SurfaceBrush}">
                ...
            </Border>

            <!-- Content with bezel-safe margins -->
            <Grid Grid.Row="1" Margin="12,0,12,16">
                ...
            </Grid>

            <!-- Bottom navigation with bezel-safe margins -->
            <Border Grid.Row="2"
                    Style="{StaticResource BezelBottomNavStyle}"
                    Background="{StaticResource SurfaceBrush}">
                ...
            </Border>
        </Grid>
    </Border>
</Window>
```

---

## Adding New Applications

### 1. Create Project

Create a new WPF project in `Apps/` folder:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <RootNamespace>WindowsPhoneNext.YourApp</RootNamespace>
    <AssemblyName>WindowsPhoneYourApp</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Shared\Services\SharedServices.csproj" />
    <!-- Add ModemLib if modem access needed -->
    <ProjectReference Include="..\Shared\ModemLib\ModemLib.csproj" />
  </ItemGroup>
</Project>
```

### 2. Create App.xaml

```xml
<Application x:Class="WindowsPhoneNext.YourApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="../Shared/Themes/BezelTheme.xaml"/>
                <ResourceDictionary Source="Themes/YourAppTheme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 3. Create App.xaml.cs

```csharp
using System.Windows;
using WindowsPhoneNext.Shared.Services;

namespace WindowsPhoneNext.YourApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.ApplyTheme(Resources);
    }
}
```

### 4. Create MainWindow.xaml

```xml
<Window x:Class="WindowsPhoneNext.YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Your App"
        Width="720" Height="1560"
        WindowStyle="None"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen"
        Background="{StaticResource BackgroundBrush}"
        KeyDown="Window_KeyDown">

    <Border Style="{StaticResource BezelWindowBorderStyle}"
            Background="{StaticResource BackgroundBrush}">
        <Grid>
            <!-- Your content here -->
        </Grid>
    </Border>
</Window>
```

### 5. Create Theme File

Create `Themes/YourAppTheme.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- App-specific accent color (optional override) -->
    <Color x:Key="AccentColor">#4CAF50</Color>
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}"/>

    <!-- App-specific styles -->

</ResourceDictionary>
```

### 6. Add to Build Script

Edit `Build/build-all.ps1` and add to `$AppProjects`:

```powershell
$AppProjects = @(
    # ... existing projects ...
    "YourApp\WindowsPhoneYourApp.csproj"
)
```

### 7. Add to Launcher

Edit `Apps/Launcher/MainWindow.xaml.cs` to add your app to the grid.

---

## AT Commands Reference

### Basic Commands

| Command | Description |
|---------|-------------|
| `AT` | Test communication |
| `ATE0` | Disable echo |
| `ATI` | Display product ID |

### Voice Calls

| Command | Description |
|---------|-------------|
| `ATD<number>;` | Dial number (voice) |
| `ATA` | Answer incoming call |
| `ATH` | Hang up |
| `AT+CLCC` | List current calls |
| `AT+VTS=<digit>` | Send DTMF tone |

### SMS

| Command | Description |
|---------|-------------|
| `AT+CMGF=1` | Set text mode |
| `AT+CMGS="<number>"` | Send SMS |
| `AT+CMGL="ALL"` | List all messages |
| `AT+CMGR=<index>` | Read message |
| `AT+CMGD=<index>` | Delete message |

### Network Status

| Command | Description |
|---------|-------------|
| `AT+CSQ` | Signal strength (0-31, 99=unknown) |
| `AT+CREG?` | Registration status |
| `AT+COPS?` | Current operator |
| `AT+CPIN?` | SIM status |

### Signal Strength (CSQ)

| Value | Description |
|-------|-------------|
| 0-9 | Poor |
| 10-14 | Fair |
| 15-19 | Good |
| 20-31 | Excellent |
| 99 | Unknown |

---

## GPS (VK-172) NMEA Reference

### Supported Sentences

| Sentence | Description |
|----------|-------------|
| `$GPGGA` | Fix data (position, altitude, satellites) |
| `$GPRMC` | Recommended minimum (position, speed, date) |
| `$GPVTG` | Course over ground and speed |
| `$GPGSA` | DOP and active satellites |

### GpsController Class

```csharp
var gps = new GpsController();

await gps.AutoConnectAsync();

// Properties
gps.Latitude
gps.Longitude
gps.Speed        // km/h
gps.Heading      // degrees
gps.Altitude     // meters
gps.Satellites   // count
gps.HasFix       // bool

// Events
gps.PositionChanged += (s, e) => { /* e.Latitude, e.Longitude */ };
gps.StatusChanged += (s, e) => { /* e.Message */ };
```

---

## Kiosk Mode

Replace Windows shell with the Launcher.

### Enable

```powershell
.\Setup\configure-autostart.ps1
```

### Disable (Restore Explorer)

```powershell
.\Setup\restore-shell.ps1
```

### Recovery

If locked out:
1. Boot into Safe Mode (hold Shift during restart)
2. Run `restore-shell.ps1`
3. Or manually reset registry:
   ```
   HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell = explorer.exe
   ```

---

## UI Specifications

| Property | Value |
|----------|-------|
| **Resolution** | 720 x 1560 pixels |
| **Aspect Ratio** | 9:19.5 |
| **Framework** | WPF (.NET 8.0) |
| **Bezel Radius** | 32px |
| **Status Bar Height** | 60px |
| **Bottom Nav Height** | 100px |
| **Content Margins** | 12px horizontal |

---

## Debugging

### View Logs

```powershell
# Setup logs
Get-Content C:\WindowsPhoneNext\Logs\setup.log

# Autostart logs
Get-Content C:\WindowsPhoneNext\Logs\autostart.log
```

### Test Without Hardware

Most apps include demo mode:
- **Dialer**: Simulated calls
- **Messaging**: Demo conversations
- **Maps**: Works without GPS

### Common Issues

**"ThemeManager not found"**
- Ensure SharedServices project reference is added
- Add `using WindowsPhoneNext.Shared.Services;`

**"BezelTheme resources not found"**
- Add BezelTheme.xaml to App.xaml MergedDictionaries

**"Build fails with missing reference"**
- Run `dotnet restore` in the app directory
- Ensure all project references are correct paths
