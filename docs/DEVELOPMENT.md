# Windows Phone Next - Development Guide

Technical documentation for developers working on the Windows Phone Next platform.

---

## Project Structure

```
windowsphone-next/
├── Apps/
│   ├── Shared/
│   │   └── ModemLib/           # Shared AT command modem library
│   ├── Launcher/               # Home screen launcher
│   ├── Dialer/                 # Phone/call application
│   ├── Messaging/              # SMS messaging
│   ├── Browser/                # Web browser (WebView2)
│   ├── Music/                  # Music player (NAudio)
│   ├── Maps/                   # GPS navigation (Leaflet.js)
│   ├── Calendar/               # Calendar app
│   └── Gallery/                # Image viewer
├── Setup/
│   ├── Install-WindowsPhone.ps1
│   └── Configure-KioskMode.ps1
├── docs/                       # Documentation
├── Build.ps1                   # Build script
└── README.md
```

---

## Build System

### Requirements
- Windows 11
- .NET 8 SDK
- Visual Studio 2022 (optional)

### Commands

```powershell
# Build all apps (Release)
.\Build.ps1

# Build specific app
.\Build.ps1 -App Launcher

# Debug build
.\Build.ps1 -Configuration Debug

# Clean build
.\Build.ps1 -Clean

# Build and deploy
.\Build.ps1 -Deploy

# List available targets
.\Build.ps1 -List
```

### Output
- `Output/` - Individual app build outputs
- `Dist/` - Packaged distribution (all apps)

---

## ModemLib API

The shared modem library provides AT command communication with the Quectel EM06-A.

### ModemController Class

```csharp
var modem = new ModemController();

// Connect to modem
await modem.AutoConnectAsync();      // Auto-detect COM port
await modem.InitializeAsync();       // Initialize modem

// Voice calls
await modem.DialAsync("+1234567890"); // Make call
await modem.AnswerCallAsync();        // Answer incoming
await modem.HangUpAsync();            // End call
await modem.SendDtmfAsync('5');       // Send DTMF tone

// SMS
await modem.SendSmsAsync(number, message);
var messages = await modem.ReadAllSmsAsync();

// Status
var signal = await modem.GetSignalStrengthAsync();
var status = await modem.GetNetworkStatusAsync();
var callStatus = await modem.GetCallStatusAsync();

// Properties
modem.IsConnected
modem.PortName
```

### Events

```csharp
modem.IncomingCall += (s, e) => { /* e.CallerId */ };
modem.CallStateChanged += (s, e) => { /* e.Status */ };
modem.SmsReceived += (s, e) => { /* New SMS */ };
```

---

## AT Commands Reference

The EM06-A modem uses standard AT commands over serial port.

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

The VK-172 outputs standard NMEA sentences.

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

await gps.AutoConnectAsync();  // Find GPS COM port

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

## Theme Colors

All apps use a consistent dark theme.

| Name | Hex | Usage |
|------|-----|-------|
| Background | `#1A1A2E` | Main background |
| Surface | `#16213E` | Cards, panels |
| Card | `#1F2937` | Elevated elements |
| Primary | `#0078D4` | Accent, buttons |
| Accent | `#00B4D8` | Secondary accent |
| Success | `#10B981` | Positive states |
| Warning | `#F59E0B` | Caution states |
| Error | `#EF4444` | Error states |
| TextPrimary | `#FFFFFF` | Main text |
| TextSecondary | `#9CA3AF` | Muted text |

---

## Adding New Applications

1. Create WPF project in `Apps/` folder
2. Target `net8.0-windows` framework
3. Reference `ModemLib` if modem access needed
4. Use shared theme resources
5. Set window: 720x720, WindowStyle="None", ResizeMode="NoResize"
6. Add project to `WindowsPhoneNext.sln`
7. Add to `Build.ps1` apps list
8. Add to Launcher's app grid

### Project Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>WindowsPhoneNext.YourApp</RootNamespace>
  </PropertyGroup>
</Project>
```

---

## Kiosk Mode

Replace Windows shell with the Launcher.

```powershell
# Enable kiosk mode
.\Setup\Configure-KioskMode.ps1

# Revert to normal Windows
.\Setup\Configure-KioskMode.ps1 -Revert
```

### Recovery
If locked out:
1. Boot into Safe Mode (hold Shift during restart)
2. Run `Configure-KioskMode.ps1 -Revert`
3. Or manually reset registry:
   ```
   HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell = explorer.exe
   ```
