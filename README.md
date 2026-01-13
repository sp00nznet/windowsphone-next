# Windows Phone Next

A custom Windows 11 phone platform for embedded hardware, featuring a modern touch-friendly launcher and phone/messaging applications.

## Target Hardware

| Component | Description |
|-----------|-------------|
| **SBC** | [UP Core](https://up-board.org/upcore/specifications/) or [LattePanda 3 Delta 864](https://www.lattepanda.com/lattepanda-3-delta) |
| **Power** | [PiSugar2 Plus](https://www.pisugar.com/products/pisugar2-plus-5000-mah-raspberry-pi-ups) 5000mAh battery/UPS |
| **Display** | [Waveshare 4" Square LCD](https://www.waveshare.com/4inch-dpi-lcd-c.htm) 720x720 IPS touchscreen |
| **Modem** | [Quectel EM06-A](https://www.quectel.com/product/lte-a-em06-series/) LTE Cat 6 M.2 module |
| **GPS** | VK-172 USB GPS/GLONASS dongle (optional) |

## Features

- **Modern Launcher** - Full-screen 720x720 home screen with app grid, status bar, and quick access navigation
- **Phone Dialer** - Complete phone application with dialpad, call history, and in-call controls using AT commands
- **SMS Messaging** - Conversation-based messaging app with send/receive functionality via AT commands
- **Web Browser** - Chromium-based web browser with tabbed browsing
- **Music Player** - Audio player with real-time spectrum analyzer visualization
- **Maps & Navigation** - GPS navigation with OpenStreetMap and turn-by-turn routing
- **Calendar** - Month/day/year views with date marking functionality
- **Gallery** - Image viewer with thumbnail navigation
- **Auto-start Kiosk Mode** - Boots directly into launcher, replacing Windows shell
- **Power Management** - Optimized for battery operation with PiSugar2 Plus
- **Demo Modes** - Hardware-dependent apps (Dialer, Maps) work without hardware for testing

## Project Structure

```
windowsphone-next/
├── Apps/
│   ├── Shared/
│   │   └── ModemLib/           # Shared AT command modem library
│   ├── Launcher/               # Home screen launcher (WPF)
│   ├── Dialer/                 # Phone/call application (WPF)
│   ├── Messaging/              # SMS messaging application (WPF)
│   ├── Browser/                # Chromium-based web browser (WPF)
│   ├── Music/                  # Music player with visualizer (WPF)
│   ├── Maps/                   # GPS navigation with OSM (WPF)
│   ├── Calendar/               # Calendar application (WPF)
│   └── Gallery/                # Image gallery viewer (WPF)
├── Setup/
│   ├── Install-WindowsPhone.ps1      # Main installation script
│   └── Configure-KioskMode.ps1       # Kiosk mode configuration
├── Build.ps1                   # Build script
└── README.md
```

## Requirements

- Windows 11
- .NET 8 SDK
- Visual Studio 2022 (optional, for development)
- Quectel EM06-A drivers

## Building

### Using PowerShell

```powershell
# Build Release
.\Build.ps1

# Build and Deploy
.\Build.ps1 -Deploy

# Clean build
.\Build.ps1 -Clean -Configuration Release
```

### Using dotnet CLI

```bash
cd Apps
dotnet build WindowsPhoneNext.sln -c Release
```

## Installation

### 1. Install Windows 11

Install Windows 11 on your UP Core / LattePanda device using standard installation media.

### 2. Install .NET Runtime

Download and install the .NET 8 Desktop Runtime from Microsoft.

### 3. Install EM06-A Drivers

Install the Quectel EM06-A drivers from the manufacturer or Windows Update.

### 4. Run Setup Script

```powershell
# Run as Administrator
.\Setup\Install-WindowsPhone.ps1
```

This will:
- Create installation directories
- Configure display settings for 720x720
- Set up modem configuration
- Configure power management for battery operation
- Install auto-start for the launcher

### 5. Enable Kiosk Mode (Optional)

```powershell
# Enable kiosk mode (launcher replaces Windows shell)
.\Setup\Configure-KioskMode.ps1

# Revert to normal Windows
.\Setup\Configure-KioskMode.ps1 -Revert
```

## Configuration

### Modem Configuration

Edit `C:\WindowsPhoneNext\Config\modem.json`:

```json
{
  "PortName": "COM3",
  "BaudRate": 115200,
  "DataBits": 8,
  "Parity": "None",
  "StopBits": 1,
  "ATTimeout": 5000
}
```

The modem port is auto-detected on startup, but you can specify it manually if needed.

## Applications

### Launcher

The main home screen featuring:
- Clock and date display
- Status bar (signal strength, network, battery)
- 3x3 app grid with touch-friendly tiles
- Bottom navigation bar (Phone, Home, Messages)
- Incoming call overlay

### Dialer

Full-featured phone application:
- T9-style dialpad
- Call history (Recents tab)
- Contacts integration
- Active call screen with mute/speaker/keypad
- DTMF tone support during calls
- **Demo Mode**: Works without modem hardware, simulates call flow

**Hardware Required**: Quectel EM06-A LTE modem (or compatible AT modem)

### Messaging

SMS messaging application:
- Conversation list view
- Chat bubble interface
- New message composition
- Auto-refresh for incoming messages
- Unread message badges
- **Demo Mode**: Works without modem with sample conversations

**Hardware Required**: Quectel EM06-A LTE modem (or compatible AT modem)

### Browser

Chromium-based web browser:
- Tabbed browsing interface
- Address bar with search
- Back/forward/refresh navigation
- Desktop-class web rendering via WebView2

### Music

Audio player with visualizer:
- Supports MP3, WAV, FLAC, OGG formats
- 64-bar real-time spectrum analyzer
- Peak hold visualization
- Playlist with shuffle/repeat
- Keyboard shortcuts (Space=play/pause, N=next, P=prev)

### Maps

GPS navigation application:
- OpenStreetMap tile rendering
- A-to-B route calculation via OSRM
- Location search via Nominatim geocoding
- Real-time GPS position tracking
- Turn-by-turn navigation
- **Demo Mode**: Simulated GPS movement when hardware unavailable

**Hardware Required**: VK-172 USB GPS dongle (or compatible NMEA GPS device)

### Calendar

Calendar application:
- Month view (default)
- Day view with hourly time slots
- Year view (zoom out)
- Mark important dates
- Data persistence in local storage

### Gallery

Image viewer application:
- Thumbnail strip navigation
- Full-size image display
- Keyboard navigation (Arrow keys, Home, End)
- Supports JPG, PNG, GIF, BMP, WebP, TIFF
- Open folder dialog

## AT Commands Reference

The EM06-A modem is controlled via AT commands over serial port:

| Command | Description |
|---------|-------------|
| `ATD<number>;` | Dial a phone number |
| `ATA` | Answer incoming call |
| `ATH` | Hang up call |
| `AT+CMGF=1` | Set SMS text mode |
| `AT+CMGS="<number>"` | Send SMS |
| `AT+CMGL="ALL"` | List all SMS messages |
| `AT+CSQ` | Get signal strength |
| `AT+CREG?` | Get network registration |
| `AT+VTS=<digit>` | Send DTMF tone |

## Development

### Adding New Applications

1. Create a new WPF project in `Apps/`
2. Reference the `ModemLib` project for modem access
3. Use the shared theme resources for consistent styling
4. Add the project to `WindowsPhoneNext.sln`
5. Update the Launcher's app grid to include the new app

### Theme Colors

The UI uses a modern dark theme:
- Background: `#1A1A2E`
- Surface: `#16213E`
- Card: `#1F2937`
- Primary: `#0078D4`
- Accent: `#00B4D8`
- Success: `#10B981`
- Error: `#EF4444`

## Troubleshooting

### Modem Not Detected

1. Check Device Manager for the EM06-A COM port
2. Verify drivers are installed correctly
3. Try specifying the port manually in `modem.json`

### Display Issues

1. Verify display resolution is set to 720x720
2. Check display scaling is set to 100%
3. Run `Setup\Scripts\Set-Display.ps1`

### Kiosk Mode Recovery

If locked out of kiosk mode:
1. Boot into Safe Mode (hold Shift during restart)
2. Run `Configure-KioskMode.ps1 -Revert`
3. Or manually reset shell in registry:
   ```
   HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell = explorer.exe
   ```

## License

This project is provided as-is for educational and personal use.
