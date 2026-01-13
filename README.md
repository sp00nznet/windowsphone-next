# Windows Phone Next

A custom Windows 11 phone platform for embedded hardware, featuring a modern touch-friendly launcher and phone/messaging applications.

## Target Hardware

- **Board**: UP Core (https://up-board.org/upcore/specifications/)
- **Processor**: LattePanda 3 Delta 864
- **Power**: PiSugar2 Plus battery management
- **Display**: 720x720 4:4 aspect ratio touchscreen
- **Modem**: Quectel EM06-A LTE card (voice calls + SMS)

## Features

- **Modern Launcher** - Full-screen 720x720 home screen with app grid, status bar, and quick access navigation
- **Phone Dialer** - Complete phone application with dialpad, call history, and in-call controls using AT commands
- **SMS Messaging** - Conversation-based messaging app with send/receive functionality via AT commands
- **Auto-start Kiosk Mode** - Boots directly into launcher, replacing Windows shell
- **Power Management** - Optimized for battery operation with PiSugar2 Plus

## Project Structure

```
windowsphone-next/
├── Apps/
│   ├── Shared/
│   │   └── ModemLib/           # Shared AT command modem library
│   ├── Launcher/               # Home screen launcher (WPF)
│   ├── Dialer/                 # Phone/call application (WPF)
│   └── Messaging/              # SMS messaging application (WPF)
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

### Messaging

SMS messaging application:
- Conversation list view
- Chat bubble interface
- New message composition
- Auto-refresh for incoming messages
- Unread message badges

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
